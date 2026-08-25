import { useState } from 'react';
import { FileCog, Plus, Trash2 } from 'lucide-react';
import {
  useTypyWnioskow,
  useUtworzTypWniosku,
  useZmienTypWniosku,
  type PoleWniosku,
  type TypPola,
  type TypWniosku,
} from '@/api/hooks/useWnioski';
import { colors } from '@/theme/tokens';

const TYPY_POL: { wartosc: TypPola; etykieta: string }[] = [
  { wartosc: 'Tekst', etykieta: 'Tekst' },
  { wartosc: 'Wielolinijkowy', etykieta: 'Tekst wielolinijkowy' },
  { wartosc: 'Liczba', etykieta: 'Liczba' },
  { wartosc: 'Data', etykieta: 'Data' },
  { wartosc: 'Wybor', etykieta: 'Lista wyboru' },
  { wartosc: 'TakNie', etykieta: 'Tak / nie' },
];

const PUSTE_POLE: PoleWniosku = { kod: '', etykieta: '', typ: 'Tekst', wymagane: false };

const pole = {
  display: 'block',
  width: '100%',
  marginTop: 4,
  padding: '7px 9px',
  borderRadius: 8,
  border: '1px solid var(--wb-line, #e3e7f1)',
  fontSize: 14,
} as const;

/**
 * Definiowanie rodzajow wnioskow.
 *
 * Kazdy rodzaj to formularz plus informacja, czy wniosek idzie do akceptacji przelozonego.
 * Obieg jest jeden dla wszystkich rodzajow, wiec dodanie nowego wniosku nie wymaga niczego
 * poza tym ekranem.
 */
