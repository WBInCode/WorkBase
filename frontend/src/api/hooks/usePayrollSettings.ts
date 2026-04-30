import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';

export interface PayrollSettings {
  overtimeMultiplier: number;
  nightMultiplier: number;
  holidayMultiplier: number;
}

export function usePayrollSettings() {
  return useQuery({
    queryKey: ['payroll', 'settings'],
    queryFn: () => api.get<PayrollSettings>('/api/payroll/settings'),
    staleTime: 60_000,
  });
}

export function useUpdatePayrollSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: PayrollSettings) =>
      api.put<void>('/api/payroll/settings', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'settings'] }),
  });
}
