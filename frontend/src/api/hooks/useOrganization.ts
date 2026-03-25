import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  OrganizationUnitTreeNode,
  EmployeeDto,
  EmployeeDetailDto,
  EmployeeStatus,
  PagedResult,
  PositionDto,
  CreateEmployeeRequest,
  AssignEmployeeRequest,
  SetSupervisorRequest,
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
