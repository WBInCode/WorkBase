import { Palmtree, Clock, CalendarCheck, Sun, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { LeaveSummaryDto } from '@/api/types/dashboard';
import { colors } from '@/theme/tokens';

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
        <Palmtree size={18} color={colors.emerald[600]} />
        <span style={titleStyle}>Urlopy</span>
      </div>

      <div style={{ fontSize: '36px', fontWeight: 700, color: data.pendingRequests > 0 ? colors.warning[500] : colors.gray[900], margin: '8px 0' }}>
        {data.pendingRequests}
        <span style={{ fontSize: '14px', fontWeight: 400, color: colors.gray[400], marginLeft: '6px' }}>oczekujących</span>
      </div>

      <div style={metricsGrid}>
        <Metric
          icon={<Clock size={14} color={colors.warning[500]} />}
          label="Oczekujące"
          value={data.pendingRequests}
          color={colors.warning[500]}
          highlight={data.pendingRequests > 0}
        />
        <Metric
          icon={<CalendarCheck size={14} color={colors.success[600]} />}
          label="Zaakceptowane (miesiąc)"
          value={data.approvedThisMonth}
          color={colors.success[600]}
        />
        <Metric
          icon={<Sun size={14} color={colors.primary[600]} />}
          label="Na urlopie dziś"
          value={data.onLeaveToday}
          color={colors.primary[600]}
        />
      </div>

      <Link to="/leave/approvals" style={drillDownStyle}>
        Akceptacje <ArrowRight size={14} />
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
      ...(highlight ? { padding: '6px 8px', backgroundColor: colors.warning[50], borderRadius: '6px' } : {}),
    }}>
      {icon}
      <div>
        <div style={{ fontSize: '18px', fontWeight: 600, color }}>{value}</div>
        <div style={{ fontSize: '11px', color: colors.gray[400] }}>{label}</div>
      </div>
    </div>
  );
}

function WidgetSkeleton() {
  return (
    <div style={cardStyle}>
      <div style={headerStyle}>
        <Palmtree size={18} color={colors.gray[300]} />
        <span style={titleStyle}>Urlopy</span>
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
