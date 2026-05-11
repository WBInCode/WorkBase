export interface TimeStatusDto {
  status: 'not-started' | 'working' | 'on-break' | 'ended';
  lastEntryTime: string | null;
  lastEntryType: string | null;
  workedToday: string;
  breaksToday: string;
  currentBreakType: 'Paid' | 'Unpaid' | null;
}

export interface ClockRequest {
  employeeId: string;
  note?: string;
}

export interface StartBreakRequest {
  employeeId: string;
  breakType: 'Paid' | 'Unpaid';
  note?: string;
}

export interface BreakPolicyDto {
  id: string;
  name: string;
  breakType: string;
  maxPerDay: number | null;
  maxMinutesPerBreak: number | null;
  maxMinutesPerDay: number | null;
  isActive: boolean;
}

export interface BreakOptionDto {
  breakType: 'Paid' | 'Unpaid';
  label: string;
  available: boolean;
  usedCount: number;
  maxPerDay: number | null;
  usedMinutesToday: number;
  maxMinutesPerDay: number | null;
  maxMinutesPerBreak: number | null;
  denialReason: string | null;
}

export interface BreakAvailabilityDto {
  options: BreakOptionDto[];
}

export interface TimeSheetEntryDto {
  id: string;
  entryTime: string;
  type: string;
  breakType: string | null;
}

export interface TimeSheetDayDto {
  date: string;
  totalWorked: string;
  totalBreaks: string;
  netWorked: string;
  status: string;
  note: string | null;
  entries: TimeSheetEntryDto[];
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
  source: 'OrgUnit' | 'Individual' | 'Unplanned';
  orgUnitScheduleId: string | null;
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

export interface AdminCreateTimeEntryRequest {
  employeeId: string;
  entryTime: string;
  type: string;
  breakType?: string;
  note?: string;
}

export interface AdminUpdateTimeEntryRequest {
  entryTime: string;
  type: string;
  breakType?: string;
  note?: string;
}

export interface DayShiftPattern {
  dayOfWeek: number; // 0=Sunday, 1=Monday, ... 6=Saturday
  plannedStart: string;
  plannedEnd: string;
  shiftType?: string;
  templateId?: string;
}

export interface GenerateBatchSchedulesRequest {
  employeeIds: string[];
  from: string;
  to: string;
  weekPattern: DayShiftPattern[];
  overwrite?: boolean;
}

export interface GenerateBatchResult {
  createdCount: number;
}

export interface OrgUnitScheduleDto {
  id: string;
  orgUnitId: string;
  name: string;
  weekPattern: string; // JSON string of DayShiftPattern[]
  effectiveFrom: string;
  isActive: boolean;
}

export interface CreateOrgUnitScheduleRequest {
  orgUnitId: string;
  name: string;
  weekPattern: string;
  effectiveFrom: string;
}

export interface UpdateOrgUnitScheduleRequest {
  name: string;
  weekPattern: string;
  effectiveFrom: string;
}
