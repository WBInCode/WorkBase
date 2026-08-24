import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';

export interface Zastepstwo {
  id: string;
  zastepowanyEmployeeId: string;
  zastepcaEmployeeId: string;
  zastepcaImieNazwisko: string;
  odKiedy: string;
  doKiedy: string;
  powod: string | null;
  obowiazujeDzis: boolean;
}

export interface WyznaczZastepstwo {
  zastepowanyEmployeeId: string;
  zastepcaEmployeeId: string;
  odKiedy: string;
  doKiedy: string;
  powod: string | null;
}

export function useZastepstwa(employeeId: string | null | undefined) {
  return useQuery({
    queryKey: ['zastepstwa', employeeId],
    queryFn: () => api.get<Zastepstwo[]>(`/api/org/zastepstwa/${employeeId}`),
    enabled: !!employeeId,
  });
}

export function useWyznaczZastepstwo() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: WyznaczZastepstwo) =>
      api.post<string>('/api/org/zastepstwa', body),
    onSuccess: (_, body) => {
      void qc.invalidateQueries({ queryKey: ['zastepstwa', body.zastepowanyEmployeeId] });
    },
  });
}

export function useOdwolajZastepstwo(employeeId: string | null | undefined) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/org/zastepstwa/${id}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['zastepstwa', employeeId] });
    },
  });
}
