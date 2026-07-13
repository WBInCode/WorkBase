import { useState, useEffect, useCallback, createContext, useContext, type ReactNode } from 'react';
import { X, Bell, CheckCircle, AlertCircle, Info } from 'lucide-react';
import { colors } from '@/theme/tokens';

// ─── Types ──────────────────────────────────────────────────

interface Toast {
  id: string;
  type: 'info' | 'success' | 'error' | 'notification';
  title: string;
  message?: string;
  duration?: number;
}

interface ToastContextValue {
  addToast: (toast: Omit<Toast, 'id'>) => void;
}

// ─── Context ────────────────────────────────────────────────

const ToastContext = createContext<ToastContextValue>({ addToast: () => {} });

export function useToast() {
  return useContext(ToastContext);
}

// ─── Provider ───────────────────────────────────────────────

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const addToast = useCallback((toast: Omit<Toast, 'id'>) => {
    const id = crypto.randomUUID();
    setToasts((prev) => [...prev, { ...toast, id }]);
  }, []);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  return (
    <ToastContext.Provider value={{ addToast }}>
      {children}
      <ToastContainer toasts={toasts} onRemove={removeToast} />
    </ToastContext.Provider>
  );
}

// ─── Container ──────────────────────────────────────────────

function ToastContainer({ toasts, onRemove }: { toasts: Toast[]; onRemove: (id: string) => void }) {
  return (
    <div
      style={{
        position: 'fixed',
        top: '16px',
        right: '16px',
        left: '16px',
        zIndex: 9999,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'flex-end',
        gap: '8px',
        pointerEvents: 'none',
      }}
    >
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} onRemove={onRemove} />
      ))}
    </div>
  );
}

// ─── Single Toast ───────────────────────────────────────────

function ToastItem({ toast, onRemove }: { toast: Toast; onRemove: (id: string) => void }) {
  const [paused, setPaused] = useState(false);
  const duration = toast.duration ?? 5000;

  // Autodismiss z pauzą na hover (timer restartuje po zjechaniu myszą)
  useEffect(() => {
    if (paused) return;
    const timer = setTimeout(() => onRemove(toast.id), duration);
    return () => clearTimeout(timer);
  }, [toast.id, duration, onRemove, paused]);

  const config = toastStyles[toast.type];

  return (
    <div
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      style={{
        position: 'relative',
        overflow: 'hidden',
        display: 'flex',
        alignItems: 'flex-start',
        gap: '10px',
        padding: '13px 15px 15px',
        borderRadius: '14px',
        backgroundColor: colors.white,
        border: `1px solid ${config.border}`,
        boxShadow: '0 12px 32px -8px rgba(20,25,43,0.18), 0 0 0 1px rgba(20,25,43,0.03)',
        pointerEvents: 'auto',
        animation: 'slideIn 0.3s cubic-bezier(0.22, 1, 0.36, 1)',
        maxWidth: '380px',
        width: '100%',
      }}
    >
      <span style={{
        width: 30, height: 30, borderRadius: 9, backgroundColor: config.iconBg,
        display: 'inline-flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
      }}>
        <config.icon size={16} color={config.iconColor} />
      </span>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontSize: '13.5px', fontWeight: 700, color: colors.gray[900] }}>{toast.title}</div>
        {toast.message && (
          <div style={{ fontSize: '12.5px', color: colors.gray[500], marginTop: '2px', lineHeight: 1.45 }}>
            {toast.message}
          </div>
        )}
      </div>
      <button
        onClick={() => onRemove(toast.id)}
        aria-label="Zamknij powiadomienie"
        style={{
          background: 'none', border: 'none', cursor: 'pointer',
          color: colors.gray[400], padding: '2px', flexShrink: 0,
        }}
      >
        <X size={14} />
      </button>

      {/* Pasek postępu do auto-zamknięcia (pauzuje na hover) */}
      <div aria-hidden style={{
        position: 'absolute', left: 0, right: 0, bottom: 0, height: 3,
        backgroundColor: `${config.iconColor}22`,
      }}>
        <div style={{
          height: '100%',
          backgroundColor: config.iconColor,
          transformOrigin: 'left',
          animation: `wb-toast-run ${duration}ms linear forwards`,
          animationPlayState: paused ? 'paused' : 'running',
        }} />
      </div>
    </div>
  );
}

const toastStyles = {
  info: { icon: Info, iconColor: colors.primary[600], iconBg: colors.primary[100], border: colors.primary[200] },
  success: { icon: CheckCircle, iconColor: colors.success[600], iconBg: colors.success[100], border: colors.success[200] },
  error: { icon: AlertCircle, iconColor: colors.danger[600], iconBg: colors.danger[100], border: colors.danger[200] },
  notification: { icon: Bell, iconColor: '#7c3aed', iconBg: '#f3e8ff', border: '#ddd6fe' },
};

// Keyframes (slideIn, wb-toast-run) są zdefiniowane globalnie w theme/workbase.css.
