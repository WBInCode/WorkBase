import { AlertCircle, RefreshCw, Inbox } from 'lucide-react';

// ─── Loading Skeleton ─────────────────────────────────────

interface SkeletonProps {
  width?: string;
  height?: string;
  borderRadius?: string;
}

export function Skeleton({ width = '100%', height = '16px', borderRadius = '4px' }: SkeletonProps) {
  return (
    <div
      style={{
        width,
        height,
        borderRadius,
        backgroundColor: '#e5e7eb',
        animation: 'pulse 1.5s ease-in-out infinite',
      }}
    />
  );
}

export function CardSkeleton({ lines = 3 }: { lines?: number }) {
  return (
    <div style={{
      padding: '20px',
      borderRadius: '12px',
      border: '1px solid #e5e7eb',
      backgroundColor: '#fff',
    }}>
      <Skeleton width="40%" height="14px" />
      <div style={{ marginTop: '12px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
        {Array.from({ length: lines }, (_, i) => (
          <Skeleton key={i} width={i === lines - 1 ? '60%' : '100%'} height="12px" />
        ))}
      </div>
    </div>
  );
}

export function TableSkeleton({ rows = 5, cols = 4 }: { rows?: number; cols?: number }) {
  return (
    <div style={{ borderRadius: '8px', border: '1px solid #e5e7eb', overflow: 'hidden' }}>
      {/* Header */}
      <div style={{ display: 'grid', gridTemplateColumns: `repeat(${cols}, 1fr)`, gap: '12px', padding: '12px 16px', backgroundColor: '#f9fafb' }}>
        {Array.from({ length: cols }, (_, i) => (
          <Skeleton key={i} width="70%" height="12px" />
        ))}
      </div>
      {/* Rows */}
      {Array.from({ length: rows }, (_, r) => (
        <div key={r} style={{ display: 'grid', gridTemplateColumns: `repeat(${cols}, 1fr)`, gap: '12px', padding: '12px 16px', borderTop: '1px solid #f3f4f6' }}>
          {Array.from({ length: cols }, (_, c) => (
            <Skeleton key={c} width={c === 0 ? '80%' : '50%'} height="12px" />
          ))}
        </div>
      ))}
    </div>
  );
}

// ─── Empty State ──────────────────────────────────────────

interface EmptyStateProps {
  title?: string;
  description?: string;
  action?: { label: string; onClick: () => void };
}

export function EmptyState({
  title = 'Brak danych',
  description = 'Nie znaleziono elementów do wyświetlenia.',
  action,
}: EmptyStateProps) {
  return (
    <div style={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '40px 24px',
      textAlign: 'center',
      borderRadius: '8px',
      border: '1px dashed #d1d5db',
      backgroundColor: '#f9fafb',
    }}>
      <Inbox size={36} color="#d1d5db" />
      <div style={{ marginTop: '12px', fontSize: '15px', fontWeight: 600, color: '#374151' }}>
        {title}
      </div>
      <div style={{ marginTop: '4px', fontSize: '13px', color: '#9ca3af', maxWidth: '300px' }}>
        {description}
      </div>
      {action && (
        <button
          onClick={action.onClick}
          style={{
            marginTop: '16px',
            padding: '8px 16px',
            fontSize: '13px',
            fontWeight: 500,
            color: '#2563eb',
            backgroundColor: '#eff6ff',
            border: '1px solid #bfdbfe',
            borderRadius: '6px',
            cursor: 'pointer',
          }}
        >
          {action.label}
        </button>
      )}
    </div>
  );
}

// ─── Error State ──────────────────────────────────────────

interface ErrorStateProps {
  message?: string;
  onRetry?: () => void;
}

export function ErrorState({
  message = 'Wystąpił błąd podczas ładowania danych.',
  onRetry,
}: ErrorStateProps) {
  return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      gap: '12px',
      padding: '16px',
      borderRadius: '8px',
      backgroundColor: '#fef2f2',
      border: '1px solid #fecaca',
    }}>
      <AlertCircle size={20} color="#dc2626" style={{ flexShrink: 0 }} />
      <div style={{ flex: 1 }}>
        <div style={{ fontSize: '14px', color: '#dc2626' }}>{message}</div>
      </div>
      {onRetry && (
        <button
          onClick={onRetry}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '4px',
            padding: '6px 12px',
            fontSize: '13px',
            fontWeight: 500,
            color: '#dc2626',
            backgroundColor: '#fff',
            border: '1px solid #fecaca',
            borderRadius: '6px',
            cursor: 'pointer',
            flexShrink: 0,
          }}
        >
          <RefreshCw size={14} />
          Ponów
        </button>
      )}
    </div>
  );
}

// ─── CSS keyframes injection ────────────────────────────────

const styleId = 'workbase-skeleton-styles';
if (typeof document !== 'undefined' && !document.getElementById(styleId)) {
  const style = document.createElement('style');
  style.id = styleId;
  style.textContent = `
    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.5; }
    }
  `;
  document.head.appendChild(style);
}
