import type { EmployeeDetailDto, EmployeeStatus } from '@/api/types/organization';
import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { useAuth } from 'react-oidc-context';
import { useSetEmployeeHourlyRate } from '@/api/hooks/useOrganization';

const statusLabels: Record<EmployeeStatus, string> = {
  Active: 'Aktywny',
  Inactive: 'Nieaktywny',
  OnLeave: 'Na urlopie',
};

const statusColors: Record<EmployeeStatus, { bg: string; text: string }> = {
  Active: { bg: '#dcfce7', text: '#166534' },
  Inactive: { bg: '#f3f4f6', text: '#6b7280' },
  OnLeave: { bg: '#fef9c3', text: '#854d0e' },
};

interface Props {
  employee: EmployeeDetailDto;
}

export function EmployeeInfoSection({ employee }: Props) {
  const navigate = useNavigate();
  const auth = useAuth();
  const roles = (auth.user?.profile?.['roles'] as string[] | undefined) ?? [];
  const isAdmin = roles.some((r) => r === 'workbase-admin' || r === 'Admin' || r === 'Super Admin');
  const setRate = useSetEmployeeHourlyRate();
  const [editingRate, setEditingRate] = useState(false);
  const [rateInput, setRateInput] = useState<string>(
    employee.hourlyRate !== null && employee.hourlyRate !== undefined ? String(employee.hourlyRate) : '',
  );
  const color = statusColors[employee.status];

  const saveRate = async () => {
    const trimmed = rateInput.trim();
    const value = trimmed === '' ? null : Number(trimmed.replace(',', '.'));
    if (value !== null && (Number.isNaN(value) || value < 0)) return;
    await setRate.mutateAsync({ id: employee.id, hourlyRate: value });
    setEditingRate(false);
  };

  return (
    <div style={cardStyle}>
      {/* Header with avatar */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '16px', marginBottom: '20px' }}>
        <div style={avatarStyle}>
          {employee.firstName.charAt(0)}{employee.lastName.charAt(0)}
        </div>
        <div style={{ flex: 1 }}>
          <h2 style={{ margin: 0, fontSize: '20px', fontWeight: 700, color: '#111827' }}>
            {employee.firstName} {employee.lastName}
          </h2>
          <p style={{ margin: '2px 0 0', fontSize: '14px', color: '#6b7280' }}>{employee.email}</p>
        </div>
        <span style={{ padding: '4px 12px', borderRadius: '9999px', fontSize: '13px', fontWeight: 500, backgroundColor: color.bg, color: color.text }}>
          {statusLabels[employee.status]}
        </span>
      </div>

      {/* Info grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px 24px', fontSize: '14px' }}>
        {employee.employeeNumber && (
          <Field label="Nr pracownika" value={employee.employeeNumber} />
        )}
        <Field label="Data zatrudnienia" value={formatDate(employee.hireDate)} />
        {employee.terminationDate && (
          <Field label="Data zakończenia" value={formatDate(employee.terminationDate)} />
        )}
        {employee.supervisor && (
          <Field label="Przełożony">
            <span
              style={{ color: '#2563eb', cursor: 'pointer', textDecoration: 'underline' }}
              onClick={() => navigate(`/org/employees/${employee.supervisor!.employeeId}`)}
            >
              {employee.supervisor.firstName} {employee.supervisor.lastName}
            </span>
          </Field>
        )}
        {isAdmin && (
          <Field label="Stawka godzinowa (PLN)">
            {editingRate ? (
              <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}>
                <input
                  type="number"
                  min={0}
                  step="0.01"
                  value={rateInput}
                  onChange={(e) => setRateInput(e.target.value)}
                  style={{ width: 100, padding: '4px 8px', border: '1px solid #cbd5e1', borderRadius: 4 }}
                />
                <button
                  onClick={saveRate}
                  disabled={setRate.isPending}
                  style={{ padding: '4px 10px', border: 'none', background: '#2563eb', color: '#fff', borderRadius: 4, cursor: 'pointer', fontSize: 12 }}
                >
                  Zapisz
                </button>
                <button
                  onClick={() => { setEditingRate(false); setRateInput(employee.hourlyRate != null ? String(employee.hourlyRate) : ''); }}
                  style={{ padding: '4px 10px', border: '1px solid #cbd5e1', background: '#fff', borderRadius: 4, cursor: 'pointer', fontSize: 12 }}
                >
                  Anuluj
                </button>
              </span>
            ) : (
              <span
                onClick={() => setEditingRate(true)}
                style={{ cursor: 'pointer', color: employee.hourlyRate != null ? '#111827' : '#9ca3af', textDecoration: 'underline' }}
                title="Kliknij aby edytować"
              >
                {employee.hourlyRate != null ? `${employee.hourlyRate.toFixed(2)} PLN/h` : 'Ustaw stawkę'}
              </span>
            )}
          </Field>
        )}
      </div>

      {/* Assignments */}
      {employee.assignments.length > 0 && (
        <div style={{ marginTop: '20px' }}>
          <h4 style={{ margin: '0 0 10px', fontSize: '14px', fontWeight: 600, color: '#374151' }}>
            Przypisania organizacyjne
          </h4>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
            {employee.assignments.map((a) => (
              <div key={a.id} style={{ padding: '8px 12px', borderRadius: '8px', backgroundColor: '#f9fafb', border: '1px solid #e5e7eb', fontSize: '13px' }}>
                <div style={{ fontWeight: 600, color: '#111827' }}>{a.organizationUnitName}</div>
                <div style={{ color: '#6b7280', marginTop: '2px' }}>
                  {a.positionName}
                  {a.isPrimary && (
                    <span style={{ marginLeft: '6px', padding: '1px 6px', borderRadius: '4px', fontSize: '11px', backgroundColor: '#dbeafe', color: '#1d4ed8' }}>
                      główne
                    </span>
                  )}
                </div>
                <div style={{ color: '#9ca3af', marginTop: '2px', fontSize: '12px' }}>
                  od {formatDate(a.startDate)}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function Field({ label, value, children }: { label: string; value?: string; children?: React.ReactNode }) {
  return (
    <div>
      <div style={{ color: '#9ca3af', fontSize: '12px', marginBottom: '2px' }}>{label}</div>
      <div style={{ color: '#111827', fontWeight: 500 }}>{children ?? value}</div>
    </div>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL');
}

const cardStyle: React.CSSProperties = {
  padding: '20px',
  border: '1px solid #e5e7eb',
  borderRadius: '12px',
  backgroundColor: '#fff',
};

const avatarStyle: React.CSSProperties = {
  width: '56px',
  height: '56px',
  borderRadius: '50%',
  backgroundColor: '#e0e7ff',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  color: '#4338ca',
  fontWeight: 700,
  fontSize: '18px',
  flexShrink: 0,
};
