import type { EmployeeDetailDto, EmployeeStatus } from '@/api/types/organization';
import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { useAuth } from 'react-oidc-context';
import { useSetEmployeeHourlyRate, useDeactivateEmployee } from '@/api/hooks/useOrganization';
import { UserMinus } from 'lucide-react';
import { colors } from '@/theme/tokens';

const statusLabels: Record<EmployeeStatus, string> = {
  Active: 'Aktywny',
  Inactive: 'Nieaktywny',
  OnLeave: 'Na urlopie',
};

const statusColors: Record<EmployeeStatus, { bg: string; text: string }> = {
  Active: { bg: colors.success[100], text: colors.success[800] },
  Inactive: { bg: colors.gray[100], text: colors.gray[500] },
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
  const deactivate = useDeactivateEmployee();
  const [editingRate, setEditingRate] = useState(false);
  const [rateInput, setRateInput] = useState<string>(
    employee.hourlyRate !== null && employee.hourlyRate !== undefined ? String(employee.hourlyRate) : '',
  );
  const [showDeactivateConfirm, setShowDeactivateConfirm] = useState(false);
  const color = statusColors[employee.status];

  const handleDeactivate = async () => {
    await deactivate.mutateAsync(employee.id);
    navigate('/org/employees');
  };

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
      <div style={{ display: 'flex', alignItems: 'center', gap: '16px', marginBottom: '20px', flexWrap: 'wrap' }}>
        <div style={avatarStyle}>
          {employee.firstName.charAt(0)}{employee.lastName.charAt(0)}
        </div>
        <div style={{ flex: 1 }}>
          <h2 style={{ margin: 0, fontSize: '20px', fontWeight: 700, color: colors.gray[900] }}>
            {employee.firstName} {employee.lastName}
          </h2>
          <p style={{ margin: '2px 0 0', fontSize: '14px', color: colors.gray[500] }}>{employee.email}</p>
        </div>
        <span style={{ padding: '4px 12px', borderRadius: '9999px', fontSize: '13px', fontWeight: 500, backgroundColor: color.bg, color: color.text }}>
          {statusLabels[employee.status]}
        </span>
      </div>

      {/* Info grid */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '12px 24px', fontSize: '14px' }}>
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
              style={{ color: colors.primary[600], cursor: 'pointer', textDecoration: 'underline' }}
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
                  style={{ padding: '4px 10px', border: 'none', background: colors.primary[600], color: colors.white, borderRadius: 4, cursor: 'pointer', fontSize: 12 }}
                >
                  Zapisz
                </button>
                <button
                  onClick={() => { setEditingRate(false); setRateInput(employee.hourlyRate != null ? String(employee.hourlyRate) : ''); }}
                  style={{ padding: '4px 10px', border: '1px solid #cbd5e1', background: colors.white, borderRadius: 4, cursor: 'pointer', fontSize: 12 }}
                >
                  Anuluj
                </button>
              </span>
            ) : (
              <span
                onClick={() => setEditingRate(true)}
                style={{ cursor: 'pointer', color: employee.hourlyRate != null ? colors.gray[900] : colors.gray[400], textDecoration: 'underline' }}
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
          <h4 style={{ margin: '0 0 10px', fontSize: '14px', fontWeight: 600, color: colors.gray[700] }}>
            Przypisania organizacyjne
          </h4>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
            {employee.assignments.map((a) => (
              <div key={a.id} style={{ padding: '8px 12px', borderRadius: '8px', backgroundColor: colors.gray[50], border: `1px solid ${colors.gray[200]}`, fontSize: '13px' }}>
                <div style={{ fontWeight: 600, color: colors.gray[900] }}>{a.organizationUnitName}</div>
                <div style={{ color: colors.gray[500], marginTop: '2px' }}>
                  {a.positionName}
                  {a.isPrimary && (
                    <span style={{ marginLeft: '6px', padding: '1px 6px', borderRadius: '4px', fontSize: '11px', backgroundColor: colors.primary[100], color: colors.primary[700] }}>
                      główne
                    </span>
                  )}
                </div>
                <div style={{ color: colors.gray[400], marginTop: '2px', fontSize: '12px' }}>
                  od {formatDate(a.startDate)}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Deactivate button — admin only, active employees only */}
      {isAdmin && employee.status === 'Active' && (
        <div style={{ marginTop: '24px', paddingTop: '20px', borderTop: `1px solid ${colors.danger[100]}` }}>
          {!showDeactivateConfirm ? (
            <button
              onClick={() => setShowDeactivateConfirm(true)}
              style={{
                display: 'inline-flex', alignItems: 'center', gap: '8px',
                padding: '10px 20px', fontSize: '13px', fontWeight: 600,
                color: colors.danger[600], backgroundColor: colors.danger[50],
                border: `1px solid ${colors.danger[200]}`, borderRadius: '8px',
                cursor: 'pointer',
              }}
            >
              <UserMinus size={16} />
              Dezaktywuj pracownika
            </button>
          ) : (
            <div style={{ padding: '16px', backgroundColor: colors.danger[50], borderRadius: '8px', border: `1px solid ${colors.danger[200]}` }}>
              <p style={{ margin: '0 0 12px', fontSize: '14px', color: colors.danger[800], fontWeight: 500 }}>
                Czy na pewno chcesz dezaktywować pracownika <strong>{employee.firstName} {employee.lastName}</strong>?
              </p>
              <p style={{ margin: '0 0 16px', fontSize: '13px', color: colors.danger[700] }}>
                Pracownik zostanie oznaczony jako nieaktywny. Nie będzie widoczny w raportach i listach aktywnych pracowników.
              </p>
              <div style={{ display: 'flex', gap: '8px' }}>
                <button
                  onClick={handleDeactivate}
                  disabled={deactivate.isPending}
                  style={{
                    padding: '8px 20px', fontSize: '13px', fontWeight: 600,
                    color: colors.white, backgroundColor: colors.danger[600],
                    border: 'none', borderRadius: '6px', cursor: 'pointer',
                    opacity: deactivate.isPending ? 0.6 : 1,
                  }}
                >
                  {deactivate.isPending ? 'Dezaktywowanie...' : 'Tak, dezaktywuj'}
                </button>
                <button
                  onClick={() => setShowDeactivateConfirm(false)}
                  style={{
                    padding: '8px 20px', fontSize: '13px', fontWeight: 500,
                    color: colors.gray[700], backgroundColor: colors.white,
                    border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', cursor: 'pointer',
                  }}
                >
                  Anuluj
                </button>
              </div>
              {deactivate.error && (
                <p style={{ margin: '8px 0 0', fontSize: '13px', color: colors.danger[600] }}>
                  Błąd: {(deactivate.error as Error)?.message || 'Nie udało się dezaktywować pracownika.'}
                </p>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function Field({ label, value, children }: { label: string; value?: string; children?: React.ReactNode }) {
  return (
    <div>
      <div style={{ color: colors.gray[400], fontSize: '12px', marginBottom: '2px' }}>{label}</div>
      <div style={{ color: colors.gray[900], fontWeight: 500 }}>{children ?? value}</div>
    </div>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL');
}

const cardStyle: React.CSSProperties = {
  padding: '20px',
  border: `1px solid ${colors.gray[200]}`,
  borderRadius: '12px',
  backgroundColor: colors.white,
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
