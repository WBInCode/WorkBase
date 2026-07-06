import { Clock, CalendarDays, ListTodo } from 'lucide-react';
import type { ReactNode } from 'react';
import { colors } from '@/theme/tokens';

interface ActivityItem {
  id: string;
  icon: ReactNode;
  iconBg: string;
  text: string;
  time: string;
}

interface ActivityFeedProps {
  employeeId: string | null;
  tasks: Array<{ id: string; title: string; statusName?: string; updatedAt?: string }>;
  leaveRequests: Array<{ id: string; leaveTypeName?: string; status: string; createdAt?: string }>;
}

export function ActivityFeed({ tasks, leaveRequests }: ActivityFeedProps) {
  const items: ActivityItem[] = [];

  // Build feed from recent tasks
  for (const t of tasks.slice(0, 5)) {
    items.push({
      id: `task-${t.id}`,
      icon: <ListTodo size={14} />,
      iconBg: colors.primary[50],
      text: `Zadanie "${t.title}" — ${t.statusName ?? ''}`,
      time: t.updatedAt ? formatRelative(t.updatedAt) : '',
    });
  }

  // Build feed from recent leave requests
  for (const lr of leaveRequests.slice(0, 5)) {
    const statusLabel = leaveStatusLabel(lr.status);
    items.push({
      id: `leave-${lr.id}`,
      icon: <CalendarDays size={14} />,
      iconBg: colors.success[50],
      text: `Wniosek urlopowy (${lr.leaveTypeName ?? ''}) — ${statusLabel}`,
      time: lr.createdAt ? formatRelative(lr.createdAt) : '',
    });
  }

  // Sort by time descending (newest first)
  items.sort((a, b) => (b.time > a.time ? 1 : -1));
  const feed = items.slice(0, 8);

  return (
    <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '8px', backgroundColor: colors.white }}>
      <div style={{ padding: '14px 16px', borderBottom: `1px solid ${colors.gray[200]}`, display: 'flex', alignItems: 'center', gap: '8px' }}>
        <Clock size={16} color={colors.gray[500]} />
        <span style={{ fontWeight: 600, fontSize: '14px', color: colors.gray[900] }}>Ostatnia aktywność</span>
      </div>

      {feed.length === 0 ? (
        <div style={{ padding: '24px 16px', textAlign: 'center', color: colors.gray[400], fontSize: '13px' }}>
          Brak ostatniej aktywności
        </div>
      ) : (
        <div style={{ padding: '8px 0' }}>
          {feed.map((item) => (
            <div
              key={item.id}
              style={{
                display: 'flex',
                alignItems: 'flex-start',
                gap: '10px',
                padding: '8px 16px',
                fontSize: '13px',
              }}
            >
              <div
                style={{
                  width: '28px',
                  height: '28px',
                  borderRadius: '6px',
                  backgroundColor: item.iconBg,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                  color: colors.gray[700],
                }}
              >
                {item.icon}
              </div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ color: colors.gray[700], lineHeight: '1.4' }}>{item.text}</div>
                {item.time && (
                  <div style={{ color: colors.gray[400], fontSize: '11px', marginTop: '2px' }}>{item.time}</div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function formatRelative(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMin = Math.floor(diffMs / 60000);

  if (diffMin < 1) return 'Przed chwilą';
  if (diffMin < 60) return `${diffMin} min temu`;
  const diffH = Math.floor(diffMin / 60);
  if (diffH < 24) return `${diffH} godz. temu`;
  const diffD = Math.floor(diffH / 24);
  if (diffD === 1) return 'Wczoraj';
  if (diffD < 7) return `${diffD} dni temu`;
  return date.toLocaleDateString('pl-PL', { day: 'numeric', month: 'short' });
}

function leaveStatusLabel(status: string): string {
  const map: Record<string, string> = {
    Draft: 'Szkic',
    Pending: 'Oczekujący',
    Approved: 'Zatwierdzony',
    Rejected: 'Odrzucony',
    Cancelled: 'Anulowany',
  };
  return map[status] ?? status;
}
