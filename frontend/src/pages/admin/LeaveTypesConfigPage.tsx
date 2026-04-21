import { useState, useCallback } from 'react';
import { Palmtree, Plus, RefreshCw, Edit2, Trash2, X } from 'lucide-react';
import { useLeaveTypes, useCreateLeaveType, useUpdateLeaveType, useDeleteLeaveType } from '@/api/hooks/useLeave';
import type { LeaveTypeDto, CreateLeaveTypeRequest, UpdateLeaveTypeRequest } from '@/api/types/leave';

export function LeaveTypesConfigPage() {
  const { data: types, isLoading, error, refetch, isFetching } = useLeaveTypes();
  const createMutation = useCreateLeaveType();
  const updateMutation = useUpdateLeaveType();
  const deleteMutation = useDeleteLeaveType();

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<LeaveTypeDto | null>(null);

  const handleCreate = useCallback(
    (req: CreateLeaveTypeRequest) => {
      createMutation.mutate(req, {
        onSuccess: () => { setShowForm(false); createMutation.reset(); },
      });
    },
    [createMutation],
  );

  const handleUpdate = useCallback(
    (id: string, req: UpdateLeaveTypeRequest) => {
      updateMutation.mutate({ id, ...req }, {
        onSuccess: () => { setEditing(null); setShowForm(false); updateMutation.reset(); },
      });
    },
    [updateMutation],
  );

  const handleDelete = useCallback(
    (id: string) => {
      if (!confirm('Czy na pewno chcesz usunąć ten typ urlopu?')) return;
      deleteMutation.mutate(id);
    },
    [deleteMutation],
  );

  return (
    <div style={{ padding: '24px 32px', maxWidth: '1000px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: '#111827' }}>Typy urlopów</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            onClick={() => refetch()}
            style={iconBtnStyle}
            title="Odśwież"
          >
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button
            onClick={() => { setEditing(null); setShowForm(true); }}
            style={primaryBtnStyle}
          >
            <Plus size={16} /> Nowy typ
          </button>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div style={errorBoxStyle}>
          Błąd ładowania typów urlopów.
          <button onClick={() => refetch()} style={retryLinkStyle}>Ponów</button>
        </div>
      )}

      {/* Loading / Empty / Table */}
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: '#6b7280', fontSize: '14px' }}>Ładowanie...</div>
      ) : !types || types.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: '#9ca3af' }}>
          <Palmtree size={40} style={{ marginBottom: '12px', opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak typów urlopów</div>
          <div style={{ fontSize: '13px', marginTop: '4px' }}>Dodaj pierwszy typ klikając „Nowy typ".</div>
        </div>
      ) : (
        <div style={{ border: '1px solid #e5e7eb', borderRadius: '8px', overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: '#f9fafb' }}>
                <Th>Kod</Th>
                <Th>Nazwa</Th>
                <Th>Kolor</Th>
                <Th>Płatny</Th>
                <Th>Wymaga akceptacji</Th>
                <Th>Dni/rok</Th>
                <Th>Kolejność</Th>
                <Th style={{ width: '80px' }}></Th>
              </tr>
            </thead>
            <tbody>
              {types.map((t) => (
                <tr key={t.id} style={{ borderTop: '1px solid #e5e7eb' }}>
                  <Td><code style={{ fontSize: '12px', background: '#f3f4f6', padding: '2px 6px', borderRadius: '4px' }}>{t.code}</code></Td>
                  <Td style={{ fontWeight: 500 }}>{t.name}</Td>
                  <Td>
                    {t.color && (
                      <span style={{ display: 'inline-block', width: 16, height: 16, borderRadius: 4, backgroundColor: t.color, border: '1px solid #d1d5db' }} />
                    )}
                  </Td>
                  <Td>{t.isPaid ? 'Tak' : 'Nie'}</Td>
                  <Td>{t.requiresApproval ? 'Tak' : 'Nie'}</Td>
                  <Td>{t.defaultDaysPerYear ?? '—'}</Td>
                  <Td>{t.sortOrder}</Td>
                  <Td>
                    <div style={{ display: 'flex', gap: '4px' }}>
                      <button onClick={() => { setEditing(t); setShowForm(true); }} style={smallIconBtn} title="Edytuj">
                        <Edit2 size={14} />
                      </button>
                      <button onClick={() => handleDelete(t.id)} style={{ ...smallIconBtn, color: '#dc2626' }} title="Usuń">
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
        <LeaveTypeFormModal
          leaveType={editing}
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
function LeaveTypeFormModal({ leaveType, isPending, error, onSubmit, onClose }: {
  leaveType: LeaveTypeDto | null;
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: CreateLeaveTypeRequest) => void;
  onClose: () => void;
}) {
  const [code, setCode] = useState(leaveType?.code ?? '');
  const [name, setName] = useState(leaveType?.name ?? '');
  const [description, setDescription] = useState(leaveType?.description ?? '');
  const [isPaid, setIsPaid] = useState(leaveType?.isPaid ?? true);
  const [requiresApproval, setRequiresApproval] = useState(leaveType?.requiresApproval ?? true);
  const [defaultDays, setDefaultDays] = useState(leaveType?.defaultDaysPerYear?.toString() ?? '');
  const [color, setColor] = useState(leaveType?.color ?? '#6366f1');
  const [sortOrder, setSortOrder] = useState(leaveType?.sortOrder?.toString() ?? '0');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({
      code,
      name,
      description: description || undefined,
      isPaid,
      requiresApproval,
      defaultDaysPerYear: defaultDays ? Number(defaultDays) : undefined,
      color: color || undefined,
      sortOrder: Number(sortOrder) || 0,
    });
  };

  return (
    <div style={overlayStyle}>
      <div style={modalStyle}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600, color: '#111827' }}>
            {leaveType ? 'Edytuj typ urlopu' : 'Nowy typ urlopu'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#6b7280' }}><X size={20} /></button>
        </div>

        {error && (
          <div style={{ ...errorBoxStyle, marginBottom: '12px' }}>Błąd: {error.message}</div>
        )}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Kod
              <input value={code} onChange={(e) => setCode(e.target.value)} required style={inputStyle} placeholder="np. VACATION" />
            </label>
            <label style={labelStyle}>
              Nazwa
              <input value={name} onChange={(e) => setName(e.target.value)} required style={inputStyle} placeholder="np. Urlop wypoczynkowy" />
            </label>
          </div>
          <label style={labelStyle}>
            Opis
            <input value={description} onChange={(e) => setDescription(e.target.value)} style={inputStyle} />
          </label>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Dni/rok
              <input type="number" value={defaultDays} onChange={(e) => setDefaultDays(e.target.value)} style={inputStyle} min={0} />
            </label>
            <label style={labelStyle}>
              Kolor
              <input type="color" value={color} onChange={(e) => setColor(e.target.value)} style={{ ...inputStyle, height: '36px', padding: '2px' }} />
            </label>
            <label style={labelStyle}>
              Kolejność
              <input type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.value)} style={inputStyle} />
            </label>
          </div>
          <div style={{ display: 'flex', gap: '16px' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', color: '#374151', cursor: 'pointer' }}>
              <input type="checkbox" checked={isPaid} onChange={(e) => setIsPaid(e.target.checked)} /> Płatny
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', color: '#374151', cursor: 'pointer' }}>
              <input type="checkbox" checked={requiresApproval} onChange={(e) => setRequiresApproval(e.target.checked)} /> Wymaga akceptacji
            </label>
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
            <button type="button" onClick={onClose} style={cancelBtnStyle}>Anuluj</button>
            <button type="submit" disabled={isPending} style={primaryBtnStyle}>
              {isPending ? 'Zapisywanie...' : leaveType ? 'Zapisz' : 'Utwórz'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
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
  backgroundColor: '#fff', borderRadius: '12px', padding: '24px', width: '520px', maxHeight: '90vh', overflow: 'auto',
  boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
};
const labelStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: '#374151' };
const inputStyle: React.CSSProperties = {
  padding: '8px 10px', fontSize: '13px', border: '1px solid #d1d5db', borderRadius: '6px', outline: 'none',
};
