import type { ReactNode } from 'react';
import { useFeatureFlags } from '@/api/hooks/useIam';

interface FeatureGateProps {
  module: string;
  children: ReactNode;
  fallback?: ReactNode;
}

export function FeatureGate({ module, children, fallback = null }: FeatureGateProps) {
  const { data: flags, isLoading } = useFeatureFlags();

  if (isLoading) return null;

  const flag = flags?.find((f) => f.module === module);
  if (!flag?.isEnabled) return <>{fallback}</>;

  return <>{children}</>;
}
