import { useState, useCallback } from 'react';
import { ListTodo, Plus, RefreshCw, Edit2, Trash2, X } from 'lucide-react';
import { useTaskStatuses, useCreateTaskStatus, useUpdateTaskStatus, useDeleteTaskStatus } from '@/api/hooks/useTasks';
import type { TaskStatusDto, CreateTaskStatusRequest, UpdateTaskStatusRequest } from '@/api/types/tasks';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function TaskStatusConfigPage() {
  const { data: statuses, isLoading, error, refetch, isFetching } = useTaskStatuses();
  const createMutation = useCreateTaskStatus();
  const updateMutation = useUpdateTaskStatus();
  const deleteMutation = useDeleteTaskStatus();
  const mobile = useIsMobile();

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<TaskStatusDto | null>(null);

  const handleCreate = useCallback(
    (req: CreateTaskStatusRequest) => {
      createMutation.mutate(req, {
        onSuccess: () => { setShowForm(false); createMutation.reset(); },
      });
    },
    [createMutation],
  );

  const handleUpdate = useCallback(
    (id: string, req: UpdateTaskStatusRequest) => {
      updateMutation.mutate({ id, ...req }, {
        onSuccess: () => { setEditing(null); setShowForm(false); updateMutation.reset(); },
      });
    },
    [updateMutation],
  );

  const handleDelete = useCallback(
    (id: string) => {
      if (!confirm('Czy na pewno chcesz usunąć ten status?')) return;
      deleteMutation.mutate(id);
    },
    [deleteMutation],
  );

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '900px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>Statusy zadań</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={() => refetch()} style={iconBtnStyle} title="Odśwież">
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button onClick={() => { setEditing(null); setShowForm(true); }} style={primaryBtnStyle}>
            <Plus size={16} /> Nowy status
          </button>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div style={errorBoxStyle}>
          Błąd ładowania statusów.
          <button onClick={() => refetch()} style={retryLinkStyle}>Ponów</button>
        </div>
      )}

      {/* Loading / Empty / Table */}
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
      ) : !statuses || statuses.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400] }}>
          <ListTodo size={40} style={{ marginBottom: '12px', opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak statusów</div>
          <div style={{ fontSize: '13px', marginTop: '4px' }}>Dodaj pierwszy status klikając „Nowy status".</div>
        </div>
      ) : (
        <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', overflowX: 'auto', backgroundColor: colors.white, boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50] }}>
                <Th>Kod</Th>
                <Th>Nazwa</Th>
                <Th>Kolor</Th>
                <Th>Domyślny</Th>
                <Th>Końcowy</Th>
                <Th>Kolejność</Th>
                <Th style={{ width: '80px' }}></Th>
              </tr>
            </thead>
            <tbody>
              {statuses.map((s) => (
                <tr key={s.id} style={{ borderTop: `1px solid ${colors.gray[200]}` }}>
                  <Td><code style={{ fontSize: '12px', background: colors.gray[100], padding: '2px 6px', borderRadius: '4px' }}>{s.code}</code></Td>
                  <Td style={{ fontWeight: 500 }}>{s.name}</Td>
                  <Td>
                    {s.color && (
                      <span style={{ display: 'inline-block', width: 16, height: 16, borderRadius: 4, backgroundColor: s.color, border: `1px solid ${colors.gray[300]}` }} />
                    )}
                  </Td>
                  <Td>
                    {s.isDefault && (
                      <span style={{ display: 'inline-block', padding: '2px 8px', fontSize: '11px', fontWeight: 600, borderRadius: '10px', backgroundColor: colors.primary[100], color: colors.primary[800] }}>Domyślny</span>
                    )}
                  </Td>
                  <Td>
                    {s.isFinal && (
                      <span style={{ display: 'inline-block', padding: '2px 8px', fontSize: '11px', fontWeight: 600, borderRadius: '10px', backgroundColor: colors.success[100], color: colors.success[800] }}>Końcowy</span>
                    )}
                  </Td>
                  <Td>{s.sortOrder}</Td>
                  <Td>
                    <div style={{ display: 'flex', gap: '4px' }}>
                      <button onClick={() => { setEditing(s); setShowForm(true); }} style={smallIconBtn} title="Edytuj">
                        <Edit2 size={14} />
                      </button>
                      <button onClick={() => handleDelete(s.id)} style={{ ...smallIconBtn, color: colors.danger[600] }} title="Usuń">
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
        <StatusFormModal
          status={editing}
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
function StatusFormModal({ status, isPending, error, onSubmit, onClose }: {
  status: TaskStatusDto | null;
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: CreateTaskStatusRequest) => void;
  onClose: () => void;
}) {
  const [code, setCode] = useState(status?.code ?? '');
  const [name, setName] = useState(status?.name ?? '');
  const [color, setColor] = useState(status?.color ?? '#3d6df2');
  const [isFinal, setIsFinal] = useState(status?.isFinal ?? false);
  const [isDefault, setIsDefault] = useState(status?.isDefault ?? false);
  const [sortOrder, setSortOrder] = useState(status?.sortOrder?.toString() ?? '0');
  const mobile = useIsMobile();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({
      code,
      name,
      color: color || undefined,
      isFinal,
      isDefault,
      sortOrder: Number(sortOrder) || 0,
    });
  };

  return (
    <div style={overlayStyle}>
      <div style={modalStyle}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600, color: colors.gray[900] }}>
            {status ? 'Edytuj status' : 'Nowy status'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500] }}><X size={20} /></button>
        </div>

        {error && (
          <div style={{ ...errorBoxStyle, marginBottom: '12px' }}>Błąd: {error.message}</div>
        )}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Kod
              <input value={code} onChange={(e) => setCode(e.target.value)} required style={inputStyle} placeholder="np. TODO" />
            </label>
            <label style={labelStyle}>
              Nazwa
              <input value={name} onChange={(e) => setName(e.target.value)} required style={inputStyle} placeholder="np. Do zrobienia" />
            </label>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
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
            <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', color: colors.gray[700], cursor: 'pointer' }}>
              <input type="checkbox" checked={isDefault} onChange={(e) => setIsDefault(e.target.checked)} /> Domyślny
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', color: colors.gray[700], cursor: 'pointer' }}>
              <input type="checkbox" checked={isFinal} onChange={(e) => setIsFinal(e.target.checked)} /> Końcowy
            </label>
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
            <button type="button" onClick={onClose} style={cancelBtnStyle}>Anuluj</button>
            <button type="submit" disabled={isPending} style={primaryBtnStyle}>
              {isPending ? 'Zapisywanie...' : status ? 'Zapisz' : 'Utwórz'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

/* ── Table helpers ── */
function Th({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <th style={{ padding: '10px 14px', textAlign: 'left', fontSize: '12px', fontWeight: 600, color: colors.gray[500], whiteSpace: 'nowrap', ...style }}>{children}</th>;
}
function Td({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <td style={{ padding: '10px 14px', color: colors.gray[900], ...style }}>{children}</td>;
}

/* ── Styles ── */
const iconBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '34px', height: '34px', border: `1px solid ${colors.gray[300]}`, borderRadius: '10px',
  background: colors.white, color: colors.gray[700], cursor: 'pointer',
};
const primaryBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', gap: '6px',
  padding: '7px 16px', fontSize: '13px', fontWeight: 600,
  color: colors.white, backgroundColor: '#3d6df2', border: 'none', borderRadius: '999px', cursor: 'pointer', boxShadow: '0 6px 14px -4px rgba(61,109,242,0.45)',
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
  backgroundColor: colors.white, borderRadius: '20px', animation: 'wb-modal-in 0.22s cubic-bezier(0.22, 1, 0.36, 1) both', padding: '24px', width: '100%', maxWidth: '460px', maxHeight: '90vh', overflow: 'auto',
  boxShadow: '0 24px 64px -12px rgba(20,25,43,0.28), 0 0 0 1px rgba(20,25,43,0.04)',
};
const labelStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: colors.gray[700] };
const inputStyle: React.CSSProperties = {
  padding: '8px 10px', fontSize: '13px', border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', outline: 'none',
};
