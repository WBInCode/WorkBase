import { useState, type FormEvent } from 'react';
import { FolderTree, Building2, RefreshCw, Plus, Edit2, Trash2, X } from 'lucide-react';
import { useOrgUnitTree, useUnitTypes, useCreateOrgUnit, useUpdateOrgUnit, useDeleteOrgUnit } from '@/api/hooks/useOrganization';
import { OrgTree } from '@/components/OrgTree';
import type { OrganizationUnitTreeNode } from '@/api/types/organization';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function OrgTreePage() {
  const { data: tree, isLoading, error, refetch } = useOrgUnitTree();
  const [selectedNode, setSelectedNode] = useState<OrganizationUnitTreeNode | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editingNode, setEditingNode] = useState<OrganizationUnitTreeNode | null>(null);
  const [addParentId, setAddParentId] = useState<string | undefined>(undefined);

  const deleteMutation = useDeleteOrgUnit();
  const mobile = useIsMobile();

  const handleAddRoot = () => {
    setEditingNode(null);
    setAddParentId(undefined);
    setShowForm(true);
  };

  const handleAddChild = (parentId: string) => {
    setEditingNode(null);
    setAddParentId(parentId);
    setShowForm(true);
  };

  const handleEdit = (node: OrganizationUnitTreeNode) => {
    setEditingNode(node);
    setAddParentId(undefined);
    setShowForm(true);
  };

  const handleDelete = (node: OrganizationUnitTreeNode) => {
    if (!confirm(`Czy na pewno usunąć jednostkę "${node.name}"?`)) return;
    deleteMutation.mutate(node.id, {
      onSuccess: () => {
        if (selectedNode?.id === node.id) setSelectedNode(null);
      },
    });
  };

  const handleFormClose = () => {
    setShowForm(false);
    setEditingNode(null);
    setAddParentId(undefined);
  };

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: mobile ? '12px 16px' : '16px 24px',
          borderBottom: `1px solid ${colors.gray[200]}`,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <span style={{
            width: 40, height: 40, borderRadius: 12, backgroundColor: colors.primary[100], color: colors.primary[600],
            display: 'inline-flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
          }}>
            <FolderTree size={19} />
          </span>
          <h1 style={{ fontSize: '20px', fontWeight: 800, letterSpacing: '-0.02em', margin: 0, color: colors.gray[900] }}>
            Struktura organizacyjna
          </h1>
        </div>

        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            onClick={() => refetch()}
            disabled={isLoading}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 16px',
              fontSize: '13px',
              fontWeight: 600,
              fontFamily: 'inherit',
              color: colors.gray[700],
              backgroundColor: colors.white,
              border: `1px solid ${colors.gray[300]}`,
              borderRadius: '999px',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              opacity: isLoading ? 0.6 : 1,
            }}
          >
            <RefreshCw size={15} style={{ animation: isLoading ? 'spin 1s linear infinite' : 'none' }} />
            Odśwież
          </button>
          <button
            onClick={handleAddRoot}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 18px',
              fontSize: '13px',
              fontWeight: 700,
              fontFamily: 'inherit',
              color: colors.white,
              backgroundColor: colors.primary[500],
              border: 'none',
              borderRadius: '999px',
              cursor: 'pointer',
              boxShadow: '0 6px 14px -4px rgba(61,109,242,0.45)',
            }}
          >
            <Plus size={15} /> Dodaj jednostkę
          </button>
        </div>
      </div>

      {/* Content */}
      <div style={{ flex: 1, display: 'flex', flexDirection: mobile ? 'column' : 'row', overflow: 'hidden' }}>
        {/* Tree panel */}
        <div
          style={{
            flex: mobile ? 'none' : (selectedNode ? '0 0 50%' : '1'),
            overflowY: 'auto',
            padding: '12px',
            transition: 'flex 0.2s',
          }}
        >
          {isLoading && (
            <div style={{ padding: '32px', textAlign: 'center', color: colors.gray[500] }}>
              Ładowanie struktury...
            </div>
          )}

          {error && (
            <div
              style={{
                padding: '16px',
                margin: '12px',
                backgroundColor: colors.danger[50],
                border: `1px solid ${colors.danger[200]}`,
                borderRadius: '12px',
                color: colors.danger[800],
              }}
            >
              <p style={{ fontWeight: 600, marginBottom: '4px' }}>Błąd ładowania</p>
              <p style={{ fontSize: '14px' }}>{error.message}</p>
              <button
                onClick={() => refetch()}
                style={{
                  marginTop: '8px',
                  padding: '6px 12px',
                  fontSize: '13px',
                  backgroundColor: colors.white,
                  border: `1px solid ${colors.gray[300]}`,
                  borderRadius: '4px',
                  cursor: 'pointer',
                }}
              >
                Spróbuj ponownie
              </button>
            </div>
          )}

          {tree && (
            <OrgTree
              nodes={tree}
              onSelect={setSelectedNode}
              selectedId={selectedNode?.id ?? null}
            />
          )}
        </div>

        {/* Detail panel */}
        {selectedNode && (
          <div
            style={{
              flex: mobile ? 'none' : '0 0 50%',
              minWidth: 0,
              borderLeft: mobile ? 'none' : `1px solid ${colors.gray[200]}`,
              borderTop: mobile ? `1px solid ${colors.gray[200]}` : 'none',
              overflowY: 'auto',
              padding: mobile ? '16px' : '24px',
              backgroundColor: '#fafafa',
            }}
          >
            <UnitDetailPanel
              node={selectedNode}
              onClose={() => setSelectedNode(null)}
              onEdit={handleEdit}
              onDelete={handleDelete}
              onAddChild={handleAddChild}
            />
          </div>
        )}
      </div>

      {/* Form modal */}
      {showForm && (
        <OrgUnitFormModal
          unit={editingNode}
          parentId={addParentId}
          onClose={handleFormClose}
        />
      )}
    </div>
  );
}

