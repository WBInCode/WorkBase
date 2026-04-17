import { Users, UserCheck, UserX, Clock } from 'lucide-react';
import type { AttendanceSummaryDto } from '@/api/types/dashboard';

interface Props {
  data: AttendanceSummaryDto | undefined;
  isLoading: boolean;
}

export function AttendanceWidget({ data, isLoading }: Props) {
  if (isLoading || !data) {
    return <WidgetSkeleton title="Obecność dziś" />;
  }

  const rate = data.totalScheduled > 0
    ? Math.round((data.presentToday / data.totalScheduled) * 100)
    : 0;

  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <Users size={18} color="#2563eb" />
        <span style={titleStyle}>Obecność dziś</span>
      </div>

      <div style={{ fontSize: '36px', fontWeight: 700, color: '#111827', margin: '8px 0' }}>
        {rate}%
      </div>

      <div style={metricsGrid}>
        <Metric icon={<UserCheck size={14} color="#16a34a" />} label="Obecni" value={data.presentToday} color="#16a34a" />
        <Metric icon={<Clock size={14} color="#f59e0b" />} label="Spóźnieni" value={data.lateToday} color="#f59e0b" />
        <Metric icon={<UserX size={14} color="#dc2626" />} label="Nieobecni" value={data.absentToday} color="#dc2626" />
        <Metric icon={<Users size={14} color="#6b7280" />} label="Zaplanowani" value={data.totalScheduled} color="#6b7280" />
      </div>
    </div>
  );
}

function Metric({ icon, label, value, color }: { icon: React.ReactNode; label: string; value: number; color: string }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
      {icon}
      <div>
        <div style={{ fontSize: '18px', fontWeight: 600, color }}>{value}</div>
        <div style={{ fontSize: '11px', color: '#9ca3af' }}>{label}</div>
      </div>
    </div>
  );
}

function WidgetSkeleton({ title }: { title: string }) {
  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <div style={{ width: 18, height: 18, borderRadius: '4px', backgroundColor: '#e5e7eb' }} />
        <span style={titleStyle}>{title}</span>
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
