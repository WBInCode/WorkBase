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
