import { useCallback } from 'react';
import { ToggleLeft, ToggleRight, RefreshCw, Flag } from 'lucide-react';
import { useFeatureFlags, useToggleFeatureFlag } from '@/api/hooks/useIam';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

// Keep in sync with src/WorkBase.Shared/Modules/ModuleCatalog.cs (Key -> DisplayName).
// FeatureFlag.Module stores the short key (e.g. "org", "time"), not the PascalCase namespace.
const moduleLabels: Record<string, string> = {
  org: 'Organizacja',
  identity: 'Zarządzanie dostępem',
  time: 'Czas pracy',
  leave: 'Urlopy',
  tasks: 'Zadania',
  workflow: 'Procesy',
  dashboard: 'Dashboard',
  notification: 'Powiadomienia',
  documents: 'Dokumenty',
  integration: 'Integracje',
  forms: 'Formularze',
  cases: 'Sprawy',
  contacts: 'Kontakty',
  sales: 'Sprzedaż',
  ai: 'AI',
};

export function FeatureFlagsPage() {
  const { data: flags, isLoading, error, refetch, isFetching } = useFeatureFlags();
  const toggleMutation = useToggleFeatureFlag();
  const mobile = useIsMobile();

  const handleToggle = useCallback(
    (module: string) => {
      toggleMutation.mutate(module);
    },
    [toggleMutation],
  );

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '800px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>Flagi funkcjonalności</h1>
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

      {/* Error */}
      {error && (
        <div style={{
          padding: '12px 16px', marginBottom: '16px', borderRadius: '12px',
          backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px',
        }}>
          Błąd ładowania flag.
          <button onClick={() => refetch()} style={{ marginLeft: '8px', color: colors.primary[600], background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: '13px' }}>Ponów</button>
        </div>
      )}

      {/* Loading */}
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>
          Ładowanie...
        </div>
      ) : !flags || flags.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400] }}>
          <Flag size={40} style={{ marginBottom: '12px', opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak flag</div>
          <div style={{ fontSize: '13px', marginTop: '4px' }}>
            Flagi funkcjonalności nie zostały jeszcze skonfigurowane.
          </div>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {flags.map((flag) => (
            <div
              key={flag.module}
              style={{
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                padding: '16px 20px', borderRadius: '12px',
                border: `1px solid ${colors.gray[200]}`, backgroundColor: colors.white,
              }}
            >
              <div>
                <div style={{ fontSize: '14px', fontWeight: 600, color: colors.gray[900] }}>
                  {moduleLabels[flag.module] ?? flag.module}
                </div>
                <div style={{ fontSize: '12px', color: colors.gray[500], marginTop: '2px' }}>
                  Moduł: {flag.module}
                  {flag.enabledAt && (
                    <span style={{ marginLeft: '12px' }}>
                      Zmieniono: {new Date(flag.enabledAt).toLocaleString('pl-PL')}
                    </span>
                  )}
                </div>
              </div>

              <button
                onClick={() => handleToggle(flag.module)}
                disabled={toggleMutation.isPending}
                style={{
                  display: 'inline-flex', alignItems: 'center', gap: '6px',
                  padding: '6px 14px', fontSize: '13px', fontWeight: 500, borderRadius: '10px',
                  border: 'none', cursor: toggleMutation.isPending ? 'wait' : 'pointer',
                  backgroundColor: flag.isEnabled ? colors.success[100] : colors.gray[100],
                  color: flag.isEnabled ? colors.success[800] : colors.gray[500],
                }}
              >
                {flag.isEnabled ? <ToggleRight size={18} /> : <ToggleLeft size={18} />}
                {flag.isEnabled ? 'Włączony' : 'Wyłączony'}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
