import { RefreshCw } from 'lucide-react';
import { useDashboardSummary } from '@/api/hooks/useDashboard';
import {
  AttendanceWidget,
  TaskSummaryWidget,
  PendingApprovalsWidget,
  AlertsWidget,
} from '@/components/Dashboard';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function DashboardPage() {
  const { data, isLoading, dataUpdatedAt, refetch, isFetching } = useDashboardSummary();
  const mobile = useIsMobile();

  const lastUpdate = dataUpdatedAt
    ? new Date(dataUpdatedAt).toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' })
    : null;

  return (
    <div style={{ padding: mobile ? '16px' : '24px', maxWidth: '1100px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '24px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: colors.gray[900] }}>
            Dashboard
          </h1>
          <p style={{ margin: '4px 0 0', fontSize: '14px', color: colors.gray[500] }}>
            Podsumowanie operacyjne
            {lastUpdate && (
              <span style={{ marginLeft: '8px', fontSize: '12px', color: colors.gray[400] }}>
                · odświeżono {lastUpdate}
              </span>
            )}
          </p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isFetching}
          style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            padding: '8px 14px', fontSize: '13px', fontWeight: 500,
            color: colors.gray[700], backgroundColor: colors.white,
            border: `1px solid ${colors.gray[300]}`, borderRadius: '6px',
            cursor: isFetching ? 'default' : 'pointer',
            opacity: isFetching ? 0.6 : 1,
          }}
        >
          <RefreshCw size={14} style={{ animation: isFetching ? 'spin 1s linear infinite' : 'none' }} />
          Odśwież
        </button>
      </div>

      {/* Widget grid */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
        gap: '20px',
      }}>
        <AttendanceWidget data={data?.attendance} isLoading={isLoading} />
        <TaskSummaryWidget data={data?.tasks} isLoading={isLoading} />
        <PendingApprovalsWidget data={data?.leave} isLoading={isLoading} />
        <AlertsWidget data={data?.anomalies} isLoading={isLoading} />
      </div>

      {/* CSS animation for spinner */}
      <style>{`
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  );
}
