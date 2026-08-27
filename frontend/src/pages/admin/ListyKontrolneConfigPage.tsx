import { useState } from 'react';
import { ListChecks, Plus, X, Trash2, ArrowDown, ArrowUp } from 'lucide-react';
import {
  useListyKontrolne,
  useZapiszListeKontrolna,
  WYZWALACZ_ETYKIETA,
  WYKONAWCA_ETYKIETA,
  type ListaKontrolna,
  type PozycjaListy,
  type WyzwalaczListy,
  type WykonawcaPozycji,
} from '@/api/hooks/useListyKontrolne';
import { useEmployees } from '@/api/hooks/useOrganization';
import { colors } from '@/theme/tokens';

/**
 * Listy kontrolne przyjecia i odejscia: szablon, ktory przy zdarzeniu sam zaklada zadania.
 *
 * Nowa firma dostaje dwa przyklady WYLACZONE — do obejrzenia i wlaczenia jednym kliknieciem,
 * nie do zaskoczenia zadaniami, o ktore nikt nie prosil. Lista wylaczona nic nie robi.
 *
 * Pozycja bez wykonawcy (nowy pracownik nie ma jeszcze przelozonego) jest przy zdarzeniu
 * pomijana, reszta listy powstaje — to zwykla sytuacja tuz po dodaniu osoby.
 */
