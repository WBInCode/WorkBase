import { useState } from 'react';
import { Package, Plus, X, Check, Undo2 } from 'lucide-react';
import {
  useMieniePracownika,
  useWydajMienie,
  useZwrocMienie,
  usePotwierdzOdbior,
  TYPOWE_RODZAJE,
  type Mienie,
} from '@/api/hooks/useMienie';
import { useCurrentUser } from '@/api/hooks/useIam';
import { useUprawnienia } from '@/auth/useUprawnienia';
import { colors } from '@/theme/tokens';

const dzisiaj = () => new Date().toISOString().slice(0, 10);
const data = (iso: string) => new Date(iso).toLocaleDateString('pl-PL');

/**
 * Mienie powierzone na karcie pracownika: laptop, telefon, klucze, odziez, narzedzia.
 *
 * Zwrot nie kasuje wpisu — historia „kto mial ten laptop przede mna" zostaje pod przelacznikiem.
 * Potwierdzenie odbioru moze zlozyc wylacznie sam pracownik ze swojego konta; przycisk widzi
 * tylko on, a serwer i tak sprawdza wlasnosc. Brak potwierdzenia niczego nie blokuje.
 */
export function EmployeeMienieSection({ employeeId }: { employeeId: string }) {
  const [zeZwroconymi, setZeZwroconymi] = useState(false);
  const { data: lista = [], isLoading, isError } = useMieniePracownika(employeeId, zeZwroconymi);
  const { data: ja } = useCurrentUser();
  const { moze } = useUprawnienia();
  const wydaj = useWydajMienie();
  const zwroc = useZwrocMienie();
  const potwierdz = usePotwierdzOdbior();

  const mozeEdytowac = moze('org.edit');
  const toJa = ja?.employeeId === employeeId;

  const [formularz, setFormularz] = useState(false);
  const [rodzaj, setRodzaj] = useState('');
  const [nazwa, setNazwa] = useState('');
  const [numer, setNumer] = useState('');
  const [wydanoDnia, setWydanoDnia] = useState(dzisiaj);
  const [zwracane, setZwracane] = useState<Mienie | null>(null);
  const [zwroconoDnia, setZwroconoDnia] = useState(dzisiaj);
  const [blad, setBlad] = useState<string | null>(null);

  const zamknij = () => {
    setFormularz(false);
    setZwracane(null);
    setRodzaj('');
    setNazwa('');
    setNumer('');
    setWydanoDnia(dzisiaj());
    setZwroconoDnia(dzisiaj());
    setBlad(null);
  };

  const zapisz = async () => {
    setBlad(null);
    try {
      if (zwracane) {
        await zwroc.mutateAsync({ id: zwracane.id, zwroconoDnia });
      } else {
        await wydaj.mutateAsync({ employeeId, rodzaj, nazwa, wydanoDnia, numerSeryjny: numer || null });
      }
      zamknij();
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zapisać.');
    }
  };

  const kompletne = zwracane ? zwroconoDnia !== '' : rodzaj.trim() !== '' && nazwa.trim() !== '' && wydanoDnia !== '';
  const niezwrocone = lista.filter((m) => !m.zwroconoDnia);

  return (
    <section
      style={{
        background: 'var(--wb-panel, #fff)',
        border: '1px solid var(--wb-line, #e3e7f1)',
        borderRadius: 14,
        padding: '16px 18px',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4, flexWrap: 'wrap' }}>
        <Package size={17} style={{ color: colors.primary[600] }} />
        <h2 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: colors.gray[900] }}>Mienie powierzone</h2>
        {niezwrocone.length > 0 && (
          <span style={{ fontSize: 11.5, fontWeight: 700, padding: '1px 8px', borderRadius: 999, background: colors.primary[100], color: colors.primary[700] }}>
            {niezwrocone.length}
          </span>
        )}

        {mozeEdytowac && (
          <button
            onClick={() => (formularz ? zamknij() : setFormularz(true))}
            style={przyciskLekki}
          >
            {formularz ? <X size={13} /> : <Plus size={13} />}
            {formularz ? 'Anuluj' : 'Wydaj'}
          </button>
        )}
      </div>

      <p style={{ margin: '0 0 12px', fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)' }}>
        Co firma wydała tej osobie i co ma wrócić przy odejściu. Zwrot zostawia wpis w historii.
      </p>

      {formularz && (
        <div style={{ display: 'grid', gap: 10, marginBottom: 14 }}>
          {zwracane ? (
            <>
              <p style={{ margin: 0, fontSize: 13, color: colors.gray[900] }}>
                Zwrot: <strong>{zwracane.rodzaj} — {zwracane.nazwa}</strong>
              </p>
              <label style={etykieta}>
                Data zwrotu
                <input type="date" value={zwroconoDnia} onChange={(e) => setZwroconoDnia(e.target.value)} style={stylPola} />
              </label>
            </>
          ) : (
            <>
              <label style={etykieta}>
                Rodzaj
                <input
                  list="mienie-rodzaje"
                  value={rodzaj}
                  onChange={(e) => setRodzaj(e.target.value)}
                  placeholder="np. Laptop"
                  style={stylPola}
                />
                {/* datalist zamiast slownika: podpowiada, nie zmusza */}
                <datalist id="mienie-rodzaje">
                  {TYPOWE_RODZAJE.map((r) => <option key={r} value={r} />)}
                </datalist>
              </label>
              <label style={etykieta}>
                Co dokładnie
                <input value={nazwa} onChange={(e) => setNazwa(e.target.value)} placeholder="np. ThinkPad T14, rozmiar L, 2 klucze" style={stylPola} />
              </label>
              <label style={etykieta}>
                Numer seryjny / inwentarzowy (opcjonalnie)
                <input value={numer} onChange={(e) => setNumer(e.target.value)} style={stylPola} />
              </label>
              <label style={etykieta}>
                Data wydania
                <input type="date" value={wydanoDnia} onChange={(e) => setWydanoDnia(e.target.value)} style={stylPola} />
              </label>
            </>
          )}

          {blad && <p style={{ margin: 0, fontSize: 12.5, color: colors.danger[600] }}>{blad}</p>}

          <button
            onClick={zapisz}
            disabled={!kompletne || wydaj.isPending || zwroc.isPending}
            style={{ ...przyciskGlowny, opacity: kompletne ? 1 : 0.55 }}
          >
            {zwracane ? 'Odnotuj zwrot' : 'Zapisz wydanie'}
          </button>
        </div>
      )}

      {isLoading && <p style={tekstPomocniczy}>Wczytywanie…</p>}
      {isError && <p style={tekstPomocniczy}>Brak dostępu do mienia tej osoby.</p>}
      {!isLoading && !isError && lista.length === 0 && (
        <p style={tekstPomocniczy}>{zeZwroconymi ? 'Nic nie wydano.' : 'Nic do oddania.'}</p>
      )}

      {lista.length > 0 && (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'grid', gap: 6 }}>
          {lista.map((m) => {
            const zwrocone = m.zwroconoDnia !== null;
            return (
              <li
                key={m.id}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 10,
                  padding: '8px 10px',
                  borderRadius: 10,
                  background: zwrocone ? 'transparent' : 'var(--wb-g-50, #f7f8fb)',
                  opacity: zwrocone ? 0.65 : 1,
                  flexWrap: 'wrap',
                }}
              >
                <div style={{ flex: 1, minWidth: 160 }}>
                  <div style={{ fontSize: 13, fontWeight: 600, color: colors.gray[900] }}>
                    {m.rodzaj} — {m.nazwa}
                    {m.numerSeryjny && <span style={{ fontWeight: 400, color: 'var(--wb-ink-2, #6b7490)' }}> · {m.numerSeryjny}</span>}
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--wb-ink-2, #6b7490)' }}>
                    wydano {data(m.wydanoDnia)}
                    {zwrocone && ` · zwrócono ${data(m.zwroconoDnia!)}`}
                    {!zwrocone && (m.potwierdzonoOdbior
                      ? ` · odbiór potwierdzony ${data(m.potwierdzonoOdbior)}`
                      : ' · odbiór niepotwierdzony')}
                  </div>
                </div>

                {!zwrocone && toJa && !m.potwierdzonoOdbior && (
                  <button onClick={() => potwierdz.mutate(m.id)} disabled={potwierdz.isPending} style={przyciskLekki} title="Potwierdzam, że odebrałem tę rzecz">
                    <Check size={13} /> Potwierdzam odbiór
                  </button>
                )}
                {!zwrocone && mozeEdytowac && (
                  <button onClick={() => { setZwracane(m); setFormularz(true); }} style={przyciskLekki}>
                    <Undo2 size={13} /> Zwrot
                  </button>
                )}
              </li>
            );
          })}
        </ul>
      )}

      <label style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 10, fontSize: 12, color: 'var(--wb-ink-2, #6b7490)' }}>
        <input type="checkbox" checked={zeZwroconymi} onChange={(e) => setZeZwroconymi(e.target.checked)} />
        pokaż także zwrócone
      </label>
    </section>
  );
}

