import { useState } from 'react';
import { FolderTree, Building2, RefreshCw } from 'lucide-react';
import { useOrgUnitTree } from '@/api/hooks/useOrganization';
import { OrgTree } from '@/components/OrgTree';
import type { OrganizationUnitTreeNode } from '@/api/types/organization';

export function OrgTreePage() {
  const { data: tree, isLoading, error, refetch } = useOrgUnitTree();
  const [selectedNode, setSelectedNode] = useState<OrganizationUnitTreeNode | null>(null);

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '16px 24px',
          borderBottom: '1px solid #e5e7eb',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <FolderTree size={24} style={{ color: '#3b82f6' }} />
          <h1 style={{ fontSize: '20px', fontWeight: 600, margin: 0, color: '#111827' }}>
            Struktura organizacyjna
          </h1>
        </div>

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
      </div>

      {/* Content */}
      <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
        {/* Tree panel */}
        <div
          style={{
            flex: selectedNode ? '0 0 50%' : '1',
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
              flex: '0 0 50%',
              borderLeft: '1px solid #e5e7eb',
              overflowY: 'auto',
              padding: '24px',
              backgroundColor: '#fafafa',
            }}
          >
            <UnitDetailPanel node={selectedNode} onClose={() => setSelectedNode(null)} />
          </div>
        )}
      </div>

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
}: {
  node: OrganizationUnitTreeNode;
  onClose: () => void;
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
        <button
          onClick={onClose}
          style={{
            padding: '4px 10px',
            fontSize: '18px',
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            color: '#6b7280',
            borderRadius: '4px',
          }}
          aria-label="Zamknij panel"
        >
          ×
        </button>
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
