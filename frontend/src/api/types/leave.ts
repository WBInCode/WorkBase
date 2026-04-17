export interface LeaveTypeDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  isPaid: boolean;
  requiresApproval: boolean;
  defaultDaysPerYear: number | null;
  color: string | null;
  sortOrder: number;
}

export interface LeaveBalanceDto {
  id: string;
  leaveTypeId: string;
  leaveTypeCode: string;
  leaveTypeName: string;
  leaveTypeColor: string | null;
  year: number;
  totalDays: number;
  usedDays: number;
  pendingDays: number;
  carriedOverDays: number;
  remainingDays: number;
}

export interface LeaveRequestDto {
  id: string;
  employeeId: string;
  leaveTypeId: string;
  leaveTypeCode: string;
  leaveTypeName: string;
  leaveTypeColor: string | null;
  startDate: string;
  endDate: string;
  totalDays: number;
  status: LeaveRequestStatus;
  reason: string | null;
  createdAt: string;
}

export type LeaveRequestStatus = 'Draft' | 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export interface SubmitLeaveRequest {
  employeeId: string;
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason?: string;
}

export interface LeaveCalendarEntryDto {
  employeeId: string;
  leaveTypeId: string;
  leaveTypeCode: string;
  leaveTypeName: string;
  leaveTypeColor: string | null;
  date: string;
  dayFraction: number;
}

export interface LeaveCalendarRequest {
  employeeIds: string[];
  from: string;
  to: string;
}
