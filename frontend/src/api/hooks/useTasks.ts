import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  TaskItemDto,
  TaskStatusDto,
  TaskPriorityDto,
  TaskCommentDto,
  TaskAttachmentDto,
  CreateTaskRequest,
  UpdateTaskRequest,
  ChangeTaskStatusRequest,
  AssignTaskRequest,
  AddTaskCommentRequest,
  CreateTaskStatusRequest,
  UpdateTaskStatusRequest,
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

export function useTaskAttachments(taskId: string | null) {
  return useQuery({
    queryKey: ['tasks', 'attachments', taskId],
    queryFn: () => api.get<TaskAttachmentDto[]>(`/api/tasks/${taskId}/attachments`),
    enabled: !!taskId,
  });
}

export function useUploadTaskAttachment(taskId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      return api.postForm<string>(`/api/tasks/${taskId}/attachments`, formData);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tasks', 'attachments', taskId] });
    },
  });
}

export function useDownloadTaskAttachment() {
  return useMutation({
    mutationFn: async ({ taskId, attachmentId, fileName }: { taskId: string; attachmentId: string; fileName: string }) => {
      const blob = await api.download(`/api/tasks/${taskId}/attachments/${attachmentId}/download`);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.click();
      URL.revokeObjectURL(url);
    },
  });
}

export function useCreateTaskStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateTaskStatusRequest) =>
      api.post<string>('/api/tasks/statuses', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tasks', 'statuses'] }),
  });
}

export function useUpdateTaskStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdateTaskStatusRequest & { id: string }) =>
      api.put<void>(`/api/tasks/statuses/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tasks', 'statuses'] }),
  });
}

export function useDeleteTaskStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      api.delete<void>(`/api/tasks/statuses/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tasks', 'statuses'] }),
  });
}
