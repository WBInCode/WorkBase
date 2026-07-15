import { useState, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { Shield, Plus, RefreshCw, Edit2, Users, Lock, X, Mail, UserCheck, UserMinus } from 'lucide-react';
import { useRoles, useCreateRole, useUpdateRole, useRoleUsers, useUnassignUserRole, useCurrentUser } from '@/api/hooks/useIam';
import type { RoleDto, RoleUserDto, CreateRoleRequest, UpdateRoleRequest } from '@/api/types/iam';
import { ApiError } from '@/api/client';
import { useToast } from '@/components/Notifications';
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
  const [usersRole, setUsersRole] = useState<RoleDto | null>(null);

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
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>Role</h1>
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
        <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', overflowX: 'auto', backgroundColor: colors.white, boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)' }}>
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
                  onShowUsers={() => setUsersRole(role)}
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

      {usersRole && (
        <RoleUsersModal role={usersRole} onClose={() => setUsersRole(null)} />
      )}
    </div>
  );
}

/* ---- Sub-components ---- */

function RoleRow({
  role,
  onEdit,
  onShowUsers,
}: {
  role: RoleDto;
  onEdit: () => void;
  onShowUsers: () => void;
}) {
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
        <button
          type="button"
          onClick={onShowUsers}
          disabled={role.userCount === 0}
          title={role.userCount > 0 ? 'Pokaż przypisanych użytkowników' : 'Brak przypisanych użytkowników'}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '5px',
            padding: '4px 8px',
            border: role.userCount > 0 ? `1px solid ${colors.primary[200]}` : '1px solid transparent',
            borderRadius: '999px',
            backgroundColor: role.userCount > 0 ? colors.primary[50] : 'transparent',
            color: role.userCount > 0 ? colors.primary[700] : colors.gray[500],
            cursor: role.userCount > 0 ? 'pointer' : 'default',
            font: 'inherit',
          }}
        >
          <Users size={14} style={{ color: colors.gray[500] }} />
          {role.userCount}
        </button>
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

