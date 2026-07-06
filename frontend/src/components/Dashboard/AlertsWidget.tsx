import { ShieldAlert, AlertCircle, CheckCircle, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { AnomalySummaryDto } from '@/api/types/dashboard';
import { colors, typography } from '@/theme/tokens';

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
        <ShieldAlert size={18} color={colors.danger[600]} />
        <span style={titleStyle}>Anomalie</span>
      </div>

      <div style={{ fontSize: '36px', fontWeight: typography.fontWeight.bold, color: data.newAnomalies > 0 ? colors.danger[600] : colors.gray[900], margin: '8px 0' }}>
        {data.newAnomalies}
        <span style={{ fontSize: typography.fontSize.base, fontWeight: typography.fontWeight.normal, color: colors.gray[400], marginLeft: '6px' }}>nowych</span>
      </div>

      <div style={metricsGrid}>
        <Metric
          icon={<AlertCircle size={14} color={colors.danger[600]} />}
          label="Nowe"
          value={data.newAnomalies}
          color={colors.danger[600]}
          highlight={data.newAnomalies > 0}
        />
        <Metric
          icon={<CheckCircle size={14} color={colors.success[600]} />}
          label="Rozpatrzone (tydzień)"
          value={data.reviewedThisWeek}
          color={colors.success[600]}
        />
      </div>

      <Link to="/time/team-report" style={drillDownStyle}>
        Raport anomalii <ArrowRight size={14} />
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
      ...(highlight ? { padding: '6px 8px', backgroundColor: colors.danger[50], borderRadius: '6px' } : {}),
    }}>
      {icon}
      <div>
        <div style={{ fontSize: typography.fontSize.xl, fontWeight: typography.fontWeight.semibold, color }}>{value}</div>
        <div style={{ fontSize: typography.fontSize.xs, color: colors.gray[400] }}>{label}</div>
      </div>
    </div>
  );
}

function WidgetSkeleton() {
  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <ShieldAlert size={18} color={colors.gray[300]} />
        <span style={titleStyle}>Anomalie</span>
      </div>
      <div style={{ height: '80px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: colors.gray[400], fontSize: typography.fontSize.base }}>
        Ładowanie...
      </div>
    </div>
  );
}

const cardStyle: React.CSSProperties = {
  backgroundColor: colors.white,
  borderRadius: '12px',
  border: `1px solid ${colors.gray[200]}`,
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
  fontSize: typography.fontSize.base,
  fontWeight: typography.fontWeight.semibold,
  color: colors.gray[700],
};

const metricsGrid: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(120px, 1fr))',
  gap: '12px',
  marginTop: '12px',
  paddingTop: '12px',
  borderTop: `1px solid ${colors.gray[100]}`,
};

const drillDownStyle: React.CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  gap: '4px',
  marginTop: '12px',
  paddingTop: '12px',
  borderTop: `1px solid ${colors.gray[100]}`,
  fontSize: '13px',
  fontWeight: typography.fontWeight.medium,
  color: colors.primary[600],
  textDecoration: 'none',
};
