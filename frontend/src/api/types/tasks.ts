export interface TaskItemDto {
  id: string;
  title: string;
  description: string | null;
  statusId: string;
  statusName: string;
  statusColor: string | null;
  priorityId: string;
  priorityName: string;
  priorityColor: string | null;
  assigneeId: string;
  coAssigneeId: string | null;
  reporterId: string | null;
  dueDate: string | null;
  completedAt: string | null;
  createdAt: string;
}

export interface TaskStatusDto {
  id: string;
  code: string;
  name: string;
  color: string | null;
  isFinal: boolean;
  isDefault: boolean;
  sortOrder: number;
}

export interface TaskPriorityDto {
  id: string;
  code: string;
  name: string;
  color: string | null;
  sortOrder: number;
}

export interface TaskCommentDto {
  id: string;
  taskId: string;
  authorId: string;
  content: string;
  createdAt: string;
}

export interface TaskAttachmentDto {
  id: string;
  taskId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedById: string;
  uploadedAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  priorityId: string;
  assigneeId: string;
  coAssigneeId?: string;
  reporterId?: string;
  dueDate?: string;
}

export interface UpdateTaskRequest {
  title: string;
  description?: string;
  priorityId: string;
  dueDate?: string;
}

export interface ChangeTaskStatusRequest {
  newStatusId: string;
  changedById: string;
}

export interface AssignTaskRequest {
  newAssigneeId: string;
  newCoAssigneeId?: string | null;
}

export interface AddTaskCommentRequest {
  authorId: string;
  content: string;
}

export interface CreateTaskStatusRequest {
  code: string;
  name: string;
  color?: string;
  isFinal: boolean;
  isDefault: boolean;
  sortOrder: number;
}

export interface UpdateTaskStatusRequest {
  code: string;
  name: string;
  color?: string;
  isFinal: boolean;
  isDefault: boolean;
  sortOrder: number;
}
