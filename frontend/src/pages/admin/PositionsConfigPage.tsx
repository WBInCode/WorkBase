import { useState, type FormEvent } from 'react';
import { Briefcase, Plus, RefreshCw, Edit2, Trash2, X } from 'lucide-react';
import { usePositions, useCreatePosition, useUpdatePosition, useDeletePosition } from '@/api/hooks/useOrganization';
import { useRoles } from '@/api/hooks/useIam';
import type { PositionDto } from '@/api/types/organization';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

// Role systemowe nadaje WB Platform — stanowisko moze wskazac tylko role organizacyjne.
const HUB_MANAGED_ROLE_NAMES = ['Admin', 'Super Admin'];

export function PositionsConfigPage() {
  const { data: positions, isLoading, error, refetch, isFetching } = usePositions();
  const { data: roles } = useRoles();
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
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>Stanowiska</h1>
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
        <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', overflowX: 'auto', backgroundColor: colors.white, boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50] }}>
                <Th>Nazwa</Th>
                <Th>Opis</Th>
                <Th>Rola w WorkBase</Th>
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
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
                      <span>{roles?.find((role) => role.id === p.defaultRoleId)?.name ?? '—'}</span>
                      {p.isManagerial && (
                        <span style={{ padding: '2px 8px', borderRadius: 16, fontSize: 11, fontWeight: 600, backgroundColor: colors.primary[100], color: colors.primary[800] }}>
                          kierownicze
                        </span>
                      )}
                    </div>
                  </Td>
                  <Td>
                    <span style={{
                      padding: '2px 8px', borderRadius: 16, fontSize: 12, fontWeight: 500,
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
          roles={(roles ?? []).filter((role) => !HUB_MANAGED_ROLE_NAMES.includes(role.name))}
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

function PositionFormModal({ position, roles, isPending, error, onSubmit, onClose }: {
  position: PositionDto | null;
  roles: { id: string; name: string }[];
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: { name: string; description?: string; defaultRoleId?: string; isManagerial: boolean }) => void;
  onClose: () => void;
}) {
  const [name, setName] = useState(position?.name ?? '');
  const [description, setDescription] = useState(position?.description ?? '');
  const [defaultRoleId, setDefaultRoleId] = useState(position?.defaultRoleId ?? '');
  const [isManagerial, setIsManagerial] = useState(position?.isManagerial ?? false);

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    onSubmit({
      name,
      description: description || undefined,
      defaultRoleId: defaultRoleId || undefined,
      isManagerial,
    });
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
          <div style={{ marginBottom: 14 }}>
            <label style={labelStyle}>Rola nadawana przy przypisaniu</label>
            <select value={defaultRoleId} onChange={(e) => setDefaultRoleId(e.target.value)} style={inputStyle}>
              <option value="">Bez zmiany roli</option>
              {roles.map((role) => (
                <option key={role.id} value={role.id}>{role.name}</option>
              ))}
            </select>
            <div style={{ marginTop: 4, fontSize: 12, color: colors.gray[500] }}>
              Pracownik dostaje tę rolę przy przypisaniu na stanowisko; poprzednia rola ze stanowiska jest odbierana.
            </div>
          </div>
          <div style={{ marginBottom: 14 }}>
            <label style={{ display: 'flex', alignItems: 'flex-start', gap: 8, fontSize: 13, color: colors.gray[700], cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={isManagerial}
                onChange={(e) => setIsManagerial(e.target.checked)}
                style={{ marginTop: 2 }}
              />
              <span>
                Stanowisko kierownicze
                <span style={{ display: 'block', fontSize: 12, color: colors.gray[500] }}>
                  Osoba na tym stanowisku zostaje przełożonym pracowników swojej jednostki i akceptuje ich wnioski.
                </span>
              </span>
            </label>
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

const iconBtnStyle: React.CSSProperties = { display: 'inline-flex', alignItems: 'center', padding: '8px', border: `1px solid ${colors.gray[300]}`, borderRadius: 10, backgroundColor: colors.white, cursor: 'pointer', color: colors.gray[700] };
const primaryBtnStyle: React.CSSProperties = { display: 'inline-flex', alignItems: 'center', gap: 6, padding: '8px 16px', fontSize: 14, fontWeight: 500, color: colors.white, backgroundColor: colors.primary[500], border: 'none', borderRadius: 10, cursor: 'pointer' };
const smBtnStyle: React.CSSProperties = { padding: '4px 6px', background: 'none', border: `1px solid ${colors.gray[200]}`, borderRadius: 4, cursor: 'pointer', color: colors.gray[500], display: 'inline-flex', alignItems: 'center' };
const errorStyle: React.CSSProperties = { padding: '12px 16px', marginBottom: 16, backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, borderRadius: 12, color: colors.danger[800], fontSize: 14 };
const retryStyle: React.CSSProperties = { marginLeft: 8, color: colors.primary[500], background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: 14 };
const overlayStyle: React.CSSProperties = { position: 'fixed', inset: 0, backgroundColor: 'rgba(20,25,43,0.45)', backdropFilter: 'blur(3px)', WebkitBackdropFilter: 'blur(3px)', animation: 'wb-backdrop-in 0.18s ease both', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modalStyle: React.CSSProperties = { backgroundColor: colors.white, borderRadius: 20, animation: 'wb-modal-in 0.22s cubic-bezier(0.22, 1, 0.36, 1) both', padding: 24, width: '100%', maxWidth: 480, boxShadow: '0 24px 64px -12px rgba(20,25,43,0.28), 0 0 0 1px rgba(20,25,43,0.04)' };
const formErrorStyle: React.CSSProperties = { padding: '10px 14px', marginBottom: 12, backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, borderRadius: 10, color: colors.danger[600], fontSize: 13 };
const labelStyle: React.CSSProperties = { display: 'block', marginBottom: 4, fontSize: 13, fontWeight: 500, color: colors.gray[700] };
const inputStyle: React.CSSProperties = { width: '100%', padding: '8px 12px', fontSize: 14, border: `1px solid ${colors.gray[300]}`, borderRadius: 10, boxSizing: 'border-box' };
const cancelBtnStyle: React.CSSProperties = { padding: '8px 16px', fontSize: 14, border: `1px solid ${colors.gray[300]}`, borderRadius: 10, backgroundColor: colors.white, cursor: 'pointer' };
const submitBtnStyle: React.CSSProperties = { padding: '8px 20px', fontSize: 14, fontWeight: 500, color: colors.white, backgroundColor: colors.primary[500], border: 'none', borderRadius: 10, cursor: 'pointer' };
