import { Users, UserCheck, UserX, Clock, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { AttendanceSummaryDto } from '@/api/types/dashboard';
import { colors } from '@/theme/tokens';

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
        <Users size={18} color={colors.primary[600]} />
        <span style={titleStyle}>Obecność dziś</span>
      </div>

      <div style={{ fontSize: '36px', fontWeight: 700, color: colors.gray[900], margin: '8px 0' }}>
        {rate}%
      </div>

      <div style={metricsGrid}>
        <Metric icon={<UserCheck size={14} color={colors.success[600]} />} label="Obecni" value={data.presentToday} color={colors.success[600]} />
        <Metric icon={<Clock size={14} color={colors.warning[500]} />} label="Spóźnieni" value={data.lateToday} color={colors.warning[500]} />
        <Metric icon={<UserX size={14} color={colors.danger[600]} />} label="Nieobecni" value={data.absentToday} color={colors.danger[600]} />
        <Metric icon={<Users size={14} color={colors.gray[500]} />} label="Zaplanowani" value={data.totalScheduled} color={colors.gray[500]} />
      </div>

      <Link to="/time/team-report" style={drillDownStyle}>
        Raport zespołu <ArrowRight size={14} />
      </Link>
    </div>
  );
}

function Metric({ icon, label, value, color }: { icon: React.ReactNode; label: string; value: number; color: string }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
      {icon}
      <div>
        <div style={{ fontSize: '18px', fontWeight: 600, color }}>{value}</div>
        <div style={{ fontSize: '11px', color: colors.gray[400] }}>{label}</div>
      </div>
    </div>
  );
}

function WidgetSkeleton({ title }: { title: string }) {
  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <div style={{ width: 18, height: 18, borderRadius: '4px', backgroundColor: colors.gray[200] }} />
        <span style={titleStyle}>{title}</span>
      </div>
      <div style={{ height: '80px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: colors.gray[400], fontSize: '14px' }}>
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
  fontSize: '14px',
  fontWeight: 600,
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
  fontWeight: 500,
  color: colors.primary[600],
  textDecoration: 'none',
};
