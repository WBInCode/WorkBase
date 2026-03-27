import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { TimeStatusDto, ClockRequest, TimeSheetPeriodDto, TimeAnomalyDto } from '@/api/types/time';

export function useTimeStatus(employeeId: string | undefined) {
  return useQuery({
    queryKey: ['time', 'status', employeeId],
    queryFn: () => api.get<TimeStatusDto>(`/api/time/status/${employeeId}`),
    enabled: !!employeeId,
    refetchInterval: 30_000,
  });
}

export function useClockIn() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: ClockRequest) =>
      api.post<string>('/api/time/clock-in', data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['time', 'status', variables.employeeId] });
    },
  });
}

export function useClockOut() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: ClockRequest) =>
      api.post<string>('/api/time/clock-out', data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['time', 'status', variables.employeeId] });
    },
  });
}

export function useStartBreak() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: ClockRequest) =>
      api.post<string>('/api/time/break/start', data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['time', 'status', variables.employeeId] });
    },
  });
}

export function useEndBreak() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: ClockRequest) =>
      api.post<string>('/api/time/break/end', data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['time', 'status', variables.employeeId] });
    },
  });
}

export interface TimesheetFilter {
  employeeId: string;
  from: string;
  to: string;
  period: 'day' | 'week' | 'month';
}

export function useTimesheet(filter: TimesheetFilter) {
  const params = new URLSearchParams({
    from: filter.from,
    to: filter.to,
    period: filter.period,
  });

  return useQuery({
    queryKey: ['time', 'timesheet', filter],
    queryFn: () =>
      api.get<TimeSheetPeriodDto>(`/api/time/timesheet/${filter.employeeId}?${params}`),
    enabled: !!filter.employeeId,
  });
}

export interface AnomaliesFilter {
  from: string;
  to: string;
  status?: string;
}

export function useAnomalies(filter: AnomaliesFilter) {
  const params = new URLSearchParams({ from: filter.from, to: filter.to });
  if (filter.status) params.set('status', filter.status);

  return useQuery({
    queryKey: ['time', 'anomalies', filter],
    queryFn: () => api.get<TimeAnomalyDto[]>(`/api/time/anomalies?${params}`),
  });
}

export function useTeamTimesheets(
  employeeIds: string[],
  from: string,
  to: string,
  period: 'day' | 'week' | 'month',
) {
  return useQuery({
    queryKey: ['time', 'team-timesheets', employeeIds, from, to, period],
    queryFn: () =>
      Promise.all(
        employeeIds.map((id) => {
          const params = new URLSearchParams({ from, to, period });
          return api.get<TimeSheetPeriodDto>(`/api/time/timesheet/${id}?${params}`);
        }),
      ),
    enabled: employeeIds.length > 0,
  });
}
