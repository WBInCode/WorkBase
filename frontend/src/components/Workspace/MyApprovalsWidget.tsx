import { useNavigate } from 'react-router-dom';
import { ClipboardCheck, Clock } from 'lucide-react';
import type { ApprovalRequestDto } from '@/api/types/workflow';
import { colors } from '@/theme/tokens';

interface Props {
  approvals: ApprovalRequestDto[];
  isLoading: boolean;
}

export function MyApprovalsWidget({ approvals, isLoading }: Props) {
  const navigate = useNavigate();
  const pending = approvals.filter((a) => a.status === 'Pending');

  if (isLoading) {
    return (
      <div style={cardStyle}>
        <div style={headerStyle}>
          <ClipboardCheck size={18} color={colors.warning[500]} />
          <span style={titleStyle}>Oczekujące akceptacje</span>
        </div>
        <div style={{ padding: '20px', textAlign: 'center', color: colors.gray[400], fontSize: '14px' }}>Ładowanie...</div>
      </div>
    );
  }

  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <ClipboardCheck size={18} color={colors.warning[500]} />
        <span style={titleStyle}>Oczekujące akceptacje</span>
        {pending.length > 0 && (
          <span style={{
            marginLeft: 'auto', padding: '2px 10px', borderRadius: '999px',
            fontSize: '12px', fontWeight: 600, backgroundColor: colors.warning[100], color: colors.warning[800],
          }}>
            {pending.length}
          </span>
        )}
      </div>

      {pending.length === 0 ? (
        <div style={emptyStyle}>Brak oczekujących akceptacji.</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
          {pending.slice(0, 5).map((a) => (
            <div
              key={a.id}
              onClick={() => navigate('/leave/approvals')}
              style={rowStyle}
            >
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: '13px', fontWeight: 500, color: colors.gray[900] }}>
                  {a.workflowEntityType ?? 'Wniosek'}
                </div>
                <div style={{ fontSize: '12px', color: colors.gray[400] }}>
                  {new Date(a.createdAt).toLocaleDateString('pl-PL')}
                </div>
              </div>
              {a.dueDate && (
                <div style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '12px', color: colors.warning[500] }}>
                  <Clock size={12} />
                  {new Date(a.dueDate).toLocaleDateString('pl-PL')}
                </div>
              )}
            </div>
          ))}
          {pending.length > 5 && (
            <div
              onClick={() => navigate('/leave/approvals')}
              style={{ padding: '8px', textAlign: 'center', fontSize: '13px', color: colors.primary[600], cursor: 'pointer', fontWeight: 500 }}
            >
              + {pending.length - 5} więcej →
            </div>
          )}
        </div>
      )}
    </div>
  );
}

const cardStyle: React.CSSProperties = {
  backgroundColor: colors.white, borderRadius: '16px', border: `1px solid ${colors.gray[200]}`,
  padding: '20px', boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
};
const headerStyle: React.CSSProperties = {
  display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '12px',
};
const titleStyle: React.CSSProperties = { fontSize: '14px', fontWeight: 600, color: colors.gray[700] };
const rowStyle: React.CSSProperties = {
  display: 'flex', alignItems: 'center', justifyContent: 'space-between',
  padding: '10px 12px', borderRadius: '12px', cursor: 'pointer',
  backgroundColor: colors.gray[50],
};
const emptyStyle: React.CSSProperties = {
  padding: '16px', textAlign: 'center', color: colors.gray[400], fontSize: '13px',
  backgroundColor: colors.gray[50], borderRadius: '12px', border: `1px dashed ${colors.gray[300]}`,
};
