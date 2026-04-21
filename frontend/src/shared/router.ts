/**
 * shared/router — Platform-agnostic router type configuration.
 * Allows switching between BrowserRouter (web) and HashRouter (Tauri/desktop).
 */

export type RouterMode = 'browser' | 'hash';

const ROUTER_MODE_KEY = 'VITE_ROUTER_MODE';

export function getRouterMode(): RouterMode {
  if (typeof import.meta !== 'undefined' && import.meta.env?.[ROUTER_MODE_KEY] === 'hash') {
    return 'hash';
  }
  if (typeof window !== 'undefined' && (window as Record<string, unknown>).__TAURI__) {
    return 'hash';
  }
  return 'browser';
}
