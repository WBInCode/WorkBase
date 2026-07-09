import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { TaskOverdueSettingsDto } from '@/api/types/taskSettings';

export function useTaskSettings() {
  return useQuery({
    queryKey: ['config', 'task-settings'],
    queryFn: () => api.get<TaskOverdueSettingsDto>('/api/tasks/settings'),
  });
}

export function useUpdateTaskSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: TaskOverdueSettingsDto) =>
      api.put<void>('/api/tasks/settings', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['config', 'task-settings'] }),
  });
}
