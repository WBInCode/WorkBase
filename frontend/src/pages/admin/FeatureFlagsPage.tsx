import { useCallback } from 'react';
import { ToggleLeft, ToggleRight, RefreshCw, Flag } from 'lucide-react';
import { useFeatureFlags, useToggleFeatureFlag } from '@/api/hooks/useIam';

const moduleLabels: Record<string, string> = {
  Organization: 'Organizacja',
  TimeTracking: 'Czas pracy',
  Leave: 'Urlopy',
  Tasks: 'Zadania',
  Documents: 'Dokumenty',
  Notifications: 'Powiadomienia',
  Dashboard: 'Dashboard',
  Workflow: 'Obieg dokumentów',
};

export function FeatureFlagsPage() {
  const { data: flags, isLoading, error, refetch, isFetching } = useFeatureFlags();
  const toggleMutation = useToggleFeatureFlag();

  const handleToggle = useCallback(
    (module: string) => {
      toggleMutation.mutate(module);
    },
    [toggleMutation],
  );

  return (
    <div style={{ padding: '24px 32px', maxWidth: '800px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: '#111827' }}>Flagi funkcjonalności</h1>
        <button
          onClick={() => refetch()}
          style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            padding: '7px 12px', fontSize: '13px', fontWeight: 500,
            color: '#374151', backgroundColor: '#fff',
            border: '1px solid #d1d5db', borderRadius: '6px', cursor: 'pointer',
          }}
          title="Odśwież"
        >
          <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
        </button>
      </div>

      {/* Error */}
      {error && (
        <div style={{
          padding: '12px 16px', marginBottom: '16px', borderRadius: '8px',
          backgroundColor: '#fef2f2', border: '1px solid #fecaca', color: '#991b1b', fontSize: '13px',
        }}>
          Błąd ładowania flag.
          <button onClick={() => refetch()} style={{ marginLeft: '8px', color: '#2563eb', background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: '13px' }}>Ponów</button>
        </div>
      )}

      {/* Loading */}
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: '#6b7280', fontSize: '14px' }}>
          Ładowanie...
        </div>
      ) : !flags || flags.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: '#9ca3af' }}>
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
                padding: '16px 20px', borderRadius: '8px',
                border: '1px solid #e5e7eb', backgroundColor: '#fff',
              }}
            >
              <div>
                <div style={{ fontSize: '14px', fontWeight: 600, color: '#111827' }}>
                  {moduleLabels[flag.module] ?? flag.module}
                </div>
                <div style={{ fontSize: '12px', color: '#6b7280', marginTop: '2px' }}>
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
                  padding: '6px 14px', fontSize: '13px', fontWeight: 500, borderRadius: '6px',
                  border: 'none', cursor: toggleMutation.isPending ? 'wait' : 'pointer',
                  backgroundColor: flag.isEnabled ? '#dcfce7' : '#f3f4f6',
                  color: flag.isEnabled ? '#166534' : '#6b7280',
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
