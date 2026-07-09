import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  NotificationDto,
  NotificationTemplateDto,
  CreateNotificationTemplateRequest,
  UpdateNotificationTemplateRequest,
} from '@/api/types/notification';

export function useNotifications(recipientUserId: string | null, unreadOnly = false) {
  const params = new URLSearchParams();
  if (recipientUserId) params.append('recipientUserId', recipientUserId);
  if (unreadOnly) params.append('unreadOnly', 'true');

  return useQuery({
    queryKey: ['notifications', recipientUserId, unreadOnly],
    queryFn: () => api.get<NotificationDto[]>(`/api/notifications?${params}`),
    enabled: !!recipientUserId,
    refetchInterval: 30_000,
  });
}

export function useUnreadCount(recipientUserId: string | null) {
  const params = new URLSearchParams();
  if (recipientUserId) params.append('recipientUserId', recipientUserId);

  return useQuery({
    queryKey: ['notifications', 'unread-count', recipientUserId],
    queryFn: () => api.get<number>(`/api/notifications/unread-count?${params}`),
    enabled: !!recipientUserId,
    refetchInterval: 15_000,
  });
}

export function useMarkNotificationRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.post(`/api/notifications/${id}/read`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['notifications'] });
    },
  });
}

export function useNotificationTemplates() {
  return useQuery({
    queryKey: ['notifications', 'templates'],
    queryFn: () => api.get<NotificationTemplateDto[]>('/api/notifications/templates'),
  });
}

export function useCreateNotificationTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateNotificationTemplateRequest) =>
      api.post<string>('/api/notifications/templates', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications', 'templates'] }),
  });
}

export function useUpdateNotificationTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdateNotificationTemplateRequest & { id: string }) =>
      api.put<void>(`/api/notifications/templates/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications', 'templates'] }),
  });
}

export function useDeleteNotificationTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/notifications/templates/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications', 'templates'] }),
  });
}
