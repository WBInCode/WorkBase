import { useNavigate } from 'react-router-dom';
import { ClipboardCheck, Clock } from 'lucide-react';
import type { ApprovalRequestDto } from '@/api/types/workflow';

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
          <ClipboardCheck size={18} color="#f59e0b" />
          <span style={titleStyle}>Oczekujące akceptacje</span>
        </div>
        <div style={{ padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '14px' }}>Ładowanie...</div>
      </div>
    );
  }

  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <ClipboardCheck size={18} color="#f59e0b" />
        <span style={titleStyle}>Oczekujące akceptacje</span>
        {pending.length > 0 && (
          <span style={{
            marginLeft: 'auto', padding: '2px 8px', borderRadius: '10px',
            fontSize: '12px', fontWeight: 600, backgroundColor: '#fef3c7', color: '#92400e',
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
                <div style={{ fontSize: '13px', fontWeight: 500, color: '#111827' }}>
                  {a.workflowEntityType ?? 'Wniosek'}
                </div>
                <div style={{ fontSize: '12px', color: '#9ca3af' }}>
                  {new Date(a.createdAt).toLocaleDateString('pl-PL')}
                </div>
              </div>
              {a.dueDate && (
                <div style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '12px', color: '#f59e0b' }}>
                  <Clock size={12} />
                  {new Date(a.dueDate).toLocaleDateString('pl-PL')}
                </div>
              )}
            </div>
          ))}
          {pending.length > 5 && (
            <div
              onClick={() => navigate('/leave/approvals')}
              style={{ padding: '8px', textAlign: 'center', fontSize: '13px', color: '#2563eb', cursor: 'pointer', fontWeight: 500 }}
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
