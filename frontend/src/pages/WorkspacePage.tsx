import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useTimeStatus } from '@/api/hooks/useTimeTracking';
import { useTasks } from '@/api/hooks/useTasks';
import { usePendingApprovals } from '@/api/hooks/useWorkflow';
import { useLeaveRequests } from '@/api/hooks/useLeave';
import { ClockButton } from '@/components/TimeTracking';
import {
  MyDayOverview,
  MyTasksList,
  MyApprovalsWidget,
  MyLeaveWidget,
} from '@/components/Workspace';

export function WorkspacePage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const employeeId = user?.employeeId ?? null;

  const { data: timeStatus, isLoading: timeLoading } = useTimeStatus(employeeId ?? undefined);
  const { data: tasks = [], isLoading: tasksLoading } = useTasks(employeeId);
  const { data: approvals = [], isLoading: approvalsLoading } = usePendingApprovals(employeeId);
  const { data: leaveRequests = [], isLoading: leaveLoading } = useLeaveRequests(employeeId);

  const greeting = getGreeting();

  return (
    <div style={{ padding: '24px', maxWidth: '1000px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '24px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: '#111827' }}>
            {greeting}, {user?.name?.split(' ')[0] ?? 'Użytkowniku'}
          </h1>
          <p style={{ margin: '4px 0 0', fontSize: '14px', color: '#6b7280' }}>
            {new Date().toLocaleDateString('pl-PL', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
          </p>
        </div>
        <ClockButton />
      </div>

      {/* Grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
        {/* Left column */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <MyDayOverview data={timeStatus} isLoading={timeLoading} />
          <MyTasksList tasks={tasks} isLoading={tasksLoading} />
        </div>

        {/* Right column */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <MyApprovalsWidget approvals={approvals} isLoading={approvalsLoading} />
          <MyLeaveWidget requests={leaveRequests} isLoading={leaveLoading} />
        </div>
      </div>
    </div>
  );
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Dzień dobry';
  if (hour < 18) return 'Cześć';
  return 'Dobry wieczór';
}
