import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  TaskItemDto,
  TaskStatusDto,
  TaskPriorityDto,
  TaskCommentDto,
  CreateTaskRequest,
  UpdateTaskRequest,
  ChangeTaskStatusRequest,
  AssignTaskRequest,
  AddTaskCommentRequest,
} from '@/api/types/tasks';

export function useTasks(assigneeId?: string | null) {
  const params = new URLSearchParams();
  if (assigneeId) params.set('assigneeId', assigneeId);
  const qs = params.toString();

  return useQuery({
    queryKey: ['tasks', 'list', assigneeId ?? 'all'],
    queryFn: () => api.get<TaskItemDto[]>(`/api/tasks${qs ? `?${qs}` : ''}`),
  });
}

export function useTaskStatuses() {
  return useQuery({
    queryKey: ['tasks', 'statuses'],
    queryFn: () => api.get<TaskStatusDto[]>('/api/tasks/statuses'),
  });
}

export function useTaskPriorities() {
  return useQuery({
    queryKey: ['tasks', 'priorities'],
    queryFn: () => api.get<TaskPriorityDto[]>('/api/tasks/priorities'),
  });
}

export function useTaskComments(taskId: string | null) {
  return useQuery({
    queryKey: ['tasks', 'comments', taskId],
    queryFn: () => api.get<TaskCommentDto[]>(`/api/tasks/${taskId}/comments`),
    enabled: !!taskId,
  });
}

export function useCreateTask() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateTaskRequest) =>
      api.post<string>('/api/tasks', data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tasks', 'list'] });
    },
  });
}

export function useUpdateTask(taskId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateTaskRequest) =>
      api.put<void>(`/api/tasks/${taskId}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tasks', 'list'] });
    },
  });
}

export function useChangeTaskStatus(taskId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: ChangeTaskStatusRequest) =>
      api.put<void>(`/api/tasks/${taskId}/status`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tasks', 'list'] });
    },
  });
}

export function useAssignTask(taskId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: AssignTaskRequest) =>
      api.put<void>(`/api/tasks/${taskId}/assign`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tasks', 'list'] });
    },
  });
}

export function useAddTaskComment(taskId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: AddTaskCommentRequest) =>
      api.post<string>(`/api/tasks/${taskId}/comments`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tasks', 'comments', taskId] });
    },
  });
}
