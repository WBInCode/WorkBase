import { useState, useCallback } from 'react';
import { Coffee, Plus, RefreshCw, Edit2, Trash2, X } from 'lucide-react';
import { useBreakPolicies, useCreateBreakPolicy, useUpdateBreakPolicy, useDeleteBreakPolicy } from '@/api/hooks/useTimeTracking';
import type { BreakPolicyDto } from '@/api/types/time';
import { useIsMobile } from '@/shared';

export function BreakPoliciesConfigPage() {
  const { data: policies, isLoading, error, refetch, isFetching } = useBreakPolicies();
  const createMutation = useCreateBreakPolicy();
  const updateMutation = useUpdateBreakPolicy();
  const deleteMutation = useDeleteBreakPolicy();
  const mobile = useIsMobile();

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<BreakPolicyDto | null>(null);

  const handleCreate = useCallback(
    (req: Omit<BreakPolicyDto, 'id'>) => {
      createMutation.mutate(req, {
        onSuccess: () => { setShowForm(false); createMutation.reset(); },
      });
    },
    [createMutation],
  );

  const handleUpdate = useCallback(
    (id: string, req: Omit<BreakPolicyDto, 'id'>) => {
      updateMutation.mutate({ id, ...req }, {
        onSuccess: () => { setEditing(null); setShowForm(false); updateMutation.reset(); },
      });
    },
    [updateMutation],
  );

  const handleDelete = useCallback(
    (id: string) => {
      if (!confirm('Czy na pewno chcesz usunąć tę politykę przerw?')) return;
      deleteMutation.mutate(id);
    },
    [deleteMutation],
  );

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '1000px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: '#111827' }}>Polityki przerw</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={() => refetch()} style={iconBtnStyle} title="Odśwież">
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button onClick={() => { setEditing(null); setShowForm(true); }} style={primaryBtnStyle}>
            <Plus size={16} /> Nowa polityka
          </button>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div style={errorBoxStyle}>
          Błąd ładowania polityk przerw.
          <button onClick={() => refetch()} style={retryLinkStyle}>Ponów</button>
        </div>
      )}

      {/* Loading / Empty / Table */}
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: '#6b7280', fontSize: '14px' }}>Ładowanie...</div>
      ) : !policies || policies.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: '#9ca3af' }}>
          <Coffee size={40} style={{ marginBottom: '12px', opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak polityk przerw</div>
          <div style={{ fontSize: '13px', marginTop: '4px' }}>Dodaj pierwszą politykę klikając „Nowa polityka".</div>
        </div>
      ) : (
        <div style={{ border: '1px solid #e5e7eb', borderRadius: '8px', overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: '#f9fafb' }}>
                <Th>Nazwa</Th>
                <Th>Typ</Th>
                <Th>Max / dzień</Th>
                <Th>Max min / przerwa</Th>
                <Th>Max min / dzień</Th>
                <Th>Aktywna</Th>
                <Th style={{ width: '80px' }}></Th>
              </tr>
            </thead>
            <tbody>
              {policies.map((p) => (
                <tr key={p.id} style={{ borderTop: '1px solid #e5e7eb' }}>
                  <Td style={{ fontWeight: 500 }}>{p.name}</Td>
                  <Td>
                    <span style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '4px',
                      padding: '2px 8px',
                      borderRadius: '12px',
                      fontSize: '12px',
                      fontWeight: 600,
                      backgroundColor: p.breakType === 'Paid' ? '#d1fae5' : '#fef3c7',
                      color: p.breakType === 'Paid' ? '#059669' : '#d97706',
                    }}>
                      {p.breakType === 'Paid' ? 'Płatna' : 'Bezpłatna'}
                    </span>
                  </Td>
                  <Td>{p.maxPerDay ?? <NoLimit />}</Td>
                  <Td>{p.maxMinutesPerBreak != null ? `${p.maxMinutesPerBreak} min` : <NoLimit />}</Td>
                  <Td>{p.maxMinutesPerDay != null ? `${p.maxMinutesPerDay} min` : <NoLimit />}</Td>
                  <Td>
                    <span style={{
                      display: 'inline-block',
                      width: '8px',
                      height: '8px',
                      borderRadius: '50%',
                      backgroundColor: p.isActive ? '#10b981' : '#d1d5db',
                    }} />
                    {' '}{p.isActive ? 'Tak' : 'Nie'}
                  </Td>
                  <Td>
                    <div style={{ display: 'flex', gap: '4px' }}>
                      <button onClick={() => { setEditing(p); setShowForm(true); }} style={smallIconBtn} title="Edytuj">
                        <Edit2 size={14} />
                      </button>
                      <button onClick={() => handleDelete(p.id)} style={{ ...smallIconBtn, color: '#dc2626' }} title="Usuń">
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

      {/* Modal */}
      {showForm && (
        <BreakPolicyFormModal
          policy={editing}
          isPending={editing ? updateMutation.isPending : createMutation.isPending}
          error={editing ? updateMutation.error : createMutation.error}
          onSubmit={(data) => {
            if (editing) handleUpdate(editing.id, data);
            else handleCreate(data);
          }}
          onClose={() => { setShowForm(false); setEditing(null); createMutation.reset(); updateMutation.reset(); }}
        />
      )}
    </div>
  );
}

/* ── Form Modal ── */
function BreakPolicyFormModal({ policy, isPending, error, onSubmit, onClose }: {
  policy: BreakPolicyDto | null;
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: Omit<BreakPolicyDto, 'id'>) => void;
  onClose: () => void;
}) {
  const [name, setName] = useState(policy?.name ?? '');
  const [breakType, setBreakType] = useState<string>(policy?.breakType ?? 'Paid');
  const [maxPerDay, setMaxPerDay] = useState(policy?.maxPerDay?.toString() ?? '');
  const [limitPerDay, setLimitPerDay] = useState(policy?.maxPerDay != null);
  const [maxMinutesPerBreak, setMaxMinutesPerBreak] = useState(policy?.maxMinutesPerBreak?.toString() ?? '');
  const [limitPerBreak, setLimitPerBreak] = useState(policy?.maxMinutesPerBreak != null);
  const [maxMinutesPerDay, setMaxMinutesPerDay] = useState(policy?.maxMinutesPerDay?.toString() ?? '');
  const [limitMinPerDay, setLimitMinPerDay] = useState(policy?.maxMinutesPerDay != null);
  const [isActive, setIsActive] = useState(policy?.isActive ?? true);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({
      name,
      breakType,
      maxPerDay: limitPerDay && maxPerDay ? Number(maxPerDay) : null,
      maxMinutesPerBreak: limitPerBreak && maxMinutesPerBreak ? Number(maxMinutesPerBreak) : null,
      maxMinutesPerDay: limitMinPerDay && maxMinutesPerDay ? Number(maxMinutesPerDay) : null,
      isActive,
    });
  };

  return (
    <div style={overlayStyle}>
      <div style={modalStyle}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600, color: '#111827' }}>
            {policy ? 'Edytuj politykę przerw' : 'Nowa polityka przerw'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#6b7280' }}><X size={20} /></button>
        </div>

        {error && (
          <div style={{ ...errorBoxStyle, marginBottom: '12px' }}>Błąd: {error.message}</div>
        )}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <label style={labelStyle}>
            Nazwa
            <input value={name} onChange={(e) => setName(e.target.value)} required style={inputStyle} placeholder="np. Przerwa śniadaniowa" />
          </label>

          <label style={labelStyle}>
            Typ przerwy
            <select value={breakType} onChange={(e) => setBreakType(e.target.value)} style={inputStyle} disabled={!!policy}>
              <option value="Paid">Płatna</option>
              <option value="Unpaid">Bezpłatna</option>
            </select>
          </label>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <LimitField
              label="Max przerw / dzień"
              enabled={limitPerDay}
              onToggle={setLimitPerDay}
              value={maxPerDay}
              onChange={setMaxPerDay}
              min={1}
            />
            <LimitField
              label="Max minut / przerwa"
              enabled={limitPerBreak}
              onToggle={setLimitPerBreak}
              value={maxMinutesPerBreak}
              onChange={setMaxMinutesPerBreak}
              min={1}
            />
            <LimitField
              label="Max minut / dzień"
              enabled={limitMinPerDay}
              onToggle={setLimitMinPerDay}
              value={maxMinutesPerDay}
              onChange={setMaxMinutesPerDay}
              min={1}
            />
          </div>

          <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', color: '#374151', cursor: 'pointer' }}>
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} /> Aktywna
          </label>

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

