export interface NotificationDto {
  id: string;
  title: string;
  body: string;
  category: string;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
  referenceType: string | null;
  referenceId: string | null;
}

export interface NotificationTemplateDto {
  id: string;
  code: string;
  name: string;
  titleTemplate: string;
  bodyTemplate: string;
  category: string;
  isActive: boolean;
}

export interface CreateNotificationTemplateRequest {
  code: string;
  name: string;
  titleTemplate: string;
  bodyTemplate: string;
  category: string;
}

export interface UpdateNotificationTemplateRequest {
  name: string;
  titleTemplate: string;
  bodyTemplate: string;
  category: string;
}
