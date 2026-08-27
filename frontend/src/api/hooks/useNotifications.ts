import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  NotificationDto,
  NotificationTemplateDto,
  CreateNotificationTemplateRequest,
  UpdateNotificationTemplateRequest,
} from '@/api/types/notification';

/**
 * `recipientUserId` NIE jest juz wysylany do serwera — odbiorce serwer wyprowadza z tokenu.
 * Zostaje tutaj jako klucz pamieci podrecznej i warunek „wiemy juz, kto jest zalogowany”,
 * zeby nie odpytywac przed zakonczeniem logowania.
 *
 * Wczesniej identyfikator szedl w adresie i serwer mu ufal, wiec dalo sie podstawic cudze konto.
 */
export function useNotifications(recipientUserId: string | null, unreadOnly = false) {
  const params = new URLSearchParams();
  if (unreadOnly) params.append('unreadOnly', 'true');

  return useQuery({
    queryKey: ['notifications', recipientUserId, unreadOnly],
    queryFn: () => api.get<NotificationDto[]>(`/api/notifications?${params}`),
    enabled: !!recipientUserId,
    refetchInterval: 30_000,
  });
}

export function useUnreadCount(recipientUserId: string | null) {
  return useQuery({
    queryKey: ['notifications', 'unread-count', recipientUserId],
    queryFn: () => api.get<number>('/api/notifications/unread-count'),
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
