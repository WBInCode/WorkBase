import { useState, useMemo } from 'react';
import { ClipboardCheck } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { usePendingApprovals, useSubmitApprovalDecision } from '@/api/hooks/useWorkflow';
import { useEmployees } from '@/api/hooks/useOrganization';
import { ApprovalActionBar } from '@/components/Leave';
import type { ApprovalDecision, ApprovalRequestDto } from '@/api/types/workflow';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

const STATUS_CONFIG: Record<string, { label: string; bg: string; color: string }> = {
  Pending: { label: 'Oczekuje', bg: colors.warning[100], color: colors.warning[800] },
  Approved: { label: 'Zaakceptowany', bg: '#d1fae5', color: '#065f46' },
  Rejected: { label: 'Odrzucony', bg: colors.danger[50], color: colors.danger[600] },
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
  color: colors.gray[500],
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
};

const tdStyle: React.CSSProperties = {
  padding: '12px 14px',
  color: colors.gray[900],
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
    <div style={{ padding: mobile ? '14px' : '24px 28px', maxWidth: '1100px', margin: '0 auto' }}>
      {/* ── Karta dowodzenia ── */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '14px',
          marginBottom: '18px',
          backgroundColor: colors.white,
          border: `1px solid ${colors.gray[200]}`,
          borderRadius: '20px',
          boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
          padding: mobile ? '16px' : '18px 22px',
        }}
      >
        <span style={{
          width: 42, height: 42, borderRadius: 13, backgroundColor: colors.warning[100], color: colors.warning[700],
          display: 'inline-flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
        }}>
          <ClipboardCheck size={20} />
        </span>
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>
            Oczekujące akceptacje
          </h1>
          <p style={{ margin: '3px 0 0', fontSize: '13px', color: colors.gray[500] }}>
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
              minWidth: '30px',
              height: '30px',
              padding: '0 10px',
              borderRadius: '999px',
              fontSize: '13.5px',
              fontWeight: 800,
              backgroundColor: colors.warning[100],
              color: colors.warning[800],
            }}
          >
            {pendingApprovals.length}
          </span>
        )}
      </div>

      {/* Content */}
      {isLoading ? (
        <div style={{ padding: '20px', textAlign: 'center', color: colors.gray[400], fontSize: '14px' }}>
          Ładowanie...
        </div>
      ) : error ? (
        <div
          style={{
            padding: '16px',
            backgroundColor: colors.danger[50],
            borderRadius: '12px',
            color: colors.danger[600],
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
            color: colors.gray[500],
            fontSize: '14px',
            backgroundColor: colors.gray[50],
            borderRadius: '10px',
            border: `1px dashed ${colors.gray[300]}`,
          }}
        >
          <ClipboardCheck size={36} style={{ color: colors.gray[300], marginBottom: '12px' }} />
          <div>Brak wniosków oczekujących na akceptację.</div>
        </div>
      ) : (
        <div
          style={{
            backgroundColor: colors.white,
            borderRadius: '10px',
            border: `1px solid ${colors.gray[200]}`,
            overflowX: 'auto',
          }}
        >
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50], borderBottom: `1px solid ${colors.gray[200]}` }}>
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
                  <tr key={approval.id} style={{ borderBottom: `1px solid ${colors.gray[100]}` }}>
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
                    <td style={{ ...tdStyle, color: approval.dueDate ? colors.gray[900] : colors.gray[400] }}>
                      {approval.dueDate
                        ? new Date(approval.dueDate).toLocaleDateString('pl-PL')
                        : '—'}
                    </td>
                    <td style={{ ...tdStyle, color: colors.gray[500] }}>
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
                              color: colors.gray[500],
                              backgroundColor: 'transparent',
                              border: `1px solid ${colors.gray[300]}`,
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
                            color: colors.primary[600],
                            backgroundColor: colors.primary[50],
                            border: `1px solid ${colors.primary[200]}`,
                            borderRadius: '10px',
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
            backgroundColor: colors.danger[50],
            borderRadius: '12px',
            color: colors.danger[600],
            fontSize: '14px',
          }}
        >
          Błąd: {decideMutation.error.message}
        </div>
      )}
    </div>
  );
}
