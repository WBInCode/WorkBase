import { useNavigate } from 'react-router-dom';
import type { TaskItemDto } from '@/api/types/tasks';
import { colors } from '@/theme/tokens';

interface Props {
  tasks: TaskItemDto[];
  isLoading: boolean;
}

export function EmployeeTasksSection({ tasks, isLoading }: Props) {
  const navigate = useNavigate();

  if (isLoading) {
    return (
      <div style={cardStyle}>
        <h3 style={headingStyle}>Zadania</h3>
        <div style={{ color: colors.gray[400], fontSize: '14px' }}>Ładowanie...</div>
      </div>
    );
  }

  const open = tasks.filter((t) => !t.completedAt);
  const completed = tasks.filter((t) => !!t.completedAt);

  return (
    <div style={cardStyle}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '14px' }}>
        <h3 style={{ ...headingStyle, margin: 0 }}>
          Zadania
          <span style={{ fontSize: '13px', fontWeight: 400, color: colors.gray[500], marginLeft: '8px' }}>
            ({open.length} otwartych, {completed.length} zakończonych)
          </span>
        </h3>
      </div>

      {tasks.length === 0 ? (
        <div style={{ color: colors.gray[400], fontSize: '14px' }}>Brak przypisanych zadań.</div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
          <thead>
            <tr style={{ borderBottom: `1px solid ${colors.gray[200]}` }}>
              <th style={thStyle}>Tytuł</th>
              <th style={thStyle}>Priorytet</th>
              <th style={thStyle}>Status</th>
              <th style={thStyle}>Termin</th>
            </tr>
          </thead>
          <tbody>
            {tasks.slice(0, 15).map((t) => {
              const isOverdue = t.dueDate && !t.completedAt && new Date(t.dueDate) < new Date();
              return (
                <tr
                  key={t.id}
                  onClick={() => navigate(`/tasks/${t.id}`)}
                  style={{ borderBottom: `1px solid ${colors.gray[100]}`, cursor: 'pointer' }}
                  onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = colors.gray[50]; }}
                  onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = ''; }}
                >
                  <td style={{ ...tdStyle, fontWeight: 500, color: colors.gray[900], maxWidth: '300px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {t.title}
                  </td>
                  <td style={tdStyle}>
                    <span style={{ padding: '2px 8px', borderRadius: '4px', fontSize: '11px', fontWeight: 500, backgroundColor: (t.priorityColor ?? '') + '20', color: t.priorityColor ?? undefined }}>
                      {t.priorityName}
                    </span>
                  </td>
                  <td style={tdStyle}>
                    <span style={{ padding: '2px 8px', borderRadius: '4px', fontSize: '11px', fontWeight: 500, backgroundColor: (t.statusColor ?? '') + '20', color: t.statusColor ?? undefined }}>
                      {t.statusName}
                    </span>
                  </td>
                  <td style={{ ...tdStyle, color: isOverdue ? colors.danger[600] : colors.gray[900], fontWeight: isOverdue ? 600 : 400 }}>
                    {t.dueDate ? formatDate(t.dueDate) : '—'}
                    {isOverdue && <span style={{ marginLeft: '4px', fontSize: '11px' }}>⚠</span>}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        </div>
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
  fontSize: '16px',
  fontWeight: 700,
  color: colors.gray[900],
};

const thStyle: React.CSSProperties = {
  textAlign: 'left',
  padding: '6px 8px',
  color: colors.gray[500],
  fontWeight: 600,
};

const tdStyle: React.CSSProperties = {
  padding: '6px 8px',
  color: colors.gray[900],
};
