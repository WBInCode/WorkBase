export interface DashboardSummaryDto {
  attendance: AttendanceSummaryDto;
  tasks: TaskSummaryDto;
  leave: LeaveSummaryDto;
  anomalies: AnomalySummaryDto;
}

export interface AttendanceSummaryDto {
  presentToday: number;
  lateToday: number;
  absentToday: number;
  totalScheduled: number;
}

export interface TaskSummaryDto {
  openTasks: number;
  overdueTasks: number;
  completedThisWeek: number;
  totalTasks: number;
}

export interface LeaveSummaryDto {
  pendingRequests: number;
  approvedThisMonth: number;
  onLeaveToday: number;
}

export interface AnomalySummaryDto {
  newAnomalies: number;
  reviewedThisWeek: number;
}
