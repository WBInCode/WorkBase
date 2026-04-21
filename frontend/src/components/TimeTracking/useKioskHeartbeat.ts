import { useEffect, useRef, useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';

const DEFAULT_HEARTBEAT_INTERVAL = 30_000; // 30s
const DEFAULT_FULL_REFRESH_INTERVAL = 5 * 60_000; // 5 min
const IDLE_RESET_DELAY = 60_000; // 1 min without activity → reset view

interface UseKioskHeartbeatOptions {
  heartbeatInterval?: number;
  fullRefreshInterval?: number;
  idleResetDelay?: number;
  onIdle?: () => void;
}

export function useKioskHeartbeat(options: UseKioskHeartbeatOptions = {}) {
  const {
    heartbeatInterval = DEFAULT_HEARTBEAT_INTERVAL,
    fullRefreshInterval = DEFAULT_FULL_REFRESH_INTERVAL,
    idleResetDelay = IDLE_RESET_DELAY,
    onIdle,
  } = options;

  const queryClient = useQueryClient();
  const lastActivity = useRef(Date.now());
  const isIdle = useRef(false);

  const resetActivity = useCallback(() => {
    lastActivity.current = Date.now();
    if (isIdle.current) {
      isIdle.current = false;
    }
  }, []);

  // Heartbeat: invalidate time-status queries regularly
  useEffect(() => {
    const id = setInterval(() => {
      queryClient.invalidateQueries({ queryKey: ['time-status'] });
    }, heartbeatInterval);
    return () => clearInterval(id);
  }, [queryClient, heartbeatInterval]);

  // Full refresh: invalidate all queries periodically to prevent stale state
  useEffect(() => {
    const id = setInterval(() => {
      queryClient.invalidateQueries();
    }, fullRefreshInterval);
    return () => clearInterval(id);
  }, [queryClient, fullRefreshInterval]);

  // Idle detection: fire onIdle when no touch/click for idleResetDelay
  useEffect(() => {
    if (!onIdle) return;

    const checkIdle = setInterval(() => {
      if (!isIdle.current && Date.now() - lastActivity.current > idleResetDelay) {
        isIdle.current = true;
        onIdle();
      }
    }, 5_000);

    const events = ['touchstart', 'mousedown', 'keydown'] as const;
    events.forEach(ev => document.addEventListener(ev, resetActivity, { passive: true }));

    return () => {
      clearInterval(checkIdle);
      events.forEach(ev => document.removeEventListener(ev, resetActivity));
    };
  }, [onIdle, idleResetDelay, resetActivity]);

  // Page visibility: refresh data when tab becomes visible again
  useEffect(() => {
    const handleVisibility = () => {
      if (document.visibilityState === 'visible') {
        queryClient.invalidateQueries({ queryKey: ['time-status'] });
        resetActivity();
      }
    };
    document.addEventListener('visibilitychange', handleVisibility);
    return () => document.removeEventListener('visibilitychange', handleVisibility);
  }, [queryClient, resetActivity]);

  return { resetActivity };
}
