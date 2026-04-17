import { useState, useEffect, useCallback, createContext, useContext, type ReactNode } from 'react';
import { X, Bell, CheckCircle, AlertCircle, Info } from 'lucide-react';

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
        zIndex: 9999,
        display: 'flex',
        flexDirection: 'column',
        gap: '8px',
        maxWidth: '380px',
        width: '100%',
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
  useEffect(() => {
    const duration = toast.duration ?? 5000;
    const timer = setTimeout(() => onRemove(toast.id), duration);
    return () => clearTimeout(timer);
  }, [toast.id, toast.duration, onRemove]);

  const config = toastStyles[toast.type];

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: '10px',
        padding: '12px 14px',
        borderRadius: '10px',
        backgroundColor: '#ffffff',
        border: `1px solid ${config.border}`,
        boxShadow: '0 4px 12px rgba(0,0,0,0.12)',
        pointerEvents: 'auto',
        animation: 'slideIn 0.25s ease-out',
      }}
    >
      <config.icon size={18} color={config.iconColor} style={{ flexShrink: 0, marginTop: '2px' }} />
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontSize: '14px', fontWeight: 600, color: '#111827' }}>{toast.title}</div>
        {toast.message && (
          <div style={{ fontSize: '13px', color: '#6b7280', marginTop: '2px', lineHeight: 1.4 }}>
            {toast.message}
          </div>
        )}
      </div>
      <button
        onClick={() => onRemove(toast.id)}
        style={{
          background: 'none', border: 'none', cursor: 'pointer',
          color: '#9ca3af', padding: '2px', flexShrink: 0,
        }}
      >
        <X size={14} />
      </button>
    </div>
  );
}

const toastStyles = {
  info: { icon: Info, iconColor: '#2563eb', border: '#bfdbfe' },
  success: { icon: CheckCircle, iconColor: '#16a34a', border: '#bbf7d0' },
  error: { icon: AlertCircle, iconColor: '#dc2626', border: '#fecaca' },
  notification: { icon: Bell, iconColor: '#7c3aed', border: '#ddd6fe' },
};

// ─── CSS keyframes ──────────────────────────────────────────

const animId = 'workbase-toast-anim';
if (typeof document !== 'undefined' && !document.getElementById(animId)) {
  const style = document.createElement('style');
  style.id = animId;
  style.textContent = `
    @keyframes slideIn {
      from { opacity: 0; transform: translateX(20px); }
      to { opacity: 1; transform: translateX(0); }
    }
  `;
  document.head.appendChild(style);
}