function UnitDetailPanel({
  node,
  onClose,
  onEdit,
  onDelete,
  onAddChild,
}: {
  node: OrganizationUnitTreeNode;
  onClose: () => void;
  onEdit: (n: OrganizationUnitTreeNode) => void;
  onDelete: (n: OrganizationUnitTreeNode) => void;
  onAddChild: (parentId: string) => void;
}) {
  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '12px', marginBottom: '20px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', minWidth: 0, flex: '1 1 auto' }}>
          <Building2 size={22} style={{ color: colors.primary[500], flexShrink: 0 }} />
          <h2 style={{ fontSize: '18px', fontWeight: 600, margin: 0, color: colors.gray[900], whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
            {node.name}
          </h2>
        </div>
        <div style={{ display: 'flex', gap: '6px', flexShrink: 0, flexWrap: 'wrap', justifyContent: 'flex-end' }}>
          <button
            onClick={() => onAddChild(node.id)}
            title="Dodaj podjednostkę"
            style={{
              display: 'inline-flex', alignItems: 'center', gap: '4px',
              padding: '6px 12px', fontSize: '13px', fontWeight: 500,
              color: colors.white, backgroundColor: colors.primary[500], border: 'none',
              borderRadius: '4px', cursor: 'pointer',
            }}
          >
            <Plus size={14} /> Podjednostka
          </button>
          <button
            onClick={() => onEdit(node)}
            title="Edytuj"
            style={{
              padding: '6px 8px', background: 'none', border: `1px solid ${colors.gray[300]}`,
              borderRadius: '4px', cursor: 'pointer', color: colors.gray[700],
            }}
          >
            <Edit2 size={14} />
          </button>
          <button
            onClick={() => onDelete(node)}
            title="Usuń"
            style={{
              padding: '6px 8px', background: 'none', border: `1px solid ${colors.danger[200]}`,
              borderRadius: '4px', cursor: 'pointer', color: colors.danger[600],
            }}
          >
            <Trash2 size={14} />
          </button>
          <button
            onClick={onClose}
            style={{
              padding: '4px 10px', fontSize: '18px', background: 'none',
              border: 'none', cursor: 'pointer', color: colors.gray[500], borderRadius: '4px',
            }}
            aria-label="Zamknij panel"
          >
            ×
          </button>
        </div>
      </div>

      <div
        style={{
          backgroundColor: colors.white,
          border: `1px solid ${colors.gray[200]}`,
          borderRadius: '12px',
          overflow: 'hidden',
        }}
      >
        <DetailRow label="ID" value={node.id} />
        <DetailRow label="Nazwa" value={node.name} />
        {node.code && <DetailRow label="Kod" value={node.code} />}
        <DetailRow label="Typ" value={node.typeName} />
        <DetailRow
          label="Status"
          value={node.isActive ? 'Aktywna' : 'Nieaktywna'}
          valueColor={node.isActive ? colors.emerald[600] : colors.danger[600]}
        />
        <DetailRow label="Podjednostki" value={String(node.children.length)} />
      </div>

      {node.children.length > 0 && (
        <div style={{ marginTop: '20px' }}>
          <h3 style={{ fontSize: '14px', fontWeight: 600, color: colors.gray[700], marginBottom: '8px' }}>
            Podjednostki ({node.children.length})
          </h3>
          <div
            style={{
              backgroundColor: colors.white,
              border: `1px solid ${colors.gray[200]}`,
              borderRadius: '12px',
              overflow: 'hidden',
            }}
          >
            {node.children.map((child, i) => (
              <div
                key={child.id}
                style={{
                  padding: '10px 16px',
                  borderTop: i > 0 ? '1px solid #f3f4f6' : 'none',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  fontSize: '14px',
                }}
              >
                <span style={{ color: colors.gray[900] }}>{child.name}</span>
                <span style={{ color: colors.gray[400], fontSize: '12px' }}>{child.typeName}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function DetailRow({
  label,
  value,
  valueColor,
}: {
  label: string;
  value: string;
  valueColor?: string;
}) {
  return (
    <div
      style={{
        display: 'flex',
        padding: '10px 16px',
        borderBottom: '1px solid #f3f4f6',
        fontSize: '14px',
      }}
    >
      <span style={{ width: '120px', flexShrink: 0, color: colors.gray[500], fontWeight: 500 }}>
        {label}
      </span>
      <span style={{ color: valueColor ?? colors.gray[900], wordBreak: 'break-all' }}>{value}</span>
    </div>
  );
}

/* ── Org Unit Form Modal ── */

function OrgUnitFormModal({
  unit,
  parentId,
  onClose,
}: {
  unit: OrganizationUnitTreeNode | null;
  parentId?: string;
  onClose: () => void;
}) {
  const { data: unitTypes = [] } = useUnitTypes();
  const createMutation = useCreateOrgUnit();
  const updateMutation = useUpdateOrgUnit();
  const isEditing = !!unit;

  const [name, setName] = useState(unit?.name ?? '');
  const [code, setCode] = useState(unit?.code ?? '');
  const [typeId, setTypeId] = useState(unit?.typeId ?? '');

  const isPending = isEditing ? updateMutation.isPending : createMutation.isPending;
  const error = isEditing ? updateMutation.error : createMutation.error;

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (isEditing) {
      updateMutation.mutate(
        { id: unit.id, name, code: code || undefined, typeId },
        { onSuccess: onClose },
      );
    } else {
      createMutation.mutate(
        { name, code: code || undefined, typeId, parentId },
        { onSuccess: onClose },
      );
    }
  };

  return (
    <div
      style={{
        position: 'fixed', inset: 0, backgroundColor: 'rgba(20,25,43,0.45)', backdropFilter: 'blur(3px)', WebkitBackdropFilter: 'blur(3px)', animation: 'wb-backdrop-in 0.18s ease both',
        display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000,
      }}
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div
        style={{
          backgroundColor: 'var(--wb-panel, #fff)', borderRadius: '16px', padding: '24px',
          width: '100%', maxWidth: '480px', boxShadow: '0 24px 64px -12px rgba(20,25,43,0.28), 0 0 0 1px rgba(20,25,43,0.04)',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600 }}>
            {isEditing ? 'Edytuj jednostkę' : parentId ? 'Dodaj podjednostkę' : 'Nowa jednostka'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500] }}>
            <X size={20} />
          </button>
        </div>

        {error && (
          <div style={{ padding: '10px 14px', marginBottom: '12px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, borderRadius: '10px', color: colors.danger[600], fontSize: '13px' }}>
            {error.message}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: '14px' }}>
            <label style={labelStyle}>Nazwa *</label>
            <input
              value={name} onChange={(e) => setName(e.target.value)}
              required style={inputStyle}
            />
          </div>
          <div style={{ marginBottom: '14px' }}>
            <label style={labelStyle}>Kod</label>
            <input
              value={code} onChange={(e) => setCode(e.target.value)}
              style={inputStyle} placeholder="np. IT-DEV"
            />
          </div>
          <div style={{ marginBottom: '14px' }}>
            <label style={labelStyle}>Typ jednostki *</label>
            <select
              value={typeId} onChange={(e) => setTypeId(e.target.value)}
              required style={inputStyle}
            >
              <option value="">-- Wybierz --</option>
              {unitTypes.map((t) => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '20px' }}>
            <button type="button" onClick={onClose} style={{ padding: '8px 16px', fontSize: '14px', border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', backgroundColor: colors.white, cursor: 'pointer' }}>
              Anuluj
            </button>
            <button
              type="submit"
              disabled={isPending || !name || !typeId}
              style={{
                padding: '8px 20px', fontSize: '14px', fontWeight: 500,
                color: colors.white, backgroundColor: colors.primary[500], border: 'none',
                borderRadius: '10px', cursor: isPending ? 'not-allowed' : 'pointer',
                opacity: isPending ? 0.7 : 1,
              }}
            >
              {isPending ? 'Zapisywanie...' : isEditing ? 'Zapisz' : 'Utwórz'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

const labelStyle: React.CSSProperties = {
  display: 'block', marginBottom: '4px', fontSize: '13px', fontWeight: 500, color: colors.gray[700],
};

const inputStyle: React.CSSProperties = {
  width: '100%', padding: '8px 12px', fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', boxSizing: 'border-box',
};
