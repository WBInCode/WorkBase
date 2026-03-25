import { useState } from 'react';
import { ChevronRight, ChevronDown, Building2, Users, Briefcase, FolderTree } from 'lucide-react';
import type { OrganizationUnitTreeNode } from '@/api/types/organization';

interface OrgTreeNodeProps {
  node: OrganizationUnitTreeNode;
  level: number;
  onSelect: (node: OrganizationUnitTreeNode) => void;
  selectedId: string | null;
}

const typeIcons: Record<string, typeof Building2> = {
  'Firma': Building2,
  'Dział': Users,
  'Zespół': Briefcase,
};

function getIcon(typeName: string) {
  return typeIcons[typeName] ?? FolderTree;
}

export function OrgTreeNode({ node, level, onSelect, selectedId }: OrgTreeNodeProps) {
  const [expanded, setExpanded] = useState(level < 2);
  const hasChildren = node.children.length > 0;
  const isSelected = selectedId === node.id;
  const Icon = getIcon(node.typeName);

  return (
    <div>
      <div
        role="treeitem"
        aria-expanded={hasChildren ? expanded : undefined}
        aria-selected={isSelected}
        tabIndex={0}
        onClick={() => onSelect(node)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            onSelect(node);
          }
          if (e.key === 'ArrowRight' && hasChildren && !expanded) {
            e.preventDefault();
            setExpanded(true);
          }
          if (e.key === 'ArrowLeft' && expanded) {
            e.preventDefault();
            setExpanded(false);
          }
        }}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '6px',
          padding: '6px 8px',
          paddingLeft: `${level * 24 + 8}px`,
          cursor: 'pointer',
          borderRadius: '6px',
          backgroundColor: isSelected ? '#e0edff' : 'transparent',
          transition: 'background-color 0.15s',
          userSelect: 'none',
          opacity: node.isActive ? 1 : 0.5,
        }}
        onMouseEnter={(e) => {
          if (!isSelected) e.currentTarget.style.backgroundColor = '#f3f4f6';
        }}
        onMouseLeave={(e) => {
          if (!isSelected) e.currentTarget.style.backgroundColor = 'transparent';
        }}
      >
        <span
          onClick={(e) => {
            e.stopPropagation();
            if (hasChildren) setExpanded(!expanded);
          }}
          style={{
            display: 'inline-flex',
            width: '20px',
            justifyContent: 'center',
            color: '#6b7280',
            flexShrink: 0,
          }}
        >
          {hasChildren ? (
            expanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />
          ) : null}
        </span>

        <Icon size={18} style={{ color: '#3b82f6', flexShrink: 0 }} />

        <span style={{ fontWeight: level === 0 ? 600 : 400, fontSize: '14px', color: '#111827' }}>
          {node.name}
        </span>

        {node.code && (
          <span style={{ fontSize: '12px', color: '#9ca3af', marginLeft: '4px' }}>
            ({node.code})
          </span>
        )}

        <span
          style={{
            fontSize: '11px',
            color: '#6b7280',
            backgroundColor: '#f3f4f6',
            padding: '1px 6px',
            borderRadius: '4px',
            marginLeft: 'auto',
            flexShrink: 0,
          }}
        >
          {node.typeName}
        </span>

        {!node.isActive && (
          <span
            style={{
              fontSize: '11px',
              color: '#ef4444',
              backgroundColor: '#fef2f2',
              padding: '1px 6px',
              borderRadius: '4px',
              flexShrink: 0,
            }}
          >
            Nieaktywna
          </span>
        )}
      </div>

      {hasChildren && expanded && (
        <div role="group">
          {node.children.map((child) => (
            <OrgTreeNode
              key={child.id}
              node={child}
              level={level + 1}
              onSelect={onSelect}
              selectedId={selectedId}
            />
          ))}
        </div>
      )}
    </div>
  );
}
