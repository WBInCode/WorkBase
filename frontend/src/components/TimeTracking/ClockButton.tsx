import { useState, useEffect, useCallback, useRef } from 'react';
import { Play, Square, Coffee, CoffeeIcon, Loader2, RotateCcw } from 'lucide-react';
import { useTimeStatus, useBreakAvailability, useClockIn, useClockOut, useStartBreak, useEndBreak } from '@/api/hooks/useTimeTracking';

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
  // Parse .NET TimeSpan format "HH:MM:SS" or "HH:MM:SS.fffffff" or "D.HH:MM:SS"
  const parts = duration.split(':');
  if (parts.length < 2) return 0;

  let hours = 0;
  let minutes = 0;
  let seconds = 0;

  if (parts.length === 3) {
    const hourPart = parts[0] ?? '';
    if (hourPart.includes('.')) {
      const [days, h] = hourPart.split('.');
      hours = parseInt(days ?? '0') * 24 + parseInt(h ?? '0');
    } else {
      hours = parseInt(hourPart);
    }
    minutes = parseInt(parts[1] ?? '0');
    // Truncate fractional seconds — parseInt ignores ".1234567"
    seconds = parseInt(parts[2] ?? '0');
  } else if (parts.length === 2) {
    minutes = parseInt(parts[0] ?? '0');
    seconds = parseInt(parts[1] ?? '0');
  }

  return hours * 3600 + minutes * 60 + seconds;
}

