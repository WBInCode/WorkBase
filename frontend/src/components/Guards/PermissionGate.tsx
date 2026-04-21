import type { ReactNode } from 'react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';

interface PermissionGateProps {
  roles: string[];
  children: ReactNode;
  fallback?: ReactNode;
}

export function PermissionGate({ roles, children, fallback = null }: PermissionGateProps) {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;

  if (!user) return <>{fallback}</>;

  const hasRole = user.roles.some((r) => roles.includes(r));
  if (!hasRole) return <>{fallback}</>;

  return <>{children}</>;
}
