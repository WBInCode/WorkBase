import { Palmtree, Clock, CalendarCheck, Sun } from 'lucide-react';
import type { LeaveSummaryDto } from '@/api/types/dashboard';

interface Props {
  data: LeaveSummaryDto | undefined;
  isLoading: boolean;
}

export function PendingApprovalsWidget({ data, isLoading }: Props) {
  if (isLoading || !data) {
    return <WidgetSkeleton />;
  }

  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <Palmtree size={18} color="#059669" />
        <span style={titleStyle}>Urlopy</span>
      </div>

      <div style={{ fontSize: '36px', fontWeight: 700, color: data.pendingRequests > 0 ? '#f59e0b' : '#111827', margin: '8px 0' }}>
        {data.pendingRequests}
        <span style={{ fontSize: '14px', fontWeight: 400, color: '#9ca3af', marginLeft: '6px' }}>oczekujących</span>
      </div>

      <div style={metricsGrid}>
        <Metric
          icon={<Clock size={14} color="#f59e0b" />}
          label="Oczekujące"
          value={data.pendingRequests}
          color="#f59e0b"
          highlight={data.pendingRequests > 0}
        />
        <Metric
          icon={<CalendarCheck size={14} color="#16a34a" />}
          label="Zaakceptowane (miesiąc)"
          value={data.approvedThisMonth}
          color="#16a34a"
        />
        <Metric
          icon={<Sun size={14} color="#2563eb" />}
          label="Na urlopie dziś"
          value={data.onLeaveToday}
          color="#2563eb"
        />
      </div>
    </div>
  );
}

function Metric({ icon, label, value, color, highlight }: {
  icon: React.ReactNode; label: string; value: number; color: string; highlight?: boolean;
}) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: '8px',
      ...(highlight ? { padding: '6px 8px', backgroundColor: '#fffbeb', borderRadius: '6px' } : {}),
    }}>
      {icon}
      <div>
        <div style={{ fontSize: '18px', fontWeight: 600, color }}>{value}</div>
        <div style={{ fontSize: '11px', color: '#9ca3af' }}>{label}</div>
      </div>
    </div>
  );
}

function WidgetSkeleton() {
  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <Palmtree size={18} color="#d1d5db" />
        <span style={titleStyle}>Urlopy</span>
      </div>
      <div style={{ height: '80px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#9ca3af', fontSize: '14px' }}>
        Ładowanie...
      </div>
    </div>
  );
}

const cardStyle: React.CSSProperties = {
  backgroundColor: '#fff',
  borderRadius: '12px',
  border: '1px solid #e5e7eb',
  padding: '20px',
  boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
};

const headerStyle: React.CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  gap: '8px',
  marginBottom: '4px',
};

const titleStyle: React.CSSProperties = {
  fontSize: '14px',
  fontWeight: 600,
  color: '#374151',
};

const metricsGrid: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: '1fr 1fr',
  gap: '12px',
  marginTop: '12px',
  paddingTop: '12px',
  borderTop: '1px solid #f3f4f6',
};
