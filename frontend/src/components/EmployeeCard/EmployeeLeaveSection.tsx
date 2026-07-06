import type { LeaveBalanceDto, LeaveRequestDto, LeaveRequestStatus } from '@/api/types/leave';
import { colors, typography, statusColors } from '@/theme/tokens';

interface Props {
  balances: LeaveBalanceDto[];
  requests: LeaveRequestDto[];
  isLoading: boolean;
}

const STATUS_LABELS: Record<LeaveRequestStatus, string> = {
  Draft: 'Szkic',
  Pending: 'Oczekujący',
  Approved: 'Zatwierdzony',
  Rejected: 'Odrzucony',
  Cancelled: 'Anulowany',
};

const STATUS_COLORS: Record<LeaveRequestStatus, { bg: string; text: string }> = {
  Draft: { bg: statusColors.leave.draft.bg, text: statusColors.leave.draft.text },
  Pending: { bg: statusColors.leave.pending.bg, text: statusColors.leave.pending.text },
  Approved: { bg: statusColors.leave.approved.bg, text: statusColors.leave.approved.text },
  Rejected: { bg: statusColors.leave.rejected.bg, text: statusColors.leave.rejected.text },
  Cancelled: { bg: statusColors.leave.cancelled.bg, text: statusColors.leave.cancelled.text },
};

const DEFAULT_COLORS: Record<string, string> = {
  ANNUAL: colors.primary[500],
  ON_DEMAND: colors.warning[500],
  SICK: colors.danger[500],
  CHILDCARE: '#8b5cf6',
};

export function EmployeeLeaveSection({ balances, requests, isLoading }: Props) {
  if (isLoading) {
    return (
      <div style={cardStyle}>
        <h3 style={headingStyle}>Urlopy</h3>
        <div style={{ color: colors.gray[400], fontSize: typography.fontSize.base }}>Ładowanie...</div>
      </div>
    );
  }

  return (
    <div style={cardStyle}>
      <h3 style={headingStyle}>Urlopy</h3>

      {/* Balances */}
      {balances.length > 0 ? (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: '10px', marginBottom: '20px' }}>
          {balances.map((b) => {
            const color = b.leaveTypeColor || DEFAULT_COLORS[b.leaveTypeCode] || colors.gray[500];
            const pct = b.totalDays > 0 ? Math.round((b.usedDays / b.totalDays) * 100) : 0;
            return (
              <div key={b.id} style={{ padding: '12px', borderRadius: '8px', border: `1px solid ${colors.gray[200]}`, backgroundColor: colors.gray[50] }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '6px' }}>
                  <span style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: color }} />
                  <span style={{ fontSize: '13px', fontWeight: typography.fontWeight.semibold, color: colors.gray[700] }}>{b.leaveTypeName}</span>
                </div>
                <div style={{ fontSize: typography.fontSize['2xl'], fontWeight: typography.fontWeight.bold, color: colors.gray[900] }}>
                  {b.remainingDays} <span style={{ fontSize: typography.fontSize.sm, fontWeight: typography.fontWeight.normal, color: colors.gray[500] }}>/ {b.totalDays} dni</span>
                </div>
                {/* Progress bar */}
                <div style={{ marginTop: '6px', height: '4px', borderRadius: '2px', backgroundColor: colors.gray[200] }}>
                  <div style={{ height: '100%', borderRadius: '2px', backgroundColor: color, width: `${Math.min(pct, 100)}%`, transition: 'width 0.3s' }} />
                </div>
                <div style={{ fontSize: typography.fontSize.xs, color: colors.gray[400], marginTop: '4px' }}>
                  Wykorzystane: {b.usedDays} · Oczekujące: {b.pendingDays}
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div style={{ color: colors.gray[400], fontSize: typography.fontSize.base, marginBottom: '16px' }}>Brak danych o saldach urlopowych.</div>
      )}

      {/* Recent requests */}
      <h4 style={{ margin: '0 0 8px', fontSize: typography.fontSize.base, fontWeight: typography.fontWeight.semibold, color: colors.gray[700] }}>Ostatnie wnioski</h4>
      {requests.length > 0 ? (
        <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
          <thead>
            <tr style={{ borderBottom: `1px solid ${colors.gray[200]}` }}>
              <th style={thStyle}>Typ</th>
              <th style={thStyle}>Od</th>
              <th style={thStyle}>Do</th>
              <th style={thStyle}>Dni</th>
              <th style={thStyle}>Status</th>
            </tr>
          </thead>
          <tbody>
            {requests.slice(0, 10).map((r) => {
              const sc = STATUS_COLORS[r.status];
              return (
                <tr key={r.id} style={{ borderBottom: `1px solid ${colors.gray[100]}` }}>
                  <td style={tdStyle}>
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                      <span style={{ width: '6px', height: '6px', borderRadius: '50%', backgroundColor: r.leaveTypeColor || colors.gray[500] }} />
                      {r.leaveTypeName}
                    </span>
                  </td>
                  <td style={tdStyle}>{formatDate(r.startDate)}</td>
                  <td style={tdStyle}>{formatDate(r.endDate)}</td>
                  <td style={tdStyle}>{r.totalDays}</td>
                  <td style={tdStyle}>
                    <span style={{ padding: '2px 8px', borderRadius: '4px', fontSize: typography.fontSize.xs, fontWeight: typography.fontWeight.medium, backgroundColor: sc.bg, color: sc.text }}>
                      {STATUS_LABELS[r.status]}
                    </span>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        </div>
      ) : (
        <div style={{ color: colors.gray[400], fontSize: typography.fontSize.base }}>Brak wniosków urlopowych.</div>
      )}
    </div>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL');
}

const cardStyle: React.CSSProperties = {
  padding: '20px',
  border: `1px solid ${colors.gray[200]}`,
  borderRadius: '12px',
  backgroundColor: colors.white,
};

const headingStyle: React.CSSProperties = {
  margin: '0 0 14px',
  fontSize: typography.fontSize.lg,
  fontWeight: typography.fontWeight.bold,
  color: colors.gray[900],
};

const thStyle: React.CSSProperties = {
  textAlign: 'left',
  padding: '6px 8px',
  color: colors.gray[500],
  fontWeight: typography.fontWeight.semibold,
};

const tdStyle: React.CSSProperties = {
  padding: '6px 8px',
  color: colors.gray[900],
};
