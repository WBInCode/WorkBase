import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  DocumentDto,
  DocumentCategoryDto,
  CreateDocumentCategoryRequest,
  UpdateDocumentCategoryRequest,
} from '@/api/types/documents';

export function useDocuments(params?: {
  categoryId?: string;
  entityType?: string;
  entityId?: string;
  includeDeleted?: boolean;
}) {
  const qs = new URLSearchParams();
  if (params?.categoryId) qs.set('categoryId', params.categoryId);
  if (params?.entityType) qs.set('entityType', params.entityType);
  if (params?.entityId) qs.set('entityId', params.entityId);
  if (params?.includeDeleted) qs.set('includeDeleted', 'true');
  const query = qs.toString();

  return useQuery({
    queryKey: ['documents', 'list', params ?? 'all'],
    queryFn: () => api.get<DocumentDto[]>(`/api/documents${query ? `?${query}` : ''}`),
  });
}

export function useDocumentCategories() {
  return useQuery({
    queryKey: ['documents', 'categories'],
    queryFn: () => api.get<DocumentCategoryDto[]>('/api/documents/categories'),
  });
}

export function useUploadDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      file: File;
      categoryId?: string;
      entityType?: string;
      entityId?: string;
      description?: string;
    }) => {
      const fd = new FormData();
      fd.append('file', data.file);
      if (data.categoryId) fd.append('categoryId', data.categoryId);
      if (data.entityType) fd.append('entityType', data.entityType);
      if (data.entityId) fd.append('entityId', data.entityId);
      if (data.description) fd.append('description', data.description);
      return api.postForm<string>('/api/documents', fd);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['documents', 'list'] });
    },
  });
}

export function useDeleteDocument() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete(`/api/documents/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['documents', 'list'] });
    },
  });
}

export function useDownloadDocument() {
  return useMutation({
    mutationFn: async (doc: { id: string; fileName: string }) => {
      const blob = await api.download(`/api/documents/${doc.id}/download`);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = doc.fileName;
      a.click();
      URL.revokeObjectURL(url);
    },
  });
}

export function useCreateDocumentCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateDocumentCategoryRequest) =>
      api.post<string>('/api/documents/categories', data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['documents', 'categories'] });
    },
  });
}

export function useUpdateDocumentCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { id: string } & UpdateDocumentCategoryRequest) =>
      api.put<void>(`/api/documents/categories/${data.id}`, {
        name: data.name,
        description: data.description,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['documents', 'categories'] });
    },
  });
}

export function useDeleteDocumentCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete(`/api/documents/categories/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['documents', 'categories'] });
    },
  });
}
