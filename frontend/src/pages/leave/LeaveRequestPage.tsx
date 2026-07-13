import { useState } from 'react';
import { Plus } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useLeaveTypes, useLeaveBalances, useLeaveRequests, useSubmitLeaveRequest } from '@/api/hooks/useLeave';
import { useEmployeeDetail } from '@/api/hooks/useOrganization';
import { LeaveBalanceCard, LeaveRequestForm } from '@/components/Leave';
import type { LeaveRequestStatus } from '@/api/types/leave';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

const STATUS_CONFIG: Record<LeaveRequestStatus, { label: string; bg: string; color: string }> = {
  Draft: { label: 'Szkic', bg: colors.gray[100], color: colors.gray[500] },
  Pending: { label: 'Oczekuje', bg: colors.warning[100], color: colors.warning[800] },
  Approved: { label: 'Zaakceptowany', bg: '#d1fae5', color: '#065f46' },
  Rejected: { label: 'Odrzucony', bg: colors.danger[50], color: colors.danger[600] },
  Cancelled: { label: 'Anulowany', bg: colors.gray[100], color: colors.gray[500] },
};

export function LeaveRequestPage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const employeeId = user?.employeeId ?? null;

  const currentYear = new Date().getFullYear();
  const [year, setYear] = useState(currentYear);
  const [showForm, setShowForm] = useState(false);

  const { data: leaveTypes = [] } = useLeaveTypes();
  const { data: balances = [], isLoading: balancesLoading } = useLeaveBalances(employeeId, year);
  const { data: requests = [], isLoading: requestsLoading } = useLeaveRequests(employeeId, year);
  const { data: employeeDetail } = useEmployeeDetail(employeeId);
  const hasNoSupervisor = !!employeeDetail && !employeeDetail.supervisor;
  const submitMutation = useSubmitLeaveRequest();
  const mobile = useIsMobile();

  const handleSubmit = (data: {
    leaveTypeId: string;
    startDate: string;
    endDate: string;
    totalDays: number;
    reason?: string;
  }) => {
    if (!employeeId) return;
    submitMutation.mutate(
      {
        employeeId,
        leaveTypeId: data.leaveTypeId,
        startDate: data.startDate,
        endDate: data.endDate,
        totalDays: data.totalDays,
        reason: data.reason,
      },
      { onSuccess: () => setShowForm(false) },
    );
  };

  return (
    <div style={{ padding: mobile ? '14px' : '24px 28px', maxWidth: '1100px', margin: '0 auto' }}>
      {/* ── Karta dowodzenia ── */}
      <div
        style={{
          backgroundColor: colors.white,
          border: `1px solid ${colors.gray[200]}`,
          borderRadius: '20px',
          boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
          padding: mobile ? '16px' : '18px 22px',
          marginBottom: '18px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '12px',
          flexWrap: 'wrap',
        }}
      >
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>
            Urlopy
          </h1>
          <p style={{ margin: '3px 0 0', fontSize: '13px', color: colors.gray[500] }}>
            Twoje saldo i wnioski urlopowe
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          {/* Year picker */}
          <select
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
            style={{
              padding: '8px 14px',
              fontSize: '13.5px',
              fontFamily: 'inherit',
              border: `1px solid ${colors.gray[300]}`,
              borderRadius: '999px',
              backgroundColor: colors.gray[50],
              cursor: 'pointer',
            }}
          >
            {[currentYear - 1, currentYear, currentYear + 1].map((y) => (
              <option key={y} value={y}>
                {y}
              </option>
            ))}
          </select>

          <button
            onClick={() => setShowForm(true)}
            disabled={hasNoSupervisor}
            title={hasNoSupervisor ? 'Nie masz przypisanego przełożonego — wniosek nie będzie mógł zostać zaakceptowany. Skontaktuj się z administratorem.' : undefined}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '9px 18px',
              fontSize: '13.5px',
              fontWeight: 700,
              fontFamily: 'inherit',
              color: colors.white,
              backgroundColor: colors.primary[600],
              border: 'none',
              borderRadius: '999px',
              cursor: hasNoSupervisor ? 'not-allowed' : 'pointer',
              opacity: hasNoSupervisor ? 0.6 : 1,
              boxShadow: hasNoSupervisor ? 'none' : '0 6px 14px -4px rgba(61,109,242,0.45)',
            }}
          >
            <Plus size={15} />
            Nowy wniosek
          </button>
        </div>
      </div>

      {hasNoSupervisor && (
        <div style={{
          padding: '12px 16px', marginBottom: '20px', borderRadius: '12px',
          backgroundColor: colors.warning[100], border: `1px solid ${colors.warning[200]}`,
          color: colors.warning[800], fontSize: '13px',
        }}>
          Nie masz przypisanego przełożonego, więc wnioski urlopowe nie będą mogły zostać zaakceptowane. Poinformuj administratora, aby ustawił Ci przełożonego w kartotece pracownika.
        </div>
      )}

      {/* Balance cards */}
      <div style={{ marginBottom: '28px' }}>
        <h2
          style={{
            fontSize: '15px',
            fontWeight: 600,
            color: colors.gray[700],
            marginBottom: '12px',
          }}
        >
          Saldo urlopowe — {year}
        </h2>
        <LeaveBalanceCard balances={balances} isLoading={balancesLoading} />
      </div>

      {/* Request list */}
      <div>
        <h2
          style={{
            fontSize: '15px',
            fontWeight: 600,
            color: colors.gray[700],
            marginBottom: '12px',
          }}
        >
          Wnioski urlopowe
        </h2>

        {requestsLoading ? (
          <div style={{ padding: '20px', textAlign: 'center', color: colors.gray[400], fontSize: '14px' }}>
            Ładowanie...
          </div>
        ) : requests.length === 0 ? (
          <div
            style={{
              padding: '32px',
              textAlign: 'center',
              color: colors.gray[500],
              fontSize: '14px',
              backgroundColor: colors.gray[50],
              borderRadius: '10px',
              border: `1px dashed ${colors.gray[300]}`,
            }}
          >
            Brak wniosków urlopowych w tym roku.
          </div>
        ) : (
          <div
            style={{
              backgroundColor: colors.white,
              borderRadius: '16px',
              border: `1px solid ${colors.gray[200]}`,
              overflowX: 'auto',
              boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)',
            }}
          >
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
              <thead>
                <tr style={{ backgroundColor: colors.gray[50], borderBottom: `1px solid ${colors.gray[200]}` }}>
                  <th style={thStyle}>Typ</th>
                  <th style={thStyle}>Od</th>
                  <th style={thStyle}>Do</th>
                  <th style={{ ...thStyle, textAlign: 'center' }}>Dni</th>
                  <th style={{ ...thStyle, textAlign: 'center' }}>Status</th>
                  <th style={thStyle}>Komentarz</th>
                </tr>
              </thead>
              <tbody>
                {requests.map((r) => {
                  const cfg = STATUS_CONFIG[r.status] ?? STATUS_CONFIG.Draft;
                  const typeColor = r.leaveTypeColor ?? colors.gray[500];
                  return (
                    <tr
                      key={r.id}
                      style={{ borderBottom: `1px solid ${colors.gray[100]}` }}
                    >
                      <td style={tdStyle}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                          <div
                            style={{
                              width: '8px',
                              height: '8px',
                              borderRadius: '2px',
                              backgroundColor: typeColor,
                              flexShrink: 0,
                            }}
                          />
                          {r.leaveTypeName}
                        </div>
                      </td>
                      <td style={tdStyle}>
                        {new Date(r.startDate).toLocaleDateString('pl-PL')}
                      </td>
                      <td style={tdStyle}>
                        {new Date(r.endDate).toLocaleDateString('pl-PL')}
                      </td>
                      <td style={{ ...tdStyle, textAlign: 'center', fontWeight: 600 }}>
                        {r.totalDays}
                      </td>
                      <td style={{ ...tdStyle, textAlign: 'center' }}>
                        <span
                          style={{
                            display: 'inline-block',
                            padding: '2px 10px',
                            borderRadius: '999px',
                            fontSize: '12px',
                            fontWeight: 600,
                            backgroundColor: cfg.bg,
                            color: cfg.color,
                          }}
                        >
                          {cfg.label}
                        </span>
                      </td>
                      <td
                        style={{
                          ...tdStyle,
                          color: colors.gray[400],
                          maxWidth: '200px',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          whiteSpace: 'nowrap',
                        }}
                      >
                        {r.reason ?? '—'}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Form modal */}
      {showForm && employeeId && (
        <LeaveRequestForm
          employeeId={employeeId}
          leaveTypes={leaveTypes}
          onSubmit={handleSubmit}
          onClose={() => {
            setShowForm(false);
            submitMutation.reset();
          }}
          isSubmitting={submitMutation.isPending}
          error={submitMutation.error?.message ?? null}
        />
      )}
    </div>
  );
}

const thStyle: React.CSSProperties = {
  padding: '10px 14px',
  textAlign: 'left',
  fontSize: '12px',
  fontWeight: 600,
  color: colors.gray[500],
  textTransform: 'uppercase',
  letterSpacing: '0.5px',
};

const tdStyle: React.CSSProperties = {
  padding: '10px 14px',
  color: colors.gray[700],
};