export function TypyWnioskowConfigPage() {
  const { data: typy = [], isLoading } = useTypyWnioskow(true);
  const utworz = useUtworzTypWniosku();
  const zmien = useZmienTypWniosku();

  const [edytowany, setEdytowany] = useState<TypWniosku | null>(null);
  const [nowy, setNowy] = useState(false);
  const [kod, setKod] = useState('');
  const [nazwa, setNazwa] = useState('');
  const [opis, setOpis] = useState('');
  const [wymagaAkceptacji, setWymagaAkceptacji] = useState(true);
  const [aktywny, setAktywny] = useState(true);
  const [pola, setPola] = useState<PoleWniosku[]>([{ ...PUSTE_POLE }]);
  const [blad, setBlad] = useState<string | null>(null);

  const otworzNowy = () => {
    setNowy(true); setEdytowany(null); setBlad(null);
    setKod(''); setNazwa(''); setOpis('');
    setWymagaAkceptacji(true); setAktywny(true);
    setPola([{ ...PUSTE_POLE }]);
  };

  const otworzEdycje = (typ: TypWniosku) => {
    setEdytowany(typ); setNowy(false); setBlad(null);
    setKod(typ.kod); setNazwa(typ.nazwa); setOpis(typ.opis ?? '');
    setWymagaAkceptacji(typ.wymagaAkceptacji); setAktywny(typ.aktywny);
    setPola(typ.pola.length > 0 ? typ.pola.map((p) => ({ ...p })) : [{ ...PUSTE_POLE }]);
  };

  const zamknij = () => { setNowy(false); setEdytowany(null); setBlad(null); };

  const zmienPole = (indeks: number, zmiana: Partial<PoleWniosku>) =>
    setPola((p) => p.map((x, i) => (i === indeks ? { ...x, ...zmiana } : x)));

  const zapisz = async () => {
    setBlad(null);
    const doZapisu = {
      kod: kod.trim(),
      nazwa: nazwa.trim(),
      opis: opis.trim() || null,
      wymagaAkceptacji,
      aktywny,
      pola: pola
        .filter((p) => p.kod.trim() !== '' || p.etykieta.trim() !== '')
        .map((p) => ({
          ...p,
          kod: p.kod.trim(),
          etykieta: p.etykieta.trim(),
          // Opcje trzymamy tylko przy liscie wyboru — inaczej zostawaly po zmianie typu pola
          // i wracaly, gdy ktos przelaczyl je z powrotem.
          opcje: p.typ === 'Wybor' ? (p.opcje ?? []).filter((o) => o.trim() !== '') : null,
        })),
    };

    try {
      if (edytowany) await zmien.mutateAsync({ id: edytowany.id, ...doZapisu });
      else await utworz.mutateAsync(doZapisu);
      zamknij();
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zapisać rodzaju wniosku.');
    }
  };

  const formularzOtwarty = nowy || edytowany !== null;
  const zapisywanie = utworz.isPending || zmien.isPending;

  return (
    <div style={{ padding: '24px 28px', maxWidth: 940, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6, flexWrap: 'wrap' }}>
        <FileCog size={20} style={{ color: colors.primary[600] }} />
        <h1 style={{ fontSize: 22, fontWeight: 800, margin: 0, color: colors.gray[900] }}>
          Rodzaje wniosków
        </h1>
        {!formularzOtwarty && (
          <button
            onClick={otworzNowy}
            style={{
              marginLeft: 'auto', display: 'inline-flex', alignItems: 'center', gap: 6,
              padding: '8px 14px', borderRadius: 10, border: 'none',
              background: colors.primary[600], color: colors.textOnAccent,
              fontSize: 13, fontWeight: 600, cursor: 'pointer',
            }}
          >
            <Plus size={15} />
            Nowy rodzaj
          </button>
        )}
      </div>

      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 20px', fontSize: 14, maxWidth: '72ch' }}>
        Każdy rodzaj wniosku to formularz, który wypełnia pracownik. Wnioski idą tą samą drogą
        akceptacji co wnioski urlopowe — do przełożonego, z eskalacjami i zastępstwami.
      </p>

      {formularzOtwarty && (
        <div
          style={{
            background: 'var(--wb-panel, #fff)',
            border: '1px solid var(--wb-line, #e3e7f1)',
            borderRadius: 14,
            padding: '16px 18px',
            marginBottom: 20,
            display: 'grid',
            gap: 12,
          }}
        >
          <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
            <label style={{ fontSize: 13, color: colors.gray[900], flex: '1 1 140px' }}>
              Kod
              <input
                value={kod}
                disabled={edytowany !== null}
                maxLength={64}
                onChange={(e) => setKod(e.target.value)}
                placeholder="ZALICZKA"
                title={edytowany ? 'Kodu nie zmienia się po utworzeniu' : undefined}
                style={{ ...pole, opacity: edytowany ? 0.6 : 1 }}
              />
            </label>
            <label style={{ fontSize: 13, color: colors.gray[900], flex: '2 1 240px' }}>
              Nazwa
              <input value={nazwa} maxLength={128} onChange={(e) => setNazwa(e.target.value)}
                placeholder="Wniosek o zaliczkę" style={pole} />
            </label>
          </div>

          <label style={{ fontSize: 13, color: colors.gray[900] }}>
            Opis (opcjonalnie)
            <input value={opis} maxLength={512} onChange={(e) => setOpis(e.target.value)}
              placeholder="Kiedy używać tego wniosku" style={pole} />
          </label>

          <div style={{ display: 'flex', gap: 18, flexWrap: 'wrap', fontSize: 13, color: colors.gray[900] }}>
            <label style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
              <input type="checkbox" checked={wymagaAkceptacji}
                onChange={(e) => setWymagaAkceptacji(e.target.checked)} />
              Wymaga akceptacji przełożonego
            </label>
            <label style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
              <input type="checkbox" checked={aktywny} onChange={(e) => setAktywny(e.target.checked)} />
              Dostępny dla pracowników
            </label>
          </div>

          <div>
            <h2 style={{ fontSize: 14, fontWeight: 700, margin: '4px 0 8px', color: colors.gray[900] }}>
              Pola formularza
            </h2>

            <div style={{ display: 'grid', gap: 10 }}>
              {pola.map((p, i) => (
                <div
                  key={i}
                  style={{
                    display: 'grid', gap: 8, padding: '10px 12px',
                    background: 'var(--wb-g-50, #f8fafc)', borderRadius: 10,
                  }}
                >
                  <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                    <label style={{ fontSize: 12, color: colors.gray[900], flex: '1 1 120px' }}>
                      Kod pola
                      <input value={p.kod} onChange={(e) => zmienPole(i, { kod: e.target.value })}
                        placeholder="kwota" style={pole} />
                    </label>
                    <label style={{ fontSize: 12, color: colors.gray[900], flex: '2 1 180px' }}>
                      Etykieta
                      <input value={p.etykieta} onChange={(e) => zmienPole(i, { etykieta: e.target.value })}
                        placeholder="Kwota zaliczki" style={pole} />
                    </label>
                    <label style={{ fontSize: 12, color: colors.gray[900], flex: '1 1 140px' }}>
                      Typ
                      <select value={p.typ}
                        onChange={(e) => zmienPole(i, { typ: e.target.value as TypPola })} style={pole}>
                        {TYPY_POL.map((t) => (
                          <option key={t.wartosc} value={t.wartosc}>{t.etykieta}</option>
                        ))}
                      </select>
                    </label>
                  </div>

                  {p.typ === 'Wybor' && (
                    <label style={{ fontSize: 12, color: colors.gray[900] }}>
                      Opcje (oddzielone przecinkiem)
                      <input
                        value={(p.opcje ?? []).join(', ')}
                        onChange={(e) => zmienPole(i, { opcje: e.target.value.split(',').map((o) => o.trim()) })}
                        placeholder="auto, pociąg, samolot"
                        style={pole}
                      />
                    </label>
                  )}

                  <div style={{ display: 'flex', alignItems: 'center', gap: 14, fontSize: 12.5 }}>
                    <label style={{ display: 'inline-flex', alignItems: 'center', gap: 6, color: colors.gray[900] }}>
                      <input type="checkbox" checked={p.wymagane}
                        onChange={(e) => zmienPole(i, { wymagane: e.target.checked })} />
                      Wymagane
                    </label>
                    {pola.length > 1 && (
                      <button
                        onClick={() => setPola((x) => x.filter((_, j) => j !== i))}
                        aria-label={`Usuń pole ${p.etykieta || i + 1}`}
                        style={{
                          marginLeft: 'auto', border: 'none', background: 'transparent',
                          cursor: 'pointer', color: 'var(--wb-ink-2, #6b7490)', display: 'flex', padding: 4,
                        }}
                      >
                        <Trash2 size={15} />
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>

            <button
              onClick={() => setPola((p) => [...p, { ...PUSTE_POLE }])}
              style={{
                marginTop: 10, display: 'inline-flex', alignItems: 'center', gap: 6,
                padding: '6px 12px', borderRadius: 8,
                border: '1px solid var(--wb-line, #e3e7f1)', background: 'var(--wb-panel, #fff)',
                color: colors.gray[900], fontSize: 12.5, cursor: 'pointer',
              }}
            >
              <Plus size={14} />
              Dodaj pole
            </button>
          </div>

          {blad && <p style={{ margin: 0, fontSize: 13, color: colors.danger[600] }}>{blad}</p>}

          <div style={{ display: 'flex', gap: 8 }}>
            <button
              onClick={zapisz}
              disabled={zapisywanie}
              style={{
                padding: '8px 16px', borderRadius: 8, border: 'none',
                background: colors.primary[600], color: colors.textOnAccent,
                fontSize: 13, fontWeight: 600, cursor: zapisywanie ? 'wait' : 'pointer',
              }}
            >
              {zapisywanie ? 'Zapisywanie…' : 'Zapisz'}
            </button>
            <button
              onClick={zamknij}
              style={{
                padding: '8px 16px', borderRadius: 8,
                border: '1px solid var(--wb-line, #e3e7f1)', background: 'var(--wb-panel, #fff)',
                color: colors.gray[900], fontSize: 13, cursor: 'pointer',
              }}
            >
              Anuluj
            </button>
          </div>
        </div>
      )}

      {isLoading ? (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Wczytywanie…</p>
      ) : typy.length === 0 ? (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14, maxWidth: '70ch' }}>
          Nie ma jeszcze żadnego rodzaju wniosku. Typowe pierwsze: zaliczka, delegacja,
          praca zdalna, wniosek o sprzęt.
        </p>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 8 }}>
          {typy.map((t) => (
            <li
              key={t.id}
              style={{
                display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap',
                padding: '11px 14px', borderRadius: 10,
                background: 'var(--wb-panel, #fff)',
                border: '1px solid var(--wb-line, #e3e7f1)',
                fontSize: 14,
                opacity: t.aktywny ? 1 : 0.6,
              }}
            >
              <strong style={{ color: colors.gray[900] }}>{t.nazwa}</strong>
              <span style={{ fontSize: 12, color: 'var(--wb-ink-2, #6b7490)' }}>
                {t.kod} · {t.pola.length} {t.pola.length === 1 ? 'pole' : 'pól'}
                {t.wymagaAkceptacji ? ' · do akceptacji' : ' · bez akceptacji'}
              </span>
              {!t.aktywny && (
                <span style={{
                  fontSize: 11, padding: '2px 8px', borderRadius: 999,
                  background: colors.gray[100], color: colors.gray[500],
                }}>
                  wyłączony
                </span>
              )}
              <button
                onClick={() => otworzEdycje(t)}
                style={{
                  marginLeft: 'auto', padding: '5px 12px', borderRadius: 8,
                  border: '1px solid var(--wb-line, #e3e7f1)', background: 'var(--wb-panel, #fff)',
                  color: colors.gray[900], fontSize: 12.5, cursor: 'pointer',
                }}
              >
                Edytuj
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
