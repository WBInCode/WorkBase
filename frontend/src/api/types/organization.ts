export interface OrganizationUnitTreeNode {
  id: string;
  name: string;
  code: string | null;
  typeId: string;
  typeName: string;
  isActive: boolean;
  children: OrganizationUnitTreeNode[];
}

export interface OrganizationUnitType {
  id: string;
  name: string;
  description: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface EmployeeDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  employeeNumber: string | null;
  hireDate: string;
  terminationDate: string | null;
  status: EmployeeStatus;
  userId: string | null;
}

export type EmployeeStatus = 'Active' | 'Inactive' | 'OnLeave';

export interface EmployeeDetailDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  employeeNumber: string | null;
  hireDate: string;
  terminationDate: string | null;
  status: EmployeeStatus;
  userId: string | null;
  assignments: EmployeeAssignmentDto[];
  supervisor: SupervisorInfoDto | null;
}

export interface EmployeeAssignmentDto {
  id: string;
  organizationUnitId: string;
  organizationUnitName: string;
  positionId: string;
  positionName: string;
  isPrimary: boolean;
  startDate: string;
  endDate: string | null;
}

export interface SupervisorInfoDto {
  employeeId: string;
  firstName: string;
  lastName: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PositionDto {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface CreateEmployeeRequest {
  firstName: string;
  lastName: string;
  email: string;
  employeeNumber?: string;
  hireDate: string;
}

export interface AssignEmployeeRequest {
  organizationUnitId: string;
  positionId: string;
  isPrimary: boolean;
  startDate: string;
}

export interface SetSupervisorRequest {
  supervisorEmployeeId: string;
}
