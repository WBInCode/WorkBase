import type { LeaveRequestDto } from '@/api/types/leave';
import type { TaskItemDto } from '@/api/types/tasks';
import { colors } from '@/theme/tokens';

interface TimelineEntry {
  id: string;
  date: Date;
  type: 'task' | 'leave';
  title: string;
  description: string;
  color: string;
}

interface Props {
  tasks: TaskItemDto[];
  leaveRequests: LeaveRequestDto[];
  isLoading: boolean;
}

export function EmployeeActivityTimeline({ tasks, leaveRequests, isLoading }: Props) {
  if (isLoading) {
    return (
      <div style={cardStyle}>
        <h3 style={headingStyle}>Ostatnia aktywność</h3>
        <div style={{ color: colors.gray[400], fontSize: '14px' }}>Ładowanie...</div>
      </div>
    );
  }

  const entries: TimelineEntry[] = [];

  // Tasks — use createdAt
  for (const t of tasks) {
    entries.push({
      id: `task-${t.id}`,
      date: new Date(t.createdAt),
      type: 'task',
      title: t.title,
      description: `Zadanie utworzone · ${t.statusName} · ${t.priorityName}`,
      color: t.statusColor || colors.gray[500],
    });
    if (t.completedAt) {
      entries.push({
        id: `task-done-${t.id}`,
        date: new Date(t.completedAt),
        type: 'task',
        title: t.title,
        description: 'Zadanie zakończone',
        color: colors.success[500],
      });
    }
  }

  // Leave requests — use createdAt
  for (const r of leaveRequests) {
    entries.push({
      id: `leave-${r.id}`,
      date: new Date(r.createdAt),
      type: 'leave',
      title: `${r.leaveTypeName} (${r.totalDays} dni)`,
      description: `${formatDate(r.startDate)} – ${formatDate(r.endDate)} · ${statusLabel(r.status)}`,
      color: r.leaveTypeColor || colors.gray[500],
    });
  }

  entries.sort((a, b) => b.date.getTime() - a.date.getTime());
  const visible = entries.slice(0, 20);

  return (
    <div style={cardStyle}>
      <h3 style={headingStyle}>Ostatnia aktywność</h3>

      {visible.length === 0 ? (
        <div style={{ color: colors.gray[400], fontSize: '14px' }}>Brak aktywności.</div>
      ) : (
        <div style={{ position: 'relative', paddingLeft: '24px' }}>
          {/* Vertical line */}
          <div style={{ position: 'absolute', left: '7px', top: '4px', bottom: '4px', width: '2px', backgroundColor: colors.gray[200] }} />

          {visible.map((entry) => (
            <div key={entry.id} style={{ position: 'relative', marginBottom: '16px' }}>
              {/* Dot */}
              <div style={{
                position: 'absolute',
                left: '-20px',
                top: '4px',
                width: '12px',
                height: '12px',
                borderRadius: '50%',
                backgroundColor: entry.color,
                border: `2px solid ${colors.white}`,
                boxShadow: `0 0 0 2px ${colors.gray[200]}`,
              }} />
              <div style={{ fontSize: '12px', color: colors.gray[400], marginBottom: '2px' }}>
                {entry.date.toLocaleDateString('pl-PL')} {entry.date.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' })}
                <span style={{
                  marginLeft: '8px',
                  padding: '1px 6px',
                  borderRadius: '4px',
                  fontSize: '10px',
                  fontWeight: 600,
                  backgroundColor: entry.type === 'task' ? '#ede9fe' : '#fce7f3',
                  color: entry.type === 'task' ? '#7c3aed' : '#db2777',
                }}>
                  {entry.type === 'task' ? 'Zadanie' : 'Urlop'}
                </span>
              </div>
              <div style={{ fontSize: '14px', fontWeight: 500, color: colors.gray[900] }}>{entry.title}</div>
              <div style={{ fontSize: '13px', color: colors.gray[500], marginTop: '1px' }}>{entry.description}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL');
}

function statusLabel(status: string): string {
  const map: Record<string, string> = {
    Draft: 'Szkic',
    Pending: 'Oczekujący',
    Approved: 'Zatwierdzony',
    Rejected: 'Odrzucony',
    Cancelled: 'Anulowany',
  };
  return map[status] ?? status;
}

const cardStyle: React.CSSProperties = {
  padding: '20px',
  border: `1px solid ${colors.gray[200]}`,
  borderRadius: '16px',
  backgroundColor: colors.white,
};

const headingStyle: React.CSSProperties = {
  margin: '0 0 14px',
  fontSize: '16px',
  fontWeight: 700,
  color: '#111827',
};
