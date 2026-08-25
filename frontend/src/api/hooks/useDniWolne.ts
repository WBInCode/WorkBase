import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';

export interface DzienWolny {
  id: string;
  data: string;
  nazwa: string;
  rodzaj: 'Swieto' | 'Firmowy';
  obnizaNorme: boolean;
}

export interface ZapiszDzienWolny {
  data: string;
  nazwa: string;
  rodzaj: 'Swieto' | 'Firmowy';
  obnizaNorme: boolean;
}

export function useDniWolne(rok: number) {
  return useQuery({
    queryKey: ['dni-wolne', rok],
    queryFn: () => api.get<DzienWolny[]>(`/api/time/dni-wolne?rok=${rok}`),
  });
}

export function useDodajDzienWolny(rok: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ZapiszDzienWolny) => api.post<string>('/api/time/dni-wolne', body),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['dni-wolne', rok] }),
  });
}

export function useUsunDzienWolny(rok: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/time/dni-wolne/${id}`),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['dni-wolne', rok] }),
  });
}

export function useWstawZestawPolski(rok: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () =>
      api.post<{ dodane: number }>(`/api/time/dni-wolne/zestaw-polski?rok=${rok}`, {}),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['dni-wolne', rok] }),
  });
}
