import { useState, useCallback } from 'react';
import { Shield, Plus, RefreshCw, Edit2, Users, Lock } from 'lucide-react';
import { useRoles, useCreateRole, useUpdateRole } from '@/api/hooks/useIam';
import type { RoleDto, CreateRoleRequest, UpdateRoleRequest } from '@/api/types/iam';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

const typeLabels: Record<string, string> = {
  System: 'Systemowa',
  Organizational: 'Organizacyjna',
  Custom: 'Niestandardowa',
};

const typeColors: Record<string, { bg: string; text: string }> = {
  System: { bg: colors.warning[100], text: colors.warning[800] },
  Organizational: { bg: colors.primary[100], text: colors.primary[800] },
  Custom: { bg: '#f3e8ff', text: '#6b21a8' },
};

export function RolesPage() {
  const { data: roles, isLoading, error, refetch, isFetching } = useRoles();
  const createMutation = useCreateRole();
  const updateMutation = useUpdateRole();
  const mobile = useIsMobile();

  const [showForm, setShowForm] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleDto | null>(null);

  const handleCreate = useCallback(
    (req: CreateRoleRequest) => {
      createMutation.mutate(req, {
        onSuccess: () => {
          setShowForm(false);
          createMutation.reset();
        },
      });
    },
    [createMutation],
  );

  const handleUpdate = useCallback(
    (id: string, req: UpdateRoleRequest) => {
      updateMutation.mutate({ id, ...req }, {
        onSuccess: () => {
          setEditingRole(null);
          updateMutation.reset();
        },
      });
    },
    [updateMutation],
  );

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '1000px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: colors.gray[900] }}>Role</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            onClick={() => refetch()}
            style={iconBtnStyle}
            title="Odśwież"
            aria-label="Odśwież"
          >
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button
            onClick={() => { setEditingRole(null); setShowForm(true); }}
            style={primaryBtnStyle}
          >
            <Plus size={16} />
            Nowa rola
          </button>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div style={errorBoxStyle}>
          Błąd ładowania ról.
          <button onClick={() => refetch()} style={retryLinkStyle}>Ponów</button>
        </div>
      )}

      {/* Loading */}
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>
          Ładowanie...
        </div>
      ) : !roles || roles.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400] }}>
          <Shield size={40} style={{ marginBottom: '12px', opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak ról</div>
          <div style={{ fontSize: '13px', marginTop: '4px' }}>
            Dodaj pierwszą rolę klikając „Nowa rola".
          </div>
        </div>
      ) : (
        <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '8px', overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50] }}>
                <Th>Nazwa</Th>
                <Th>Typ</Th>
                <Th>Poziom</Th>
                <Th>Uprawnienia</Th>
                <Th>Użytkownicy</Th>
                <Th>Status</Th>
                <Th style={{ width: '60px' }}></Th>
              </tr>
            </thead>
            <tbody>
              {roles.map((role) => (
                <RoleRow
                  key={role.id}
                  role={role}
                  onEdit={() => { setEditingRole(role); setShowForm(true); }}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create / Edit modal */}
      {showForm && (
        <RoleFormModal
          role={editingRole}
          isPending={editingRole ? updateMutation.isPending : createMutation.isPending}
          error={editingRole ? updateMutation.error : createMutation.error}
          onSubmit={(data) => {
            if (editingRole) {
              handleUpdate(editingRole.id, data);
            } else {
              handleCreate(data);
            }
          }}
          onClose={() => {
            setShowForm(false);
            setEditingRole(null);
            createMutation.reset();
            updateMutation.reset();
          }}
        />
      )}
    </div>
  );
}

/* ---- Sub-components ---- */

function RoleRow({ role, onEdit }: { role: RoleDto; onEdit: () => void }) {
  const tc = typeColors[role.type] ?? typeColors.Custom;
  return (
    <tr style={{ borderTop: `1px solid ${colors.gray[200]}` }}>
      <td style={cellStyle}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          {role.type === 'System' && <Lock size={14} style={{ color: colors.gray[400] }} />}
          <span style={{ fontWeight: 500, color: colors.gray[900] }}>{role.name}</span>
        </div>
        {role.description && (
          <div style={{ fontSize: '12px', color: colors.gray[500], marginTop: '2px' }}>{role.description}</div>
        )}
      </td>
      <td style={cellStyle}>
        <span style={{
          display: 'inline-block',
          padding: '2px 8px',
          borderRadius: '999px',
          fontSize: '12px',
          fontWeight: 500,
          backgroundColor: tc?.bg,
          color: tc?.text,
        }}>
          {typeLabels[role.type] ?? role.type}
        </span>
      </td>
      <td style={cellStyle}>{role.level}</td>
      <td style={cellStyle}>
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
          <Shield size={14} style={{ color: colors.gray[500] }} />
          {role.permissionCount}
        </span>
      </td>
      <td style={cellStyle}>
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
          <Users size={14} style={{ color: colors.gray[500] }} />
          {role.userCount}
        </span>
      </td>
      <td style={cellStyle}>
        <span style={{
          display: 'inline-block',
          padding: '2px 8px',
          borderRadius: '999px',
          fontSize: '12px',
          fontWeight: 500,
          backgroundColor: role.isActive ? colors.success[100] : colors.gray[100],
          color: role.isActive ? colors.success[800] : colors.gray[500],
        }}>
          {role.isActive ? 'Aktywna' : 'Nieaktywna'}
        </span>
      </td>
      <td style={cellStyle}>
        {role.type !== 'System' && (
          <button onClick={onEdit} style={iconBtnStyle} title="Edytuj" aria-label="Edytuj">
            <Edit2 size={14} />
          </button>
        )}
      </td>
    </tr>
  );
}

