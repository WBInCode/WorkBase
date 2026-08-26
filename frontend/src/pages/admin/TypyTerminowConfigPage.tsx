import { useState } from 'react';
import { CalendarClock, Plus, X } from 'lucide-react';
import { useTypyTerminow, useZapiszTypTerminu, type TypTerminu } from '@/api/hooks/useTerminy';
import { colors } from '@/theme/tokens';

/**
 * Rodzaje terminów pilnowanych w firmie.
 *
 * To słownik FIRMY, nie nasz — nowa firma dostaje edytowalny zestaw startowy (badania, BHP,
 * uprawnienia, koniec umowy) i może go zmienić, wyłączyć albo rozszerzyć. System nie wie z góry,
 * jakich terminów pilnuje dana branża.
 *
 * Wyprzedzenie ostrzeżenia ustawia się osobno dla każdego rodzaju, bo różne terminy mają różny
 * czas reakcji: badanie umawia się z miesięcznym wyprzedzeniem, a wypowiedzenie umowy wymaga dwóch.
 */
export function TypyTerminowConfigPage() {
  const { data: typy = [], isLoading } = useTypyTerminow(true);
  const zapisz = useZapiszTypTerminu();

  const [formularzOtwarty, setFormularzOtwarty] = useState(false);
  const [edytowany, setEdytowany] = useState<TypTerminu | null>(null);
  const [blad, setBlad] = useState<string | null>(null);

  const otworz = (typ: TypTerminu | null) => {
    setEdytowany(typ);
    setFormularzOtwarty(true);
    setBlad(null);
  };

  const zapiszTyp = async (dane: { kod: string; nazwa: string; opis: string; dniOstrzezenia: number; aktywny: boolean }) => {
    setBlad(null);
    try {
      await zapisz.mutateAsync({
        id: edytowany?.id ?? null,
        kod: dane.kod.trim().toUpperCase(),
        nazwa: dane.nazwa.trim(),
        opis: dane.opis.trim() || null,
        dniOstrzezenia: dane.dniOstrzezenia,
        aktywny: dane.aktywny,
      });
      setFormularzOtwarty(false);
      setEdytowany(null);
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zapisać.');
    }
  };

  return (
    <div style={{ padding: '24px 28px', maxWidth: 860, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6, flexWrap: 'wrap' }}>
        <CalendarClock size={20} style={{ color: colors.primary[600] }} />
        <h1 style={{ fontSize: 22, fontWeight: 800, margin: 0, color: colors.gray[900] }}>
          Rodzaje terminów
        </h1>
        <button
          onClick={() => (formularzOtwarty ? setFormularzOtwarty(false) : otworz(null))}
          style={{
            marginLeft: 'auto',
            display: 'inline-flex',
            alignItems: 'center',
            gap: 6,
            padding: '8px 14px',
            borderRadius: 10,
            border: 'none',
            background: colors.primary[600],
            color: colors.textOnAccent,
            fontSize: 13,
            fontWeight: 600,
            cursor: 'pointer',
          }}
        >
          {formularzOtwarty ? <X size={15} /> : <Plus size={15} />}
          {formularzOtwarty ? 'Anuluj' : 'Dodaj rodzaj'}
        </button>
      </div>

      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 20px', fontSize: 14, maxWidth: '72ch' }}>
        Czego pilnujemy przy pracownikach. Zestaw startowy możesz dowolnie zmienić — to ustawienie
        firmy, nie systemu. Wyprzedzenie decyduje, ile dni wcześniej pojawi się ostrzeżenie
        i wyjdzie powiadomienie.
      </p>

      {formularzOtwarty && (
        <Formularz
          typ={edytowany}
          onZapisz={zapiszTyp}
          zapisywanie={zapisz.isPending}
          blad={blad}
        />
      )}

      {isLoading ? (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Wczytywanie…</p>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 10 }}>
          {typy.map((typ) => (
            <li
              key={typ.id}
              style={{
                background: 'var(--wb-panel, #fff)',
                border: '1px solid var(--wb-line, #e3e7f1)',
                borderRadius: 12,
                padding: '13px 15px',
                display: 'flex',
                gap: 12,
                alignItems: 'flex-start',
                opacity: typ.aktywny ? 1 : 0.55,
              }}
            >
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                  <strong style={{ fontSize: 14, color: colors.gray[900] }}>{typ.nazwa}</strong>
                  <code style={{ fontSize: 11.5, color: 'var(--wb-ink-2, #9aa3b8)' }}>{typ.kod}</code>
                  {!typ.aktywny && (
                    <span style={{ fontSize: 11.5, color: 'var(--wb-ink-2, #6b7490)' }}>wyłączony</span>
                  )}
                </div>
                <p style={{ margin: '4px 0 0', fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
                  {typ.opis ? `${typ.opis} · ` : ''}ostrzeżenie {typ.dniOstrzezenia} dni wcześniej
                </p>
              </div>
              <button
                onClick={() => otworz(typ)}
                style={{
                  padding: '6px 12px',
                  borderRadius: 8,
                  border: '1px solid var(--wb-line, #e3e7f1)',
                  background: 'var(--wb-panel, #fff)',
                  color: 'var(--wb-ink-2, #6b7490)',
                  fontSize: 12.5,
                  cursor: 'pointer',
                }}
              >
                Zmień
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

const stylPola: React.CSSProperties = {
  display: 'block',
  width: '100%',
  marginTop: 4,
  padding: '7px 9px',
  borderRadius: 8,
  border: '1px solid var(--wb-line, #e3e7f1)',
  fontFamily: 'inherit',
  fontSize: 14,
};

function Formularz({
  typ,
  onZapisz,
  zapisywanie,
  blad,
}: {
  typ: TypTerminu | null;
  onZapisz: (dane: { kod: string; nazwa: string; opis: string; dniOstrzezenia: number; aktywny: boolean }) => void;
  zapisywanie: boolean;
  blad: string | null;
}) {
  const [kod, setKod] = useState(typ?.kod ?? '');
  const [nazwa, setNazwa] = useState(typ?.nazwa ?? '');
  const [opis, setOpis] = useState(typ?.opis ?? '');
  const [dni, setDni] = useState(typ?.dniOstrzezenia ?? 30);
  const [aktywny, setAktywny] = useState(typ?.aktywny ?? true);

  const kompletne = kod.trim().length > 0 && nazwa.trim().length > 0;

  return (
    <div
      style={{
        background: 'var(--wb-panel, #fff)',
        border: '1px solid var(--wb-line, #e3e7f1)',
        borderRadius: 14,
        padding: '16px 18px',
        marginBottom: 18,
        display: 'grid',
        gap: 12,
      }}
    >
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 10 }}>
        <label style={{ fontSize: 13, color: colors.gray[900] }}>
          Kod
          <input
            value={kod}
            onChange={(e) => setKod(e.target.value)}
            disabled={typ !== null}
            placeholder="BADANIA"
            style={{ ...stylPola, opacity: typ ? 0.6 : 1 }}
          />
        </label>
        <label style={{ fontSize: 13, color: colors.gray[900] }}>
          Nazwa
          <input value={nazwa} onChange={(e) => setNazwa(e.target.value)} style={stylPola} />
        </label>
        <label style={{ fontSize: 13, color: colors.gray[900] }}>
          Ostrzeż na ile dni przed
          <input
            type="number"
            min={0}
            max={730}
            value={dni}
            onChange={(e) => setDni(Number(e.target.value))}
            style={stylPola}
          />
        </label>
      </div>

      <label style={{ fontSize: 13, color: colors.gray[900] }}>
        Opis
        <input value={opis} onChange={(e) => setOpis(e.target.value)} style={stylPola} />
      </label>

      <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13 }}>
        <input type="checkbox" checked={aktywny} onChange={(e) => setAktywny(e.target.checked)} />
        Aktywny — wyłączonego rodzaju nie da się wybrać przy dodawaniu terminu
      </label>

      {blad && <p style={{ margin: 0, fontSize: 13, color: colors.danger[600] }}>{blad}</p>}

      <button
        onClick={() => onZapisz({ kod, nazwa, opis, dniOstrzezenia: dni, aktywny })}
        disabled={zapisywanie || !kompletne}
        style={{
          justifySelf: 'start',
          padding: '8px 16px',
          borderRadius: 8,
          border: 'none',
          background: colors.primary[600],
          color: colors.textOnAccent,
          fontSize: 13,
          fontWeight: 600,
          cursor: zapisywanie || !kompletne ? 'not-allowed' : 'pointer',
          opacity: zapisywanie || !kompletne ? 0.55 : 1,
        }}
      >
        {zapisywanie ? 'Zapisywanie…' : 'Zapisz'}
      </button>
    </div>
  );
}
