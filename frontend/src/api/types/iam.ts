export interface RoleDto {
  id: string;
  name: string;
  description: string | null;
  type: string;
  isActive: boolean;
  level: number;
  permissionCount: number;
  userCount: number;
}

export interface PermissionDto {
  id: string;
  module: string;
  action: string;
  scope: string | null;
  description: string | null;
  fullCode: string;
}

export interface UserRoleDto {
  roleId: string;
  roleName: string;
  roleType: string;
  assignedAt: string;
  assignedBy: string | null;
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
  level?: number;
}

export interface UpdateRoleRequest {
  name: string;
  description?: string;
  level?: number;
}

export interface UpdateRolePermissionsRequest {
  permissionIds: string[];
}

export interface AssignUserRoleRequest {
  roleId: string;
}
