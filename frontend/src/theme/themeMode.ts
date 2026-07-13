// Tryb motywu: 'light' | 'dark' | 'system'. Wybór trzymany w localStorage,
// aplikowany jako html[data-theme] PRZED renderem Reacta (import w main.tsx),
// żeby uniknąć błysku jasnego tła. Tokens.ts czyta zmienne CSS — komponenty
// przełączają się automatycznie.
import { useSyncExternalStore, useCallback } from 'react';

const STORE_KEY = 'wb.theme';

export type ThemeMode = 'light' | 'dark' | 'system';

function systemPrefersDark(): boolean {
  return typeof window !== 'undefined' && window.matchMedia?.('(prefers-color-scheme: dark)').matches;
}

function readMode(): ThemeMode {
  try {
    const v = localStorage.getItem(STORE_KEY);
    return v === 'dark' || v === 'light' ? v : 'system';
  } catch {
    return 'system';
  }
}

function resolved(mode: ThemeMode): 'light' | 'dark' {
  return mode === 'system' ? (systemPrefersDark() ? 'dark' : 'light') : mode;
}

function apply(mode: ThemeMode): void {
  const theme = resolved(mode);
  const root = document.documentElement;
  if (theme === 'dark') root.setAttribute('data-theme', 'dark');
  else root.removeAttribute('data-theme');
}

const listeners = new Set<() => void>();
let currentMode: ThemeMode = readMode();

function setMode(mode: ThemeMode): void {
  currentMode = mode;
  try {
    if (mode === 'system') localStorage.removeItem(STORE_KEY);
    else localStorage.setItem(STORE_KEY, mode);
  } catch { /* private mode */ }
  apply(mode);
  listeners.forEach((l) => l());
}

/** Wywołaj raz przy starcie aplikacji (main.tsx), przed renderem. */
export function initTheme(): void {
  apply(currentMode);
  // Reaguj na zmianę preferencji systemowych w trybie 'system'
  window.matchMedia?.('(prefers-color-scheme: dark)').addEventListener?.('change', () => {
    if (currentMode === 'system') {
      apply(currentMode);
      listeners.forEach((l) => l());
    }
  });
}

function subscribe(cb: () => void): () => void {
  listeners.add(cb);
  return () => listeners.delete(cb);
}

/** Hook do przełącznika motywu (topbar). */
export function useTheme(): { mode: ThemeMode; theme: 'light' | 'dark'; toggle: () => void } {
  const mode = useSyncExternalStore(subscribe, () => currentMode);
  const toggle = useCallback(() => {
    // Prosty cykl: aktualny efektywny motyw → przeciwny (zapisany jawnie)
    setMode(resolved(currentMode) === 'dark' ? 'light' : 'dark');
  }, []);
  return { mode, theme: resolved(mode), toggle };
}
