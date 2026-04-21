import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  LeaveTypeDto,
  LeaveBalanceDto,
  LeaveRequestDto,
  SubmitLeaveRequest,
  LeaveCalendarEntryDto,
  LeaveCalendarRequest,
  CreateLeaveTypeRequest,
  UpdateLeaveTypeRequest,
} from '@/api/types/leave';

export function useLeaveTypes() {
  return useQuery({
    queryKey: ['leave', 'types'],
    queryFn: () => api.get<LeaveTypeDto[]>('/api/leave/types'),
  });
}

export function useLeaveBalances(employeeId: string | null, year?: number) {
  const params = new URLSearchParams();
  if (year) params.set('year', String(year));
  const qs = params.toString();

  return useQuery({
    queryKey: ['leave', 'balances', employeeId, year],
    queryFn: () =>
      api.get<LeaveBalanceDto[]>(`/api/leave/balances/${employeeId}${qs ? `?${qs}` : ''}`),
    enabled: !!employeeId,
  });
}

export function useLeaveRequests(employeeId: string | null, year?: number) {
  const params = new URLSearchParams();
  if (year) params.set('year', String(year));
  const qs = params.toString();

  return useQuery({
    queryKey: ['leave', 'requests', employeeId, year],
    queryFn: () =>
      api.get<LeaveRequestDto[]>(`/api/leave/requests/${employeeId}${qs ? `?${qs}` : ''}`),
    enabled: !!employeeId,
  });
}

export function useSubmitLeaveRequest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SubmitLeaveRequest) =>
      api.post<string>('/api/leave/requests', data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['leave', 'requests'] });
      qc.invalidateQueries({ queryKey: ['leave', 'balances'] });
    },
  });
}

export function useLeaveCalendar(request: LeaveCalendarRequest | null) {
  return useQuery({
    queryKey: ['leave', 'calendar', request?.employeeIds, request?.from, request?.to],
    queryFn: () =>
      api.post<LeaveCalendarEntryDto[]>('/api/leave/calendar', request),
    enabled: !!request && request.employeeIds.length > 0,
  });
}

export function useCreateLeaveType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateLeaveTypeRequest) =>
      api.post<string>('/api/leave/types', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['leave', 'types'] }),
  });
}

export function useUpdateLeaveType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdateLeaveTypeRequest & { id: string }) =>
      api.put<void>(`/api/leave/types/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['leave', 'types'] }),
  });
}

export function useDeleteLeaveType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      api.delete<void>(`/api/leave/types/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['leave', 'types'] }),
  });
}