function RoleUsersModal({ role, onClose }: { role: RoleDto; onClose: () => void }) {
  const { data: users, isLoading, error, refetch, isFetching } = useRoleUsers(role.id);
  const { data: currentUser } = useCurrentUser();
  const unassignMutation = useUnassignUserRole();
  const { addToast } = useToast();
  const [userToUnassign, setUserToUnassign] = useState<RoleUserDto | null>(null);
  const canUnassignRoles = currentUser?.permissions.includes('platform.manage-tenants') ?? false;

  const unassignRole = () => {
    if (!userToUnassign) return;
    unassignMutation.mutate(
      { userId: userToUnassign.userId, roleId: role.id },
      {
        onSuccess: () => {
          addToast({ type: 'success', title: 'Odebrano rolę', message: `Użytkownik ${userToUnassign.email} nie ma już roli ${role.name}.` });
          setUserToUnassign(null);
        },
        onError: (mutationError) => {
          addToast({
            type: 'error',
            title: 'Nie udało się odebrać roli',
            message: mutationError instanceof ApiError ? mutationError.message : 'Spróbuj ponownie.',
          });
        },
      },
    );
  };

  return createPortal(
    <div style={{ ...overlayStyle, padding: '16px' }} onClick={onClose}>
      <div style={roleUsersModalStyle} onClick={(event) => event.stopPropagation()}>
        <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: '16px', padding: '20px 22px', borderBottom: `1px solid ${colors.gray[200]}` }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px', minWidth: 0 }}>
            <span style={{ width: '40px', height: '40px', borderRadius: '12px', display: 'grid', placeItems: 'center', flexShrink: 0, color: colors.primary[700], backgroundColor: colors.primary[100] }}>
              <Users size={19} />
            </span>
            <div style={{ minWidth: 0 }}>
              <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 700, color: colors.gray[900] }}>Użytkownicy roli</h2>
              <div style={{ marginTop: '2px', fontSize: '13px', color: colors.gray[500] }}>
                {role.name} · {users?.length ?? role.userCount} {(users?.length ?? role.userCount) === 1 ? 'osoba' : 'osób'}
              </div>
            </div>
          </div>
          <button type="button" onClick={onClose} style={iconBtnStyle} title="Zamknij" aria-label="Zamknij">
            <X size={16} />
          </button>
        </div>

        <div style={{ overflowY: 'auto', minHeight: '160px', maxHeight: 'min(60vh, 560px)' }}>
          {isLoading ? (
            <div style={{ padding: '48px 24px', textAlign: 'center', color: colors.gray[500], fontSize: '14px' }}>Ładowanie użytkowników...</div>
          ) : error ? (
            <div style={{ margin: '20px', ...errorBoxStyle }}>
              Nie udało się pobrać użytkowników.
              <button type="button" onClick={() => refetch()} style={retryLinkStyle}>{isFetching ? 'Ładowanie...' : 'Ponów'}</button>
            </div>
          ) : !users?.length ? (
            <div style={{ padding: '48px 24px', textAlign: 'center', color: colors.gray[500] }}>
              <UserCheck size={34} style={{ marginBottom: '10px', opacity: 0.45 }} />
              <div style={{ fontSize: '14px', fontWeight: 600 }}>Brak przypisanych użytkowników</div>
            </div>
          ) : (
            <>
              {userToUnassign && (
                <div style={{ margin: '16px 22px 4px', padding: '14px 16px', border: `1px solid ${colors.danger[200]}`, borderRadius: '12px', backgroundColor: colors.danger[50] }}>
                  <div style={{ fontSize: '13px', fontWeight: 700, color: colors.danger[700] }}>Odebrać rolę „{role.name}”?</div>
                  <div style={{ marginTop: '4px', fontSize: '12px', color: colors.gray[700] }}>{userToUnassign.email} utraci uprawnienia wynikające z tej roli.</div>
                  <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '12px' }}>
                    <button type="button" onClick={() => setUserToUnassign(null)} style={secondaryBtnStyle}>Anuluj</button>
                    <button
                      type="button"
                      onClick={unassignRole}
                      disabled={unassignMutation.isPending}
                      style={{ ...dangerBtnStyle, opacity: unassignMutation.isPending ? 0.6 : 1 }}
                    >
                      {unassignMutation.isPending ? 'Odbieranie...' : 'Odbierz rolę'}
                    </button>
                  </div>
                </div>
              )}
              {users.map((user, index) => {
              const fullName = `${user.firstName} ${user.lastName}`.trim() || user.email;
              return (
                <div
                  key={user.userId}
                  style={{ display: 'flex', alignItems: 'center', gap: '12px', padding: '14px 22px', borderTop: index > 0 ? `1px solid ${colors.gray[100]}` : 'none' }}
                >
                  <span style={{ width: '36px', height: '36px', borderRadius: '50%', display: 'grid', placeItems: 'center', flexShrink: 0, fontSize: '12px', fontWeight: 700, color: colors.primary[700], backgroundColor: colors.primary[100] }}>
                    {initials(fullName)}
                  </span>
                  <div style={{ minWidth: 0, flex: 1 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
                      <span style={{ fontSize: '14px', fontWeight: 600, color: colors.gray[900] }}>{fullName}</span>
                      {!user.isActive && (
                        <span style={{ padding: '2px 6px', borderRadius: '999px', fontSize: '10px', color: colors.gray[600], backgroundColor: colors.gray[100] }}>Nieaktywny</span>
                      )}
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '5px', marginTop: '2px', minWidth: 0, fontSize: '12px', color: colors.gray[500] }}>
                      <Mail size={12} style={{ flexShrink: 0 }} />
                      <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{user.email}</span>
                    </div>
                  </div>
                  <div style={{ flexShrink: 0, textAlign: 'right', fontSize: '11px', color: colors.gray[500] }}>
                    <div>{new Date(user.assignedAt).toLocaleDateString('pl-PL')}</div>
                    <div style={{ marginTop: '2px' }}>{user.assignedBy === 'system' ? 'Zarządzane w WB Platform' : 'Przez administratora'}</div>
                  </div>
                  {canUnassignRoles && user.assignedBy !== 'system' && (
                    <button
                      type="button"
                      onClick={() => setUserToUnassign(user)}
                      style={{ ...iconBtnStyle, color: colors.danger[600], borderColor: colors.danger[200] }}
                      title="Odbierz rolę"
                      aria-label={`Odbierz rolę użytkownikowi ${fullName}`}
                    >
                      <UserMinus size={15} />
                    </button>
                  )}
                </div>
              );
              })}
            </>
          )}
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '14px 22px', borderTop: `1px solid ${colors.gray[200]}` }}>
          <button type="button" onClick={onClose} style={secondaryBtnStyle}>Zamknij</button>
        </div>
      </div>
    </div>,
    document.body,
  );
}

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('');
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
  borderRadius: '10px',
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
  borderRadius: '10px',
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
  borderRadius: '10px',
  cursor: 'pointer',
};

const dangerBtnStyle: React.CSSProperties = {
  ...secondaryBtnStyle,
  color: colors.white,
  backgroundColor: colors.danger[600],
  borderColor: colors.danger[600],
};

const errorBoxStyle: React.CSSProperties = {
  padding: '12px 16px',
  backgroundColor: colors.danger[50],
  border: `1px solid ${colors.danger[200]}`,
  borderRadius: '12px',
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
  backgroundColor: 'rgba(20,25,43,0.45)', backdropFilter: 'blur(3px)', WebkitBackdropFilter: 'blur(3px)', animation: 'wb-backdrop-in 0.18s ease both',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  zIndex: 1000,
};

const modalStyle: React.CSSProperties = {
  backgroundColor: colors.white,
  borderRadius: '16px',
  padding: '24px',
  width: '100%',
  maxWidth: '480px',
  boxShadow: '0 24px 64px -12px rgba(20,25,43,0.28), 0 0 0 1px rgba(20,25,43,0.04)',
};

const roleUsersModalStyle: React.CSSProperties = {
  width: '100%',
  maxWidth: '640px',
  maxHeight: 'calc(100vh - 32px)',
  display: 'flex',
  flexDirection: 'column',
  overflow: 'hidden',
  backgroundColor: colors.white,
  borderRadius: '16px',
  boxShadow: '0 24px 64px -12px rgba(20,25,43,0.28), 0 0 0 1px rgba(20,25,43,0.04)',
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
  borderRadius: '10px',
  outline: 'none',
  boxSizing: 'border-box',
};
