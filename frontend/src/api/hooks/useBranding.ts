import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { BrandingDto, UpdateBrandingRequest } from '@/api/types/branding';

export function useBranding() {
  return useQuery({
    queryKey: ['config', 'branding'],
    queryFn: () => api.get<BrandingDto>('/api/config/branding'),
  });
}

export function useUpdateBranding() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateBrandingRequest) => api.put<BrandingDto>('/api/config/branding', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['config', 'branding'] }),
  });
}
