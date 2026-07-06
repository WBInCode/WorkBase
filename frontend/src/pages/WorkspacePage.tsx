import { useAuth } from 'react-oidc-context';
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
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const employeeId = user?.employeeId ?? null;

  const { data: timeStatus, isLoading: timeLoading } = useTimeStatus(employeeId ?? undefined);
  const { data: employeeDetail } = useEmployeeDetail(employeeId);
  const { data: tasks = [], isLoading: tasksLoading } = useTasks(employeeId);
  const { data: approvals = [], isLoading: approvalsLoading } = usePendingApprovals(employeeId);
  const { data: leaveRequests = [], isLoading: leaveLoading } = useLeaveRequests(employeeId);

  const greeting = getGreeting();
  const mobile = useIsMobile();

  return (
    <div style={{ padding: mobile ? '16px' : '24px', maxWidth: '1000px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '24px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: colors.gray[900] }}>
            {greeting}, {user?.name?.split(' ')[0] ?? 'Użytkowniku'}
          </h1>
          <p style={{ margin: '4px 0 0', fontSize: '14px', color: colors.gray[500] }}>
            {new Date().toLocaleDateString('pl-PL', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
          </p>
        </div>
        {employeeId && <ClockButton employeeId={employeeId} />}
      </div>

      {/* Grid */}
      <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '20px' }}>
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

      {/* Activity feed */}
      <div style={{ marginTop: '20px' }}>
        <ActivityFeed employeeId={employeeId} tasks={tasks} leaveRequests={leaveRequests} />
      </div>

      {/* Personal QR badge */}
      {employeeId && (
        <div style={{ marginTop: '20px' }}>
          <MyQrBadge
            employeeId={employeeId}
            employeeName={user?.name ?? ''}
            employeeNumber={employeeDetail?.employeeNumber ?? undefined}
          />
        </div>
      )}
    </div>
  );
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Dzień dobry';
  if (hour < 18) return 'Cześć';
  return 'Dobry wieczór';
}
