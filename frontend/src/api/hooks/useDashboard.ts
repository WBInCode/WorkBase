import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { DashboardSummaryDto } from '@/api/types/dashboard';

export function useDashboardSummary() {
  return useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => api.get<DashboardSummaryDto>('/api/dashboard/summary'),
    refetchInterval: 60_000,
  });
}
