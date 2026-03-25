import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { OrganizationUnitTreeNode } from '@/api/types/organization';

export function useOrgUnitTree() {
  return useQuery({
    queryKey: ['org', 'units', 'tree'],
    queryFn: () => api.get<OrganizationUnitTreeNode[]>('/api/org/units/tree'),
  });
}
