import { useNavigate } from 'react-router-dom';
import { Palmtree } from 'lucide-react';
import type { LeaveRequestDto, LeaveRequestStatus } from '@/api/types/leave';
import { colors } from '@/theme/tokens';

interface Props {
  requests: LeaveRequestDto[];
  isLoading: boolean;
}

const STATUS_CFG: Record<LeaveRequestStatus, { label: string; bg: string; color: string }> = {
  Draft: { label: 'Szkic', bg: colors.gray[100], color: colors.gray[500] },
  Pending: { label: 'Oczekuje', bg: colors.warning[100], color: colors.warning[800] },
  Approved: { label: 'Zaakceptowany', bg: '#d1fae5', color: '#065f46' },
  Rejected: { label: 'Odrzucony', bg: colors.danger[50], color: colors.danger[600] },
  Cancelled: { label: 'Anulowany', bg: colors.gray[100], color: colors.gray[500] },
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
          <Palmtree size={18} color={colors.emerald[600]} />
          <span style={titleStyle}>Moje urlopy</span>
        </div>
        <div style={{ padding: '20px', textAlign: 'center', color: colors.gray[400], fontSize: '14px' }}>Ładowanie...</div>
      </div>
    );
  }

  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <Palmtree size={18} color={colors.emerald[600]} />
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
                    <span style={{ fontSize: '13px', fontWeight: 500, color: colors.gray[900] }}>{r.leaveTypeName}</span>
                  </div>
                  <div style={{ fontSize: '12px', color: colors.gray[400], marginTop: '2px' }}>
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
  backgroundColor: colors.white, borderRadius: '12px', border: `1px solid ${colors.gray[200]}`,
  padding: '20px', boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
};
const headerStyle: React.CSSProperties = {
  display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '12px',
};
const titleStyle: React.CSSProperties = { fontSize: '14px', fontWeight: 600, color: colors.gray[700] };
const rowStyle: React.CSSProperties = {
  display: 'flex', alignItems: 'center', justifyContent: 'space-between',
  padding: '10px 12px', borderRadius: '8px', cursor: 'pointer',
  backgroundColor: colors.gray[50],
};
const emptyStyle: React.CSSProperties = {
  padding: '16px', textAlign: 'center', color: colors.gray[400], fontSize: '13px',
  backgroundColor: colors.gray[50], borderRadius: '8px', border: `1px dashed ${colors.gray[300]}`,
};