function RoleFormModal({
  role,
  isPending,
  error,
  onSubmit,
  onClose,
}: {
  role: RoleDto | null;
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: CreateRoleRequest | UpdateRoleRequest) => void;
  onClose: () => void;
}) {
  const [name, setName] = useState(role?.name ?? '');
  const [description, setDescription] = useState(role?.description ?? '');
  const [level, setLevel] = useState(role?.level ?? 0);

  return (
    <div style={overlayStyle} onClick={onClose}>
      <div
        style={modalStyle}
        onClick={(e) => e.stopPropagation()}
      >
        <h2 style={{ margin: '0 0 16px', fontSize: '18px', fontWeight: 600 }}>
          {role ? 'Edytuj rolę' : 'Nowa rola'}
        </h2>

        {error && (
          <div style={{ ...errorBoxStyle, marginBottom: '12px' }}>
            {(error as { message?: string })?.message ?? 'Wystąpił błąd.'}
          </div>
        )}

        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <label style={labelStyle}>
            Nazwa *
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              style={inputStyle}
              maxLength={128}
              autoFocus
            />
          </label>

          <label style={labelStyle}>
            Opis
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              style={{ ...inputStyle, minHeight: '60px', resize: 'vertical' }}
              maxLength={512}
            />
          </label>

          <label style={labelStyle}>
            Poziom
            <input
              type="number"
              value={level}
              onChange={(e) => setLevel(Number(e.target.value))}
              style={{ ...inputStyle, width: '100px' }}
              min={0}
              max={100}
            />
          </label>
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '20px' }}>
          <button onClick={onClose} style={secondaryBtnStyle}>Anuluj</button>
          <button
            onClick={() => onSubmit({ name, description: description || undefined, level })}
            disabled={isPending || !name.trim()}
            style={{
              ...primaryBtnStyle,
              opacity: isPending || !name.trim() ? 0.6 : 1,
              cursor: isPending || !name.trim() ? 'not-allowed' : 'pointer',
            }}
          >
            {isPending ? 'Zapisywanie...' : role ? 'Zapisz' : 'Utwórz'}
          </button>
        </div>
      </div>
    </div>
  );
}

function Th({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return (
    <th
      style={{
        padding: '10px 14px',
        textAlign: 'left',
        fontSize: '12px',
        fontWeight: 600,
        color: colors.gray[500],
        textTransform: 'uppercase',
        letterSpacing: '0.05em',
        ...style,
      }}
    >
      {children}
    </th>
  );
}

/* ---- Styles ---- */

const cellStyle: React.CSSProperties = {
  padding: '12px 14px',
  verticalAlign: 'middle',
};

const iconBtnStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  width: '32px',
  height: '32px',
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px',
  backgroundColor: colors.white,
  cursor: 'pointer',
  color: colors.gray[700],
};

const primaryBtnStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  gap: '6px',
  padding: '8px 16px',
  fontSize: '14px',
  fontWeight: 500,
  color: colors.white,
  backgroundColor: colors.primary[600],
  border: 'none',
  borderRadius: '6px',
  cursor: 'pointer',
};

const secondaryBtnStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  gap: '6px',
  padding: '8px 16px',
  fontSize: '14px',
  fontWeight: 500,
  color: colors.gray[700],
  backgroundColor: colors.white,
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px',
  cursor: 'pointer',
};

const errorBoxStyle: React.CSSProperties = {
  padding: '12px 16px',
  backgroundColor: colors.danger[50],
  border: `1px solid ${colors.danger[200]}`,
  borderRadius: '8px',
  color: colors.danger[600],
  fontSize: '14px',
};

const retryLinkStyle: React.CSSProperties = {
  marginLeft: '12px',
  color: colors.primary[600],
  background: 'none',
  border: 'none',
  cursor: 'pointer',
  fontSize: '14px',
  textDecoration: 'underline',
};

const overlayStyle: React.CSSProperties = {
  position: 'fixed',
  inset: 0,
  backgroundColor: 'rgba(0,0,0,0.4)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  zIndex: 1000,
};

const modalStyle: React.CSSProperties = {
  backgroundColor: colors.white,
  borderRadius: '12px',
  padding: '24px',
  width: '100%',
  maxWidth: '480px',
  boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
};

const labelStyle: React.CSSProperties = {
  display: 'flex',
  flexDirection: 'column',
  gap: '4px',
  fontSize: '13px',
  fontWeight: 500,
  color: colors.gray[700],
};

const inputStyle: React.CSSProperties = {
  padding: '8px 12px',
  fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px',
  outline: 'none',
  boxSizing: 'border-box',
};
