import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { useEmployeeDetail } from '@/api/hooks/useOrganization';
import { useTimeStatus, useTimesheet } from '@/api/hooks/useTimeTracking';
import { useLeaveBalances, useLeaveRequests } from '@/api/hooks/useLeave';
import { useTasks } from '@/api/hooks/useTasks';
import {
  EmployeeInfoSection,
  EmployeeTeamSection,
  EmployeeTimesheetSection,
  EmployeeLeaveSection,
  EmployeeTasksSection,
  EmployeeActivityTimeline,
} from '@/components/EmployeeCard';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

function getCurrentWeekRange() {
  const now = new Date();
  const monday = new Date(now);
  monday.setDate(now.getDate() - ((now.getDay() + 6) % 7));
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  return {
    from: monday.toISOString().slice(0, 10),
    to: sunday.toISOString().slice(0, 10),
  };
}

export function EmployeeCardPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: employee, isLoading: empLoading, error } = useEmployeeDetail(id ?? null);

  // Time tracking — date range (defaults to current week)
  const [dateRange, setDateRange] = useState(getCurrentWeekRange);

  const { data: timeStatus, isLoading: timeStatusLoading } = useTimeStatus(id);
  const { data: timesheet, isLoading: timesheetLoading } = useTimesheet({ employeeId: id!, from: dateRange.from, to: dateRange.to, period: 'custom' });

  // Leave — current year
  const year = new Date().getFullYear();
  const { data: balances = [], isLoading: balancesLoading } = useLeaveBalances(id ?? null, year);
  const { data: leaveRequests = [], isLoading: leaveLoading } = useLeaveRequests(id ?? null, year);

  // Tasks
  const { data: tasks = [], isLoading: tasksLoading } = useTasks(id);
  const mobile = useIsMobile();
  const pad = mobile ? '16px' : '24px 32px';

  if (empLoading) {
    return (
      <div style={{ padding: pad, maxWidth: '1000px' }}>
        <div style={{ color: colors.gray[400], fontSize: '14px', textAlign: 'center', padding: '48px 0' }}>
          Ładowanie danych pracownika...
        </div>
      </div>
    );
  }

  if (error || !employee) {
    return (
      <div style={{ padding: pad, maxWidth: '1000px' }}>
        <div style={{ padding: '16px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, borderRadius: '12px', color: colors.danger[600], fontSize: '14px' }}>
          Nie udało się załadować danych pracownika.
          <button
            onClick={() => navigate('/org/employees')}
            style={{ marginLeft: '12px', color: colors.primary[600], background: 'none', border: 'none', cursor: 'pointer', fontSize: '14px', textDecoration: 'underline' }}
          >
            Wróć do listy
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={{ padding: pad, maxWidth: '1240px', margin: '0 auto' }}>
      {/* Back button */}
      <button
        onClick={() => navigate('/org/employees')}
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: '6px',
          padding: '7px 14px',
          fontSize: '13px',
          fontWeight: 600,
          fontFamily: 'inherit',
          color: colors.gray[600],
          backgroundColor: colors.white,
          border: `1px solid ${colors.gray[200]}`,
          borderRadius: '999px',
          cursor: 'pointer',
          marginBottom: '16px',
          boxShadow: '0 1px 2px rgba(20,25,43,0.05)',
        }}
      >
        <ArrowLeft size={14} />
        Pracownicy
      </button>

      {/* Hero: dane pracownika na pełnej szerokości */}
      <EmployeeInfoSection employee={employee} />

      {/* Strefy: operacyjna (8) / kontekstowa (4) */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: mobile ? '1fr' : 'minmax(0, 8fr) minmax(300px, 4fr)',
          gap: '18px',
          marginTop: '18px',
          alignItems: 'start',
        }}
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: '18px', minWidth: 0 }}>
          <EmployeeTimesheetSection
            timeStatus={timeStatus}
            timesheet={timesheet}
            isLoading={timeStatusLoading || timesheetLoading}
            employeeId={employee.id}
            from={dateRange.from}
            to={dateRange.to}
            onDateRangeChange={(from, to) => setDateRange({ from, to })}
          />

          <EmployeeTasksSection
            tasks={tasks}
            isLoading={tasksLoading}
          />

          <EmployeeActivityTimeline
            tasks={tasks}
            leaveRequests={leaveRequests}
            isLoading={tasksLoading || leaveLoading}
          />
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '18px', minWidth: 0 }}>
          <EmployeeTeamSection
            employeeId={employee.id}
            primaryUnitId={employee.assignments.find(a => a.isPrimary)?.organizationUnitId ?? employee.assignments[0]?.organizationUnitId ?? null}
            primaryUnitName={employee.assignments.find(a => a.isPrimary)?.organizationUnitName ?? employee.assignments[0]?.organizationUnitName ?? null}
          />

          <EmployeeLeaveSection
            balances={balances}
            requests={leaveRequests}
            isLoading={balancesLoading || leaveLoading}
          />
        </div>
      </div>
    </div>
  );
}