/* ── Limit toggle field ── */
function LimitField({ label, enabled, onToggle, value, onChange, min }: {
  label: string;
  enabled: boolean;
  onToggle: (v: boolean) => void;
  value: string;
  onChange: (v: string) => void;
  min: number;
}) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
      <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', color: '#374151', cursor: 'pointer', minWidth: '160px' }}>
        <input type="checkbox" checked={enabled} onChange={(e) => onToggle(e.target.checked)} />
        {label}
      </label>
      {enabled ? (
        <input
          type="number"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          style={{ ...inputStyle, width: '100px' }}
          min={min}
          required
        />
      ) : (
        <span style={{ fontSize: '12px', color: '#9ca3af', fontStyle: 'italic' }}>bez limitu</span>
      )}
    </div>
  );
}

/* ── NoLimit badge ── */
function NoLimit() {
  return <span style={{ fontSize: '11px', color: '#9ca3af', fontStyle: 'italic' }}>bez limitu</span>;
}

/* ── Table helpers ── */
function Th({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <th style={{ padding: '10px 14px', textAlign: 'left', fontSize: '12px', fontWeight: 600, color: '#6b7280', whiteSpace: 'nowrap', ...style }}>{children}</th>;
}
function Td({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <td style={{ padding: '10px 14px', color: '#111827', ...style }}>{children}</td>;
}

/* ── Styles ── */
const iconBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '34px', height: '34px', border: '1px solid #d1d5db', borderRadius: '6px',
  background: '#fff', color: '#374151', cursor: 'pointer',
};
const primaryBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', gap: '6px',
  padding: '7px 16px', fontSize: '13px', fontWeight: 600,
  color: '#fff', backgroundColor: '#4f46e5', border: 'none', borderRadius: '6px', cursor: 'pointer',
};
const cancelBtnStyle: React.CSSProperties = {
  padding: '7px 16px', fontSize: '13px', fontWeight: 500,
  color: '#374151', backgroundColor: '#fff', border: '1px solid #d1d5db', borderRadius: '6px', cursor: 'pointer',
};
const smallIconBtn: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '28px', height: '28px', background: 'none', border: 'none', cursor: 'pointer', color: '#6b7280', borderRadius: '4px',
};
const errorBoxStyle: React.CSSProperties = {
  padding: '12px 16px', marginBottom: '16px', borderRadius: '8px',
  backgroundColor: '#fef2f2', border: '1px solid #fecaca', color: '#991b1b', fontSize: '13px',
};
const retryLinkStyle: React.CSSProperties = {
  marginLeft: '8px', color: '#2563eb', background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: '13px',
};
const overlayStyle: React.CSSProperties = {
  position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100,
};
const modalStyle: React.CSSProperties = {
  backgroundColor: '#fff', borderRadius: '12px', padding: '24px', width: '100%', maxWidth: '520px', maxHeight: '90vh', overflow: 'auto',
  boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
};
const labelStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: '#374151' };
const inputStyle: React.CSSProperties = {
  padding: '8px 10px', fontSize: '13px', border: '1px solid #d1d5db', borderRadius: '6px', outline: 'none',
};
