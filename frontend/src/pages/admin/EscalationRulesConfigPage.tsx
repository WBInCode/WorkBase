import { useState, useCallback } from 'react';
import { AlarmClockCheck, Plus, RefreshCw, Edit2, Trash2, X } from 'lucide-react';
import {
  useEscalationRules,
  useCreateEscalationRule,
  useUpdateEscalationRule,
  useDeleteEscalationRule,
  useWorkflowDefinitions,
} from '@/api/hooks/useWorkflow';
import type {
  EscalationRuleDto,
  CreateEscalationRuleRequest,
  UpdateEscalationRuleRequest,
} from '@/api/types/workflow';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

const ACTION_TYPES = [
  { value: 'notify', label: 'Wyślij powiadomienie' },
  { value: 'create_task', label: 'Utwórz zadanie' },
  { value: 'update_entity', label: 'Zaktualizuj encję' },
];

export function EscalationRulesConfigPage() {
  const { data: definitions } = useWorkflowDefinitions();
  const { data: rules, isLoading, error, refetch, isFetching } = useEscalationRules();
  const createMutation = useCreateEscalationRule();
  const updateMutation = useUpdateEscalationRule();
  const deleteMutation = useDeleteEscalationRule();
  const mobile = useIsMobile();

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<EscalationRuleDto | null>(null);

  const definitionName = (id: string) => definitions?.find((d) => d.id === id)?.name ?? '—';

  const handleCreate = useCallback(
    (req: CreateEscalationRuleRequest) => {
      createMutation.mutate(req, {
        onSuccess: () => { setShowForm(false); createMutation.reset(); },
      });
    },
    [createMutation],
  );

  const handleUpdate = useCallback(
    (id: string, req: UpdateEscalationRuleRequest) => {
      updateMutation.mutate({ id, ...req }, {
        onSuccess: () => { setEditing(null); setShowForm(false); updateMutation.reset(); },
      });
    },
    [updateMutation],
  );

  const handleDelete = useCallback(
    (id: string) => {
      if (!confirm('Czy na pewno chcesz usunąć tę regułę eskalacji?')) return;
      deleteMutation.mutate(id);
    },
    [deleteMutation],
  );

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '1100px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: colors.gray[900] }}>Reguły eskalacji</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={() => refetch()} style={iconBtnStyle} title="Odśwież">
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button onClick={() => { setEditing(null); setShowForm(true); }} style={primaryBtnStyle} disabled={!definitions?.length}>
            <Plus size={16} /> Nowa reguła
          </button>
        </div>
      </div>

      <p style={{ fontSize: '13px', color: colors.gray[500], marginTop: '-12px', marginBottom: '20px' }}>
        Automatyczne działania, gdy krok procesu akceptacji nie zostanie obsłużony w zadanym czasie.
      </p>

      {error && (
        <div style={errorBoxStyle}>
          Błąd ładowania reguł eskalacji.
          <button onClick={() => refetch()} style={retryLinkStyle}>Ponów</button>
        </div>
      )}

      {!definitions?.length && !isLoading && (
        <div style={{ ...errorBoxStyle, backgroundColor: colors.warning[100], borderColor: colors.warning[200], color: colors.warning[800] }}>
          Najpierw utwórz definicję procesu (Workflow Builder), aby móc zdefiniować regułę eskalacji.
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
      ) : !rules || rules.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400] }}>
          <AlarmClockCheck size={40} style={{ marginBottom: '12px', opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak reguł eskalacji</div>
          <div style={{ fontSize: '13px', marginTop: '4px' }}>Dodaj pierwszą regułę klikając „Nowa reguła".</div>
        </div>
      ) : (
        <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '8px', overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50] }}>
                <Th>Proces</Th>
                <Th>Krok</Th>
                <Th>Timeout (min)</Th>
                <Th>Akcja</Th>
                <Th style={{ width: '80px' }}></Th>
              </tr>
            </thead>
            <tbody>
              {rules.map((r) => (
                <tr key={r.id} style={{ borderTop: `1px solid ${colors.gray[200]}` }}>
                  <Td style={{ fontWeight: 500 }}>{definitionName(r.definitionId)}</Td>
                  <Td>{r.stepName}</Td>
                  <Td>{r.timeoutMinutes}</Td>
                  <Td>{ACTION_TYPES.find((a) => a.value === r.actionType)?.label ?? r.actionType}</Td>
                  <Td>
                    <div style={{ display: 'flex', gap: '4px' }}>
                      <button onClick={() => { setEditing(r); setShowForm(true); }} style={smallIconBtn} title="Edytuj">
                        <Edit2 size={14} />
                      </button>
                      <button onClick={() => handleDelete(r.id)} style={{ ...smallIconBtn, color: colors.danger[600] }} title="Usuń">
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </Td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <RuleFormModal
          rule={editing}
          definitions={definitions ?? []}
          isPending={editing ? updateMutation.isPending : createMutation.isPending}
          error={editing ? updateMutation.error : createMutation.error}
          onSubmit={(data) => {
            if (editing) handleUpdate(editing.id, data);
            else handleCreate(data as CreateEscalationRuleRequest);
          }}
          onClose={() => { setShowForm(false); setEditing(null); createMutation.reset(); updateMutation.reset(); }}
        />
      )}
    </div>
  );
}

