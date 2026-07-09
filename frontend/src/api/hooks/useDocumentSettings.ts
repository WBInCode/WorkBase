import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { DocumentUploadSettingsDto } from '@/api/types/documentSettings';

export function useDocumentSettings() {
  return useQuery({
    queryKey: ['config', 'document-settings'],
    queryFn: () => api.get<DocumentUploadSettingsDto>('/api/documents/settings'),
  });
}

export function useUpdateDocumentSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: DocumentUploadSettingsDto) =>
      api.put<void>('/api/documents/settings', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['config', 'document-settings'] }),
  });
}
