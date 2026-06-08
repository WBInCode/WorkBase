import { useState, useEffect, useCallback, useRef } from 'react';
import { LogIn, LogOut, Coffee, CheckCircle, XCircle, User, ArrowLeft, Monitor, ScanLine } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useTimeStatus, useClockIn, useClockOut, useStartBreak, useEndBreak } from '@/api/hooks/useTimeTracking';
import { QrScanner } from '@/components/QrScanner';
import { KioskQrDisplay } from '@/components/TimeTracking/KioskQrDisplay';
import { useKioskHeartbeat } from '@/components/TimeTracking/useKioskHeartbeat';
import { api } from '@/api/client';

type KioskMode = 'badge' | 'qr-display' | 'qr-scan' | 'clock';

interface KioskEmployee {
  id: string;
  firstName: string;
  lastName: string;
  employeeNumber: string;
}

export function KioskPage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const kioskLocation = user?.kioskLocation ?? null;
  const isKioskAccount = user?.roles.includes('workbase-kiosk') ?? false;

  // If logged in as a regular user (not kiosk), use their employeeId directly
  const directEmployeeId = !isKioskAccount ? (user?.employeeId ?? null) : null;

  const [mode, setMode] = useState<KioskMode>('badge');
  const [identifiedEmployee, setIdentifiedEmployee] = useState<KioskEmployee | null>(null);
  const [badgeInput, setBadgeInput] = useState('');
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [lookupLoading, setLookupLoading] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const activeEmployeeId = directEmployeeId ?? identifiedEmployee?.id ?? undefined;

  const { data: timeStatus, isLoading } = useTimeStatus(activeEmployeeId);
  const clockIn = useClockIn();
  const clockOut = useClockOut();
  const startBreak = useStartBreak();
  const endBreak = useEndBreak();

  const [now, setNow] = useState(new Date());
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [showQrScanner, setShowQrScanner] = useState(false);

  // Kiosk heartbeat: auto-refresh, reset on idle
  useKioskHeartbeat({
    heartbeatInterval: 30_000,
    fullRefreshInterval: 5 * 60_000,
    idleResetDelay: isKioskAccount ? 30_000 : 60_000,
    onIdle: () => {
      if (isKioskAccount) {
        setIdentifiedEmployee(null);
        setBadgeInput('');
        setLookupError(null);
        setMode('badge');
      }
      setShowQrScanner(false);
      setFeedback(null);
    },
  });

  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);

  useEffect(() => {
    if (feedback) {
      const t = setTimeout(() => setFeedback(null), 4000);
      return () => clearTimeout(t);
    }
  }, [feedback]);

  // Auto-focus badge input
  useEffect(() => {
    if (isKioskAccount && !identifiedEmployee && inputRef.current) {
      inputRef.current.focus();
    }
  }, [isKioskAccount, identifiedEmployee]);

  const lookupEmployee = useCallback(async (badgeNumber: string, retryCount = 0) => {
    if (!badgeNumber.trim()) return;
    setLookupLoading(true);
    setLookupError(null);
    try {
      const emp = await api.get<KioskEmployee>(
        `/api/org/employees/by-number/${encodeURIComponent(badgeNumber.trim())}`
      );
      setIdentifiedEmployee(emp);
      setBadgeInput('');
    } catch (err) {
      // Fallback: try lookup by employee ID (QR may contain GUID)
      const isGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(badgeNumber.trim());
      if (isGuid) {
        try {
          const emp = await api.get<KioskEmployee>(
            `/api/org/employees/${encodeURIComponent(badgeNumber.trim())}`
          );
          setIdentifiedEmployee(emp);
          setBadgeInput('');
          return;
        } catch { /* fall through to error */ }
      }
      // Retry once on network/server errors (API cold start on free tier)
      if (retryCount < 1 && err instanceof Error && !('status' in err && (err as { status: number }).status === 404)) {
        await new Promise(r => setTimeout(r, 2000));
        setLookupLoading(false);
        return lookupEmployee(badgeNumber, retryCount + 1);
      }
      setLookupError('Nie znaleziono pracownika o tym numerze.');
      setBadgeInput('');
    } finally {
      setLookupLoading(false);
    }
  }, []);

  const handleBadgeSubmit = useCallback((e: React.FormEvent) => {
    e.preventDefault();
    lookupEmployee(badgeInput);
  }, [badgeInput, lookupEmployee]);

  // Handle QR scan — could be an employee number or a QR token
  const handleQrScan = useCallback(async (value: string) => {
    // First try employee number lookup
    await lookupEmployee(value);
  }, [lookupEmployee]);

  const handleAction = useCallback(async (action: 'clock-in' | 'clock-out' | 'break-start' | 'break-end') => {
    if (!activeEmployeeId) return;
    try {
      const request = { employeeId: activeEmployeeId, note: kioskLocation ? `Kiosk: ${kioskLocation}` : 'Kiosk' };
      if (action === 'clock-in') await clockIn.mutateAsync(request);
      if (action === 'clock-out') await clockOut.mutateAsync(request);
      if (action === 'break-start') await startBreak.mutateAsync({ ...request, breakType: 'Paid' });
      if (action === 'break-end') await endBreak.mutateAsync(request);
      setFeedback({ type: 'success', message: actionLabels[action] + ' — OK' });
      // After action, return to badge screen after delay (kiosk mode)
      if (isKioskAccount) {
        setTimeout(() => {
          setIdentifiedEmployee(null);
          setBadgeInput('');
        }, 3000);
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Błąd operacji.';
      setFeedback({ type: 'error', message: msg });
    }
  }, [activeEmployeeId, clockIn, clockOut, startBreak, endBreak, kioskLocation, isKioskAccount]);

  const status = timeStatus?.status ?? 'not-started';
  const isPending = clockIn.isPending || clockOut.isPending || startBreak.isPending || endBreak.isPending;

  // --- KIOSK MODE: identification screen ---
  if (isKioskAccount && !identifiedEmployee) {
    return (
      <div style={containerStyle}>
        <div style={clockStyle}>
          {now.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
        </div>
        <div style={dateStyle}>
          {now.toLocaleDateString('pl-PL', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
        </div>

        {/* Mode tabs */}
        <div style={{ display: 'flex', gap: '8px', marginTop: '32px', marginBottom: '32px' }}>
          <ModeTab icon={User} label="Numer" active={mode === 'badge'} onClick={() => setMode('badge')} />
          <ModeTab icon={Monitor} label="Pokaż QR" active={mode === 'qr-display'} onClick={() => setMode('qr-display')} />
          <ModeTab icon={ScanLine} label="Skanuj QR" active={mode === 'qr-scan'} onClick={() => setMode('qr-scan')} />
        </div>

        {/* Mode: Badge / PIN entry */}
        {mode === 'badge' && (
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '18px', color: '#94a3b8', marginBottom: '24px' }}>
              Wpisz numer identyfikacyjny
            </div>
            <form onSubmit={handleBadgeSubmit} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '16px' }}>
              <input
                ref={inputRef}
                type="text"
                value={badgeInput}
                onChange={(e) => { setBadgeInput(e.target.value); setLookupError(null); }}
                placeholder="Numer pracownika..."
                autoFocus
                style={badgeInputStyle(!!lookupError)}
              />
              {lookupError && (
                <div style={{ color: '#f87171', fontSize: '16px', fontWeight: 500 }}>{lookupError}</div>
              )}
              <button type="submit" disabled={!badgeInput.trim() || lookupLoading} style={submitButtonStyle(!!badgeInput.trim(), lookupLoading)}>
                {lookupLoading ? 'Szukam...' : 'Potwierdź'}
              </button>
            </form>
          </div>
        )}

        {/* Mode: QR display (kiosk shows rotating QR, employee scans with phone) */}
        {mode === 'qr-display' && (
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '18px', color: '#94a3b8', marginBottom: '24px' }}>
              Zeskanuj ten kod swoim telefonem
            </div>
            <KioskQrDisplay locationId={kioskLocation ?? undefined} ttlSeconds={25} />
          </div>
        )}

        {/* Mode: QR scan (kiosk scans employee's QR badge from phone) */}
        {mode === 'qr-scan' && (
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '18px', color: '#94a3b8', marginBottom: '24px' }}>
              Pokaż swój kod QR do kamery
            </div>
            <QrScanner
              facingMode="user"
              onScan={(value) => {
                handleQrScan(value);
                setMode('badge');
              }}
              onClose={() => setMode('badge')}
            />
          </div>
        )}

        <KioskFooter kioskLocation={kioskLocation} />
      </div>
    );
  }

  // --- CLOCK IN/OUT screen ---
  const employeeName = identifiedEmployee
    ? `${identifiedEmployee.firstName} ${identifiedEmployee.lastName}`
    : user?.name ?? '';

  return (
    <div style={containerStyle}>
      {isKioskAccount && identifiedEmployee && (
        <button
          onClick={() => { setIdentifiedEmployee(null); setBadgeInput(''); setFeedback(null); }}
          style={{
            position: 'fixed',
            top: '24px',
            left: '24px',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            padding: '12px 20px',
            borderRadius: '12px',
            border: '1px solid #334155',
            backgroundColor: 'transparent',
            color: '#94a3b8',
            fontSize: '15px',
            fontWeight: 500,
            cursor: 'pointer',
          }}
        >
          <ArrowLeft size={18} />
          Zmień osobę
        </button>
      )}

      <div style={clockStyle}>
        {now.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
      </div>
      <div style={dateStyle}>
        {now.toLocaleDateString('pl-PL', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
      </div>

      <div style={{ fontSize: '24px', fontWeight: 600, marginBottom: '8px', marginTop: '24px' }}>
        {employeeName}
      </div>
      {identifiedEmployee?.employeeNumber && (
        <div style={{ fontSize: '14px', color: '#64748b', marginBottom: '32px' }}>
          Nr: {identifiedEmployee.employeeNumber}
        </div>
      )}

      <div style={{
        padding: '8px 24px',
        borderRadius: '9999px',
        fontSize: '16px',
        fontWeight: 600,
        marginBottom: '40px',
        ...statusBadgeStyle[status] ?? { backgroundColor: '#334155', color: '#94a3b8' },
      }}>
        {statusLabels[status] ?? 'Nieznany'}
      </div>

      {isLoading ? (
        <div style={{ color: '#94a3b8', fontSize: '16px' }}>Ładowanie...</div>
      ) : !activeEmployeeId ? (
        <div style={{ color: '#f87171', fontSize: '16px' }}>Brak przypisanego pracownika</div>
      ) : (
        <div style={{ display: 'flex', gap: '20px', flexWrap: 'wrap', justifyContent: 'center' }}>
          {status === 'not-started' && (
            <KioskButton icon={LogIn} label="Rozpocznij" color="#22c55e" onClick={() => handleAction('clock-in')} disabled={isPending} />
          )}
          {status === 'working' && (
            <>
              <KioskButton icon={Coffee} label="Przerwa" color="#f59e0b" onClick={() => handleAction('break-start')} disabled={isPending} />
              <KioskButton icon={LogOut} label="Zakończ" color="#ef4444" onClick={() => handleAction('clock-out')} disabled={isPending} />
            </>
          )}
          {status === 'on-break' && (
            <KioskButton icon={Coffee} label="Wznów" color="#3b82f6" onClick={() => handleAction('break-end')} disabled={isPending} />
          )}
          {status === 'ended' && (
            <>
              <div style={{ fontSize: '18px', color: '#94a3b8', width: '100%', textAlign: 'center', marginBottom: '12px' }}>Dzień zakończony ✓</div>
              <KioskButton icon={LogIn} label="Rozpocznij ponownie" color="#22c55e" onClick={() => handleAction('clock-in')} disabled={isPending} />
            </>
          )}
        </div>
      )}

      {feedback && (
        <div style={{
          position: 'fixed',
          bottom: '40px',
          left: '50%',
          transform: 'translateX(-50%)',
          display: 'flex',
          alignItems: 'center',
          gap: '10px',
          padding: '16px 28px',
          borderRadius: '12px',
          fontSize: '18px',
          fontWeight: 600,
          backgroundColor: feedback.type === 'success' ? '#166534' : '#991b1b',
          color: '#fff',
          boxShadow: '0 8px 24px rgba(0,0,0,0.3)',
          zIndex: 50,
        }}>
          {feedback.type === 'success' ? <CheckCircle size={24} /> : <XCircle size={24} />}
          {feedback.message}
        </div>
      )}

      {showQrScanner && (
        <QrScanner
          onScan={(value) => {
            setShowQrScanner(false);
            setFeedback({ type: 'success', message: `Zeskanowano: ${value}` });
          }}
          onClose={() => setShowQrScanner(false)}
        />
      )}

      <KioskFooter kioskLocation={kioskLocation} />
    </div>
  );
}

// --- Sub-components ---

function ModeTab({ icon: Icon, label, active, onClick }: {
  icon: React.ComponentType<{ size?: number; color?: string }>;
  label: string;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: '8px',
        padding: '12px 24px',
        borderRadius: '12px',
        border: active ? '2px solid #6366f1' : '2px solid #334155',
        backgroundColor: active ? '#6366f1' : 'transparent',
        color: active ? '#fff' : '#94a3b8',
        fontSize: '15px',
        fontWeight: 600,
        cursor: 'pointer',
        transition: 'all 0.15s',
      }}
    >
      <Icon size={20} />
      {label}
    </button>
  );
}

