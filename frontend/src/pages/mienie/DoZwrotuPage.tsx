import { Link } from 'react-router-dom';
import { Package, Undo2 } from 'lucide-react';
import { useMienieDoZwrotu, useZwrocMienie } from '@/api/hooks/useMienie';
import { useUprawnienia } from '@/auth/useUprawnienia';
import { colors } from '@/theme/tokens';

const data = (iso: string) => new Date(iso).toLocaleDateString('pl-PL');

/**
 * „Co do zwrotu" — niezwrocone rzeczy u osob, ktore odchodza albo juz odeszly.
 *
 * Celowo NIE ma tu wszystkich wydanych rzeczy: laptop u kogos, kto pracuje, nie jest do zwrotu.
 * Pelny stan jednej osoby jest na jej karcie. Serwer zaweza liste do zakresu danych pytajacego,
 * wiec kierownik widzi swoich ludzi, kadry — wszystkich.
 */
export function DoZwrotuPage() {
  const { data: lista = [], isLoading } = useMienieDoZwrotu();
  const { moze } = useUprawnienia();
  const zwroc = useZwrocMienie();
  const mozeEdytowac = moze('org.edit');

  // Grupujemy po osobie: przy odejsciu oddaje sie wszystko naraz, wiec tak sie to czyta.
  const osoby = new Map<string, { imieNazwisko: string; powod: string; rzeczy: typeof lista }>();
  for (const m of lista) {
    const wpis = osoby.get(m.employeeId) ?? { imieNazwisko: m.imieNazwisko, powod: m.powod, rzeczy: [] };
    wpis.rzeczy.push(m);
    osoby.set(m.employeeId, wpis);
  }

  return (
    <div style={{ padding: '24px 28px', maxWidth: 900, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
        <Package size={20} style={{ color: colors.primary[600] }} />
        <h1 style={{ fontSize: 22, fontWeight: 800, margin: 0, color: colors.gray[900] }}>Do zwrotu</h1>
      </div>
      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 22px', fontSize: 14, maxWidth: '70ch' }}>
        Rzeczy firmy u osób, które odchodzą albo już odeszły. Sprzęt osób pracujących jest na ich
        kartach — tu trafia wyłącznie to, co ktoś powinien odebrać.
      </p>

      {isLoading && <p style={{ fontSize: 14, color: 'var(--wb-ink-2, #6b7490)' }}>Wczytywanie…</p>}

      {!isLoading && osoby.size === 0 && (
        <div style={{ padding: '18px 20px', borderRadius: 14, background: 'var(--wb-emr-100, #d1fae5)', color: 'var(--wb-emr-800, #065f46)', fontSize: 14 }}>
          Nikt z odchodzących nie ma nic do oddania.
        </div>
      )}

      <div style={{ display: 'grid', gap: 14 }}>
        {[...osoby.entries()].map(([employeeId, osoba]) => (
          <section
            key={employeeId}
            style={{ background: 'var(--wb-panel, #fff)', border: '1px solid var(--wb-line, #e3e7f1)', borderRadius: 14, padding: '14px 18px' }}
          >
            <div style={{ display: 'flex', alignItems: 'baseline', gap: 10, marginBottom: 8, flexWrap: 'wrap' }}>
              <Link to={`/org/employees/${employeeId}`} style={{ fontSize: 15, fontWeight: 700, color: colors.gray[900], textDecoration: 'none' }}>
                {osoba.imieNazwisko}
              </Link>
              <span style={{ fontSize: 12.5, color: colors.warning[800], background: colors.warning[100], padding: '1px 8px', borderRadius: 999, fontWeight: 600 }}>
                {osoba.powod}
              </span>
              <span style={{ fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)' }}>
                {osoba.rzeczy.length} {osoba.rzeczy.length === 1 ? 'rzecz' : osoba.rzeczy.length < 5 ? 'rzeczy' : 'rzeczy'}
              </span>
            </div>

            <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'grid', gap: 6 }}>
              {osoba.rzeczy.map((m) => (
                <li key={m.id} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 10px', borderRadius: 10, background: 'var(--wb-g-50, #f7f8fb)', flexWrap: 'wrap' }}>
                  <div style={{ flex: 1, minWidth: 180 }}>
                    <div style={{ fontSize: 13, fontWeight: 600, color: colors.gray[900] }}>
                      {m.rodzaj} — {m.nazwa}
                      {m.numerSeryjny && <span style={{ fontWeight: 400, color: 'var(--wb-ink-2, #6b7490)' }}> · {m.numerSeryjny}</span>}
                    </div>
                    <div style={{ fontSize: 12, color: 'var(--wb-ink-2, #6b7490)' }}>
                      wydano {data(m.wydanoDnia)}
                      {m.wartosc !== null && ` · ${m.wartosc.toLocaleString('pl-PL', { style: 'currency', currency: 'PLN' })}`}
                    </div>
                  </div>
                  {mozeEdytowac && (
                    <button
                      onClick={() => zwroc.mutate({ id: m.id, zwroconoDnia: new Date().toISOString().slice(0, 10) })}
                      disabled={zwroc.isPending}
                      style={{
                        display: 'inline-flex', alignItems: 'center', gap: 5, padding: '5px 11px', borderRadius: 8,
                        border: '1px solid var(--wb-line, #e3e7f1)', background: 'var(--wb-panel, #fff)',
                        color: colors.primary[600], fontSize: 12.5, fontWeight: 600, fontFamily: 'inherit', cursor: 'pointer',
                      }}
                    >
                      <Undo2 size={13} /> Zwrócono dziś
                    </button>
                  )}
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>
    </div>
  );
}