export function ListyKontrolneConfigPage() {
  const { data: listy = [], isLoading } = useListyKontrolne();
  const zapisz = useZapiszListeKontrolna();
  const [edytowana, setEdytowana] = useState<ListaKontrolna | 'nowa' | null>(null);

  const przelacz = (lista: ListaKontrolna) =>
    zapisz.mutate({ id: lista.id, nazwa: lista.nazwa, wyzwalacz: lista.wyzwalacz, aktywna: !lista.aktywna, pozycje: lista.pozycje });

  return (
    <div style={{ padding: '24px 28px', maxWidth: 860, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6, flexWrap: 'wrap' }}>
        <ListChecks size={20} style={{ color: colors.primary[600] }} />
        <h1 style={{ fontSize: 22, fontWeight: 800, margin: 0, color: colors.gray[900] }}>Listy kontrolne</h1>
        <button onClick={() => setEdytowana(edytowana ? null : 'nowa')} style={przyciskGlowny}>
          {edytowana ? <X size={15} /> : <Plus size={15} />}
          {edytowana ? 'Anuluj' : 'Dodaj listę'}
        </button>
      </div>

      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 20px', fontSize: 14, maxWidth: '72ch' }}>
        Co trzeba zrobić, gdy ktoś przychodzi albo odchodzi. Przy dodaniu lub dezaktywacji pracownika
        system sam zakłada zadania z tej listy — z terminem i przypisaniem. Lista wyłączona nic nie robi;
        przykłady startowe są wyłączone celowo.
      </p>

      {edytowana && (
        <Formularz
          lista={edytowana === 'nowa' ? null : edytowana}
          zapisywanie={zapisz.isPending}
          onZapisz={async (dane) => {
            await zapisz.mutateAsync({ id: edytowana === 'nowa' ? null : edytowana.id, ...dane });
            setEdytowana(null);
          }}
        />
      )}

      {isLoading ? (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Wczytywanie…</p>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 10 }}>
          {listy.map((lista) => (
            <li
              key={lista.id}
              style={{
                background: 'var(--wb-panel, #fff)', border: '1px solid var(--wb-line, #e3e7f1)',
                borderRadius: 12, padding: '13px 15px', opacity: lista.aktywna ? 1 : 0.6,
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                <strong style={{ fontSize: 14, color: colors.gray[900] }}>{lista.nazwa}</strong>
                <span style={{ fontSize: 11.5, padding: '1px 8px', borderRadius: 999, background: colors.primary[50], color: colors.primary[700], fontWeight: 600 }}>
                  {WYZWALACZ_ETYKIETA[lista.wyzwalacz]}
                </span>
                {!lista.aktywna && <span style={{ fontSize: 11.5, color: 'var(--wb-ink-2, #6b7490)' }}>wyłączona</span>}
                <span style={{ marginLeft: 'auto', display: 'flex', gap: 6 }}>
                  <button onClick={() => przelacz(lista)} disabled={zapisz.isPending} style={przyciskLekki}>
                    {lista.aktywna ? 'Wyłącz' : 'Włącz'}
                  </button>
                  <button onClick={() => setEdytowana(lista)} style={przyciskLekki}>Zmień</button>
                </span>
              </div>
              <ol style={{ margin: '8px 0 0', paddingLeft: 20, fontSize: 13, color: 'var(--wb-ink-2, #6b7490)', display: 'grid', gap: 2 }}>
                {lista.pozycje.map((p, i) => (
                  <li key={i}>
                    <span style={{ color: colors.gray[900] }}>{p.tytul}</span>
                    {' — '}{WYKONAWCA_ETYKIETA[p.wykonawca]}, {p.dniOdZdarzenia === 0 ? 'tego samego dnia' : `${p.dniOdZdarzenia} dni później`}
                  </li>
                ))}
              </ol>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function Formularz({
  lista,
  zapisywanie,
  onZapisz,
}: {
  lista: ListaKontrolna | null;
  zapisywanie: boolean;
  onZapisz: (dane: { nazwa: string; wyzwalacz: WyzwalaczListy; aktywna: boolean; pozycje: PozycjaListy[] }) => Promise<void>;
}) {
  const [nazwa, setNazwa] = useState(lista?.nazwa ?? '');
  const [wyzwalacz, setWyzwalacz] = useState<WyzwalaczListy>(lista?.wyzwalacz ?? 'Przyjecie');
  const [aktywna, setAktywna] = useState(lista?.aktywna ?? false);
  const [pozycje, setPozycje] = useState<PozycjaListy[]>(
    lista?.pozycje ?? [{ tytul: '', dniOdZdarzenia: 0, wykonawca: 'Przelozony', osobaId: null }],
  );
  const [blad, setBlad] = useState<string | null>(null);
  // Do wyboru „wskazana osoba". 200 wystarcza dla odbiorcy, o ktorym mowa; wieksze firmy
  // dostana wyszukiwarke, gdy pierwsza o nia zapyta.
  const { data: pracownicy } = useEmployees({ page: 1, pageSize: 200, status: 'Active' });

  const zmien = (i: number, zmiana: Partial<PozycjaListy>) =>
    setPozycje((p) => p.map((x, j) => (j === i ? { ...x, ...zmiana } : x)));
  const przesun = (i: number, o: -1 | 1) =>
    setPozycje((p) => {
      const n = [...p];
      const j = i + o;
      if (j < 0 || j >= n.length) return p;
      [n[i], n[j]] = [n[j]!, n[i]!];
      return n;
    });

  const kompletne =
    nazwa.trim() !== '' &&
    pozycje.length > 0 &&
    pozycje.every((p) => p.tytul.trim() !== '' && (p.wykonawca !== 'Osoba' || p.osobaId));

  const wyslij = async () => {
    setBlad(null);
    try {
      await onZapisz({ nazwa: nazwa.trim(), wyzwalacz, aktywna, pozycje });
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zapisać.');
    }
  };

  return (
    <div style={{ background: 'var(--wb-panel, #fff)', border: '1px solid var(--wb-line, #e3e7f1)', borderRadius: 12, padding: '16px 18px', marginBottom: 18, display: 'grid', gap: 12 }}>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 12 }}>
        <label style={etykieta}>
          Nazwa
          <input value={nazwa} onChange={(e) => setNazwa(e.target.value)} style={stylPola} placeholder="np. Przyjęcie do działu produkcji" />
        </label>
        <label style={etykieta}>
          Kiedy
          <select value={wyzwalacz} onChange={(e) => setWyzwalacz(e.target.value as WyzwalaczListy)} style={stylPola}>
            {(Object.keys(WYZWALACZ_ETYKIETA) as WyzwalaczListy[]).map((w) => (
              <option key={w} value={w}>{WYZWALACZ_ETYKIETA[w]}</option>
            ))}
          </select>
        </label>
      </div>

      <div style={{ display: 'grid', gap: 8 }}>
        <span style={{ fontSize: 13, fontWeight: 600, color: colors.gray[900] }}>Pozycje — każda stanie się zadaniem</span>
        {pozycje.map((p, i) => (
          <div key={i} style={{ display: 'grid', gridTemplateColumns: 'minmax(180px, 2fr) 90px minmax(140px, 1fr) auto', gap: 8, alignItems: 'end' }}>
            <label style={etykieta}>
              {i === 0 && 'Co zrobić'}
              <input value={p.tytul} onChange={(e) => zmien(i, { tytul: e.target.value })} style={stylPola} placeholder="np. Przygotuj stanowisko" />
            </label>
            <label style={etykieta}>
              {i === 0 && 'Dni po'}
              <input type="number" min={0} value={p.dniOdZdarzenia} onChange={(e) => zmien(i, { dniOdZdarzenia: Math.max(0, Number(e.target.value)) })} style={stylPola} />
            </label>
            <label style={etykieta}>
              {i === 0 && 'Kto'}
              <select
                value={p.wykonawca === 'Osoba' ? `osoba:${p.osobaId ?? ''}` : p.wykonawca}
                onChange={(e) => {
                  const v = e.target.value;
                  if (v.startsWith('osoba:')) zmien(i, { wykonawca: 'Osoba', osobaId: v.slice(6) || null });
                  else zmien(i, { wykonawca: v as WykonawcaPozycji, osobaId: null });
                }}
                style={stylPola}
              >
                <option value="Przelozony">{WYKONAWCA_ETYKIETA.Przelozony}</option>
                <option value="Pracownik">{WYKONAWCA_ETYKIETA.Pracownik}</option>
                <optgroup label="wskazana osoba">
                  {(pracownicy?.items ?? []).map((e) => (
                    <option key={e.id} value={`osoba:${e.id}`}>{e.firstName} {e.lastName}</option>
                  ))}
                </optgroup>
              </select>
            </label>
            <span style={{ display: 'flex', gap: 2 }}>
              <button onClick={() => przesun(i, -1)} title="W górę" style={ikonaPrzycisk} disabled={i === 0}><ArrowUp size={13} /></button>
              <button onClick={() => przesun(i, 1)} title="W dół" style={ikonaPrzycisk} disabled={i === pozycje.length - 1}><ArrowDown size={13} /></button>
              <button onClick={() => setPozycje((x) => x.filter((_, j) => j !== i))} title="Usuń pozycję" style={ikonaPrzycisk} disabled={pozycje.length === 1}><Trash2 size={13} /></button>
            </span>
          </div>
        ))}
        <button
          onClick={() => setPozycje((p) => [...p, { tytul: '', dniOdZdarzenia: 0, wykonawca: 'Przelozony', osobaId: null }])}
          style={{ ...przyciskLekki, justifySelf: 'start' }}
        >
          <Plus size={13} /> Dodaj pozycję
        </button>
      </div>

      <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, color: colors.gray[900] }}>
        <input type="checkbox" checked={aktywna} onChange={(e) => setAktywna(e.target.checked)} />
        Włączona — od zapisu zakłada zadania przy każdym zdarzeniu
      </label>

      {blad && <p style={{ margin: 0, fontSize: 13, color: colors.danger[600] }}>{blad}</p>}

      <button onClick={wyslij} disabled={!kompletne || zapisywanie} style={{ ...przyciskGlowny, marginLeft: 0, justifySelf: 'start', opacity: kompletne ? 1 : 0.55 }}>
        {zapisywanie ? 'Zapisywanie…' : 'Zapisz listę'}
      </button>
    </div>
  );
}

const etykieta: React.CSSProperties = { fontSize: 12.5, color: colors.gray[900], display: 'block' };

const stylPola: React.CSSProperties = {
  display: 'block', width: '100%', marginTop: 4, padding: '7px 9px', borderRadius: 8,
  border: '1px solid var(--wb-line, #e3e7f1)', fontFamily: 'inherit', fontSize: 14, boxSizing: 'border-box',
};

const przyciskGlowny: React.CSSProperties = {
  marginLeft: 'auto', display: 'inline-flex', alignItems: 'center', gap: 6, padding: '8px 14px',
  borderRadius: 10, border: 'none', background: colors.primary[600], color: colors.textOnAccent,
  fontSize: 13, fontWeight: 600, fontFamily: 'inherit', cursor: 'pointer',
};

const przyciskLekki: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', gap: 5, padding: '6px 12px', borderRadius: 8,
  border: '1px solid var(--wb-line, #e3e7f1)', background: 'var(--wb-panel, #fff)',
  color: colors.primary[600], fontSize: 12.5, fontWeight: 600, fontFamily: 'inherit', cursor: 'pointer',
};

const ikonaPrzycisk: React.CSSProperties = {
  padding: 7, borderRadius: 8, border: '1px solid var(--wb-line, #e3e7f1)', background: 'var(--wb-panel, #fff)',
  color: 'var(--wb-ink-2, #6b7490)', cursor: 'pointer', display: 'inline-flex',
};
