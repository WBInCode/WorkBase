import { useEffect } from 'react';
import { isTauri } from '@/shared';

type TrayAction = 'clock-in' | 'clock-out' | 'break-start' | 'break-end';

/**
 * Hook to listen for system tray actions dispatched by the Tauri backend.
 * The Tauri tray module calls window.__WORKBASE_TRAY_ACTION__(action)
 * which this hook picks up and forwards to the provided callback.
 *
 * No-op when not running inside Tauri.
 */
export function useTrayActions(onAction: (action: TrayAction) => void) {
  useEffect(() => {
    if (!isTauri()) return;

    const handler = (action: string) => {
      if (['clock-in', 'clock-out', 'break-start', 'break-end'].includes(action)) {
        onAction(action as TrayAction);
      }
    };

    (window as unknown as Record<string, unknown>).__WORKBASE_TRAY_ACTION__ = handler;

    return () => {
      delete (window as unknown as Record<string, unknown>).__WORKBASE_TRAY_ACTION__;
    };
  }, [onAction]);
}
