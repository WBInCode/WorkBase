import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { ListTodo, ClipboardCheck, Palmtree, AlertTriangle, ChevronRight, type LucideIcon } from 'lucide-react';
import { mapUserClaims } from '@/auth';
import { useTimeStatus } from '@/api/hooks/useTimeTracking';
import { useTasks } from '@/api/hooks/useTasks';
import { usePendingApprovals } from '@/api/hooks/useWorkflow';
import { useLeaveRequests } from '@/api/hooks/useLeave';
import { useEmployeeDetail } from '@/api/hooks/useOrganization';
import { ClockButton, MyQrBadge } from '@/components/TimeTracking';
import {
  MyDayOverview,
  MyTasksList,
  MyApprovalsWidget,
  MyLeaveWidget,
  ActivityFeed,
} from '@/components/Workspace';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function WorkspacePage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const employeeId = user?.employeeId ?? null;

  const { data: timeStatus, isLoading: timeLoading } = useTimeStatus(employeeId ?? undefined);
  const { data: employeeDetail } = useEmployeeDetail(employeeId);
  const { data: tasks = [], isLoading: tasksLoading } = useTasks(employeeId);
  const { data: approvals = [], isLoading: approvalsLoading } = usePendingApprovals(employeeId);
  const { data: leaveRequests = [], isLoading: leaveLoading } = useLeaveRequests(employeeId);

  const greeting = getGreeting();
  const mobile = useIsMobile();

  // Pas szybkich liczb — mostek między hero a strefami szczegółów
  const stats = useMemo(() => {
    const now = new Date();
    const openTasks = tasks.filter((t) => !t.completedAt);
    const overdue = openTasks.filter((t) => t.dueDate && new Date(t.dueDate) < now).length;
    const pendingApprovals = approvals.filter((a) => a.status === 'Pending').length;
    const activeLeaves = leaveRequests.filter((r) => r.status === 'Pending' || r.status === 'Approved').length;
    return { openTasks: openTasks.length, overdue, pendingApprovals, activeLeaves };
  }, [tasks, approvals, leaveRequests]);

  return (
    <div style={{ padding: mobile ? '14px' : '24px 28px', maxWidth: '1240px', margin: '0 auto' }}>
      {/* ── Hero: powitanie + czas pracy + akcje ── */}
      <MyDayOverview
        data={timeStatus}
        isLoading={timeLoading}
        greeting={greeting}
        name={user?.name?.split(' ')[0] ?? 'Użytkowniku'}
        actions={employeeId ? <ClockButton employeeId={employeeId} /> : undefined}
      />

      {/* ── Pas szybkich statystyk ── */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: mobile ? 'repeat(2, 1fr)' : 'repeat(auto-fit, minmax(190px, 1fr))',
          gap: '12px',
          marginTop: '16px',
        }}
      >
        <StatChip
          icon={ListTodo}
          label="Otwarte zadania"
          value={tasksLoading ? '…' : String(stats.openTasks)}
          tone="iris"
          onClick={() => navigate('/tasks/my')}
        />
        <StatChip
          icon={AlertTriangle}
          label="Po terminie"
          value={tasksLoading ? '…' : String(stats.overdue)}
          tone={stats.overdue > 0 ? 'danger' : 'neutral'}
          onClick={() => navigate('/tasks/my')}
        />
        <StatChip
          icon={ClipboardCheck}
          label="Do akceptacji"
          value={approvalsLoading ? '…' : String(stats.pendingApprovals)}
          tone={stats.pendingApprovals > 0 ? 'warning' : 'neutral'}
          onClick={() => navigate('/leave/approvals')}
        />
        <StatChip
          icon={Palmtree}
          label="Aktywne wnioski"
          value={leaveLoading ? '…' : String(stats.activeLeaves)}
          tone="emerald"
          onClick={() => navigate('/leave/request')}
        />
      </div>

      {/* ── Strefy: główna kolumna (zadania + aktywność) / boczna (akceptacje, urlopy, QR) ── */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: mobile ? '1fr' : 'minmax(0, 8fr) minmax(280px, 4fr)',
          gap: '18px',
          marginTop: '18px',
          alignItems: 'start',
        }}
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: '18px', minWidth: 0 }}>
          <MyTasksList tasks={tasks} isLoading={tasksLoading} />
          <ActivityFeed employeeId={employeeId} tasks={tasks} leaveRequests={leaveRequests} />
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '18px', minWidth: 0 }}>
          <MyApprovalsWidget approvals={approvals} isLoading={approvalsLoading} />
          <MyLeaveWidget requests={leaveRequests} isLoading={leaveLoading} />
          {employeeId && (
            <MyQrBadge
              employeeId={employeeId}
              employeeName={user?.name ?? ''}
              employeeNumber={employeeDetail?.employeeNumber ?? undefined}
            />
          )}
        </div>
      </div>
    </div>
  );
}

/* ── Stat chip ─────────────────────────────────────────── */

const TONES = {
  iris: { fg: colors.primary[600], bg: colors.primary[100] },
  warning: { fg: colors.warning[700], bg: colors.warning[100] },
  danger: { fg: colors.danger[600], bg: colors.danger[100] },
  emerald: { fg: '#047857', bg: '#d1fae5' },
  neutral: { fg: colors.gray[500], bg: colors.gray[100] },
} as const;

function StatChip({ icon: Icon, label, value, tone, onClick }: {
  icon: LucideIcon;
  label: string;
  value: string;
  tone: keyof typeof TONES;
  onClick: () => void;
}) {
  const t = TONES[tone];
  return (
    <button
      onClick={onClick}
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: '12px',
        padding: '13px 16px',
        minWidth: 0,
        backgroundColor: colors.white,
        border: `1px solid ${colors.gray[200]}`,
        borderRadius: '16px',
        cursor: 'pointer',
        fontFamily: 'inherit',
        textAlign: 'left',
        boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 8px 22px -12px rgba(20,25,43,0.10)',
        transition: 'transform 0.15s ease, box-shadow 0.15s ease',
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'translateY(-2px)';
        e.currentTarget.style.boxShadow = '0 2px 4px rgba(20,25,43,0.05), 0 14px 30px -12px rgba(20,25,43,0.16)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'translateY(0)';
        e.currentTarget.style.boxShadow = '0 1px 2px rgba(20,25,43,0.04), 0 8px 22px -12px rgba(20,25,43,0.10)';
      }}
    >
      <span style={{
        width: 38, height: 38, borderRadius: 12, backgroundColor: t.bg, color: t.fg,
        display: 'inline-flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
      }}>
        <Icon size={18} />
      </span>
      <span style={{ minWidth: 0, flex: 1 }}>
        <span className="wb-tnum" style={{ display: 'block', fontSize: 20, fontWeight: 800, color: colors.gray[900], letterSpacing: '-0.02em', lineHeight: 1.2 }}>
          {value}
        </span>
        <span style={{ display: 'block', fontSize: 11.5, fontWeight: 600, color: colors.gray[500], lineHeight: 1.3 }}>
          {label}
        </span>
      </span>
      <ChevronRight size={15} color={colors.gray[300]} style={{ flexShrink: 0 }} />
    </button>
  );
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Dzień dobry';
  if (hour < 18) return 'Cześć';
  return 'Dobry wieczór';
}
