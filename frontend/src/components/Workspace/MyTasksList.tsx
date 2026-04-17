import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertTriangle, CheckCircle2, ListTodo } from 'lucide-react';
import type { TaskItemDto } from '@/api/types/tasks';

interface Props {
  tasks: TaskItemDto[];
  isLoading: boolean;
}

export function MyTasksList({ tasks, isLoading }: Props) {
  const navigate = useNavigate();
  const now = new Date();

  const { upcoming, other } = useMemo(() => {
    const open = tasks.filter((t) => !t.completedAt);
    const threeDays = new Date(now.getTime() + 3 * 24 * 60 * 60 * 1000);

    const up: TaskItemDto[] = [];
    const ot: TaskItemDto[] = [];
    for (const t of open) {
      if (t.dueDate && new Date(t.dueDate) <= threeDays) up.push(t);
      else ot.push(t);
    }
    up.sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime());
    return { upcoming: up, other: ot };
  }, [tasks]);

  const completedToday = tasks.filter(
    (t) => t.completedAt && new Date(t.completedAt).toDateString() === now.toDateString(),
  ).length;

  if (isLoading) {
    return (
      <div style={cardStyle}>
        <Header count={0} completedToday={0} />
        <div style={{ padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '14px' }}>Ładowanie...</div>
      </div>
    );
  }

  return (
    <div style={cardStyle}>
      <Header count={upcoming.length + other.length} completedToday={completedToday} />

      {upcoming.length === 0 && other.length === 0 ? (
        <div style={emptyStyle}>Brak otwartych zadań. Dobra robota!</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
          {upcoming.map((t) => {
            const isOverdue = t.dueDate && new Date(t.dueDate) < now;
            return (
              <div
                key={t.id}
                onClick={() => navigate(`/tasks/${t.id}`)}
                style={{
                  ...rowStyle,
                  borderLeft: `3px solid ${isOverdue ? '#dc2626' : '#f59e0b'}`,
                }}
              >
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: '14px', fontWeight: 500, color: '#111827' }}>{t.title}</div>
                  <div style={{ fontSize: '12px', color: '#9ca3af', marginTop: '2px' }}>
                    <Badge label={t.statusName} color={t.statusColor ?? '#6b7280'} />
                    <span style={{ marginLeft: '6px' }}>
                      <Badge label={t.priorityName} color={t.priorityColor ?? '#6b7280'} />
                    </span>
                  </div>
                </div>
                <div style={{ textAlign: 'right', flexShrink: 0 }}>
                  <span style={{ fontSize: '12px', color: isOverdue ? '#dc2626' : '#f59e0b', fontWeight: 600 }}>
                    {isOverdue && <AlertTriangle size={12} style={{ marginRight: '3px', verticalAlign: 'middle' }} />}
                    {new Date(t.dueDate!).toLocaleDateString('pl-PL')}
                  </span>
                </div>
              </div>
            );
          })}
          {other.slice(0, 5).map((t) => (
            <div key={t.id} onClick={() => navigate(`/tasks/${t.id}`)} style={rowStyle}>
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: '14px', fontWeight: 500, color: '#111827' }}>{t.title}</div>
                <div style={{ fontSize: '12px', color: '#9ca3af', marginTop: '2px' }}>
                  <Badge label={t.statusName} color={t.statusColor ?? '#6b7280'} />
                </div>
              </div>
              {t.dueDate && (
                <span style={{ fontSize: '12px', color: '#9ca3af' }}>
                  {new Date(t.dueDate).toLocaleDateString('pl-PL')}
                </span>
              )}
            </div>
          ))}
          {other.length > 5 && (
            <div
              onClick={() => navigate('/tasks/my')}
              style={{ padding: '8px', textAlign: 'center', fontSize: '13px', color: '#2563eb', cursor: 'pointer', fontWeight: 500 }}
            >
              + {other.length - 5} więcej →
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function Header({ count, completedToday }: { count: number; completedToday: number }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '12px' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <ListTodo size={18} color="#7c3aed" />
        <span style={{ fontSize: '14px', fontWeight: 600, color: '#374151' }}>Moje zadania</span>
        <span style={{ fontSize: '12px', color: '#9ca3af' }}>({count})</span>
      </div>
      {completedToday > 0 && (
        <div style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '12px', color: '#16a34a' }}>
          <CheckCircle2 size={14} /> {completedToday} ukończone dziś
        </div>
      )}
    </div>
  );
}

function Badge({ label, color }: { label: string; color: string }) {
  return (
    <span style={{
      display: 'inline-block', padding: '1px 7px', borderRadius: '4px',
      fontSize: '11px', fontWeight: 500, backgroundColor: color + '20', color,
    }}>
      {label}
    </span>
  );
}

const cardStyle: React.CSSProperties = {
  backgroundColor: '#fff', borderRadius: '12px', border: '1px solid #e5e7eb',
  padding: '20px', boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
};
const rowStyle: React.CSSProperties = {
  display: 'flex', alignItems: 'center', justifyContent: 'space-between',
  padding: '10px 12px', borderRadius: '8px', cursor: 'pointer',
  backgroundColor: '#f9fafb', transition: 'background-color 0.15s',
};
const emptyStyle: React.CSSProperties = {
  padding: '20px', textAlign: 'center', color: '#6b7280', fontSize: '14px',
  backgroundColor: '#f9fafb', borderRadius: '8px', border: '1px dashed #d1d5db',
};
