import { useState, useEffect } from 'react';
import { breakpoints } from '@/theme';

/**
 * Shared hook for responsive breakpoint detection.
 * Default breakpoint matches the md (768px) used by MainLayout.
 */
export function useIsMobile(bp: number = breakpoints.md): boolean {
  const [mobile, setMobile] = useState(window.innerWidth < bp);
  useEffect(() => {
    const handler = () => setMobile(window.innerWidth < bp);
    window.addEventListener('resize', handler);
    return () => window.removeEventListener('resize', handler);
  }, [bp]);
  return mobile;
}
