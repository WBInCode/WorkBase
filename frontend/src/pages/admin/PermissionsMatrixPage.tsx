import { useState, useMemo, useCallback, useEffect } from 'react';
import { Save, RefreshCw, ChevronDown, ChevronRight, Check } from 'lucide-react';
import {
  useRoles,
  usePermissions,
  useRolePermissions,
  useUpdateRolePermissions,
} from '@/api/hooks/useIam';
import type { PermissionDto } from '@/api/types/iam';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

interface ModuleGroup {
  module: string;
  permissions: PermissionDto[];
}

export function PermissionsMatrixPage() {
  const { data: roles, isLoading: rolesLoading } = useRoles();
  const { data: permissions, isLoading: permsLoading, refetch, isFetching } = usePermissions();

  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const rolePermsQuery = useRolePermissions(selectedRoleId);
  const updateMutation = useUpdateRolePermissions();

  const [checkedIds, setCheckedIds] = useState<Set<string>>(new Set());
  const [dirty, setDirty] = useState(false);
  const [expandedModules, setExpandedModules] = useState<Set<string>>(new Set());

  // When role permissions are loaded, sync with checkbox state
  useEffect(() => {
    if (rolePermsQuery.data) {
      setCheckedIds(new Set(rolePermsQuery.data));
      setDirty(false);
    }
  }, [rolePermsQuery.data]);

  // Auto-select first non-system role, or first at all
  useEffect(() => {
    if (!selectedRoleId && roles && roles.length > 0) {
      const nonSystem = roles.find((r) => r.type !== 'System');
      setSelectedRoleId(nonSystem?.id ?? roles[0]?.id ?? null);
    }
  }, [roles, selectedRoleId]);

  // Expand all modules by default
  useEffect(() => {
    if (permissions && expandedModules.size === 0) {
      const modules = new Set(permissions.map((p) => p.module));
      setExpandedModules(modules);
    }
  }, [permissions, expandedModules.size]);

  const mobile = useIsMobile();

  const moduleGroups = useMemo<ModuleGroup[]>(() => {
    if (!permissions) return [];
    const map = new Map<string, PermissionDto[]>();
    for (const p of permissions) {
      const arr = map.get(p.module) ?? [];
      arr.push(p);
      map.set(p.module, arr);
    }
    return Array.from(map.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([module, perms]) => ({ module, permissions: perms }));
  }, [permissions]);

  const selectedRole = roles?.find((r) => r.id === selectedRoleId) ?? null;
  const isSystem = selectedRole?.type === 'System';

  const togglePermission = useCallback((permissionId: string) => {
    setCheckedIds((prev) => {
      const next = new Set(prev);
      if (next.has(permissionId)) {
        next.delete(permissionId);
      } else {
        next.add(permissionId);
      }
      return next;
    });
    setDirty(true);
  }, []);

  const toggleModule = useCallback((modulePerms: PermissionDto[]) => {
    setCheckedIds((prev) => {
      const next = new Set(prev);
      const allChecked = modulePerms.every((p) => next.has(p.id));
      for (const p of modulePerms) {
        if (allChecked) {
          next.delete(p.id);
        } else {
          next.add(p.id);
        }
      }
      return next;
    });
    setDirty(true);
  }, []);

  const toggleExpand = useCallback((module: string) => {
    setExpandedModules((prev) => {
      const next = new Set(prev);
      if (next.has(module)) {
        next.delete(module);
      } else {
        next.add(module);
      }
      return next;
    });
  }, []);

  const handleSave = useCallback(() => {
    if (!selectedRoleId) return;
    updateMutation.mutate(
      { roleId: selectedRoleId, permissionIds: Array.from(checkedIds) },
      { onSuccess: () => setDirty(false) },
    );
  }, [selectedRoleId, checkedIds, updateMutation]);

  const isLoading = rolesLoading || permsLoading;

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '1200px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: colors.gray[900] }}>
          Matryca uprawnień
        </h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            onClick={() => refetch()}
            style={iconBtnStyle}
            title="Odśwież"
            aria-label="Odśwież"
          >
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          {dirty && !isSystem && (
            <button
              onClick={handleSave}
              disabled={updateMutation.isPending}
              style={{
                ...primaryBtnStyle,
                opacity: updateMutation.isPending ? 0.6 : 1,
              }}
            >
              <Save size={16} />
              {updateMutation.isPending ? 'Zapisywanie...' : 'Zapisz zmiany'}
            </button>
          )}
        </div>
      </div>

      {/* Role selector */}
      <div style={{ marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '12px' }}>
        <label style={{ fontSize: '14px', fontWeight: 500, color: colors.gray[700] }}>Rola:</label>
        <select
          value={selectedRoleId ?? ''}
          onChange={(e) => {
            setSelectedRoleId(e.target.value || null);
            setDirty(false);
          }}
          style={selectStyle}
        >
          <option value="">— wybierz rolę —</option>
          {roles?.map((r) => (
            <option key={r.id} value={r.id}>
              {r.name} ({typeLabels[r.type] ?? r.type})
            </option>
          ))}
        </select>
        {isSystem && (
          <span style={{ fontSize: '13px', color: colors.warning[800], backgroundColor: colors.warning[100], padding: '4px 10px', borderRadius: '4px' }}>
            Rola systemowa — tylko podgląd
          </span>
        )}
      </div>

      {/* Saved confirmation */}
      {updateMutation.isSuccess && !dirty && (
        <div style={{ padding: '10px 16px', backgroundColor: colors.success[100], border: `1px solid ${colors.success[200]}`, borderRadius: '8px', color: colors.success[800], fontSize: '14px', marginBottom: '12px', display: 'flex', alignItems: 'center', gap: '6px' }}>
          <Check size={16} />
          Uprawnienia zostały zapisane.
        </div>
      )}

      {/* Error */}
      {updateMutation.error && (
        <div style={errorBoxStyle}>
          {(updateMutation.error as { message?: string })?.message ?? 'Błąd zapisywania uprawnień.'}
        </div>
      )}

      {/* Content */}
      {isLoading || rolePermsQuery.isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>
          Ładowanie...
        </div>
      ) : !selectedRoleId ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400], fontSize: '14px' }}>
          Wybierz rolę, aby zobaczyć matrycę uprawnień.
        </div>
      ) : (
        <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '8px', overflowX: 'auto' }}>
          {moduleGroups.map((group) => (
            <ModuleSection
              key={group.module}
              group={group}
              checkedIds={checkedIds}
              expanded={expandedModules.has(group.module)}
              disabled={isSystem}
              onTogglePermission={togglePermission}
              onToggleModule={toggleModule}
              onToggleExpand={toggleExpand}
            />
          ))}
        </div>
      )}
    </div>
  );
}