const etykieta: React.CSSProperties = { fontSize: 13, color: colors.gray[900] };

const stylPola: React.CSSProperties = {
  display: 'block',
  width: '100%',
  marginTop: 4,
  padding: '7px 9px',
  borderRadius: 8,
  border: '1px solid var(--wb-line, #e3e7f1)',
  fontFamily: 'inherit',
  fontSize: 14,
  boxSizing: 'border-box',
};

const tekstPomocniczy: React.CSSProperties = { margin: 0, fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' };

const przyciskLekki: React.CSSProperties = {
  marginLeft: 'auto',
  display: 'inline-flex',
  alignItems: 'center',
  gap: 5,
  padding: '5px 11px',
  borderRadius: 8,
  border: '1px solid var(--wb-line, #e3e7f1)',
  background: 'var(--wb-panel, #fff)',
  color: colors.primary[600],
  fontSize: 12.5,
  fontWeight: 600,
  fontFamily: 'inherit',
  cursor: 'pointer',
};

const przyciskGlowny: React.CSSProperties = {
  padding: '8px 14px',
  borderRadius: 10,
  border: 'none',
  background: colors.primary[600],
  color: colors.textOnAccent,
  fontSize: 13,
  fontWeight: 600,
  fontFamily: 'inherit',
  cursor: 'pointer',
  justifySelf: 'start',
};
