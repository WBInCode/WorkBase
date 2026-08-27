import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';

/**
 * Zapisane widoki list — nazwane zestawy filtrow.
 *
 * Backend mial komplet CRUD-a ze wspoldzieleniem i widokiem domyslnym, zamapowany w Program.cs,
 * i ZERO trafien we froncie. Serwer traktuje `filtersJson` jako nieprzezroczysty tekst, wiec
 * kazda lista wrzuca tu swoj wlasny stan filtrow bez zmian po stronie API.
 */
export interface ZapisanyWidok {
  id: string;
  entityType: string;
  name: string;
  filtersJson: string;
  sortJson: string;
  columnsJson: string | null;
  isDefault: boolean;
  isShared: boolean;
}

export interface ZapiszWidokBody {
  entityType: string;
  name: string;
  filtersJson: string;
  sortJson: string;
  columnsJson?: string | null;
  isDefault: boolean;
  isShared: boolean;
}

export function useZapisaneWidoki(entityType: string) {
  return useQuery({
    queryKey: ['views', entityType],
    queryFn: () => api.get<ZapisanyWidok[]>(`/api/views/${entityType}`),
  });
}

export function useZapiszWidok() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ZapiszWidokBody) => api.post<ZapisanyWidok>('/api/views', body),
    onSuccess: (_d, body) => qc.invalidateQueries({ queryKey: ['views', body.entityType] }),
  });
}

export function useUsunWidok(entityType: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/views/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['views', entityType] }),
  });
}
