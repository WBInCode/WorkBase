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

export function EmployeeCardPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: employee, isLoading: empLoading, error } = useEmployeeDetail(id ?? null);

  // Time tracking — current week
  const now = new Date();
  const monday = new Date(now);
  monday.setDate(now.getDate() - ((now.getDay() + 6) % 7));
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  const from = monday.toISOString().slice(0, 10);
  const to = sunday.toISOString().slice(0, 10);

  const { data: timeStatus, isLoading: timeStatusLoading } = useTimeStatus(id);
  const { data: timesheet, isLoading: timesheetLoading } = useTimesheet({ employeeId: id!, from, to, period: 'week' });

  // Leave — current year
  const year = now.getFullYear();
  const { data: balances = [], isLoading: balancesLoading } = useLeaveBalances(id ?? null, year);
  const { data: leaveRequests = [], isLoading: leaveLoading } = useLeaveRequests(id ?? null, year);

  // Tasks
  const { data: tasks = [], isLoading: tasksLoading } = useTasks(id);

  if (empLoading) {
    return (
      <div style={{ padding: '24px 32px', maxWidth: '1000px' }}>
        <div style={{ color: '#9ca3af', fontSize: '14px', textAlign: 'center', padding: '48px 0' }}>
          Ładowanie danych pracownika...
        </div>
      </div>
    );
  }

  if (error || !employee) {
    return (
      <div style={{ padding: '24px 32px', maxWidth: '1000px' }}>
        <div style={{ padding: '16px', backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: '8px', color: '#dc2626', fontSize: '14px' }}>
          Nie udało się załadować danych pracownika.
          <button
            onClick={() => navigate('/org/employees')}
            style={{ marginLeft: '12px', color: '#2563eb', background: 'none', border: 'none', cursor: 'pointer', fontSize: '14px', textDecoration: 'underline' }}
          >
            Wróć do listy
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={{ padding: '24px 32px', maxWidth: '1000px' }}>
      {/* Back button */}
      <button
        onClick={() => navigate('/org/employees')}
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: '6px',
          padding: '6px 12px',
          fontSize: '13px',
          fontWeight: 500,
          color: '#6b7280',
          backgroundColor: 'transparent',
          border: '1px solid #e5e7eb',
          borderRadius: '6px',
          cursor: 'pointer',
          marginBottom: '16px',
        }}
      >
        <ArrowLeft size={14} />
        Pracownicy
      </button>

      {/* Sections stacked vertically */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
        <EmployeeInfoSection employee={employee} />

        <EmployeeTeamSection
          employeeId={employee.id}
          primaryUnitId={employee.assignments.find(a => a.isPrimary)?.organizationUnitId ?? employee.assignments[0]?.organizationUnitId ?? null}
          primaryUnitName={employee.assignments.find(a => a.isPrimary)?.organizationUnitName ?? employee.assignments[0]?.organizationUnitName ?? null}
        />

        <EmployeeTimesheetSection
          timeStatus={timeStatus}
          timesheet={timesheet}
          isLoading={timeStatusLoading || timesheetLoading}
        />

        <EmployeeLeaveSection
          balances={balances}
          requests={leaveRequests}
          isLoading={balancesLoading || leaveLoading}
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
    </div>
  );
}
