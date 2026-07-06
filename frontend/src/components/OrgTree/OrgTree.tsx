import { OrgTreeNode } from './OrgTreeNode';
import type { OrganizationUnitTreeNode } from '@/api/types/organization';
import { colors } from '@/theme/tokens';

interface OrgTreeProps {
  nodes: OrganizationUnitTreeNode[];
  onSelect: (node: OrganizationUnitTreeNode) => void;
  selectedId: string | null;
}

export function OrgTree({ nodes, onSelect, selectedId }: OrgTreeProps) {
  if (nodes.length === 0) {
    return (
      <div style={{ padding: '32px', textAlign: 'center', color: colors.gray[500] }}>
        <p style={{ fontSize: '16px', marginBottom: '8px' }}>Brak jednostek organizacyjnych</p>
        <p style={{ fontSize: '14px' }}>Utw\u00f3rz pierwsz\u0105 jednostk\u0119 za pomoc\u0105 API.</p>
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