/* ---- Sub-components ---- */

function ModuleSection({
  group,
  checkedIds,
  expanded,
  disabled,
  onTogglePermission,
  onToggleModule,
  onToggleExpand,
}: {
  group: ModuleGroup;
  checkedIds: Set<string>;
  expanded: boolean;
  disabled: boolean;
  onTogglePermission: (id: string) => void;
  onToggleModule: (perms: PermissionDto[]) => void;
  onToggleExpand: (module: string) => void;
}) {
  const checkedCount = group.permissions.filter((p) => checkedIds.has(p.id)).length;
  const allChecked = checkedCount === group.permissions.length;
  const someChecked = checkedCount > 0 && !allChecked;

  return (
    <div style={{ borderBottom: `1px solid ${colors.gray[200]}` }}>
      {/* Module header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '10px',
          padding: '10px 14px',
          backgroundColor: colors.gray[50],
          cursor: 'pointer',
          userSelect: 'none',
        }}
        onClick={() => onToggleExpand(group.module)}
      >
        {expanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
        <input
          type="checkbox"
          checked={allChecked}
          ref={(el) => { if (el) el.indeterminate = someChecked; }}
          disabled={disabled}
          onChange={(e) => {
            e.stopPropagation();
            if (!disabled) onToggleModule(group.permissions);
          }}
          onClick={(e) => e.stopPropagation()}
          style={{ width: '16px', height: '16px', cursor: disabled ? 'default' : 'pointer' }}
        />
        <span style={{ fontWeight: 600, fontSize: '14px', color: colors.gray[900], textTransform: 'capitalize' }}>
          {moduleDisplayName(group.module)}
        </span>
        <span style={{ fontSize: '12px', color: colors.gray[500] }}>
          ({checkedCount}/{group.permissions.length})
        </span>
      </div>

      {/* Permissions list */}
      {expanded && (
        <div style={{ padding: '4px 14px 8px 44px' }}>
          {group.permissions.map((perm) => (
            <label
              key={perm.id}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '10px',
                padding: '6px 0',
                fontSize: '13px',
                color: colors.gray[700],
                cursor: disabled ? 'default' : 'pointer',
              }}
            >
              <input
                type="checkbox"
                checked={checkedIds.has(perm.id)}
                disabled={disabled}
                onChange={() => { if (!disabled) onTogglePermission(perm.id); }}
                style={{ width: '15px', height: '15px', cursor: disabled ? 'default' : 'pointer' }}
              />
              <span style={{ fontFamily: 'monospace', fontSize: '12px', color: colors.gray[500], minWidth: '180px' }}>
                {perm.fullCode}
              </span>
              <span>{perm.description ?? `${perm.action}${perm.scope ? ` (${perm.scope})` : ''}`}</span>
            </label>
          ))}
        </div>
      )}
    </div>
  );
}

/* ---- Helpers ---- */

const moduleDisplayNames: Record<string, string> = {
  org: 'Organizacja',
  employee: 'Pracownicy',
  time: 'Czas pracy',
  leave: 'Urlopy',
  task: 'Zadania',
  workflow: 'Procesy',
  doc: 'Dokumenty',
  report: 'Raporty',
  iam: 'Zarządzanie dostępem',
};

function moduleDisplayName(module: string): string {
  return moduleDisplayNames[module] ?? module;
}

const typeLabels: Record<string, string> = {
  System: 'Systemowa',
  Organizational: 'Organizacyjna',
  Custom: 'Niestandardowa',
};

/* ---- Styles ---- */

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

const selectStyle: React.CSSProperties = {
  padding: '8px 12px',
  fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px',
  outline: 'none',
  minWidth: '260px',
  backgroundColor: colors.white,
};

const errorBoxStyle: React.CSSProperties = {
  padding: '12px 16px',
  backgroundColor: colors.danger[50],
  border: `1px solid ${colors.danger[200]}`,
  borderRadius: '8px',
  color: colors.danger[600],
  fontSize: '14px',
  marginBottom: '12px',
};
