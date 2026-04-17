import { useState } from 'react';
import { Plus } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useLeaveTypes, useLeaveBalances, useLeaveRequests, useSubmitLeaveRequest } from '@/api/hooks/useLeave';
import { LeaveBalanceCard, LeaveRequestForm } from '@/components/Leave';
import type { LeaveRequestStatus } from '@/api/types/leave';

const STATUS_CONFIG: Record<LeaveRequestStatus, { label: string; bg: string; color: string }> = {
  Draft: { label: 'Szkic', bg: '#f3f4f6', color: '#6b7280' },
  Pending: { label: 'Oczekuje', bg: '#fef3c7', color: '#92400e' },
  Approved: { label: 'Zaakceptowany', bg: '#d1fae5', color: '#065f46' },
  Rejected: { label: 'Odrzucony', bg: '#fef2f2', color: '#dc2626' },
  Cancelled: { label: 'Anulowany', bg: '#f3f4f6', color: '#6b7280' },
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
  const submitMutation = useSubmitLeaveRequest();

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
    <div style={{ padding: '24px', maxWidth: '960px' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: '24px',
        }}
      >
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: '#111827' }}>
            Urlopy
          </h1>
          <p style={{ margin: '4px 0 0', fontSize: '14px', color: '#6b7280' }}>
            Twoje saldo i wnioski urlopowe
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          {/* Year picker */}
          <select
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
            style={{
              padding: '7px 12px',
              fontSize: '14px',
              border: '1px solid #d1d5db',
              borderRadius: '6px',
              backgroundColor: '#fff',
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
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 16px',
              fontSize: '14px',
              fontWeight: 500,
              color: '#fff',
              backgroundColor: '#2563eb',
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
            }}
          >
            <Plus size={16} />
            Nowy wniosek
          </button>
        </div>
      </div>

      {/* Balance cards */}
      <div style={{ marginBottom: '28px' }}>
        <h2
          style={{
            fontSize: '15px',
            fontWeight: 600,
            color: '#374151',
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
            color: '#374151',
            marginBottom: '12px',
          }}
        >
          Wnioski urlopowe
        </h2>

        {requestsLoading ? (
          <div style={{ padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '14px' }}>
            Ładowanie...
          </div>
        ) : requests.length === 0 ? (
          <div
            style={{
              padding: '32px',
              textAlign: 'center',
              color: '#6b7280',
              fontSize: '14px',
              backgroundColor: '#f9fafb',
              borderRadius: '10px',
              border: '1px dashed #d1d5db',
            }}
          >
            Brak wniosków urlopowych w tym roku.
          </div>
        ) : (
          <div
            style={{
              backgroundColor: '#fff',
              borderRadius: '10px',
              border: '1px solid #e5e7eb',
              overflow: 'hidden',
            }}
          >
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
              <thead>
                <tr style={{ backgroundColor: '#f9fafb', borderBottom: '1px solid #e5e7eb' }}>
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
                  const typeColor = r.leaveTypeColor ?? '#6b7280';
                  return (
                    <tr
                      key={r.id}
                      style={{ borderBottom: '1px solid #f3f4f6' }}
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
                            borderRadius: '4px',
                            fontSize: '12px',
                            fontWeight: 500,
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
                          color: '#9ca3af',
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
  color: '#6b7280',
  textTransform: 'uppercase',
  letterSpacing: '0.5px',
};

const tdStyle: React.CSSProperties = {
  padding: '10px 14px',
  color: '#374151',
};