function formatDuration(totalSeconds: number): string {
  const s = Math.floor(totalSeconds);
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = s % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`;
}

export function ClockButton({ employeeId }: ClockButtonProps) {
  const { data: statusData, isLoading } = useTimeStatus(employeeId);
  const clockIn = useClockIn();
  const clockOut = useClockOut();
  const startBreak = useStartBreak();
  const endBreak = useEndBreak();

  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const [flash, setFlash] = useState<{ type: 'ok' | 'err'; msg: string } | null>(null);
  const [confirmAction, setConfirmAction] = useState<{ label: string; onConfirm: () => void } | null>(null);
  const [showBreakPicker, setShowBreakPicker] = useState(false);
  const flashTimer = useRef<ReturnType<typeof setTimeout>>(null);

  const status: TimeStatus = (statusData?.status as TimeStatus) ?? 'not-started';
  const breakTypeLabel = statusData?.currentBreakType === 'Paid' ? 'Płatna' : statusData?.currentBreakType === 'Unpaid' ? 'Bezpłatna' : null;
  const isActive = status === 'working' || status === 'on-break';
  const isMutating = clockIn.isPending || clockOut.isPending || startBreak.isPending || endBreak.isPending;

  const { data: breakAvail } = useBreakAvailability(employeeId, showBreakPicker);

  const showFlash = useCallback((type: 'ok' | 'err', msg: string) => {
    if (flashTimer.current) clearTimeout(flashTimer.current);
    setFlash({ type, msg });
    flashTimer.current = setTimeout(() => setFlash(null), 3500);
  }, []);

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

    let extraSeconds = 0;
    if (statusData.lastEntryTime) {
      const lastEntry = new Date(statusData.lastEntryTime);
      extraSeconds = Math.max(0, Math.floor((Date.now() - lastEntry.getTime()) / 1000));
      if (statusData.lastEntryType === 'ClockIn' || statusData.lastEntryType === 'BreakEnd') {
        setElapsedSeconds(baseSeconds + extraSeconds);
      } else {
        setElapsedSeconds(baseSeconds);
      }
    } else {
      setElapsedSeconds(baseSeconds);
    }

    const interval = setInterval(() => {
      setElapsedSeconds((prev) => prev + 1);
    }, 1000);

    return () => clearInterval(interval);
  }, [statusData, status]);

  const mutOpts = useCallback(
    (okMsg: string) => ({
      onSuccess: () => showFlash('ok', okMsg),
      onError: (e: Error) => showFlash('err', e.message || 'Błąd operacji'),
    }),
    [showFlash],
  );

  const handleClockIn = useCallback(() => {
    clockIn.mutate({ employeeId }, mutOpts('Czas pracy rozpoczęty'));
  }, [employeeId, clockIn, mutOpts]);

  const handleClockOut = useCallback(() => {
    setConfirmAction({
      label: 'Czy na pewno chcesz zakończyć czas pracy?',
      onConfirm: () => {
        clockOut.mutate({ employeeId }, mutOpts('Czas pracy zakończony'));
        setConfirmAction(null);
      },
    });
  }, [employeeId, clockOut, mutOpts]);

  const handleStartBreak = useCallback(() => {
    setShowBreakPicker(true);
  }, []);

  const handleBreakType = useCallback((breakType: 'Paid' | 'Unpaid') => {
    setShowBreakPicker(false);
    startBreak.mutate({ employeeId, breakType }, mutOpts('Przerwa rozpoczęta'));
  }, [employeeId, startBreak, mutOpts]);

  const handleEndBreak = useCallback(() => {
    endBreak.mutate({ employeeId }, mutOpts('Przerwa zakończona'));
  }, [employeeId, endBreak, mutOpts]);

  if (isLoading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '4px 12px' }}>
        <Loader2 size={14} style={{ animation: 'wb-spin 1s linear infinite', color: '#9ca3af' }} />
        <span style={{ fontSize: '13px', color: '#9ca3af' }}>Ładowanie...</span>
      </div>
    );
  }

  const config = STATUS_CONFIG[status];

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
        position: 'relative',
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
            animation: isActive ? 'wb-pulse 2s infinite' : undefined,
          }}
        />
        <span style={{ fontSize: '12px', fontWeight: 500, color: config.color, whiteSpace: 'nowrap' }}>
          {status === 'on-break' && breakTypeLabel ? `${config.label} (${breakTypeLabel})` : config.label}
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
            loading={clockIn.isPending}
            disabled={isMutating}
            icon={<Play size={14} />}
            label="Rozpocznij"
            color="#059669"
            bg="#d1fae5"
            hoverBg="#a7f3d0"
          />
        )}

        {status === 'working' && (
          <>
            <ActionButton
              onClick={handleStartBreak}
              loading={startBreak.isPending}
              disabled={isMutating}
              icon={<Coffee size={14} />}
              label="Przerwa"
              color="#d97706"
              bg="#fef3c7"
              hoverBg="#fde68a"
            />
            <ActionButton
              onClick={handleClockOut}
              loading={clockOut.isPending}
              disabled={isMutating}
              icon={<Square size={14} />}
              label="Zakończ"
              color="#dc2626"
              bg="#fee2e2"
              hoverBg="#fecaca"
            />
          </>
        )}

        {status === 'on-break' && (
          <ActionButton
            onClick={handleEndBreak}
            loading={endBreak.isPending}
            disabled={isMutating}
            icon={<CoffeeIcon size={14} />}
            label="Wróć"
            color="#059669"
            bg="#d1fae5"
            hoverBg="#a7f3d0"
          />
        )}

        {status === 'ended' && (
          <ActionButton
            onClick={handleClockIn}
            loading={clockIn.isPending}
            disabled={isMutating}
            icon={<RotateCcw size={14} />}
            label="Wznów"
            color="#059669"
            bg="#d1fae5"
            hoverBg="#a7f3d0"
          />
        )}
      </div>

      {/* Confirmation popup */}
      {confirmAction && (
        <>
          <div
            onClick={() => setConfirmAction(null)}
            style={{
              position: 'fixed',
              inset: 0,
              zIndex: 99,
            }}
          />
          <div
            style={{
              position: 'absolute',
              top: 'calc(100% + 8px)',
              right: 0,
              zIndex: 100,
              backgroundColor: '#fff',
              borderRadius: '10px',
              boxShadow: '0 4px 20px rgba(0,0,0,0.15)',
              border: '1px solid #e5e7eb',
              padding: '16px',
              minWidth: '240px',
              animation: 'wb-fadeIn 0.15s ease-out',
            }}
          >
            <p style={{ margin: '0 0 12px', fontSize: '13px', fontWeight: 500, color: '#374151' }}>
              {confirmAction.label}
            </p>
            <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
              <button
                className="wb-action-btn"
                onClick={() => setConfirmAction(null)}
                style={{
                  color: '#6b7280',
                  backgroundColor: '#f3f4f6',
                  border: '1px solid #d1d5db',
                }}
              >
                Anuluj
              </button>
              <button
                className="wb-action-btn"
                onClick={confirmAction.onConfirm}
                style={{
                  color: '#fff',
                  backgroundColor: '#dc2626',
                  border: '1px solid #dc2626',
                }}
              >
                Zakończ
              </button>
            </div>
          </div>
        </>
      )}

      {/* Break type picker popup */}
      {showBreakPicker && (
        <>
          <div
            onClick={() => setShowBreakPicker(false)}
            style={{
              position: 'fixed',
              inset: 0,
              zIndex: 99,
            }}
          />
          <div
            style={{
              position: 'absolute',
              top: 'calc(100% + 8px)',
              right: 0,
              zIndex: 100,
              backgroundColor: '#fff',
              borderRadius: '10px',
              boxShadow: '0 4px 20px rgba(0,0,0,0.15)',
              border: '1px solid #e5e7eb',
              padding: '14px',
              minWidth: '260px',
              animation: 'wb-fadeIn 0.15s ease-out',
            }}
          >
            <p style={{ margin: '0 0 10px', fontSize: '13px', fontWeight: 600, color: '#374151' }}>
              Wybierz typ przerwy
            </p>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {breakAvail?.options.map((opt) => {
                const isPaid = opt.breakType === 'Paid';
                const color = isPaid ? '#059669' : '#d97706';
                const bg = isPaid ? '#d1fae5' : '#fef3c7';
                const disabledBg = '#f3f4f6';
                return (
                  <button
                    key={opt.breakType}
                    className="wb-break-tile"
                    onClick={() => opt.available && handleBreakType(opt.breakType)}
                    disabled={!opt.available || isMutating}
                    style={{
                      display: 'block',
                      width: '100%',
                      textAlign: 'left',
                      borderRadius: '8px',
                      border: `1px solid ${opt.available ? color + '33' : '#d1d5db'}`,
                      backgroundColor: opt.available ? bg : disabledBg,
                      padding: '10px 12px',
                      opacity: opt.available ? 1 : 0.6,
                      cursor: opt.available ? 'pointer' : 'not-allowed',
                      transition: 'transform 0.1s, box-shadow 0.15s',
                    }}
                  >
                    <div style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '6px',
                      color: opt.available ? color : '#9ca3af',
                      fontSize: '13px',
                      fontWeight: 600,
                    }}>
                      <Coffee size={14} /> {opt.label}
                    </div>
                    <div style={{ marginTop: '6px', fontSize: '11px', color: '#6b7280', lineHeight: '1.5' }}>
                      {opt.maxPerDay != null ? (
                        <div>Przerwy: <b>{opt.usedCount}</b> / {opt.maxPerDay}</div>
                      ) : (
                        <div>Przerwy: <b>{opt.usedCount}</b> <span style={{ color: '#9ca3af' }}>(bez limitu)</span></div>
                      )}
                      {opt.maxMinutesPerDay != null ? (
                        <div>Czas: <b>{Math.round(opt.usedMinutesToday)}</b> / {opt.maxMinutesPerDay} min</div>
                      ) : (
                        <div>Czas: <b>{Math.round(opt.usedMinutesToday)}</b> min <span style={{ color: '#9ca3af' }}>(bez limitu)</span></div>
                      )}
                      {opt.maxMinutesPerBreak != null && (
                        <div>Max na przerwe: {opt.maxMinutesPerBreak} min</div>
                      )}
                    </div>
                    {opt.denialReason && (
                      <div style={{ marginTop: '4px', fontSize: '11px', color: '#dc2626', fontWeight: 500 }}>
                        {opt.denialReason}
                      </div>
                    )}
                  </button>
                );
              }) ?? (
                <div style={{ textAlign: 'center', color: '#9ca3af', fontSize: '12px', padding: '8px' }}>
                  <Loader2 size={14} style={{ animation: 'wb-spin 1s linear infinite', display: 'inline-block', marginRight: '4px' }} />
                  Ładowanie...
                </div>
              )}
            </div>
          </div>
        </>
      )}

      {/* Flash message */}
      {flash && (
        <div
          style={{
            position: 'absolute',
            top: 'calc(100% + 6px)',
            right: 0,
            padding: '6px 12px',
            borderRadius: '6px',
            fontSize: '12px',
            fontWeight: 500,
            whiteSpace: 'nowrap',
            zIndex: 50,
            boxShadow: '0 2px 8px rgba(0,0,0,0.12)',
            color: flash.type === 'ok' ? '#065f46' : '#991b1b',
            backgroundColor: flash.type === 'ok' ? '#d1fae5' : '#fee2e2',
            border: `1px solid ${flash.type === 'ok' ? '#6ee7b7' : '#fca5a5'}`,
            animation: 'wb-fadeIn 0.2s ease-out',
          }}
        >
          {flash.msg}
        </div>
      )}

      <style>{`
        @keyframes wb-pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
        @keyframes wb-spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
        @keyframes wb-fadeIn {
          from { opacity: 0; transform: translateY(-4px); }
          to { opacity: 1; transform: translateY(0); }
        }
        .wb-action-btn {
          display: inline-flex;
          align-items: center;
          gap: 4px;
          padding: 4px 10px;
          font-size: 12px;
          font-weight: 600;
          border-radius: 6px;
          white-space: nowrap;
          cursor: pointer;
          transition: background-color 0.15s, transform 0.1s, box-shadow 0.15s;
          outline: none;
        }
        .wb-action-btn:hover:not(:disabled) {
          transform: translateY(-1px);
          box-shadow: 0 2px 6px rgba(0,0,0,0.1);
        }
        .wb-action-btn:active:not(:disabled) {
          transform: scale(0.96);
          box-shadow: none;
        }
        .wb-action-btn:focus-visible {
          box-shadow: 0 0 0 2px rgba(99,102,241,0.5);
        }
        .wb-action-btn:disabled {
          cursor: not-allowed;
          opacity: 0.6;
        }
        .wb-break-tile {
          font-family: inherit;
        }
        .wb-break-tile:hover:not(:disabled) {
          transform: translateY(-1px);
          box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }
        .wb-break-tile:active:not(:disabled) {
          transform: scale(0.98);
          box-shadow: none;
        }
      `}</style>
    </div>
  );
}

interface ActionButtonProps {
  onClick: () => void;
  loading: boolean;
  disabled: boolean;
  icon: React.ReactNode;
  label: string;
  color: string;
  bg: string;
  hoverBg: string;
}

function ActionButton({ onClick, loading, disabled, icon, label, color, bg, hoverBg }: ActionButtonProps) {
  const [hovered, setHovered] = useState(false);

  return (
    <button
      className="wb-action-btn"
      onClick={onClick}
      disabled={disabled}
      title={label}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      style={{
        color,
        backgroundColor: hovered && !disabled ? hoverBg : bg,
        border: `1px solid ${color}33`,
      }}
    >
      {loading ? (
        <Loader2 size={14} style={{ animation: 'wb-spin 1s linear infinite' }} />
      ) : (
        icon
      )}
      {loading ? 'Ładuję...' : label}
    </button>
  );
}
