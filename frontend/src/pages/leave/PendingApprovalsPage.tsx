import { useState, useMemo } from 'react';
import { ClipboardCheck } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { usePendingApprovals, useSubmitApprovalDecision } from '@/api/hooks/useWorkflow';
import { useEmployees } from '@/api/hooks/useOrganization';
import { ApprovalActionBar } from '@/components/Leave';
import type { ApprovalDecision, ApprovalRequestDto } from '@/api/types/workflow';
import { useIsMobile } from '@/shared';

const STATUS_CONFIG: Record<string, { label: string; bg: string; color: string }> = {
  Pending: { label: 'Oczekuje', bg: '#fef3c7', color: '#92400e' },
  Approved: { label: 'Zaakceptowany', bg: '#d1fae5', color: '#065f46' },
  Rejected: { label: 'Odrzucony', bg: '#fef2f2', color: '#dc2626' },
  Returned: { label: 'Cofnięty', bg: '#fff7ed', color: '#c2410c' },
};

const ENTITY_TYPE_LABEL: Record<string, string> = {
  LeaveRequest: 'Wniosek urlopowy',
};

const thStyle: React.CSSProperties = {
  padding: '10px 14px',
  textAlign: 'left',
  fontSize: '12px',
  fontWeight: 600,
  color: '#6b7280',
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
};

const tdStyle: React.CSSProperties = {
  padding: '12px 14px',
  color: '#111827',
};

