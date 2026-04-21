import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  FormDefinitionDto,
  FormSubmissionDto,
  CreateFormDefinitionRequest,
} from '@/api/types/forms';

export function useFormDefinitions() {
  return useQuery({
    queryKey: ['forms', 'definitions'],
    queryFn: () => api.get<FormDefinitionDto[]>('/api/forms/definitions'),
  });
}

export function useFormDefinition(id: string | null) {
  return useQuery({
    queryKey: ['forms', 'definitions', id],
    queryFn: () => api.get<FormDefinitionDto>(`/api/forms/definitions/${id}`),
    enabled: !!id,
  });
}

export function useCreateFormDefinition() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateFormDefinitionRequest) =>
      api.post<string>('/api/forms/definitions', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['forms', 'definitions'] }),
  });
}

export function useUpdateFormDefinition() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...body }: CreateFormDefinitionRequest & { id: string }) =>
      api.put<void>(`/api/forms/definitions/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['forms', 'definitions'] }),
  });
}

export function useDeleteFormDefinition() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/forms/definitions/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['forms', 'definitions'] }),
  });
}

export function useFormSubmissions(formDefinitionId: string | null) {
  return useQuery({
    queryKey: ['forms', 'submissions', formDefinitionId],
    queryFn: () => api.get<FormSubmissionDto[]>(`/api/forms/submissions/${formDefinitionId}`),
    enabled: !!formDefinitionId,
  });
}

export function useSubmitForm() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { formDefinitionId: string; valuesJson: string }) =>
      api.post<string>('/api/forms/submit', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['forms'] }),
  });
}
