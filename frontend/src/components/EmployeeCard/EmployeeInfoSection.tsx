import type { EmployeeAccessState, EmployeeDetailDto, EmployeeStatus } from '@/api/types/organization';
import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import {
  useDeactivateEmployee,
  useEmployeeAccessStatus,
  useRetryEmployeeAccess,
  useSetEmployeeHourlyRate,
} from '@/api/hooks/useOrganization';
import { useCurrentUser } from '@/api/hooks/useIam';
import { RefreshCw, UserMinus } from 'lucide-react';
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

const accessLabels: Record<EmployeeAccessState, string> = {
  NotRequested: 'Oczekuje na synchronizację',
  Pending: 'Oczekuje na wysłanie',
  Processing: 'Synchronizacja',
  Invited: 'Zaproszenie wysłane',
  Active: 'Aktywny',
  Failed: 'Błąd synchronizacji',
  RevocationPending: 'Odbieranie dostępu',
  Revoked: 'Dostęp odebrany',
};

const accessColors: Record<EmployeeAccessState, { bg: string; text: string }> = {
  NotRequested: { bg: colors.gray[100], text: colors.gray[600] },
  Pending: { bg: colors.warning[100], text: colors.warning[800] },
  Processing: { bg: colors.primary[100], text: colors.primary[700] },
  Invited: { bg: colors.primary[100], text: colors.primary[700] },
  Active: { bg: colors.success[100], text: colors.success[800] },
  Failed: { bg: colors.danger[50], text: colors.danger[600] },
  RevocationPending: { bg: colors.warning[100], text: colors.warning[800] },
  Revoked: { bg: colors.gray[100], text: colors.gray[600] },
};

interface Props {
  employee: EmployeeDetailDto;
}

export function EmployeeInfoSection({ employee }: Props) {
  const navigate = useNavigate();
  // isAdmin is sourced from the app's own Role/Permission data, not the Keycloak "roles" claim
  // — see docs/AUDIT-KNOWLEDGE-MAP.md (role system consistency).
  const { data: currentUser } = useCurrentUser();
  const isAdmin = !!currentUser?.isAdmin;
  const setRate = useSetEmployeeHourlyRate();
  const deactivate = useDeactivateEmployee();
  const { data: access } = useEmployeeAccessStatus(employee.id);
  const retryAccess = useRetryEmployeeAccess();
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
        {access?.managedByHub && access.status && (
          <Field label="Dostęp do WorkBase">
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
              <span
                style={{
                  padding: '3px 8px',
                  borderRadius: 4,
                  fontSize: 12,
                  fontWeight: 600,
                  backgroundColor: accessColors[access.status].bg,
                  color: accessColors[access.status].text,
                }}
              >
                {accessLabels[access.status]}
              </span>
              {isAdmin && access.status === 'Failed' && (
                <button
                  type="button"
                  title="Ponów synchronizację dostępu"
                  aria-label="Ponów synchronizację dostępu"
                  onClick={() => retryAccess.mutate(employee.id)}
                  disabled={retryAccess.isPending}
                  style={{
                    width: 28,
                    height: 28,
                    display: 'inline-grid',
                    placeItems: 'center',
                    padding: 0,
                    border: `1px solid ${colors.gray[200]}`,
                    borderRadius: 4,
                    color: colors.gray[600],
                    background: colors.white,
                    cursor: retryAccess.isPending ? 'wait' : 'pointer',
                  }}
                >
                  <RefreshCw size={14} />
                </button>
              )}
            </span>
          </Field>
        )}
        {employee.supervisor ? (
          <Field label="Przełożony">
            <span
              style={{ color: colors.primary[600], cursor: 'pointer', textDecoration: 'underline' }}
              onClick={() => navigate(`/org/employees/${employee.supervisor!.employeeId}`)}
            >
              {employee.supervisor.firstName} {employee.supervisor.lastName}
            </span>
          </Field>
        ) : (
          <Field label="Przełożony">
            <span style={{ color: colors.warning[700], fontSize: '13px' }} title="Bez przełożonego pracownik nie będzie mógł składać wniosków wymagających akceptacji (np. urlopowych).">
              Brak — wnioski o akceptację się nie powiodą
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
              <div key={a.id} style={{ padding: '8px 12px', borderRadius: '12px', backgroundColor: colors.gray[50], border: `1px solid ${colors.gray[200]}`, fontSize: '13px' }}>
                <div style={{ fontWeight: 600, color: colors.gray[900] }}>{a.organizationUnitName}</div>
                <div style={{ color: colors.gray[500], marginTop: '2px' }}>
                  {a.positionName}
                  {a.isPrimary && (
                    <span style={{ marginLeft: '6px', padding: '1px 8px', borderRadius: '999px', fontSize: '11px', backgroundColor: colors.primary[100], color: colors.primary[700] }}>
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
                border: `1px solid ${colors.danger[200]}`, borderRadius: '12px',
                cursor: 'pointer',
              }}
            >
              <UserMinus size={16} />
              Dezaktywuj pracownika
            </button>
          ) : (
            <div style={{ padding: '16px', backgroundColor: colors.danger[50], borderRadius: '12px', border: `1px solid ${colors.danger[200]}` }}>
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
                    border: 'none', borderRadius: '10px', cursor: 'pointer',
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
                    border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', cursor: 'pointer',
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
  padding: '22px 24px',
  border: `1px solid ${colors.gray[200]}`,
  borderRadius: '20px',
  backgroundColor: colors.white,
  boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
};

const avatarStyle: React.CSSProperties = {
  width: '58px',
  height: '58px',
  borderRadius: '50%',
  background: 'linear-gradient(135deg, #3d6df2, #2b55d4)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  color: '#fff',
  fontWeight: 800,
  fontSize: '19px',
  flexShrink: 0,
  boxShadow: '0 8px 18px -6px rgba(61,109,242,0.55)',
};
