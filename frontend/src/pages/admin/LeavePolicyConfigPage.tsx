import { useState, useCallback } from 'react';
import { ShieldCheck, Plus, RefreshCw, Edit2, Trash2, X } from 'lucide-react';
import {
  useLeavePolicies,
  useCreateLeavePolicy,
  useUpdateLeavePolicy,
  useDeleteLeavePolicy,
  useLeaveTypes,
} from '@/api/hooks/useLeave';
import type {
  LeavePolicyDto,
  CreateLeavePolicyRequest,
  UpdateLeavePolicyRequest,
} from '@/api/types/leave';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function LeavePolicyConfigPage() {
  const { data: policies, isLoading, error, refetch, isFetching } = useLeavePolicies();
  const { data: leaveTypes } = useLeaveTypes();
  const createMutation = useCreateLeavePolicy();
  const updateMutation = useUpdateLeavePolicy();
  const deleteMutation = useDeleteLeavePolicy();
  const mobile = useIsMobile();

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<LeavePolicyDto | null>(null);

  const leaveTypeName = (id: string) => leaveTypes?.find((t) => t.id === id)?.name ?? '—';

  const handleCreate = useCallback(
    (req: CreateLeavePolicyRequest) => {
      createMutation.mutate(req, {
        onSuccess: () => { setShowForm(false); createMutation.reset(); },
      });
    },
    [createMutation],
  );

  const handleUpdate = useCallback(
    (id: string, req: UpdateLeavePolicyRequest) => {
      updateMutation.mutate({ id, ...req }, {
        onSuccess: () => { setEditing(null); setShowForm(false); updateMutation.reset(); },
      });
    },
    [updateMutation],
  );

  const handleDelete = useCallback(
    (id: string) => {
      if (!confirm('Czy na pewno chcesz usunąć tę politykę urlopową?')) return;
      deleteMutation.mutate(id);
    },
    [deleteMutation],
  );

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '1100px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>Polityki urlopowe</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={() => refetch()} style={iconBtnStyle} title="Odśwież">
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button onClick={() => { setEditing(null); setShowForm(true); }} style={primaryBtnStyle} disabled={!leaveTypes?.length}>
            <Plus size={16} /> Nowa polityka
          </button>
        </div>
      </div>

      <p style={{ fontSize: '13px', color: colors.gray[500], marginTop: '-12px', marginBottom: '20px' }}>
        Zasady naliczania i przenoszenia dni urlopowych per typ urlopu — limit roczny, przenoszenie na kolejny rok, wymagany okres wyprzedzenia wniosku.
      </p>

      {error && (
        <div style={errorBoxStyle}>
          Błąd ładowania polityk urlopowych.
          <button onClick={() => refetch()} style={retryLinkStyle}>Ponów</button>
        </div>
      )}

      {!leaveTypes?.length && !isLoading && (
        <div style={{ ...errorBoxStyle, backgroundColor: colors.warning[100], borderColor: colors.warning[200], color: colors.warning[800] }}>
          Najpierw dodaj typ urlopu (Administracja → Typy urlopów), aby móc zdefiniować politykę.
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
      ) : !policies || policies.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400] }}>
          <ShieldCheck size={40} style={{ marginBottom: '12px', opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak polityk urlopowych</div>
          <div style={{ fontSize: '13px', marginTop: '4px' }}>Dodaj pierwszą politykę klikając „Nowa polityka".</div>
        </div>
      ) : (
        <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', overflowX: 'auto', backgroundColor: colors.white, boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50] }}>
                <Th>Nazwa</Th>
                <Th>Typ urlopu</Th>
                <Th>Dni/rok</Th>
                <Th>Przenoszenie</Th>
                <Th>Maks. przeniesienie</Th>
                <Th>Maks. dni z rzędu</Th>
                <Th>Wyprzedzenie (dni)</Th>
                <Th style={{ width: '80px' }}></Th>
              </tr>
            </thead>
            <tbody>
              {policies.map((p) => (
                <tr key={p.id} style={{ borderTop: `1px solid ${colors.gray[200]}` }}>
                  <Td style={{ fontWeight: 500 }}>{p.name}</Td>
                  <Td>{leaveTypeName(p.leaveTypeId)}</Td>
                  <Td>{p.daysPerYear}</Td>
                  <Td>{p.allowCarryOver ? 'Tak' : 'Nie'}</Td>
                  <Td>{p.allowCarryOver ? p.maxCarryOverDays : '—'}</Td>
                  <Td>{p.maxConsecutiveDays ?? '—'}</Td>
                  <Td>{p.minNoticeDays ?? '—'}</Td>
                  <Td>
                    <div style={{ display: 'flex', gap: '4px' }}>
                      <button onClick={() => { setEditing(p); setShowForm(true); }} style={smallIconBtn} title="Edytuj">
                        <Edit2 size={14} />
                      </button>
                      <button onClick={() => handleDelete(p.id)} style={{ ...smallIconBtn, color: colors.danger[600] }} title="Usuń">
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
        <PolicyFormModal
          policy={editing}
          leaveTypes={leaveTypes ?? []}
          isPending={editing ? updateMutation.isPending : createMutation.isPending}
          error={editing ? updateMutation.error : createMutation.error}
          onSubmit={(data) => {
            if (editing) handleUpdate(editing.id, data);
            else handleCreate(data as CreateLeavePolicyRequest);
          }}
          onClose={() => { setShowForm(false); setEditing(null); createMutation.reset(); updateMutation.reset(); }}
        />
      )}
    </div>
  );
}

function PolicyFormModal({ policy, leaveTypes, isPending, error, onSubmit, onClose }: {
  policy: LeavePolicyDto | null;
  leaveTypes: { id: string; name: string }[];
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: CreateLeavePolicyRequest) => void;
  onClose: () => void;
}) {
  const [leaveTypeId, setLeaveTypeId] = useState(policy?.leaveTypeId ?? leaveTypes[0]?.id ?? '');
  const [name, setName] = useState(policy?.name ?? '');
  const [daysPerYear, setDaysPerYear] = useState(policy?.daysPerYear?.toString() ?? '26');
  const [allowCarryOver, setAllowCarryOver] = useState(policy?.allowCarryOver ?? false);
  const [maxCarryOverDays, setMaxCarryOverDays] = useState(policy?.maxCarryOverDays?.toString() ?? '0');
  const [maxConsecutiveDays, setMaxConsecutiveDays] = useState(policy?.maxConsecutiveDays?.toString() ?? '');
  const [minNoticeDays, setMinNoticeDays] = useState(policy?.minNoticeDays?.toString() ?? '0');
  const mobile = useIsMobile();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({
      leaveTypeId,
      name,
      daysPerYear: Number(daysPerYear) || 0,
      allowCarryOver,
      maxCarryOverDays: allowCarryOver ? Number(maxCarryOverDays) || 0 : 0,
      maxConsecutiveDays: maxConsecutiveDays ? Number(maxConsecutiveDays) : null,
      minNoticeDays: Number(minNoticeDays) || 0,
    });
  };

  return (
    <div style={overlayStyle}>
      <div style={modalStyle}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600, color: colors.gray[900] }}>
            {policy ? 'Edytuj politykę urlopową' : 'Nowa polityka urlopowa'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500] }}><X size={20} /></button>
        </div>

        {error && <div style={{ ...errorBoxStyle, marginBottom: '12px' }}>Błąd: {error.message}</div>}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Typ urlopu
              <select value={leaveTypeId} onChange={(e) => setLeaveTypeId(e.target.value)} required disabled={!!policy} style={inputStyle}>
                {leaveTypes.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            </label>
            <label style={labelStyle}>
              Nazwa polityki
              <input value={name} onChange={(e) => setName(e.target.value)} required style={inputStyle} placeholder="np. Standardowa" />
            </label>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Dni/rok
              <input type="number" min={0} value={daysPerYear} onChange={(e) => setDaysPerYear(e.target.value)} required style={inputStyle} />
            </label>
            <label style={labelStyle}>
              Wymagane wyprzedzenie wniosku (dni)
              <input type="number" min={0} value={minNoticeDays} onChange={(e) => setMinNoticeDays(e.target.value)} style={inputStyle} />
            </label>
          </div>
          <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', color: colors.gray[700], cursor: 'pointer' }}>
            <input type="checkbox" checked={allowCarryOver} onChange={(e) => setAllowCarryOver(e.target.checked)} /> Zezwól na przeniesienie niewykorzystanych dni na kolejny rok
          </label>
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Maks. dni do przeniesienia
              <input type="number" min={0} value={maxCarryOverDays} onChange={(e) => setMaxCarryOverDays(e.target.value)} disabled={!allowCarryOver} style={inputStyle} />
            </label>
            <label style={labelStyle}>
              Maks. dni urlopu z rzędu (opcjonalnie)
              <input type="number" min={0} value={maxConsecutiveDays} onChange={(e) => setMaxConsecutiveDays(e.target.value)} style={inputStyle} placeholder="brak limitu" />
            </label>
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
            <button type="button" onClick={onClose} style={cancelBtnStyle}>Anuluj</button>
            <button type="submit" disabled={isPending} style={primaryBtnStyle}>
              {isPending ? 'Zapisywanie...' : policy ? 'Zapisz' : 'Utwórz'}
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
  width: '34px', height: '34px', border: `1px solid ${colors.gray[300]}`, borderRadius: '10px',
  background: colors.white, color: colors.gray[700], cursor: 'pointer',
};
const primaryBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', gap: '6px',
  padding: '7px 16px', fontSize: '13px', fontWeight: 600,
  color: colors.white, backgroundColor: colors.primary[600], border: 'none', borderRadius: '999px', cursor: 'pointer', boxShadow: '0 6px 14px -4px rgba(61,109,242,0.45)',
};
const cancelBtnStyle: React.CSSProperties = {
  padding: '7px 16px', fontSize: '13px', fontWeight: 500,
  color: colors.gray[700], backgroundColor: colors.white, border: `1px solid ${colors.gray[300]}`, borderRadius: '999px', cursor: 'pointer',
};
const smallIconBtn: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '28px', height: '28px', background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500], borderRadius: '4px',
};
const errorBoxStyle: React.CSSProperties = {
  padding: '12px 16px', marginBottom: '16px', borderRadius: '12px',
  backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px',
};
const retryLinkStyle: React.CSSProperties = {
  marginLeft: '8px', color: colors.primary[600], background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: '13px',
};
const overlayStyle: React.CSSProperties = {
  position: 'fixed', inset: 0, backgroundColor: 'rgba(20,25,43,0.45)', backdropFilter: 'blur(3px)', WebkitBackdropFilter: 'blur(3px)', animation: 'wb-backdrop-in 0.18s ease both', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100,
};
const modalStyle: React.CSSProperties = {
  backgroundColor: colors.white, borderRadius: '20px', animation: 'wb-modal-in 0.22s cubic-bezier(0.22, 1, 0.36, 1) both', padding: '24px', width: '100%', maxWidth: '560px', maxHeight: '90vh', overflow: 'auto',
  boxShadow: '0 24px 64px -12px rgba(20,25,43,0.28), 0 0 0 1px rgba(20,25,43,0.04)',
};
const labelStyle: React.CSSProperties = {
  display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: colors.gray[700],
};
const inputStyle: React.CSSProperties = {
  width: '100%', padding: '8px 12px', fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', boxSizing: 'border-box',
};
