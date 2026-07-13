import { RefreshCw, Users, ListTodo, Palmtree, ShieldAlert, type LucideIcon } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
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
  const navigate = useNavigate();

  const lastUpdate = dataUpdatedAt
    ? new Date(dataUpdatedAt).toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' })
    : null;

  const attendanceRate = data && data.attendance.totalScheduled > 0
    ? Math.round((data.attendance.presentToday / data.attendance.totalScheduled) * 100)
    : null;

  return (
    <div style={{ padding: mobile ? '14px' : '24px 28px', maxWidth: '1240px', margin: '0 auto' }}>
      {/* ── Nagłówek ── */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '18px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>
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
            padding: '8px 16px', fontSize: '13px', fontWeight: 600, fontFamily: 'inherit',
            color: colors.gray[700], backgroundColor: colors.white,
            border: `1px solid ${colors.gray[300]}`, borderRadius: '999px',
            cursor: isFetching ? 'default' : 'pointer',
            opacity: isFetching ? 0.6 : 1,
            boxShadow: '0 1px 2px rgba(20,25,43,0.05)',
          }}
        >
          <RefreshCw size={14} style={{ animation: isFetching ? 'spin 1s linear infinite' : 'none' }} />
          Odśwież
        </button>
      </div>

      {/* ── Pas KPI — najważniejsze liczby dnia ── */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: mobile ? 'repeat(2, 1fr)' : 'repeat(auto-fit, minmax(210px, 1fr))',
        gap: '12px',
        marginBottom: '18px',
      }}>
        <KpiCard
          icon={Users}
          label="Obecność dziś"
          value={isLoading ? '…' : attendanceRate === null ? '—' : `${attendanceRate}%`}
          sub={data ? `${data.attendance.presentToday}/${data.attendance.totalScheduled} obecnych` : undefined}
          tone={attendanceRate === null ? 'neutral' : attendanceRate >= 90 ? 'success' : attendanceRate >= 70 ? 'warning' : 'danger'}
          onClick={() => navigate('/time/team-report')}
        />
        <KpiCard
          icon={ListTodo}
          label="Otwarte zadania"
          value={isLoading || !data ? '…' : String(data.tasks.openTasks)}
          sub={data && data.tasks.overdueTasks > 0 ? `${data.tasks.overdueTasks} po terminie` : 'wszystko w terminie'}
          tone={data && data.tasks.overdueTasks > 0 ? 'danger' : 'iris'}
          onClick={() => navigate('/tasks')}
        />
        <KpiCard
          icon={Palmtree}
          label="Wnioski urlopowe"
          value={isLoading || !data ? '…' : String(data.leave.pendingRequests)}
          sub={data ? `${data.leave.onLeaveToday} os. na urlopie dziś` : undefined}
          tone={data && data.leave.pendingRequests > 0 ? 'warning' : 'neutral'}
          onClick={() => navigate('/leave/approvals')}
        />
        <KpiCard
          icon={ShieldAlert}
          label="Nowe anomalie"
          value={isLoading || !data ? '…' : String(data.anomalies.newAnomalies)}
          sub={data ? `${data.anomalies.reviewedThisWeek} sprawdzonych w tym tyg.` : undefined}
          tone={data && data.anomalies.newAnomalies > 0 ? 'danger' : 'success'}
          onClick={() => navigate('/time/team-report')}
        />
      </div>

      {/* ── Strefy szczegółów: operacyjna (8) / wymagające uwagi (4) ── */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: mobile ? '1fr' : 'minmax(0, 8fr) minmax(280px, 4fr)',
        gap: '18px',
        alignItems: 'start',
      }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '18px', minWidth: 0 }}>
          <AttendanceWidget data={data?.attendance} isLoading={isLoading} />
          <TaskSummaryWidget data={data?.tasks} isLoading={isLoading} />
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '18px', minWidth: 0 }}>
          <PendingApprovalsWidget data={data?.leave} isLoading={isLoading} />
          <AlertsWidget data={data?.anomalies} isLoading={isLoading} />
        </div>
      </div>
    </div>
  );
}

/* ── KPI card ──────────────────────────────────────────── */

const KPI_TONES = {
  iris: { fg: colors.primary[600], bg: colors.primary[100] },
  success: { fg: colors.success[700], bg: colors.success[100] },
  warning: { fg: colors.warning[700], bg: colors.warning[100] },
  danger: { fg: colors.danger[600], bg: colors.danger[100] },
  neutral: { fg: colors.gray[500], bg: colors.gray[100] },
} as const;

function KpiCard({ icon: Icon, label, value, sub, tone, onClick }: {
  icon: LucideIcon;
  label: string;
  value: string;
  sub?: string;
  tone: keyof typeof KPI_TONES;
  onClick: () => void;
}) {
  const t = KPI_TONES[tone];
  return (
    <button
      onClick={onClick}
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'flex-start',
        gap: '10px',
        padding: '16px 18px',
        minWidth: 0,
        backgroundColor: colors.white,
        border: `1px solid ${colors.gray[200]}`,
        borderRadius: '18px',
        cursor: 'pointer',
        fontFamily: 'inherit',
        textAlign: 'left',
        boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 26px -14px rgba(20,25,43,0.12)',
        transition: 'transform 0.15s ease, box-shadow 0.15s ease',
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'translateY(-2px)';
        e.currentTarget.style.boxShadow = '0 2px 4px rgba(20,25,43,0.05), 0 16px 34px -14px rgba(20,25,43,0.18)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'translateY(0)';
        e.currentTarget.style.boxShadow = '0 1px 2px rgba(20,25,43,0.04), 0 10px 26px -14px rgba(20,25,43,0.12)';
      }}
    >
      <span style={{ display: 'flex', alignItems: 'center', gap: 8, width: '100%' }}>
        <span style={{
          width: 32, height: 32, borderRadius: 10, backgroundColor: t.bg, color: t.fg,
          display: 'inline-flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
        }}>
          <Icon size={16} />
        </span>
        <span style={{ fontSize: 12, fontWeight: 700, color: colors.gray[500], textTransform: 'uppercase', letterSpacing: '0.05em' }}>
          {label}
        </span>
      </span>
      <span className="wb-tnum" style={{ fontSize: 'clamp(26px, 2.6vw, 32px)', fontWeight: 800, color: colors.gray[900], letterSpacing: '-0.02em', lineHeight: 1.1 }}>
        {value}
      </span>
      {sub && (
        <span style={{ fontSize: 12, fontWeight: 500, color: colors.gray[400] }}>
          {sub}
        </span>
      )}
    </button>
  );
}
