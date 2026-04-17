import { useState, useEffect, useCallback } from 'react';
import { Play, Square, Coffee, CoffeeIcon } from 'lucide-react';
import { useTimeStatus, useClockIn, useClockOut, useStartBreak, useEndBreak } from '@/api/hooks/useTimeTracking';

interface ClockButtonProps {
  employeeId: string;
}

type TimeStatus = 'not-started' | 'working' | 'on-break' | 'ended';

const STATUS_CONFIG: Record<TimeStatus, { label: string; color: string; bg: string; dot: string }> = {
  'not-started': { label: 'Nierozpoczęty', color: '#6b7280', bg: '#f3f4f6', dot: '#9ca3af' },
  'working': { label: 'W pracy', color: '#059669', bg: '#ecfdf5', dot: '#10b981' },
  'on-break': { label: 'Przerwa', color: '#d97706', bg: '#fffbeb', dot: '#f59e0b' },
  'ended': { label: 'Zakończony', color: '#6b7280', bg: '#f3f4f6', dot: '#9ca3af' },
};

function parseDuration(duration: string): number {
  // Parse .NET TimeSpan format "HH:MM:SS" or "D.HH:MM:SS"
  const parts = duration.split(':');
  if (parts.length < 2) return 0;

  let hours = 0;
  let minutes = 0;
  let seconds = 0;

  if (parts.length === 3) {
    const hourPart = parts[0] ?? '';
    // Handle "D.HH" format
    if (hourPart.includes('.')) {
      const [days, h] = hourPart.split('.');
      hours = parseInt(days ?? '0') * 24 + parseInt(h ?? '0');
    } else {
      hours = parseInt(hourPart);
    }
    minutes = parseInt(parts[1] ?? '0');
    seconds = parseFloat(parts[2] ?? '0');
  } else if (parts.length === 2) {
    minutes = parseInt(parts[0] ?? '0');
    seconds = parseFloat(parts[1] ?? '0');
  }

  return Math.floor(hours * 3600 + minutes * 60 + seconds);
}

function formatDuration(totalSeconds: number): string {
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}

export function ClockButton({ employeeId }: ClockButtonProps) {
  const { data: statusData, isLoading } = useTimeStatus(employeeId);
  const clockIn = useClockIn();
  const clockOut = useClockOut();
  const startBreak = useStartBreak();
  const endBreak = useEndBreak();

  const [elapsedSeconds, setElapsedSeconds] = useState(0);

  const status: TimeStatus = (statusData?.status as TimeStatus) ?? 'not-started';
  const isActive = status === 'working' || status === 'on-break';
  const isMutating = clockIn.isPending || clockOut.isPending || startBreak.isPending || endBreak.isPending;

  // Calculate initial elapsed and run timer
  useEffect(() => {
    if (!statusData?.workedToday) {
      setElapsedSeconds(0);
      return;
    }

    const baseSeconds = parseDuration(statusData.workedToday);

    if (status !== 'working') {
      setElapsedSeconds(baseSeconds);
      return;
    }

    // For "working" status, add time elapsed since last entry
    let extraSeconds = 0;
    if (statusData.lastEntryTime) {
      const lastEntry = new Date(statusData.lastEntryTime);
      extraSeconds = Math.max(0, Math.floor((Date.now() - lastEntry.getTime()) / 1000));
      // Only add extra if last entry was ClockIn or BreakEnd (meaning timer is running)
      if (statusData.lastEntryType === 'ClockIn' || statusData.lastEntryType === 'BreakEnd') {
        setElapsedSeconds(baseSeconds + extraSeconds);
      } else {
        setElapsedSeconds(baseSeconds);
      }
    } else {
      setElapsedSeconds(baseSeconds);
    }

    // Live tick every second while working
    const interval = setInterval(() => {
      setElapsedSeconds((prev) => prev + 1);
    }, 1000);

    return () => clearInterval(interval);
  }, [statusData, status]);

  const handleClockIn = useCallback(() => {
    clockIn.mutate({ employeeId });
  }, [employeeId, clockIn]);

  const handleClockOut = useCallback(() => {
    clockOut.mutate({ employeeId });
  }, [employeeId, clockOut]);

  const handleStartBreak = useCallback(() => {
    startBreak.mutate({ employeeId });
  }, [employeeId, startBreak]);

  const handleEndBreak = useCallback(() => {
    endBreak.mutate({ employeeId });
  }, [employeeId, endBreak]);

  if (isLoading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '4px 12px' }}>
        <div style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: '#d1d5db' }} />
        <span style={{ fontSize: '13px', color: '#9ca3af' }}>Ładowanie...</span>
      </div>
    );
  }

  const config = STATUS_CONFIG[status];
  const error = clockIn.error || clockOut.error || startBreak.error || endBreak.error;

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: '8px',
        padding: '4px 8px',
        borderRadius: '8px',
        backgroundColor: config.bg,
        border: `1px solid ${config.color}22`,
      }}
    >
      {/* Status dot + label */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', minWidth: '90px' }}>
        <div
          style={{
            width: '8px',
            height: '8px',
            borderRadius: '50%',
            backgroundColor: config.dot,
            animation: isActive ? 'pulse 2s infinite' : undefined,
          }}
        />
        <span style={{ fontSize: '12px', fontWeight: 500, color: config.color, whiteSpace: 'nowrap' }}>
          {config.label}
        </span>
      </div>

      {/* Timer */}
      {(isActive || status === 'ended') && (
        <span
          style={{
            fontSize: '13px',
            fontWeight: 600,
            color: config.color,
            fontVariantNumeric: 'tabular-nums',
            minWidth: '62px',
            textAlign: 'center',
          }}
        >
          {formatDuration(elapsedSeconds)}
        </span>
      )}

      {/* Action buttons */}
      <div style={{ display: 'flex', gap: '4px' }}>
        {status === 'not-started' && (
          <ActionButton
            onClick={handleClockIn}
            disabled={isMutating}
            icon={<Play size={14} />}
            label="Rozpocznij"
            color="#059669"
            bg="#d1fae5"
          />
        )}

        {status === 'working' && (
          <>
            <ActionButton
              onClick={handleStartBreak}
              disabled={isMutating}
              icon={<Coffee size={14} />}
              label="Przerwa"
              color="#d97706"
              bg="#fef3c7"
            />
            <ActionButton
              onClick={handleClockOut}
              disabled={isMutating}
              icon={<Square size={14} />}
              label="Zakończ"
              color="#dc2626"
              bg="#fee2e2"
            />
          </>
        )}

        {status === 'on-break' && (
          <ActionButton
            onClick={handleEndBreak}
            disabled={isMutating}
            icon={<CoffeeIcon size={14} />}
            label="Wróć"
            color="#059669"
            bg="#d1fae5"
          />
        )}
      </div>

      {/* Error tooltip */}
      {error && (
        <span style={{ fontSize: '11px', color: '#dc2626', maxWidth: '150px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {error.message}
        </span>
      )}

      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
      `}</style>
    </div>
  );
}

interface ActionButtonProps {
  onClick: () => void;
  disabled: boolean;
  icon: React.ReactNode;
  label: string;
  color: string;
  bg: string;
}

function ActionButton({ onClick, disabled, icon, label, color, bg }: ActionButtonProps) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      title={label}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '4px',
        padding: '4px 10px',
        fontSize: '12px',
        fontWeight: 500,
        color,
        backgroundColor: bg,
        border: `1px solid ${color}33`,
        borderRadius: '6px',
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.6 : 1,
        whiteSpace: 'nowrap',
        transition: 'opacity 0.15s',
      }}
    >
      {icon}
      {label}
    </button>
  );
}
