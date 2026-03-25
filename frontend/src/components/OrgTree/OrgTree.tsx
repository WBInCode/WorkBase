import { OrgTreeNode } from './OrgTreeNode';
import type { OrganizationUnitTreeNode } from '@/api/types/organization';

interface OrgTreeProps {
  nodes: OrganizationUnitTreeNode[];
  onSelect: (node: OrganizationUnitTreeNode) => void;
  selectedId: string | null;
}

export function OrgTree({ nodes, onSelect, selectedId }: OrgTreeProps) {
  if (nodes.length === 0) {
    return (
      <div style={{ padding: '32px', textAlign: 'center', color: '#6b7280' }}>
        <p style={{ fontSize: '16px', marginBottom: '8px' }}>Brak jednostek organizacyjnych</p>
        <p style={{ fontSize: '14px' }}>Utwórz pierwszą jednostkę za pomocą API.</p>
      </div>
    );
  }

  return (
    <div role="tree" aria-label="Drzewo organizacyjne">
      {nodes.map((node) => (
        <OrgTreeNode
          key={node.id}
          node={node}
          level={0}
          onSelect={onSelect}
          selectedId={selectedId}
        />
      ))}
    </div>
  );
}
