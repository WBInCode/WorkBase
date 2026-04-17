import { ListTodo, AlertTriangle, CheckCircle2, BarChart3, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { TaskSummaryDto } from '@/api/types/dashboard';

interface Props {
  data: TaskSummaryDto | undefined;
  isLoading: boolean;
}

export function TaskSummaryWidget({ data, isLoading }: Props) {
  if (isLoading || !data) {
    return <WidgetSkeleton />;
  }

  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <ListTodo size={18} color="#7c3aed" />
        <span style={titleStyle}>Zadania</span>
      </div>

      <div style={{ fontSize: '36px', fontWeight: 700, color: '#111827', margin: '8px 0' }}>
        {data.openTasks}
        <span style={{ fontSize: '14px', fontWeight: 400, color: '#9ca3af', marginLeft: '6px' }}>otwartych</span>
      </div>

      <div style={metricsGrid}>
        <Metric
          icon={<AlertTriangle size={14} color="#dc2626" />}
          label="Zaległe"
          value={data.overdueTasks}
          color="#dc2626"
          highlight={data.overdueTasks > 0}
        />
        <Metric
          icon={<CheckCircle2 size={14} color="#16a34a" />}
          label="Ukończone w tym tyg."
          value={data.completedThisWeek}
          color="#16a34a"
        />
        <Metric
          icon={<BarChart3 size={14} color="#6b7280" />}
          label="Wszystkie"
          value={data.totalTasks}
          color="#6b7280"
        />
      </div>

      <Link to="/tasks" style={drillDownStyle}>
        Wszystkie zadania <ArrowRight size={14} />
      </Link>
    </div>
  );
}

function Metric({ icon, label, value, color, highlight }: {
  icon: React.ReactNode; label: string; value: number; color: string; highlight?: boolean;
}) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: '8px',
      ...(highlight ? { padding: '6px 8px', backgroundColor: '#fef2f2', borderRadius: '6px' } : {}),
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
        <ListTodo size={18} color="#d1d5db" />
        <span style={titleStyle}>Zadania</span>
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

const drillDownStyle: React.CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  gap: '4px',
  marginTop: '12px',
  paddingTop: '12px',
  borderTop: '1px solid #f3f4f6',
  fontSize: '13px',
  fontWeight: 500,
  color: '#2563eb',
  textDecoration: 'none',
};