function RuleFormModal({ rule, definitions, isPending, error, onSubmit, onClose }: {
  rule: EscalationRuleDto | null;
  definitions: { id: string; name: string }[];
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: CreateEscalationRuleRequest) => void;
  onClose: () => void;
}) {
  const [definitionId, setDefinitionId] = useState(rule?.definitionId ?? definitions[0]?.id ?? '');
  const [stepName, setStepName] = useState(rule?.stepName ?? '');
  const [timeoutMinutes, setTimeoutMinutes] = useState(rule?.timeoutMinutes?.toString() ?? '60');
  const [actionType, setActionType] = useState(rule?.actionType ?? ACTION_TYPES[0]!.value);
  const [actionPayloadJson, setActionPayloadJson] = useState(rule?.actionPayloadJson ?? '');
  const mobile = useIsMobile();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({
      definitionId,
      stepName,
      timeoutMinutes: Number(timeoutMinutes) || 0,
      actionType,
      actionPayloadJson: actionPayloadJson || undefined,
    });
  };

  return (
    <div style={overlayStyle}>
      <div style={modalStyle}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600, color: colors.gray[900] }}>
            {rule ? 'Edytuj regułę eskalacji' : 'Nowa reguła eskalacji'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500] }}><X size={20} /></button>
        </div>

        {error && <div style={{ ...errorBoxStyle, marginBottom: '12px' }}>Błąd: {error.message}</div>}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Proces (definicja)
              <select value={definitionId} onChange={(e) => setDefinitionId(e.target.value)} required disabled={!!rule} style={inputStyle}>
                {definitions.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
            </label>
            <label style={labelStyle}>
              Nazwa kroku
              <input value={stepName} onChange={(e) => setStepName(e.target.value)} required disabled={!!rule} style={inputStyle} placeholder="np. ManagerApproval" />
            </label>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Timeout (minuty)
              <input type="number" min={1} value={timeoutMinutes} onChange={(e) => setTimeoutMinutes(e.target.value)} required style={inputStyle} />
            </label>
            <label style={labelStyle}>
              Akcja po przekroczeniu czasu
              <select value={actionType} onChange={(e) => setActionType(e.target.value)} style={inputStyle}>
                {ACTION_TYPES.map((a) => <option key={a.value} value={a.value}>{a.label}</option>)}
              </select>
            </label>
          </div>
          <label style={labelStyle}>
            Payload akcji (JSON, opcjonalnie)
            <textarea value={actionPayloadJson} onChange={(e) => setActionPayloadJson(e.target.value)} rows={3} style={{ ...inputStyle, resize: 'vertical', fontFamily: 'monospace', fontSize: '12px' }} placeholder='np. {"recipientRole": "manager"}' />
          </label>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
            <button type="button" onClick={onClose} style={cancelBtnStyle}>Anuluj</button>
            <button type="submit" disabled={isPending} style={primaryBtnStyle}>
              {isPending ? 'Zapisywanie...' : rule ? 'Zapisz' : 'Utwórz'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Th({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <th style={{ padding: '10px 14px', textAlign: 'left', fontSize: '12px', fontWeight: 600, color: colors.gray[500], whiteSpace: 'nowrap', ...style }}>{children}</th>;
}
function Td({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <td style={{ padding: '10px 14px', color: colors.gray[900], ...style }}>{children}</td>;
}

const iconBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '34px', height: '34px', border: `1px solid ${colors.gray[300]}`, borderRadius: '6px',
  background: colors.white, color: colors.gray[700], cursor: 'pointer',
};
const primaryBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', gap: '6px',
  padding: '7px 16px', fontSize: '13px', fontWeight: 600,
  color: colors.white, backgroundColor: colors.primary[600], border: 'none', borderRadius: '6px', cursor: 'pointer',
};
const cancelBtnStyle: React.CSSProperties = {
  padding: '7px 16px', fontSize: '13px', fontWeight: 500,
  color: colors.gray[700], backgroundColor: colors.white, border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', cursor: 'pointer',
};
const smallIconBtn: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '28px', height: '28px', background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500], borderRadius: '4px',
};
const errorBoxStyle: React.CSSProperties = {
  padding: '12px 16px', marginBottom: '16px', borderRadius: '8px',
  backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px',
};
const retryLinkStyle: React.CSSProperties = {
  marginLeft: '8px', color: colors.primary[600], background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: '13px',
};
const overlayStyle: React.CSSProperties = {
  position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100,
};
const modalStyle: React.CSSProperties = {
  backgroundColor: colors.white, borderRadius: '12px', padding: '24px', width: '100%', maxWidth: '560px', maxHeight: '90vh', overflow: 'auto',
  boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
};
const labelStyle: React.CSSProperties = {
  display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: colors.gray[700],
};
const inputStyle: React.CSSProperties = {
  width: '100%', padding: '8px 12px', fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', boxSizing: 'border-box',
};
