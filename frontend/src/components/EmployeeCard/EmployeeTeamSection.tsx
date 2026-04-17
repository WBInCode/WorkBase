import { Link } from 'react-router-dom';
import { Users, User } from 'lucide-react';
import { useEmployees } from '@/api/hooks/useOrganization';

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
        <Users size={18} color="#2563eb" />
        <h3 style={{ margin: 0, fontSize: '15px', fontWeight: 600, color: '#111827' }}>
          Zespół{primaryUnitName ? ` — ${primaryUnitName}` : ''}
        </h3>
      </div>

      {isLoading ? (
        <div style={{ color: '#9ca3af', fontSize: '13px', padding: '12px 0' }}>Ładowanie...</div>
      ) : teammates.length === 0 ? (
        <div style={{ color: '#9ca3af', fontSize: '13px', padding: '12px 0', textAlign: 'center', border: '1px dashed #e5e7eb', borderRadius: '6px' }}>
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
                border: '1px solid #e5e7eb',
                textDecoration: 'none',
                color: '#374151',
                fontSize: '13px',
                transition: 'background-color 0.15s',
              }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#f9fafb')}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = '#fff')}
            >
              <div style={{
                width: '32px', height: '32px', borderRadius: '50%',
                backgroundColor: '#eff6ff', display: 'flex', alignItems: 'center', justifyContent: 'center',
                flexShrink: 0,
              }}>
                <User size={16} color="#2563eb" />
              </div>
              <div style={{ minWidth: 0 }}>
                <div style={{ fontWeight: 500, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {emp.firstName} {emp.lastName}
                </div>
                <div style={{ fontSize: '11px', color: '#9ca3af' }}>
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
  backgroundColor: '#fff',
  borderRadius: '12px',
  border: '1px solid #e5e7eb',
  padding: '20px',
};
