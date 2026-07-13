import { useState, useEffect } from 'react';
import { Type, RefreshCw, RotateCcw } from 'lucide-react';
import { useTerminology, useUpdateTerminology } from '@/api/hooks/useTenantConfig';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';
import pl from '@/i18n/pl';

// Curated list of the naming keys that are actually wired through t() in the app today
// (see MainLayout.tsx nav labels). Extend this list as more of the UI is migrated from
// hardcoded strings to i18next keys — see docs/AUDIT-KNOWLEDGE-MAP.md.
const TERMINOLOGY_KEYS: { key: string; hint: string }[] = [
  { key: 'nav.myDay', hint: 'Sekcja startowa w menu' },
  { key: 'nav.dashboard', hint: 'Pulpit menedżerski' },
  { key: 'nav.organization', hint: 'Nazwa modułu organizacji' },
  { key: 'nav.structure', hint: 'Struktura jednostek' },
  { key: 'nav.employees', hint: 'Nazwa encji pracownika (liczba mnoga)' },
  { key: 'nav.csvImport', hint: 'Import z pliku CSV' },
  { key: 'nav.timeTracking', hint: 'Nazwa modułu czasu pracy' },
  { key: 'nav.timesheet', hint: 'Karta czasu pracy' },
  { key: 'nav.teamReport', hint: 'Raport zespołu' },
  { key: 'nav.schedule', hint: 'Grafik pracy' },
  { key: 'nav.payroll', hint: 'Wynagrodzenia' },
  { key: 'nav.leave', hint: 'Nazwa modułu urlopów' },
  { key: 'nav.requests', hint: 'Wnioski urlopowe' },
  { key: 'nav.approvals', hint: 'Akceptacje' },
  { key: 'nav.calendar', hint: 'Kalendarz urlopów' },
  { key: 'nav.tasks', hint: 'Nazwa modułu zadań' },
  { key: 'nav.allTasks', hint: 'Wszystkie zadania' },
  { key: 'nav.myTasks', hint: 'Moje zadania' },
  { key: 'nav.documents', hint: 'Nazwa modułu dokumentów' },
  { key: 'nav.files', hint: 'Pliki' },
  { key: 'nav.categories', hint: 'Kategorie dokumentów' },
  { key: 'nav.admin', hint: 'Sekcja administracji' },
];

function getDefault(key: string): string {
  const value = key.split('.').reduce<unknown>((obj, part) => {
    if (obj && typeof obj === 'object' && part in obj) return (obj as Record<string, unknown>)[part];
    return undefined;
  }, pl);
  return typeof value === 'string' ? value : key;
}

export function TerminologyConfigPage() {
  const { data: overrides, isLoading, error, refetch, isFetching } = useTerminology();
  const updateMutation = useUpdateTerminology();
  const mobile = useIsMobile();
  const [values, setValues] = useState<Record<string, string>>({});

  useEffect(() => {
    setValues(overrides ?? {});
  }, [overrides]);

  const handleChange = (key: string, value: string) => {
    setValues((prev) => ({ ...prev, [key]: value }));
  };

  const handleReset = (key: string) => {
    setValues((prev) => {
      const next = { ...prev };
      delete next[key];
      return next;
    });
  };

  const handleSave = () => {
    updateMutation.mutate(values);
  };

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '760px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900], display: 'flex', alignItems: 'center', gap: '8px' }}>
          <Type size={22} /> Nazewnictwo
        </h1>
        <button
          onClick={() => refetch()}
          style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            padding: '7px 12px', fontSize: '13px', fontWeight: 500,
            color: colors.gray[700], backgroundColor: colors.white,
            border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', cursor: 'pointer',
          }}
          title="Odśwież"
        >
          <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
        </button>
      </div>

      <p style={{ fontSize: '13px', color: colors.gray[500], marginTop: '-12px', marginBottom: '20px' }}>
        Nadpisz domyślne nazwy widoczne w menu i na stronach. Puste pole = wartość domyślna.
      </p>

      {error && (
        <div style={{ padding: '12px 16px', marginBottom: '16px', borderRadius: '12px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px' }}>
          Błąd ładowania nazewnictwa.
        </div>
      )}
      {updateMutation.isSuccess && (
        <div style={{ padding: '10px 16px', marginBottom: '16px', borderRadius: '12px', backgroundColor: colors.success[100], border: `1px solid ${colors.success[200]}`, color: colors.success[800], fontSize: '13px' }}>
          Zapisano. Zmiany będą widoczne po odświeżeniu strony.
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
      ) : (
        <>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {TERMINOLOGY_KEYS.map(({ key, hint }) => {
              const defaultValue = getDefault(key);
              const value = values[key] ?? '';
              return (
                <div key={key} style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr auto', gap: '10px', alignItems: 'center', padding: '10px 12px', borderRadius: '12px', border: `1px solid ${colors.gray[200]}` }}>
                  <div>
                    <div style={{ fontSize: '13px', fontWeight: 600, color: colors.gray[900] }}>{defaultValue}</div>
                    <div style={{ fontSize: '11px', color: colors.gray[500] }}>{hint}</div>
                  </div>
                  <input
                    value={value}
                    onChange={(e) => handleChange(key, e.target.value)}
                    placeholder={defaultValue}
                    maxLength={256}
                    style={{ width: '100%', padding: '7px 10px', fontSize: '13px', border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', boxSizing: 'border-box' }}
                  />
                  <button
                    type="button"
                    onClick={() => handleReset(key)}
                    disabled={!value}
                    title="Przywróć domyślną"
                    style={{ background: 'none', border: 'none', cursor: value ? 'pointer' : 'default', color: value ? colors.gray[500] : colors.gray[300], padding: '4px' }}
                  >
                    <RotateCcw size={16} />
                  </button>
                </div>
              );
            })}
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '20px' }}>
            <button
              onClick={handleSave}
              disabled={updateMutation.isPending}
              style={{
                padding: '9px 22px', fontSize: '14px', fontWeight: 500,
                color: colors.white, backgroundColor: colors.primary[600], border: 'none',
                borderRadius: '999px', cursor: updateMutation.isPending ? 'not-allowed' : 'pointer',
                opacity: updateMutation.isPending ? 0.6 : 1,
              }}
            >
              {updateMutation.isPending ? 'Zapisywanie...' : 'Zapisz'}
            </button>
          </div>
        </>
      )}
    </div>
  );
}
