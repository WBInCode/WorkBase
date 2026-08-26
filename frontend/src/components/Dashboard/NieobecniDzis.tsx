import { useMemo } from 'react';
import { Palmtree } from 'lucide-react';
import { useLeaveCalendar } from '@/api/hooks/useLeave';
import { useEmployees } from '@/api/hooks/useOrganization';
import { colors } from '@/theme/tokens';

/**
 * „Kto jest dziś nieobecny" — na pulpicie, czyli tam, gdzie ludzie patrzą.
 *
 * Dane istniały od dawna, ale wyłącznie na osobnym ekranie kalendarza, do którego trzeba było
 * świadomie wejść. Kierownik planujący dzień potrzebuje tej informacji od razu, a nie po
 * dwóch kliknięciach.
 *
 * Zawężenie robi serwer: `/api/leave/calendar` przepuszcza wyłącznie osoby z zakresu danych
 * pytającego (`leave.view-team`), więc lista identyfikatorów wysłana stąd jest propozycją,
 * a nie żądaniem — szeregowy pracownik zobaczy co najwyżej siebie.
 */
export function NieobecniDzis() {
  const { data: pracownicy } = useEmployees({ page: 1, pageSize: 500 });

  const dzisiaj = useMemo(() => new Date().toISOString().slice(0, 10), []);

  const zapytanie = useMemo(() => {
    const identyfikatory = (pracownicy?.items ?? []).map((p) => p.id);
    return identyfikatory.length > 0
      ? { employeeIds: identyfikatory, from: dzisiaj, to: dzisiaj }
      : null;
  }, [pracownicy, dzisiaj]);

  const { data: wpisy = [], isLoading } = useLeaveCalendar(zapytanie);

  const nazwiska = useMemo(() => {
    const mapa = new Map<string, string>();
    for (const p of pracownicy?.items ?? []) mapa.set(p.id, `${p.firstName} ${p.lastName}`);
    return mapa;
  }, [pracownicy]);

  return (
    <section
      style={{
        background: 'var(--wb-panel, #fff)',
        border: '1px solid var(--wb-line, #e3e7f1)',
        borderRadius: 14,
        padding: '16px 18px',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 10 }}>
        <Palmtree size={17} style={{ color: colors.primary[600] }} />
        <h2 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: colors.gray[900] }}>
          Dziś nieobecni
        </h2>
        {wpisy.length > 0 && (
          <span
            style={{
              marginLeft: 'auto',
              fontSize: 12,
              fontWeight: 700,
              padding: '2px 9px',
              borderRadius: 999,
              background: 'var(--wb-bg, #f1f4f9)',
              color: 'var(--wb-ink-2, #6b7490)',
            }}
          >
            {wpisy.length}
          </span>
        )}
      </div>

      {isLoading && <p style={stylPusty}>Wczytywanie…</p>}

      {!isLoading && wpisy.length === 0 && (
        <p style={stylPusty}>Dziś wszyscy są w pracy.</p>
      )}

      {wpisy.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 7 }}>
          {wpisy.map((wpis) => (
            <li
              key={`${wpis.employeeId}-${wpis.leaveTypeId}`}
              style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, flexWrap: 'wrap' }}
            >
              <span
                aria-hidden
                style={{
                  width: 8,
                  height: 8,
                  borderRadius: 999,
                  background: wpis.leaveTypeColor ?? colors.primary[600],
                  flexShrink: 0,
                }}
              />
              <span style={{ color: colors.gray[900], fontWeight: 500 }}>
                {nazwiska.get(wpis.employeeId) ?? '—'}
              </span>
              <span style={{ color: 'var(--wb-ink-2, #6b7490)' }}>{wpis.leaveTypeName}</span>
              {/* Pół dnia to realny przypadek w urlopie na żądanie — bez tego wyglądałby
                  identycznie jak nieobecność całodniowa. */}
              {wpis.dayFraction < 1 && (
                <span style={{ fontSize: 11.5, color: 'var(--wb-ink-2, #9aa3b8)' }}>
                  {Math.round(wpis.dayFraction * 100)}% dnia
                </span>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

const stylPusty: React.CSSProperties = {
  margin: 0,
  fontSize: 13,
  color: 'var(--wb-ink-2, #6b7490)',
};
