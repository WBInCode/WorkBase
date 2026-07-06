import { Link } from 'react-router-dom';
import { Users, User } from 'lucide-react';
import { useEmployees } from '@/api/hooks/useOrganization';
import { colors } from '@/theme/tokens';

interface Props {
  employeeId: string;
  primaryUnitId: string | null;
  primaryUnitName: string | null;
}

export function EmployeeTeamSection({ employeeId, primaryUnitId, primaryUnitName }: Props) {
  const { data, isLoading } = useEmployees({
    organizationUnitId: primaryUnitId ?? undefined,
    page: 1,
    pageSize: 20,
  });

  const teammates = (data?.items ?? []).filter((e) => e.id !== employeeId);

  return (
    <div style={cardStyle}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '12px' }}>
        <Users size={18} color={colors.primary[600]} />
        <h3 style={{ margin: 0, fontSize: '15px', fontWeight: 600, color: colors.gray[900] }}>
          Zespół{primaryUnitName ? ` — ${primaryUnitName}` : ''}
        </h3>
      </div>

      {isLoading ? (
        <div style={{ color: colors.gray[400], fontSize: '13px', padding: '12px 0' }}>Ładowanie...</div>
      ) : teammates.length === 0 ? (
        <div style={{ color: colors.gray[400], fontSize: '13px', padding: '12px 0', textAlign: 'center', border: `1px dashed ${colors.gray[200]}`, borderRadius: '6px' }}>
          Brak innych członków zespołu
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '8px' }}>
          {teammates.map((emp) => (
            <Link
              key={emp.id}
              to={`/org/employees/${emp.id}`}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '10px',
                padding: '10px 12px',
                borderRadius: '8px',
                border: `1px solid ${colors.gray[200]}`,
                textDecoration: 'none',
                color: colors.gray[700],
                fontSize: '13px',
                transition: 'background-color 0.15s',
              }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = colors.gray[50])}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = colors.white)}
            >
              <div style={{
                width: '32px', height: '32px', borderRadius: '50%',
                backgroundColor: colors.primary[50], display: 'flex', alignItems: 'center', justifyContent: 'center',
                flexShrink: 0,
              }}>
                <User size={16} color={colors.primary[600]} />
              </div>
              <div style={{ minWidth: 0 }}>
                <div style={{ fontWeight: 500, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {emp.firstName} {emp.lastName}
                </div>
                <div style={{ fontSize: '11px', color: colors.gray[400] }}>
                  {emp.email}
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

const cardStyle: React.CSSProperties = {
  backgroundColor: colors.white,
  borderRadius: '12px',
  border: `1px solid ${colors.gray[200]}`,
  padding: '20px',
};
