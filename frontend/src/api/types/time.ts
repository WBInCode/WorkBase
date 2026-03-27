export interface TimeStatusDto {
  status: 'not-started' | 'working' | 'on-break' | 'ended';
  lastEntryTime: string | null;
  lastEntryType: string | null;
  workedToday: string;
  breaksToday: string;
}

export interface ClockRequest {
  employeeId: string;
  note?: string;
}

export interface TimeSheetDayDto {
  date: string;
  totalWorked: string;
  totalBreaks: string;
  netWorked: string;
  status: string;
  note: string | null;
}

export interface TimeSheetPeriodDto {
  from: string;
  to: string;
  period: string;
  employeeId: string;
  totalWorked: string;
  totalBreaks: string;
  netWorked: string;
  daysWorked: number;
  daysIncomplete: number;
  days: TimeSheetDayDto[];
}

export interface TimeAnomalyDto {
  id: string;
  employeeId: string;
  date: string;
  type: string;
  status: string;
  description: string | null;
  details: string | null;
  reviewedBy: string | null;
  reviewedAt: string | null;
  createdAt: string;
}

export interface ScheduleDto {
  id: string;
  employeeId: string;
  date: string;
  plannedStart: string;
  plannedEnd: string;
  shiftType: string | null;
  templateId: string | null;
  plannedDuration: string;
}

export interface CreateScheduleRequest {
  employeeId: string;
  date: string;
  plannedStart: string;
  plannedEnd: string;
  shiftType?: string;
  templateId?: string;
}

export interface UpdateScheduleRequest {
  plannedStart: string;
  plannedEnd: string;
  shiftType?: string;
}

export interface ScheduleTemplateDto {
  id: string;
  name: string;
  description: string | null;
  definition: string;
  isActive: boolean;
}
