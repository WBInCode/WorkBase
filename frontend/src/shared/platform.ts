/**
 * shared/platform — Platform detection utilities.
 * Used to adapt behavior for Web / Tauri / Capacitor / Kiosk.
 */

export type Platform = 'web' | 'tauri' | 'capacitor' | 'kiosk';

export function detectPlatform(): Platform {
  if (typeof window !== 'undefined') {
    if ((window as unknown as Record<string, unknown>).__TAURI__) return 'tauri';
    if ((window as unknown as Record<string, unknown>).Capacitor) return 'capacitor';
    if (window.location.pathname.startsWith('/kiosk')) return 'kiosk';
  }
  return 'web';
}

export function isTauri(): boolean {
  return typeof window !== 'undefined' && !!(window as unknown as Record<string, unknown>).__TAURI__;
}

export function isCapacitor(): boolean {
  return typeof window !== 'undefined' && !!(window as unknown as Record<string, unknown>).Capacitor;
}

export function isDesktop(): boolean {
  return isTauri();
}

export function isMobile(): boolean {
  return isCapacitor();
}

export function isKiosk(): boolean {
  return typeof window !== 'undefined' && window.location.pathname.startsWith('/kiosk');
}