export function PendingApprovalsPage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const employeeId = user?.employeeId ?? null;

  const { data: approvals = [], isLoading, error } = usePendingApprovals(employeeId);
  const decideMutation = useSubmitApprovalDecision();
  const mobile = useIsMobile();

  // Fetch employees to resolve requester names
  const { data: employeesResult } = useEmployees({ page: 1, pageSize: 500 });
  const employeeMap = useMemo(() => {
    const map = new Map<string, string>();
    if (employeesResult?.items) {
      for (const e of employeesResult.items) {
        map.set(e.id, `${e.firstName} ${e.lastName}`);
      }
    }
    return map;
  }, [employeesResult]);

  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [decidedIds, setDecidedIds] = useState<Set<string>>(new Set());

  const handleDecide = (approval: ApprovalRequestDto, decision: ApprovalDecision, comment: string) => {
    if (!employeeId) return;
    decideMutation.mutate(
      {
        approvalId: approval.id,
        decision,
        decidedByEmployeeId: employeeId,
        comment: comment || undefined,
      },
      {
        onSuccess: () => {
          setDecidedIds((prev) => new Set(prev).add(approval.id));
          setExpandedId(null);
        },
      },
    );
  };

  const pendingApprovals = approvals.filter((a) => !decidedIds.has(a.id));

  return (
    <div style={{ padding: mobile ? '16px' : '24px', maxWidth: '960px' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '12px',
          marginBottom: '24px',
        }}
      >
        <ClipboardCheck size={24} style={{ color: '#2563eb' }} />
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: '#111827' }}>
            Oczekujące akceptacje
          </h1>
          <p style={{ margin: '4px 0 0', fontSize: '14px', color: '#6b7280' }}>
            Wnioski oczekujące na Twoją decyzję
          </p>
        </div>
        {pendingApprovals.length > 0 && (
          <span
            style={{
              marginLeft: 'auto',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              minWidth: '28px',
              height: '28px',
              padding: '0 8px',
              borderRadius: '14px',
              fontSize: '13px',
              fontWeight: 600,
              backgroundColor: '#fef3c7',
              color: '#92400e',
            }}
          >
            {pendingApprovals.length}
          </span>
        )}
      </div>

      {/* Content */}
      {isLoading ? (
        <div style={{ padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '14px' }}>
          Ładowanie...
        </div>
      ) : error ? (
        <div
          style={{
            padding: '16px',
            backgroundColor: '#fef2f2',
            borderRadius: '8px',
            color: '#dc2626',
            fontSize: '14px',
          }}
        >
          Błąd ładowania: {(error as Error).message}
        </div>
      ) : pendingApprovals.length === 0 ? (
        <div
          style={{
            padding: '48px 32px',
            textAlign: 'center',
            color: '#6b7280',
            fontSize: '14px',
            backgroundColor: '#f9fafb',
            borderRadius: '10px',
            border: '1px dashed #d1d5db',
          }}
        >
          <ClipboardCheck size={36} style={{ color: '#d1d5db', marginBottom: '12px' }} />
          <div>Brak wniosków oczekujących na akceptację.</div>
        </div>
      ) : (
        <div
          style={{
            backgroundColor: '#fff',
            borderRadius: '10px',
            border: '1px solid #e5e7eb',
            overflowX: 'auto',
          }}
        >
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: '#f9fafb', borderBottom: '1px solid #e5e7eb' }}>
                <th style={thStyle}>Typ wniosku</th>
                <th style={thStyle}>Wnioskodawca</th>
                <th style={{ ...thStyle, textAlign: 'center' }}>Status</th>
                <th style={thStyle}>Termin</th>
                <th style={thStyle}>Złożony</th>
                <th style={{ ...thStyle, textAlign: 'center' }}>Akcje</th>
              </tr>
            </thead>
            <tbody>
              {pendingApprovals.map((approval) => {
                const statusCfg = STATUS_CONFIG[approval.status] ?? STATUS_CONFIG.Pending;
                const requesterName = employeeMap.get(approval.requesterId) ?? '—';
                const entityLabel = approval.workflowEntityType
                  ? ENTITY_TYPE_LABEL[approval.workflowEntityType] ?? approval.workflowEntityType
                  : '—';
                const isExpanded = expandedId === approval.id;

                return (
                  <tr key={approval.id} style={{ borderBottom: '1px solid #f3f4f6' }}>
                    {/* Row cells */}
                    <td style={tdStyle}>
                      <div style={{ fontWeight: 500 }}>{entityLabel}</div>
                    </td>
                    <td style={tdStyle}>{requesterName}</td>
                    <td style={{ ...tdStyle, textAlign: 'center' }}>
                      <span
                        style={{
                          display: 'inline-block',
                          padding: '2px 10px',
                          borderRadius: '4px',
                          fontSize: '12px',
                          fontWeight: 500,
                          backgroundColor: statusCfg?.bg,
                          color: statusCfg?.color,
                        }}
                      >
                        {statusCfg?.label}
                      </span>
                    </td>
                    <td style={{ ...tdStyle, color: approval.dueDate ? '#111827' : '#9ca3af' }}>
                      {approval.dueDate
                        ? new Date(approval.dueDate).toLocaleDateString('pl-PL')
                        : '—'}
                    </td>
                    <td style={{ ...tdStyle, color: '#6b7280' }}>
                      {new Date(approval.createdAt).toLocaleDateString('pl-PL')}
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'center' }}>
                      {isExpanded ? (
                        <div style={{ minWidth: mobile ? 'auto' : '420px', textAlign: 'left' }}>
                          <ApprovalActionBar
                            onDecide={(decision, comment) =>
                              handleDecide(approval, decision, comment)
                            }
                            isPending={decideMutation.isPending}
                          />
                          <button
                            onClick={() => setExpandedId(null)}
                            style={{
                              marginTop: '4px',
                              padding: '4px 10px',
                              fontSize: '12px',
                              color: '#6b7280',
                              backgroundColor: 'transparent',
                              border: '1px solid #d1d5db',
                              borderRadius: '4px',
                              cursor: 'pointer',
                            }}
                          >
                            Anuluj
                          </button>
                        </div>
                      ) : (
                        <button
                          onClick={() => setExpandedId(approval.id)}
                          style={{
                            padding: '6px 14px',
                            fontSize: '13px',
                            fontWeight: 500,
                            color: '#2563eb',
                            backgroundColor: '#eff6ff',
                            border: '1px solid #bfdbfe',
                            borderRadius: '6px',
                            cursor: 'pointer',
                          }}
                        >
                          Rozpatrz
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Mutation error */}
      {decideMutation.error && (
        <div
          style={{
            marginTop: '16px',
            padding: '12px 16px',
            backgroundColor: '#fef2f2',
            borderRadius: '8px',
            color: '#dc2626',
            fontSize: '14px',
          }}
        >
          Błąd: {decideMutation.error.message}
        </div>
      )}
    </div>
  );
}
