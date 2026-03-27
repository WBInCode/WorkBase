import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { TimeStatusDto, ClockRequest } from '@/api/types/time';

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
