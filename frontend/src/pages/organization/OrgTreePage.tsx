import { useState, type FormEvent } from 'react';
import { FolderTree, Building2, RefreshCw, Plus, Edit2, Trash2, X } from 'lucide-react';
import { useOrgUnitTree, useUnitTypes, useCreateOrgUnit, useUpdateOrgUnit, useDeleteOrgUnit } from '@/api/hooks/useOrganization';
import { OrgTree } from '@/components/OrgTree';
import type { OrganizationUnitTreeNode } from '@/api/types/organization';
import { useIsMobile } from '@/shared';

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
          borderBottom: '1px solid #e5e7eb',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <FolderTree size={24} style={{ color: '#3b82f6' }} />
          <h1 style={{ fontSize: '20px', fontWeight: 600, margin: 0, color: '#111827' }}>
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
              fontSize: '14px',
              fontWeight: 500,
              color: '#374151',
              backgroundColor: '#ffffff',
              border: '1px solid #d1d5db',
              borderRadius: '6px',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              opacity: isLoading ? 0.6 : 1,
            }}
          >
            <RefreshCw size={16} style={{ animation: isLoading ? 'spin 1s linear infinite' : 'none' }} />
            Odśwież
          </button>
          <button
            onClick={handleAddRoot}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 16px',
              fontSize: '14px',
              fontWeight: 500,
              color: '#ffffff',
              backgroundColor: '#3b82f6',
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
            }}
          >
            <Plus size={16} /> Dodaj jednostkę
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
            <div style={{ padding: '32px', textAlign: 'center', color: '#6b7280' }}>
              Ładowanie struktury...
            </div>
          )}

          {error && (
            <div
              style={{
                padding: '16px',
                margin: '12px',
                backgroundColor: '#fef2f2',
                border: '1px solid #fecaca',
                borderRadius: '8px',
                color: '#991b1b',
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
                  backgroundColor: '#ffffff',
                  border: '1px solid #d1d5db',
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
              borderLeft: mobile ? 'none' : '1px solid #e5e7eb',
              borderTop: mobile ? '1px solid #e5e7eb' : 'none',
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

      <style>{`
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
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
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <Building2 size={22} style={{ color: '#3b82f6' }} />
          <h2 style={{ fontSize: '18px', fontWeight: 600, margin: 0, color: '#111827' }}>
            {node.name}
          </h2>
        </div>
        <div style={{ display: 'flex', gap: '6px' }}>
          <button
            onClick={() => onAddChild(node.id)}
            title="Dodaj podjednostkę"
            style={{
              display: 'inline-flex', alignItems: 'center', gap: '4px',
              padding: '6px 12px', fontSize: '13px', fontWeight: 500,
              color: '#fff', backgroundColor: '#3b82f6', border: 'none',
              borderRadius: '4px', cursor: 'pointer',
            }}
          >
            <Plus size={14} /> Podjednostka
          </button>
          <button
            onClick={() => onEdit(node)}
            title="Edytuj"
            style={{
              padding: '6px 8px', background: 'none', border: '1px solid #d1d5db',
              borderRadius: '4px', cursor: 'pointer', color: '#374151',
            }}
          >
            <Edit2 size={14} />
          </button>
          <button
            onClick={() => onDelete(node)}
            title="Usuń"
            style={{
              padding: '6px 8px', background: 'none', border: '1px solid #fecaca',
              borderRadius: '4px', cursor: 'pointer', color: '#dc2626',
            }}
          >
            <Trash2 size={14} />
          </button>
          <button
            onClick={onClose}
            style={{
              padding: '4px 10px', fontSize: '18px', background: 'none',
              border: 'none', cursor: 'pointer', color: '#6b7280', borderRadius: '4px',
            }}
            aria-label="Zamknij panel"
          >
            ×
          </button>
        </div>
      </div>

      <div
        style={{
          backgroundColor: '#ffffff',
          border: '1px solid #e5e7eb',
          borderRadius: '8px',
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
          valueColor={node.isActive ? '#059669' : '#dc2626'}
        />
        <DetailRow label="Podjednostki" value={String(node.children.length)} />
      </div>

      {node.children.length > 0 && (
        <div style={{ marginTop: '20px' }}>
          <h3 style={{ fontSize: '14px', fontWeight: 600, color: '#374151', marginBottom: '8px' }}>
            Podjednostki ({node.children.length})
          </h3>
          <div
            style={{
              backgroundColor: '#ffffff',
              border: '1px solid #e5e7eb',
              borderRadius: '8px',
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
                <span style={{ color: '#111827' }}>{child.name}</span>
                <span style={{ color: '#9ca3af', fontSize: '12px' }}>{child.typeName}</span>
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
      <span style={{ width: '120px', flexShrink: 0, color: '#6b7280', fontWeight: 500 }}>
        {label}
      </span>
      <span style={{ color: valueColor ?? '#111827', wordBreak: 'break-all' }}>{value}</span>
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
        position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)',
        display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000,
      }}
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div
        style={{
          backgroundColor: '#fff', borderRadius: '12px', padding: '24px',
          width: '100%', maxWidth: '480px', boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600 }}>
            {isEditing ? 'Edytuj jednostkę' : parentId ? 'Dodaj podjednostkę' : 'Nowa jednostka'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#6b7280' }}>
            <X size={20} />
          </button>
        </div>

        {error && (
          <div style={{ padding: '10px 14px', marginBottom: '12px', backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: '6px', color: '#dc2626', fontSize: '13px' }}>
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
            <button type="button" onClick={onClose} style={{ padding: '8px 16px', fontSize: '14px', border: '1px solid #d1d5db', borderRadius: '6px', backgroundColor: '#fff', cursor: 'pointer' }}>
              Anuluj
            </button>
            <button
              type="submit"
              disabled={isPending || !name || !typeId}
              style={{
                padding: '8px 20px', fontSize: '14px', fontWeight: 500,
                color: '#fff', backgroundColor: '#3b82f6', border: 'none',
                borderRadius: '6px', cursor: isPending ? 'not-allowed' : 'pointer',
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
  display: 'block', marginBottom: '4px', fontSize: '13px', fontWeight: 500, color: '#374151',
};

const inputStyle: React.CSSProperties = {
  width: '100%', padding: '8px 12px', fontSize: '14px',
  border: '1px solid #d1d5db', borderRadius: '6px', boxSizing: 'border-box',
};