function KioskFooter({ kioskLocation }: { kioskLocation: string | null }) {
  const auth = useAuth();
  return (
    <div style={{ position: 'fixed', bottom: '16px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px' }}>
      <button
        onClick={() => auth.signoutRedirect()}
        style={{
          padding: '8px 20px',
          borderRadius: '8px',
          border: '1px solid #334155',
          backgroundColor: 'transparent',
          color: '#64748b',
          fontSize: '13px',
          cursor: 'pointer',
        }}
      >
        <LogOut size={14} style={{ marginRight: '6px', verticalAlign: 'middle' }} />
        Wyloguj
      </button>
      <div style={{ fontSize: '13px', color: '#475569', textAlign: 'center' }}>
        WorkBase Kiosk{kioskLocation ? ` — ${kioskLocation}` : ''}
      </div>
    </div>
  );
}

function KioskButton({ icon: Icon, label, color, onClick, disabled }: {
  icon: React.ComponentType<{ size?: number; color?: string }>;
  label: string;
  color: string;
  onClick: () => void;
  disabled: boolean;
}) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        width: '160px',
        height: '160px',
        borderRadius: '24px',
        border: 'none',
        backgroundColor: color,
        color: '#fff',
        cursor: disabled ? 'default' : 'pointer',
        opacity: disabled ? 0.6 : 1,
        fontSize: '18px',
        fontWeight: 600,
        gap: '12px',
        transition: 'transform 0.1s, opacity 0.2s',
        touchAction: 'manipulation',
      }}
    >
      <Icon size={48} color="#fff" />
      {label}
    </button>
  );
}

