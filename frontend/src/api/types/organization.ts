export interface OrganizationUnitTreeNode {
  id: string;
  name: string;
  code: string | null;
  typeId: string;
  typeName: string;
  isActive: boolean;
  children: OrganizationUnitTreeNode[];
}

export interface OrganizationUnitType {
  id: string;
  name: string;
  description: string | null;
  sortOrder: number;
  isActive: boolean;
}
