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
