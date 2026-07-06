import { useState, type FormEvent } from 'react';
import { Briefcase, Plus, RefreshCw, Edit2, Trash2, X } from 'lucide-react';
import { usePositions, useCreatePosition, useUpdatePosition, useDeletePosition } from '@/api/hooks/useOrganization';
import type { PositionDto } from '@/api/types/organization';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function PositionsConfigPage() {
  const { data: positions, isLoading, error, refetch, isFetching } = usePositions();
  const createMut = useCreatePosition();
  const updateMut = useUpdatePosition();
  const deleteMut = useDeletePosition();
  const mobile = useIsMobile();

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<PositionDto | null>(null);

  const handleDelete = (id: string) => {
    if (!confirm('Czy na pewno usunąć to stanowisko?')) return;
    deleteMut.mutate(id);
  };

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '900px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: colors.gray[900] }}>Stanowiska</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={() => refetch()} style={iconBtnStyle} title="Odśwież">
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button onClick={() => { setEditing(null); setShowForm(true); }} style={primaryBtnStyle}>
            <Plus size={16} /> Nowe stanowisko
          </button>
        </div>
      </div>

      {error && (
        <div style={errorStyle}>
          Błąd ładowania stanowisk.
          <button onClick={() => refetch()} style={retryStyle}>Ponów</button>
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500] }}>Ładowanie...</div>
      ) : !positions || positions.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400] }}>
          <Briefcase size={40} style={{ marginBottom: 12, opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak stanowisk</div>
          <div style={{ fontSize: '13px', marginTop: 4 }}>Dodaj pierwsze klikając „Nowe stanowisko".</div>
        </div>
      ) : (
        <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '8px', overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50] }}>
                <Th>Nazwa</Th>
                <Th>Opis</Th>
                <Th>Status</Th>
                <Th style={{ width: 80 }} />
              </tr>
            </thead>
            <tbody>
              {positions.map((p) => (
                <tr key={p.id} style={{ borderTop: `1px solid ${colors.gray[200]}` }}>
                  <Td style={{ fontWeight: 500 }}>{p.name}</Td>
                  <Td>{p.description ?? '—'}</Td>
                  <Td>
                    <span style={{
                      padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 500,
                      backgroundColor: p.isActive ? '#d1fae5' : colors.gray[100],
                      color: p.isActive ? '#065f46' : colors.gray[500],
                    }}>
                      {p.isActive ? 'Aktywne' : 'Nieaktywne'}
                    </span>
                  </Td>
                  <Td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button onClick={() => { setEditing(p); setShowForm(true); }} style={smBtnStyle} title="Edytuj">
                        <Edit2 size={14} />
                      </button>
                      <button onClick={() => handleDelete(p.id)} style={{ ...smBtnStyle, color: colors.danger[600] }} title="Usuń">
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
        <PositionFormModal
          position={editing}
          isPending={editing ? updateMut.isPending : createMut.isPending}
          error={editing ? updateMut.error : createMut.error}
          onSubmit={(data) => {
            if (editing) {
              updateMut.mutate({ id: editing.id, ...data }, {
                onSuccess: () => { setShowForm(false); setEditing(null); updateMut.reset(); },
              });
            } else {
              createMut.mutate(data, {
                onSuccess: () => { setShowForm(false); createMut.reset(); },
              });
            }
          }}
          onClose={() => { setShowForm(false); setEditing(null); createMut.reset(); updateMut.reset(); }}
        />
      )}
    </div>
  );
}

function PositionFormModal({ position, isPending, error, onSubmit, onClose }: {
  position: PositionDto | null;
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: { name: string; description?: string }) => void;
  onClose: () => void;
}) {
  const [name, setName] = useState(position?.name ?? '');
  const [description, setDescription] = useState(position?.description ?? '');

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    onSubmit({ name, description: description || undefined });
  };

  return (
    <div style={overlayStyle} onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div style={modalStyle}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
          <h2 style={{ margin: 0, fontSize: 18, fontWeight: 600 }}>
            {position ? 'Edytuj stanowisko' : 'Nowe stanowisko'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500] }}>
            <X size={20} />
          </button>
        </div>

        {error && <div style={formErrorStyle}>{error.message}</div>}

        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: 14 }}>
            <label style={labelStyle}>Nazwa *</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required style={inputStyle} />
          </div>
          <div style={{ marginBottom: 14 }}>
            <label style={labelStyle}>Opis</label>
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} style={inputStyle} />
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 20 }}>
            <button type="button" onClick={onClose} style={cancelBtnStyle}>Anuluj</button>
            <button type="submit" disabled={isPending || !name} style={{ ...submitBtnStyle, opacity: isPending ? 0.7 : 1 }}>
              {isPending ? 'Zapisywanie...' : position ? 'Zapisz' : 'Utwórz'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Th({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 12, fontWeight: 600, color: colors.gray[500], textTransform: 'uppercase', letterSpacing: '0.05em', ...style }}>{children}</th>;
}

function Td({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <td style={{ padding: '10px 16px', ...style }}>{children}</td>;
}

const iconBtnStyle: React.CSSProperties = { display: 'inline-flex', alignItems: 'center', padding: '8px', border: `1px solid ${colors.gray[300]}`, borderRadius: 6, backgroundColor: colors.white, cursor: 'pointer', color: colors.gray[700] };
const primaryBtnStyle: React.CSSProperties = { display: 'inline-flex', alignItems: 'center', gap: 6, padding: '8px 16px', fontSize: 14, fontWeight: 500, color: colors.white, backgroundColor: colors.primary[500], border: 'none', borderRadius: 6, cursor: 'pointer' };
const smBtnStyle: React.CSSProperties = { padding: '4px 6px', background: 'none', border: `1px solid ${colors.gray[200]}`, borderRadius: 4, cursor: 'pointer', color: colors.gray[500], display: 'inline-flex', alignItems: 'center' };
const errorStyle: React.CSSProperties = { padding: '12px 16px', marginBottom: 16, backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, borderRadius: 8, color: colors.danger[800], fontSize: 14 };
const retryStyle: React.CSSProperties = { marginLeft: 8, color: colors.primary[500], background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: 14 };
const overlayStyle: React.CSSProperties = { position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modalStyle: React.CSSProperties = { backgroundColor: colors.white, borderRadius: 12, padding: 24, width: '100%', maxWidth: 480, boxShadow: '0 20px 60px rgba(0,0,0,0.15)' };
const formErrorStyle: React.CSSProperties = { padding: '10px 14px', marginBottom: 12, backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, borderRadius: 6, color: colors.danger[600], fontSize: 13 };
const labelStyle: React.CSSProperties = { display: 'block', marginBottom: 4, fontSize: 13, fontWeight: 500, color: colors.gray[700] };
const inputStyle: React.CSSProperties = { width: '100%', padding: '8px 12px', fontSize: 14, border: `1px solid ${colors.gray[300]}`, borderRadius: 6, boxSizing: 'border-box' };
const cancelBtnStyle: React.CSSProperties = { padding: '8px 16px', fontSize: 14, border: `1px solid ${colors.gray[300]}`, borderRadius: 6, backgroundColor: colors.white, cursor: 'pointer' };
const submitBtnStyle: React.CSSProperties = { padding: '8px 20px', fontSize: 14, fontWeight: 500, color: colors.white, backgroundColor: colors.primary[500], border: 'none', borderRadius: 6, cursor: 'pointer' };
