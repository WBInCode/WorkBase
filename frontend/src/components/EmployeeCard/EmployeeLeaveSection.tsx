import type { LeaveBalanceDto, LeaveRequestDto, LeaveRequestStatus } from '@/api/types/leave';

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
  Draft: { bg: '#f3f4f6', text: '#6b7280' },
  Pending: { bg: '#fef9c3', text: '#854d0e' },
  Approved: { bg: '#dcfce7', text: '#166534' },
  Rejected: { bg: '#fef2f2', text: '#dc2626' },
  Cancelled: { bg: '#f3f4f6', text: '#9ca3af' },
};

const DEFAULT_COLORS: Record<string, string> = {
  ANNUAL: '#3b82f6',
  ON_DEMAND: '#f59e0b',
  SICK: '#ef4444',
  CHILDCARE: '#8b5cf6',
};

export function EmployeeLeaveSection({ balances, requests, isLoading }: Props) {
  if (isLoading) {
    return (
      <div style={cardStyle}>
        <h3 style={headingStyle}>Urlopy</h3>
        <div style={{ color: '#9ca3af', fontSize: '14px' }}>Ładowanie...</div>
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
            const color = b.leaveTypeColor || DEFAULT_COLORS[b.leaveTypeCode] || '#6b7280';
            const pct = b.totalDays > 0 ? Math.round((b.usedDays / b.totalDays) * 100) : 0;
            return (
              <div key={b.id} style={{ padding: '12px', borderRadius: '8px', border: '1px solid #e5e7eb', backgroundColor: '#fafafa' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '6px' }}>
                  <span style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: color }} />
                  <span style={{ fontSize: '13px', fontWeight: 600, color: '#374151' }}>{b.leaveTypeName}</span>
                </div>
                <div style={{ fontSize: '20px', fontWeight: 700, color: '#111827' }}>
                  {b.remainingDays} <span style={{ fontSize: '12px', fontWeight: 400, color: '#6b7280' }}>/ {b.totalDays} dni</span>
                </div>
                {/* Progress bar */}
                <div style={{ marginTop: '6px', height: '4px', borderRadius: '2px', backgroundColor: '#e5e7eb' }}>
                  <div style={{ height: '100%', borderRadius: '2px', backgroundColor: color, width: `${Math.min(pct, 100)}%`, transition: 'width 0.3s' }} />
                </div>
                <div style={{ fontSize: '11px', color: '#9ca3af', marginTop: '4px' }}>
                  Wykorzystane: {b.usedDays} · Oczekujące: {b.pendingDays}
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div style={{ color: '#9ca3af', fontSize: '14px', marginBottom: '16px' }}>Brak danych o saldach urlopowych.</div>
      )}

      {/* Recent requests */}
      <h4 style={{ margin: '0 0 8px', fontSize: '14px', fontWeight: 600, color: '#374151' }}>Ostatnie wnioski</h4>
      {requests.length > 0 ? (
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid #e5e7eb' }}>
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
                <tr key={r.id} style={{ borderBottom: '1px solid #f3f4f6' }}>
                  <td style={tdStyle}>
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                      <span style={{ width: '6px', height: '6px', borderRadius: '50%', backgroundColor: r.leaveTypeColor || '#6b7280' }} />
                      {r.leaveTypeName}
                    </span>
                  </td>
                  <td style={tdStyle}>{formatDate(r.startDate)}</td>
                  <td style={tdStyle}>{formatDate(r.endDate)}</td>
                  <td style={tdStyle}>{r.totalDays}</td>
                  <td style={tdStyle}>
                    <span style={{ padding: '2px 8px', borderRadius: '4px', fontSize: '11px', fontWeight: 500, backgroundColor: sc.bg, color: sc.text }}>
                      {STATUS_LABELS[r.status]}
                    </span>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      ) : (
        <div style={{ color: '#9ca3af', fontSize: '14px' }}>Brak wniosków urlopowych.</div>
      )}
    </div>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL');
}

const cardStyle: React.CSSProperties = {
  padding: '20px',
  border: '1px solid #e5e7eb',
  borderRadius: '12px',
  backgroundColor: '#fff',
};

const headingStyle: React.CSSProperties = {
  margin: '0 0 14px',
  fontSize: '16px',
  fontWeight: 700,
  color: '#111827',
};

const thStyle: React.CSSProperties = {
  textAlign: 'left',
  padding: '6px 8px',
  color: '#6b7280',
  fontWeight: 600,
};

const tdStyle: React.CSSProperties = {
  padding: '6px 8px',
  color: '#111827',
};
