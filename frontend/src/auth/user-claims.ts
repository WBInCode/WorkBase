import type { User } from 'oidc-client-ts';

export interface WorkBaseUser {
  sub: string;
  email: string;
  name: string;
  tenantId: string;
  employeeId: string;
  roles: string[];
  kioskLocation: string;
}

export function mapUserClaims(user: User): WorkBaseUser {
  const profile = user.profile;
  return {
    sub: profile.sub,
    email: (profile.email as string) ?? '',
    name: profile.name ?? profile.preferred_username ?? '',
    tenantId: (profile['tenant_id'] as string) ?? '',
    employeeId: (profile['employee_id'] as string) ?? '',
    roles: (profile['roles'] as string[]) ?? [],
    kioskLocation: (profile['kiosk_location'] as string) ?? '',
  };
}
