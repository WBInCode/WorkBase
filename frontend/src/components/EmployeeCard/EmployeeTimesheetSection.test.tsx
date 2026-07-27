import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import type { TimeSheetPeriodDto } from '@/api/types/time';
import { EmployeeTimesheetSection } from './EmployeeTimesheetSection';

vi.mock('@/api/hooks/useTimeTracking', () => {
  const mutation = () => ({ mutate: vi.fn(), isPending: false, error: null });

  return {
    useAdminCreateTimeEntry: mutation,
    useAdminUpdateTimeEntry: mutation,
    useAdminDeleteTimeEntry: mutation,
  };
});

const timesheet: TimeSheetPeriodDto = {
  from: '2026-07-27',
  to: '2026-08-02',
  period: 'custom',
  employeeId: 'employee-1',
  totalWorked: '00:00:00',
  totalBreaks: '00:00:00',
  netWorked: '00:00:00',
  daysWorked: 0,
  daysIncomplete: 0,
  days: [{
    date: '2026-07-27',
    totalWorked: '00:00:00',
    totalBreaks: '00:00:00',
    netWorked: '00:00:00',
    status: 'empty',
    note: null,
    entries: [],
  }],
};

describe('EmployeeTimesheetSection entry modal', () => {
  afterEach(cleanup);

  it('renders one dialog in document.body outside the animated page container', () => {
    const { container } = render(
      <EmployeeTimesheetSection
        timeStatus={undefined}
        timesheet={timesheet}
        isLoading={false}
        employeeId="employee-1"
        from="2026-07-27"
        to="2026-08-02"
        onDateRangeChange={vi.fn()}
      />,
    );

    const addEntryButton = screen.getByTitle('Dodaj wpis');
    fireEvent.click(addEntryButton);
    fireEvent.click(addEntryButton);

    const dialogs = screen.getAllByRole('dialog', { name: 'Dodaj wpis' });
    expect(dialogs).toHaveLength(1);
    const dialog = dialogs[0]!;
    expect(dialog.parentElement).toBe(document.body);
    expect(container).not.toContainElement(dialog);
  });
});