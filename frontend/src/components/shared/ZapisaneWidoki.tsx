import { useEffect, useRef, useState } from 'react';
import { Bookmark, Check, Plus, Trash2, Users } from 'lucide-react';
import {
  useZapisaneWidoki,
  useZapiszWidok,
  useUsunWidok,
  type ZapisanyWidok,
} from '@/api/hooks/useZapisaneWidoki';
import { colors } from '@/theme/tokens';

interface Props<T> {
  /** Klucz listy po stronie serwera, np. "employees". Widoki nie mieszaja sie miedzy listami. */
  entityType: string;
  /** Aktualny stan filtrow — zapisywany jako nieprzezroczysty JSON. */
  filtry: T;
  /** Wywolywane przy wyborze widoku. */
  onZastosuj: (filtry: T) => void;
  /** Czy w ogole jest co zapisywac (pusty zestaw filtrow to nie widok). */
  maFiltry: boolean;
}

/**
 * Nazwane zestawy filtrow nad lista.
 *
 * Backend mial komplet CRUD-a ze wspoldzieleniem i widokiem domyslnym, zamapowany w Program.cs,
 * i ZERO trafien we froncie — funkcja istniala wylacznie w bazie.
 *
 * Widok mozna oznaczyc jako wspolny: wtedy widza go wszyscy w firmie, ale edytowac i kasowac
 * moze wylacznie autor. Tak dziala backend i tego celowo nie obchodzimy.
 */
export function ZapisaneWidoki<T extends object>({ entityType, filtry, onZastosuj, maFiltry }: Props<T>) {
  const { data: widoki = [] } = useZapisaneWidoki(entityType);
  const zapisz = useZapiszWidok();
  const usun = useUsunWidok(entityType);

  const [otwarte, setOtwarte] = useState(false);
  const [nazwa, setNazwa] = useState('');
  const [wspolny, setWspolny] = useState(false);
  const [wybrany, setWybrany] = useState<string | null>(null);
  const panel = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function pozaPanelem(e: MouseEvent) {
      if (panel.current && !panel.current.contains(e.target as Node)) setOtwarte(false);
    }
    if (otwarte) document.addEventListener('mousedown', pozaPanelem);
    return () => document.removeEventListener('mousedown', pozaPanelem);
  }, [otwarte]);

  const zastosuj = (widok: ZapisanyWidok) => {
    try {
      onZastosuj(JSON.parse(widok.filtersJson) as T);
      setWybrany(widok.id);
    } catch {
      // Uszkodzony JSON nie moze wywalic calej listy — widok po prostu sie nie zastosuje.
      setWybrany(null);
    }
    setOtwarte(false);
  };

  const zapiszBiezacy = async () => {
    const nazwaDoZapisu = nazwa.trim();
    if (!nazwaDoZapisu) return;
    await zapisz.mutateAsync({
      entityType,
      name: nazwaDoZapisu,
      filtersJson: JSON.stringify(filtry),
      sortJson: '{}',
      isDefault: false,
      isShared: wspolny,
    });
    setNazwa('');
    setWspolny(false);
  };

  return (
    <div ref={panel} style={{ position: 'relative' }}>
      <button
        onClick={() => setOtwarte((o) => !o)}
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 6,
          padding: '8px 12px',
          fontSize: 13,
          fontWeight: 600,
          fontFamily: 'inherit',
          color: wybrany ? colors.primary[700] : colors.gray[700],
          backgroundColor: wybrany ? colors.primary[50] : colors.white,
          border: '1px solid ' + (wybrany ? colors.primary[200] : colors.gray[300]),
          borderRadius: 12,
          cursor: 'pointer',
        }}
      >
        <Bookmark size={14} />
        Widoki
        {widoki.length > 0 && (
          <span style={{ fontSize: 11.5, fontWeight: 700, color: colors.gray[500] }}>{widoki.length}</span>
        )}
      </button>

      {otwarte && (
        <div
          style={{
            position: 'absolute',
            top: '100%',
            left: 0,
            marginTop: 8,
            width: 300,
            zIndex: 900,
            backgroundColor: colors.white,
            border: '1px solid ' + colors.gray[200],
            borderRadius: 14,
            boxShadow: '0 12px 32px -8px rgba(20,25,43,0.18)',
            padding: 10,
          }}
        >
          {widoki.length === 0 ? (
            <p style={{ margin: '6px 8px 12px', fontSize: 12.5, color: colors.gray[500] }}>
              Nie masz jeszcze zapisanych widoków. Ustaw filtry i zapisz je pod nazwą.
            </p>
          ) : (
            <ul style={{ listStyle: 'none', margin: '0 0 10px', padding: 0, display: 'grid', gap: 2 }}>
              {widoki.map((widok) => (
                <li key={widok.id} style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                  <button
                    onClick={() => zastosuj(widok)}
                    style={{
                      flex: 1,
                      textAlign: 'left',
                      padding: '7px 9px',
                      fontSize: 13,
                      fontFamily: 'inherit',
                      color: colors.gray[900],
                      backgroundColor: 'transparent',
                      border: 'none',
                      borderRadius: 9,
                      cursor: 'pointer',
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: 6,
                    }}
                  >
                    {wybrany === widok.id && <Check size={13} style={{ color: colors.primary[600] }} />}
                    {widok.name}
                    {widok.isShared && <Users size={12} style={{ color: colors.gray[400] }} />}
                  </button>
                  <button
                    onClick={() => usun.mutate(widok.id)}
                    title="Usuń widok"
                    aria-label={'Usuń widok ' + widok.name}
                    style={{
                      padding: 6,
                      color: colors.gray[400],
                      backgroundColor: 'transparent',
                      border: 'none',
                      borderRadius: 8,
                      cursor: 'pointer',
                    }}
                  >
                    <Trash2 size={13} />
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div style={{ borderTop: '1px solid ' + colors.gray[200], paddingTop: 10 }}>
            {!maFiltry ? (
              <p style={{ margin: '0 0 2px', fontSize: 12.5, color: colors.gray[500] }}>
                Ustaw filtry, żeby zapisać je jako widok.
              </p>
            ) : (
              <>
                <input
                  value={nazwa}
                  onChange={(e) => setNazwa(e.target.value)}
                  placeholder="Nazwa widoku"
                  aria-label="Nazwa widoku"
                  style={{
                    width: '100%',
                    boxSizing: 'border-box',
                    padding: '7px 10px',
                    fontSize: 13,
                    fontFamily: 'inherit',
                    border: '1px solid ' + colors.gray[300],
                    borderRadius: 9,
                  }}
                />
                <label
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 6,
                    margin: '8px 0',
                    fontSize: 12.5,
                    color: colors.gray[600],
                  }}
                >
                  <input type="checkbox" checked={wspolny} onChange={(e) => setWspolny(e.target.checked)} />
                  Widoczny dla całej firmy
                </label>
                <button
                  onClick={zapiszBiezacy}
                  disabled={!nazwa.trim() || zapisz.isPending}
                  style={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: 6,
                    width: '100%',
                    justifyContent: 'center',
                    padding: '8px',
                    fontSize: 13,
                    fontWeight: 600,
                    fontFamily: 'inherit',
                    color: colors.textOnAccent,
                    backgroundColor: colors.primary[600],
                    border: 'none',
                    borderRadius: 10,
                    cursor: nazwa.trim() ? 'pointer' : 'default',
                    opacity: !nazwa.trim() || zapisz.isPending ? 0.55 : 1,
                  }}
                >
                  <Plus size={14} />
                  {zapisz.isPending ? 'Zapisywanie…' : 'Zapisz obecne filtry'}
                </button>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
