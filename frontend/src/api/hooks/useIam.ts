import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  RoleDto,
  PermissionDto,
  UserRoleDto,
  CreateRoleRequest,
  UpdateRoleRequest,
  UpdateRolePermissionsRequest,
  AssignUserRoleRequest,
  FeatureFlagDto,
  LicensePlanSummaryDto,
  TenantSummaryDto,
} from '@/api/types/iam';

export function useRoles() {
  return useQuery({
    queryKey: ['iam', 'roles'],
    queryFn: () => api.get<RoleDto[]>('/api/iam/roles'),
  });
}

export function useRole(id: string | null) {
  return useQuery({
    queryKey: ['iam', 'roles', id],
    queryFn: () => api.get<RoleDto>(`/api/iam/roles/${id}`),
    enabled: !!id,
  });
}

export function usePermissions() {
  return useQuery({
    queryKey: ['iam', 'permissions'],
    queryFn: () => api.get<PermissionDto[]>('/api/iam/permissions'),
  });
}

export function useRolePermissions(roleId: string | null) {
  return useQuery({
    queryKey: ['iam', 'roles', roleId, 'permissions'],
    queryFn: () => api.get<string[]>(`/api/iam/roles/${roleId}/permissions`),
    enabled: !!roleId,
  });
}

export function useCreateRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateRoleRequest) =>
      api.post<string>('/api/iam/roles', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['iam', 'roles'] }),
  });
}

export function useUpdateRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdateRoleRequest & { id: string }) =>
      api.put<void>(`/api/iam/roles/${id}`, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['iam', 'roles'] }),
  });
}

export function useUpdateRolePermissions() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ roleId, ...data }: UpdateRolePermissionsRequest & { roleId: string }) =>
      api.put<void>(`/api/iam/roles/${roleId}/permissions`, data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['iam', 'roles', variables.roleId, 'permissions'] });
      qc.invalidateQueries({ queryKey: ['iam', 'roles'] });
    },
  });
}

export function useUserRoles(userId: string | null) {
  return useQuery({
    queryKey: ['iam', 'users', userId, 'roles'],
    queryFn: () => api.get<UserRoleDto[]>(`/api/iam/users/${userId}/roles`),
    enabled: !!userId,
  });
}

export function useAssignUserRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, ...data }: AssignUserRoleRequest & { userId: string }) =>
      api.post<void>(`/api/iam/users/${userId}/roles`, data),
    onSuccess: (_data, variables) =>
      qc.invalidateQueries({ queryKey: ['iam', 'users', variables.userId, 'roles'] }),
  });
}

export function useUnassignUserRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, roleId }: { userId: string; roleId: string }) =>
      api.delete<void>(`/api/iam/users/${userId}/roles/${roleId}`),
    onSuccess: (_data, variables) =>
      qc.invalidateQueries({ queryKey: ['iam', 'users', variables.userId, 'roles'] }),
  });
}

export function useFeatureFlags() {
  return useQuery({
    queryKey: ['iam', 'feature-flags'],
    queryFn: () => api.get<FeatureFlagDto[]>('/api/iam/feature-flags'),
  });
}

export function useToggleFeatureFlag() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (module: string) =>
      api.put<void>(`/api/iam/feature-flags/${module}/toggle`, {}),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['iam', 'feature-flags'] }),
  });
}

export function useLicensePlans() {
  return useQuery({
    queryKey: ['iam', 'license-plans'],
    queryFn: () => api.get<LicensePlanSummaryDto[]>('/api/iam/feature-flags/plans'),
  });
}

// --- Platform-operator "companies" panel (docs/05-module-licensing-architecture.md step 5).
// Only usable by users authenticated in the operator tenant with the platform.manage-tenants
// permission — the backend enforces this (RequirePlatformOperator), these hooks will simply
// receive a 403 for anyone else.

export function usePlatformTenants() {
  return useQuery({
    queryKey: ['platform', 'tenants'],
    queryFn: () => api.get<TenantSummaryDto[]>('/api/org/tenants'),
  });
}

export function usePlatformTenantFeatureFlags(tenantId: string | null) {
  return useQuery({
    queryKey: ['platform', 'tenants', tenantId, 'feature-flags'],
    queryFn: () => api.get<FeatureFlagDto[]>(`/api/iam/feature-flags/tenant/${tenantId}`),
    enabled: !!tenantId,
  });
}

export function useTogglePlatformTenantFeatureFlag(tenantId: string | null) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (module: string) =>
      api.put<void>(`/api/iam/feature-flags/tenant/${tenantId}/${module}/toggle`, {}),
    onSuccess: () =>
      qc.invalidateQueries({ queryKey: ['platform', 'tenants', tenantId, 'feature-flags'] }),
  });
}

export function useApplyLicensePlanToPlatformTenant(tenantId: string | null) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (planId: string) =>
      api.post<void>(`/api/iam/feature-flags/tenant/${tenantId}/apply-plan/${planId}`, {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['platform', 'tenants', tenantId, 'feature-flags'] });
      qc.invalidateQueries({ queryKey: ['platform', 'tenants'] });
    },
  });
}
