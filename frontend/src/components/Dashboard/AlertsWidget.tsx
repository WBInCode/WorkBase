import { ShieldAlert, AlertCircle, CheckCircle } from 'lucide-react';
import type { AnomalySummaryDto } from '@/api/types/dashboard';

interface Props {
  data: AnomalySummaryDto | undefined;
  isLoading: boolean;
}

export function AlertsWidget({ data, isLoading }: Props) {
  if (isLoading || !data) {
    return <WidgetSkeleton />;
  }

  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <ShieldAlert size={18} color="#dc2626" />
        <span style={titleStyle}>Anomalie</span>
      </div>

      <div style={{ fontSize: '36px', fontWeight: 700, color: data.newAnomalies > 0 ? '#dc2626' : '#111827', margin: '8px 0' }}>
        {data.newAnomalies}
        <span style={{ fontSize: '14px', fontWeight: 400, color: '#9ca3af', marginLeft: '6px' }}>nowych</span>
      </div>

      <div style={metricsGrid}>
        <Metric
          icon={<AlertCircle size={14} color="#dc2626" />}
          label="Nowe"
          value={data.newAnomalies}
          color="#dc2626"
          highlight={data.newAnomalies > 0}
        />
        <Metric
          icon={<CheckCircle size={14} color="#16a34a" />}
          label="Rozpatrzone (tydzień)"
          value={data.reviewedThisWeek}
          color="#16a34a"
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
        <ShieldAlert size={18} color="#d1d5db" />
        <span style={titleStyle}>Anomalie</span>
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
