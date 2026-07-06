import { AlertCircle, RefreshCw, Inbox } from 'lucide-react';
import { colors } from '@/theme/tokens';

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
        backgroundColor: colors.gray[200],
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
      border: `1px solid ${colors.gray[200]}`,
      backgroundColor: colors.white,
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
    <div style={{ borderRadius: '8px', border: `1px solid ${colors.gray[200]}`, overflow: 'hidden' }}>
      {/* Header */}
      <div style={{ display: 'grid', gridTemplateColumns: `repeat(${cols}, 1fr)`, gap: '12px', padding: '12px 16px', backgroundColor: colors.gray[50] }}>
        {Array.from({ length: cols }, (_, i) => (
          <Skeleton key={i} width="70%" height="12px" />
        ))}
      </div>
      {/* Rows */}
      {Array.from({ length: rows }, (_, r) => (
        <div key={r} style={{ display: 'grid', gridTemplateColumns: `repeat(${cols}, 1fr)`, gap: '12px', padding: '12px 16px', borderTop: `1px solid ${colors.gray[100]}` }}>
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
      border: `1px dashed ${colors.gray[300]}`,
      backgroundColor: colors.gray[50],
    }}>
      <Inbox size={36} color={colors.gray[300]} />
      <div style={{ marginTop: '12px', fontSize: '15px', fontWeight: 600, color: colors.gray[700] }}>
        {title}
      </div>
      <div style={{ marginTop: '4px', fontSize: '13px', color: colors.gray[400], maxWidth: '300px' }}>
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
            color: colors.primary[600],
            backgroundColor: colors.primary[50],
            border: `1px solid ${colors.primary[200]}`,
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
      backgroundColor: colors.danger[50],
      border: `1px solid ${colors.danger[200]}`,
    }}>
      <AlertCircle size={20} color={colors.danger[600]} style={{ flexShrink: 0 }} />
      <div style={{ flex: 1 }}>
        <div style={{ fontSize: '14px', color: colors.danger[600] }}>{message}</div>
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
            color: colors.danger[600],
            backgroundColor: colors.white,
            border: `1px solid ${colors.danger[200]}`,
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
