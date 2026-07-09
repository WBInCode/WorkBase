import { useEffect } from 'react';
import i18n from '@/i18n';
import { useBranding } from '@/api/hooks/useBranding';
import { useTerminology } from '@/api/hooks/useTenantConfig';
import { applyBranding } from './applyBranding';

/**
 * Applies tenant branding (colors/font/app name) as CSS variables and merges tenant
 * terminology overrides into i18next. Call once from a component that only renders after
 * authentication — both endpoints require an authenticated tenant user.
 */
export function useTenantThemeBootstrap(): void {
  const { data: branding } = useBranding();
  const { data: terminology } = useTerminology();

  useEffect(() => {
    applyBranding(branding);
  }, [branding]);

  useEffect(() => {
    if (!terminology) return;
    for (const [key, value] of Object.entries(terminology)) {
      i18n.addResource('pl', 'translation', key, value);
    }
  }, [terminology]);
}
