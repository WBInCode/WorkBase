import { useState, type FormEvent } from 'react';
import { Layers, Plus, RefreshCw, Edit2, Trash2, X } from 'lucide-react';
import { useUnitTypes, useCreateUnitType, useUpdateUnitType, useDeleteUnitType } from '@/api/hooks/useOrganization';
import type { OrganizationUnitType } from '@/api/types/organization';

export function UnitTypesConfigPage() {
  const { data: types, isLoading, error, refetch, isFetching } = useUnitTypes();
  const createMut = useCreateUnitType();
  const updateMut = useUpdateUnitType();
  const deleteMut = useDeleteUnitType();

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<OrganizationUnitType | null>(null);

  const handleDelete = (id: string) => {
    if (!confirm('Czy na pewno usunąć ten typ jednostki?')) return;
    deleteMut.mutate(id);
  };

  return (
    <div style={{ padding: '24px 32px', maxWidth: '900px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: '#111827' }}>Typy jednostek organizacyjnych</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={() => refetch()} style={iconBtnStyle} title="Odśwież">
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button onClick={() => { setEditing(null); setShowForm(true); }} style={primaryBtnStyle}>
            <Plus size={16} /> Nowy typ
          </button>
        </div>
      </div>

      {error && (
        <div style={errorStyle}>
          Błąd ładowania typów jednostek.
          <button onClick={() => refetch()} style={retryStyle}>Ponów</button>
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: '#6b7280' }}>Ładowanie...</div>
      ) : !types || types.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: '#9ca3af' }}>
          <Layers size={40} style={{ marginBottom: 12, opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak typów jednostek</div>
          <div style={{ fontSize: '13px', marginTop: 4 }}>Dodaj pierwszy klikając „Nowy typ".</div>
        </div>
      ) : (
        <div style={{ border: '1px solid #e5e7eb', borderRadius: '8px', overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: '#f9fafb' }}>
                <Th>Nazwa</Th>
                <Th>Opis</Th>
                <Th>Kolejność</Th>
                <Th>Status</Th>
                <Th style={{ width: 80 }} />
              </tr>
            </thead>
            <tbody>
              {types.map((t) => (
                <tr key={t.id} style={{ borderTop: '1px solid #e5e7eb' }}>
                  <Td style={{ fontWeight: 500 }}>{t.name}</Td>
                  <Td>{t.description ?? '—'}</Td>
                  <Td>{t.sortOrder}</Td>
                  <Td>
                    <span style={{
                      padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 500,
                      backgroundColor: t.isActive ? '#d1fae5' : '#f3f4f6',
                      color: t.isActive ? '#065f46' : '#6b7280',
                    }}>
                      {t.isActive ? 'Aktywny' : 'Nieaktywny'}
                    </span>
                  </Td>
                  <Td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button onClick={() => { setEditing(t); setShowForm(true); }} style={smBtnStyle} title="Edytuj">
                        <Edit2 size={14} />
                      </button>
                      <button onClick={() => handleDelete(t.id)} style={{ ...smBtnStyle, color: '#dc2626' }} title="Usuń">
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
        <UnitTypeFormModal
          unitType={editing}
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

function UnitTypeFormModal({ unitType, isPending, error, onSubmit, onClose }: {
  unitType: OrganizationUnitType | null;
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: { name: string; description?: string; sortOrder: number }) => void;
  onClose: () => void;
}) {
  const [name, setName] = useState(unitType?.name ?? '');
  const [description, setDescription] = useState(unitType?.description ?? '');
  const [sortOrder, setSortOrder] = useState(unitType?.sortOrder?.toString() ?? '0');

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    onSubmit({ name, description: description || undefined, sortOrder: Number(sortOrder) || 0 });
  };

  return (
    <div style={overlayStyle} onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div style={modalStyle}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
          <h2 style={{ margin: 0, fontSize: 18, fontWeight: 600 }}>
            {unitType ? 'Edytuj typ jednostki' : 'Nowy typ jednostki'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#6b7280' }}>
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
          <div style={{ marginBottom: 14 }}>
            <label style={labelStyle}>Kolejność sortowania</label>
            <input type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.value)} style={inputStyle} />
          </div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 20 }}>
            <button type="button" onClick={onClose} style={cancelBtnStyle}>Anuluj</button>
            <button type="submit" disabled={isPending || !name} style={{ ...submitBtnStyle, opacity: isPending ? 0.7 : 1 }}>
              {isPending ? 'Zapisywanie...' : unitType ? 'Zapisz' : 'Utwórz'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Th({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 12, fontWeight: 600, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em', ...style }}>{children}</th>;
}

function Td({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <td style={{ padding: '10px 16px', ...style }}>{children}</td>;
}

const iconBtnStyle: React.CSSProperties = { display: 'inline-flex', alignItems: 'center', padding: '8px', border: '1px solid #d1d5db', borderRadius: 6, backgroundColor: '#fff', cursor: 'pointer', color: '#374151' };
const primaryBtnStyle: React.CSSProperties = { display: 'inline-flex', alignItems: 'center', gap: 6, padding: '8px 16px', fontSize: 14, fontWeight: 500, color: '#fff', backgroundColor: '#3b82f6', border: 'none', borderRadius: 6, cursor: 'pointer' };
const smBtnStyle: React.CSSProperties = { padding: '4px 6px', background: 'none', border: '1px solid #e5e7eb', borderRadius: 4, cursor: 'pointer', color: '#6b7280', display: 'inline-flex', alignItems: 'center' };
const errorStyle: React.CSSProperties = { padding: '12px 16px', marginBottom: 16, backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: 8, color: '#991b1b', fontSize: 14 };
const retryStyle: React.CSSProperties = { marginLeft: 8, color: '#3b82f6', background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: 14 };
const overlayStyle: React.CSSProperties = { position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modalStyle: React.CSSProperties = { backgroundColor: '#fff', borderRadius: 12, padding: 24, width: '100%', maxWidth: 480, boxShadow: '0 20px 60px rgba(0,0,0,0.15)' };
const formErrorStyle: React.CSSProperties = { padding: '10px 14px', marginBottom: 12, backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: 6, color: '#dc2626', fontSize: 13 };
const labelStyle: React.CSSProperties = { display: 'block', marginBottom: 4, fontSize: 13, fontWeight: 500, color: '#374151' };
const inputStyle: React.CSSProperties = { width: '100%', padding: '8px 12px', fontSize: 14, border: '1px solid #d1d5db', borderRadius: 6, boxSizing: 'border-box' };
const cancelBtnStyle: React.CSSProperties = { padding: '8px 16px', fontSize: 14, border: '1px solid #d1d5db', borderRadius: 6, backgroundColor: '#fff', cursor: 'pointer' };
const submitBtnStyle: React.CSSProperties = { padding: '8px 20px', fontSize: 14, fontWeight: 500, color: '#fff', backgroundColor: '#3b82f6', border: 'none', borderRadius: 6, cursor: 'pointer' };
