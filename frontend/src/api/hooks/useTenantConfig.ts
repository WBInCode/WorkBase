import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { TerminologyOverrides } from '@/api/types/branding';
import type { AnomalyDetectionSettingsDto } from '@/api/types/timeTrackingSettings';

export function useTerminology() {
  return useQuery({
    queryKey: ['config', 'terminology'],
    queryFn: () => api.get<TerminologyOverrides>('/api/config/terminology'),
    staleTime: 5 * 60_000,
  });
}

export function useUpdateTerminology() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (overrides: TerminologyOverrides) =>
      api.put<void>('/api/config/terminology', { overrides }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['config', 'terminology'] }),
  });
}

export function useTimeTrackingSettings() {
  return useQuery({
    queryKey: ['config', 'time-tracking-settings'],
    queryFn: () => api.get<AnomalyDetectionSettingsDto>('/api/time-tracking/settings'),
  });
}

export function useUpdateTimeTrackingSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: AnomalyDetectionSettingsDto) =>
      api.put<void>('/api/time-tracking/settings', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['config', 'time-tracking-settings'] }),
  });
}
