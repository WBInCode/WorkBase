import { useState, useEffect, useCallback } from 'react';
import { Clock, LogIn, LogOut, Coffee, QrCode, CheckCircle, XCircle } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useTimeStatus, useClockIn, useClockOut, useStartBreak, useEndBreak } from '@/api/hooks/useTimeTracking';

export function KioskPage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const employeeId = user?.employeeId ?? null;

  const { data: timeStatus, isLoading } = useTimeStatus(employeeId ?? undefined);
  const clockIn = useClockIn();
  const clockOut = useClockOut();
  const startBreak = useStartBreak();
  const endBreak = useEndBreak();

  const [now, setNow] = useState(new Date());
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [pin, setPin] = useState('');

  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);

  useEffect(() => {
    if (feedback) {
      const t = setTimeout(() => setFeedback(null), 3000);
      return () => clearTimeout(t);
    }
  }, [feedback]);

  const handleAction = useCallback(async (action: 'clock-in' | 'clock-out' | 'break-start' | 'break-end') => {
    if (!employeeId) return;
    try {
      if (action === 'clock-in') await clockIn.mutateAsync(employeeId);
      if (action === 'clock-out') await clockOut.mutateAsync(employeeId);
      if (action === 'break-start') await startBreak.mutateAsync(employeeId);
      if (action === 'break-end') await endBreak.mutateAsync(employeeId);
      setFeedback({ type: 'success', message: actionLabels[action] + ' — OK' });
    } catch {
      setFeedback({ type: 'error', message: 'Błąd operacji. Spróbuj ponownie.' });
    }
  }, [employeeId, clockIn, clockOut, startBreak, endBreak]);

  const status = timeStatus?.status ?? 'not-started';
  const isPending = clockIn.isPending || clockOut.isPending || startBreak.isPending || endBreak.isPending;

  return (
    <div style={{
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
    }}>
      {/* Clock */}
      <div style={{ fontSize: '72px', fontWeight: 700, fontVariantNumeric: 'tabular-nums', marginBottom: '8px' }}>
        {now.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
      </div>
      <div style={{ fontSize: '18px', color: '#94a3b8', marginBottom: '48px' }}>
        {now.toLocaleDateString('pl-PL', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
      </div>

      {/* Status indicator */}
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

      {/* Action buttons */}
      {isLoading ? (
        <div style={{ color: '#94a3b8', fontSize: '16px' }}>Ładowanie...</div>
      ) : !employeeId ? (
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
            <div style={{ fontSize: '18px', color: '#94a3b8' }}>Dzień zakończony</div>
          )}
        </div>
      )}

      {/* Feedback toast */}
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
        }}>
          {feedback.type === 'success' ? <CheckCircle size={24} /> : <XCircle size={24} />}
          {feedback.message}
        </div>
      )}

      {/* WorkBase branding */}
      <div style={{ position: 'fixed', bottom: '16px', fontSize: '13px', color: '#475569' }}>
        WorkBase Kiosk
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
        width: '140px',
        height: '140px',
        borderRadius: '24px',
        border: 'none',
        backgroundColor: color,
        color: '#fff',
        cursor: disabled ? 'default' : 'pointer',
        opacity: disabled ? 0.6 : 1,
        fontSize: '16px',
        fontWeight: 600,
        gap: '10px',
        transition: 'transform 0.1s, opacity 0.2s',
        touchAction: 'manipulation',
      }}
    >
      <Icon size={40} color="#fff" />
      {label}
    </button>
  );
}

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
