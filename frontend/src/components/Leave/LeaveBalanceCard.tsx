import type { LeaveBalanceDto } from '@/api/types/leave';
import { colors } from '@/theme/tokens';

interface LeaveBalanceCardProps {
  balances: LeaveBalanceDto[];
  isLoading: boolean;
}

const DEFAULT_COLORS: Record<string, string> = {
  ANNUAL: colors.primary[500],
  ON_DEMAND: colors.warning[500],
  SICK: colors.danger[500],
  CHILDCARE: '#8b5cf6',
};

function getColor(balance: LeaveBalanceDto): string {
  return balance.leaveTypeColor ?? DEFAULT_COLORS[balance.leaveTypeCode] ?? colors.gray[500];
}

export function LeaveBalanceCard({ balances, isLoading }: LeaveBalanceCardProps) {
  if (isLoading) {
    return (
      <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            style={{
              width: '180px',
              maxWidth: '100%',
              height: '100px',
              borderRadius: '10px',
              backgroundColor: colors.gray[100],
              animation: 'pulse 1.5s infinite',
            }}
          />
        ))}
      </div>
    );
  }

  if (balances.length === 0) {
    return (
      <div
        style={{
          padding: '24px',
          textAlign: 'center',
          color: colors.gray[500],
          fontSize: '14px',
          backgroundColor: colors.gray[50],
          borderRadius: '10px',
          border: `1px dashed ${colors.gray[300]}`,
        }}
      >
        Brak naliczonego salda urlopowego na ten rok.
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
      {balances.map((b) => {
        const color = getColor(b);
        const usedPercent = b.totalDays + b.carriedOverDays > 0
          ? Math.round(((b.usedDays + b.pendingDays) / (b.totalDays + b.carriedOverDays)) * 100)
          : 0;

        return (
          <div
            key={b.id}
            style={{
              minWidth: '180px',
              padding: '16px',
              borderRadius: '10px',
              backgroundColor: colors.white,
              border: `1px solid ${color}33`,
              boxShadow: '0 1px 3px rgba(0,0,0,0.06)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '10px' }}>
              <div
                style={{
                  width: '10px',
                  height: '10px',
                  borderRadius: '3px',
                  backgroundColor: color,
                  flexShrink: 0,
                }}
              />
              <span style={{ fontSize: '13px', fontWeight: 600, color: colors.gray[700] }}>
                {b.leaveTypeName}
              </span>
            </div>

            <div style={{ fontSize: '28px', fontWeight: 700, color: color, lineHeight: 1.1 }}>
              {b.remainingDays}
              <span style={{ fontSize: '13px', fontWeight: 400, color: colors.gray[400], marginLeft: '4px' }}>
                / {b.totalDays + b.carriedOverDays}
              </span>
            </div>
            <div style={{ fontSize: '11px', color: colors.gray[400], marginTop: '2px' }}>
              dni pozostało
            </div>

            {/* Progress bar */}
            <div
              style={{
                marginTop: '10px',
                height: '4px',
                borderRadius: '2px',
                backgroundColor: `${color}1a`,
                overflow: 'hidden',
              }}
            >
              <div
                style={{
                  height: '100%',
                  width: `${Math.min(usedPercent, 100)}%`,
                  borderRadius: '2px',
                  backgroundColor: color,
                  transition: 'width 0.3s ease',
                }}
              />
            </div>

            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                marginTop: '6px',
                fontSize: '11px',
                color: colors.gray[400],
              }}
            >
              <span>Wykorzystano: {b.usedDays}</span>
              {b.pendingDays > 0 && (
                <span style={{ color: colors.warning[500] }}>Oczekuje: {b.pendingDays}</span>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
