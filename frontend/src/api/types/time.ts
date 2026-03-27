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
