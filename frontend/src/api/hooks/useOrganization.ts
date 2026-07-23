import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  OrganizationUnitTreeNode,
  OrganizationUnitType,
  EmployeeDto,
  EmployeeDetailDto,
  EmployeeAccessStatus,
  EmployeeStatus,
  PagedResult,
  PositionDto,
  CreateEmployeeRequest,
  AssignEmployeeRequest,
  SetSupervisorRequest,
  CreateOrgUnitRequest,
  UpdateOrgUnitRequest,
  CreatePositionRequest,
  UpdatePositionRequest,
  CreateUnitTypeRequest,
  UpdateUnitTypeRequest,
  UpdateEmployeeRequest,
} from '@/api/types/organization';

export function useOrgUnitTree() {
  return useQuery({
    queryKey: ['org', 'units', 'tree'],
    queryFn: () => api.get<OrganizationUnitTreeNode[]>('/api/org/units/tree'),
  });
}

export interface EmployeesFilter {
  search?: string;
  organizationUnitId?: string;
  status?: EmployeeStatus;
  page: number;
  pageSize: number;
}

export function useEmployees(filter: EmployeesFilter) {
  const params = new URLSearchParams();
  if (filter.search) params.set('search', filter.search);
  if (filter.organizationUnitId) params.set('organizationUnitId', filter.organizationUnitId);
  if (filter.status) params.set('status', filter.status);
  params.set('page', String(filter.page));
  params.set('pageSize', String(filter.pageSize));

  return useQuery({
    queryKey: ['org', 'employees', filter],
    queryFn: () => api.get<PagedResult<EmployeeDto>>(`/api/org/employees?${params}`),
  });
}

export function useEmployeeDetail(id: string | null) {
  return useQuery({
    queryKey: ['org', 'employees', id],
    queryFn: () => api.get<EmployeeDetailDto>(`/api/org/employees/${id}`),
    enabled: !!id,
  });
}

export function useEmployeeAccessStatus(id: string | null) {
  return useQuery({
    queryKey: ['org', 'employees', id, 'access-status'],
    queryFn: () => api.get<EmployeeAccessStatus>(`/api/org/employees/${id}/access-status`),
    enabled: !!id,
  });
}

export function useRetryEmployeeAccess() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (employeeId: string) =>
      api.post<void>(`/api/org/employees/${employeeId}/access-status/retry`),
    onSuccess: (_, employeeId) =>
      qc.invalidateQueries({ queryKey: ['org', 'employees', employeeId, 'access-status'] }),
  });
}

export function usePositions() {
  return useQuery({
    queryKey: ['org', 'positions'],
    queryFn: () => api.get<PositionDto[]>('/api/org/positions'),
  });
}

export function useCreateEmployee() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateEmployeeRequest) =>
      api.post<string>('/api/org/employees', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'employees'] }),
  });
}

export function useAssignEmployee() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ employeeId, ...data }: AssignEmployeeRequest & { employeeId: string }) =>
      api.put<string>(`/api/org/employees/${employeeId}/assignment`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'employees'] }),
  });
}

export function useSetSupervisor() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ employeeId, ...data }: SetSupervisorRequest & { employeeId: string }) =>
      api.put<void>(`/api/org/employees/${employeeId}/supervisor`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'employees'] }),
  });
}

// ─── Org Unit CRUD ───

export function useCreateOrgUnit() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateOrgUnitRequest) =>
      api.post<string>('/api/org/units', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'units'] }),
  });
}

export function useUpdateOrgUnit() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdateOrgUnitRequest & { id: string }) =>
      api.put<void>(`/api/org/units/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'units'] }),
  });
}

export function useDeleteOrgUnit() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/org/units/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'units'] }),
  });
}

// ─── Unit Types CRUD ───

export function useUnitTypes() {
  return useQuery({
    queryKey: ['org', 'unit-types'],
    queryFn: () => api.get<OrganizationUnitType[]>('/api/org/unit-types'),
  });
}

export function useCreateUnitType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateUnitTypeRequest) =>
      api.post<string>('/api/org/unit-types', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'unit-types'] }),
  });
}

export function useUpdateUnitType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdateUnitTypeRequest & { id: string }) =>
      api.put<void>(`/api/org/unit-types/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'unit-types'] }),
  });
}

export function useDeleteUnitType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/org/unit-types/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'unit-types'] }),
  });
}

// ─── Positions CRUD ───

export function useCreatePosition() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreatePositionRequest) =>
      api.post<string>('/api/org/positions', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'positions'] }),
  });
}

export function useUpdatePosition() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdatePositionRequest & { id: string }) =>
      api.put<void>(`/api/org/positions/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'positions'] }),
  });
}

export function useDeletePosition() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/org/positions/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'positions'] }),
  });
}

// ─── Employee Update/Deactivate ───

export function useUpdateEmployee() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdateEmployeeRequest & { id: string }) =>
      api.put<void>(`/api/org/employees/${id}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['org', 'employees'] });
    },
  });
}

export function useDeactivateEmployee() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/org/employees/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['org', 'employees'] }),
  });
}

export function useSetEmployeeHourlyRate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, hourlyRate }: { id: string; hourlyRate: number | null }) =>
      api.put<void>(`/api/org/employees/${id}/hourly-rate`, { hourlyRate }),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: ['org', 'employees'] });
      qc.invalidateQueries({ queryKey: ['org', 'employee', vars.id] });
    },
  });
}
