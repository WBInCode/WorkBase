import { useNavigate } from 'react-router-dom';
import { Palmtree } from 'lucide-react';
import type { LeaveRequestDto, LeaveRequestStatus } from '@/api/types/leave';

interface Props {
  requests: LeaveRequestDto[];
  isLoading: boolean;
}

const STATUS_CFG: Record<LeaveRequestStatus, { label: string; bg: string; color: string }> = {
  Draft: { label: 'Szkic', bg: '#f3f4f6', color: '#6b7280' },
  Pending: { label: 'Oczekuje', bg: '#fef3c7', color: '#92400e' },
  Approved: { label: 'Zaakceptowany', bg: '#d1fae5', color: '#065f46' },
  Rejected: { label: 'Odrzucony', bg: '#fef2f2', color: '#dc2626' },
  Cancelled: { label: 'Anulowany', bg: '#f3f4f6', color: '#6b7280' },
};

export function MyLeaveWidget({ requests, isLoading }: Props) {
  const navigate = useNavigate();

  const recent = requests
    .filter((r) => r.status === 'Pending' || r.status === 'Approved')
    .slice(0, 4);

  if (isLoading) {
    return (
      <div style={cardStyle}>
        <div style={headerStyle}>
          <Palmtree size={18} color="#059669" />
          <span style={titleStyle}>Moje urlopy</span>
        </div>
        <div style={{ padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '14px' }}>Ładowanie...</div>
      </div>
    );
  }

  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <Palmtree size={18} color="#059669" />
        <span style={titleStyle}>Moje urlopy</span>
      </div>

      {recent.length === 0 ? (
        <div style={emptyStyle}>Brak aktywnych wniosków urlopowych.</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
          {recent.map((r) => {
            const cfg = STATUS_CFG[r.status] ?? STATUS_CFG.Draft;
            return (
              <div
                key={r.id}
                onClick={() => navigate('/leave/request')}
                style={rowStyle}
              >
                <div style={{ flex: 1 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    {r.leaveTypeColor && (
                      <div style={{ width: '8px', height: '8px', borderRadius: '2px', backgroundColor: r.leaveTypeColor, flexShrink: 0 }} />
                    )}
                    <span style={{ fontSize: '13px', fontWeight: 500, color: '#111827' }}>{r.leaveTypeName}</span>
                  </div>
                  <div style={{ fontSize: '12px', color: '#9ca3af', marginTop: '2px' }}>
                    {new Date(r.startDate).toLocaleDateString('pl-PL')} – {new Date(r.endDate).toLocaleDateString('pl-PL')}
                    <span style={{ marginLeft: '6px' }}>({r.totalDays} dni)</span>
                  </div>
                </div>
                <span style={{
                  padding: '2px 8px', borderRadius: '4px', fontSize: '11px', fontWeight: 500,
                  backgroundColor: cfg.bg, color: cfg.color,
                }}>
                  {cfg.label}
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

const cardStyle: React.CSSProperties = {
  backgroundColor: '#fff', borderRadius: '12px', border: '1px solid #e5e7eb',
  padding: '20px', boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
};
const headerStyle: React.CSSProperties = {
  display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '12px',
};
const titleStyle: React.CSSProperties = { fontSize: '14px', fontWeight: 600, color: '#374151' };
const rowStyle: React.CSSProperties = {
  display: 'flex', alignItems: 'center', justifyContent: 'space-between',
  padding: '10px 12px', borderRadius: '8px', cursor: 'pointer',
  backgroundColor: '#f9fafb',
};
const emptyStyle: React.CSSProperties = {
  padding: '16px', textAlign: 'center', color: '#9ca3af', fontSize: '13px',
  backgroundColor: '#f9fafb', borderRadius: '8px', border: '1px dashed #d1d5db',
};