// --- Styles & constants ---

const containerStyle: React.CSSProperties = {
  minHeight: '100vh',
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  justifyContent: 'center',
  backgroundColor: '#0f172a',
  color: '#ffffff',
  fontFamily: 'system-ui, -apple-system, sans-serif',
  padding: '24px',
  userSelect: 'none',
};

const clockStyle: React.CSSProperties = {
  fontSize: '80px',
  fontWeight: 700,
  fontVariantNumeric: 'tabular-nums',
  marginBottom: '8px',
};

const dateStyle: React.CSSProperties = {
  fontSize: '18px',
  color: '#94a3b8',
  marginBottom: '16px',
};

const statusLabels: Record<string, string> = {
  'not-started': 'Nie rozpoczęto',
  working: 'W pracy',
  'on-break': 'Na przerwie',
  ended: 'Zakończono',
};

const statusBadgeStyle: Record<string, React.CSSProperties> = {
  'not-started': { backgroundColor: '#334155', color: '#94a3b8' },
  working: { backgroundColor: '#166534', color: '#dcfce7' },
  'on-break': { backgroundColor: '#92400e', color: '#fef3c7' },
  ended: { backgroundColor: '#1e40af', color: '#dbeafe' },
};

const actionLabels: Record<string, string> = {
  'clock-in': 'Rozpoczęto pracę',
  'clock-out': 'Zakończono pracę',
  'break-start': 'Rozpoczęto przerwę',
  'break-end': 'Wznowiono pracę',
};

function badgeInputStyle(hasError: boolean): React.CSSProperties {
  return {
    width: '320px',
    maxWidth: '90vw',
    padding: '18px 24px',
    fontSize: '28px',
    fontWeight: 600,
    fontVariantNumeric: 'tabular-nums',
    textAlign: 'center',
    borderRadius: '16px',
    border: hasError ? '2px solid #ef4444' : '2px solid #334155',
    backgroundColor: '#1e293b',
    color: '#fff',
    outline: 'none',
    letterSpacing: '4px',
  };
}

function submitButtonStyle(hasValue: boolean, loading: boolean): React.CSSProperties {
  return {
    padding: '16px 48px',
    fontSize: '18px',
    fontWeight: 600,
    borderRadius: '16px',
    border: 'none',
    backgroundColor: hasValue ? '#6366f1' : '#334155',
    color: '#fff',
    cursor: hasValue ? 'pointer' : 'default',
    opacity: loading ? 0.6 : 1,
    transition: 'background-color 0.2s',
  };
}
