import { useState, useEffect } from 'react';
import { ListChecks, RefreshCw } from 'lucide-react';
import { useTaskSettings, useUpdateTaskSettings } from '@/api/hooks/useTaskSettings';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function TaskSettingsConfigPage() {
  const { data, isLoading, error, refetch, isFetching } = useTaskSettings();
  const updateMutation = useUpdateTaskSettings();
  const mobile = useIsMobile();

  const [gracePeriodHours, setGracePeriodHours] = useState(0);
  const [notifyOnOverdue, setNotifyOnOverdue] = useState(true);

  useEffect(() => {
    if (!data) return;
    setGracePeriodHours(data.gracePeriodHours);
    setNotifyOnOverdue(data.notifyOnOverdue);
  }, [data]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate({ gracePeriodHours, notifyOnOverdue });
  };

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '600px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: colors.gray[900], display: 'flex', alignItems: 'center', gap: '8px' }}>
          <ListChecks size={22} /> Ustawienia zadań
        </h1>
        <button
          onClick={() => refetch()}
          style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            padding: '7px 12px', fontSize: '13px', fontWeight: 500,
            color: colors.gray[700], backgroundColor: colors.white,
            border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', cursor: 'pointer',
          }}
          title="Odśwież"
        >
          <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
        </button>
      </div>

      <p style={{ fontSize: '13px', color: colors.gray[500], marginTop: '-12px', marginBottom: '20px' }}>
        Kiedy i czy zadania po terminie mają być oznaczane jako zaległe (codzienne sprawdzenie o 6:00 UTC).
      </p>

      {error && (
        <div style={{ padding: '12px 16px', marginBottom: '16px', borderRadius: '8px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px' }}>
          Błąd ładowania ustawień.
        </div>
      )}
      {updateMutation.isError && (
        <div style={{ padding: '12px 16px', marginBottom: '16px', borderRadius: '8px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px' }}>
          {updateMutation.error instanceof Error ? updateMutation.error.message : 'Błąd zapisu ustawień.'}
        </div>
      )}
      {updateMutation.isSuccess && (
        <div style={{ padding: '10px 16px', marginBottom: '16px', borderRadius: '8px', backgroundColor: colors.success[100], border: `1px solid ${colors.success[200]}`, color: colors.success[800], fontSize: '13px' }}>
          Zapisano.
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
      ) : (
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', color: colors.gray[700], cursor: 'pointer' }}>
            <input type="checkbox" checked={notifyOnOverdue} onChange={(e) => setNotifyOnOverdue(e.target.checked)} />
            Wykrywaj i oznaczaj zadania po terminie
          </label>

          <label style={labelStyle}>
            Okres karencji po terminie (godziny)
            <input
              type="number" min={0} max={720}
              value={gracePeriodHours}
              onChange={(e) => setGracePeriodHours(Number(e.target.value))}
              disabled={!notifyOnOverdue}
              style={inputStyle}
            />
          </label>
          <p style={{ fontSize: '12px', color: colors.gray[500], marginTop: '-8px' }}>
            Zadanie zostanie oznaczone jako zaległe dopiero po upływie terminu + tego okresu (np. 24h = doba tolerancji).
          </p>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '8px' }}>
            <button
              type="submit"
              disabled={updateMutation.isPending}
              style={{
                padding: '9px 22px', fontSize: '14px', fontWeight: 500,
                color: colors.white, backgroundColor: colors.primary[600], border: 'none',
                borderRadius: '6px', cursor: updateMutation.isPending ? 'not-allowed' : 'pointer',
                opacity: updateMutation.isPending ? 0.6 : 1,
              }}
            >
              {updateMutation.isPending ? 'Zapisywanie...' : 'Zapisz'}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

const labelStyle: React.CSSProperties = {
  display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: colors.gray[700],
};
const inputStyle: React.CSSProperties = {
  width: '100%', padding: '8px 12px', fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', boxSizing: 'border-box',
};
