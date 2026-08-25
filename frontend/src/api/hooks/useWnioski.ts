import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';

export type TypPola = 'Tekst' | 'Wielolinijkowy' | 'Liczba' | 'Data' | 'Wybor' | 'TakNie';

export interface PoleWniosku {
  kod: string;
  etykieta: string;
  typ: TypPola;
  wymagane: boolean;
  opcje?: string[] | null;
  podpowiedz?: string | null;
}

export interface TypWniosku {
  id: string;
  kod: string;
  nazwa: string;
  opis: string | null;
  pola: PoleWniosku[];
  wymagaAkceptacji: boolean;
  aktywny: boolean;
}

export type StatusWniosku = 'Oczekuje' | 'Zaakceptowany' | 'Odrzucony' | 'Anulowany';

export interface Wniosek {
  id: string;
  typWnioskuId: string;
  typNazwa: string;
  status: StatusWniosku;
  wartosci: Record<string, string | null>;
  zlozonyO: string;
  rozstrzygnietyO: string | null;
}

export interface ZapiszTypWniosku {
  kod: string;
  nazwa: string;
  opis: string | null;
  pola: PoleWniosku[];
  wymagaAkceptacji: boolean;
  aktywny?: boolean;
}

/** `wszystkie` = także wyłączone; pracownikowi pokazujemy tylko aktywne. */
export function useTypyWnioskow(wszystkie = false) {
  return useQuery({
    queryKey: ['wnioski', 'typy', wszystkie],
    queryFn: () => api.get<TypWniosku[]>(`/api/wnioski/typy${wszystkie ? '?wszystkie=true' : ''}`),
  });
}

export function useMojeWnioski() {
  return useQuery({
    queryKey: ['wnioski', 'moje'],
    queryFn: () => api.get<Wniosek[]>('/api/wnioski/moje'),
  });
}

export function useZlozWniosek() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { typWnioskuId: string; wartosci: Record<string, string | null> }) =>
      api.post<string>('/api/wnioski', body),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['wnioski', 'moje'] }),
  });
}

export function useAnulujWniosek() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.post<void>(`/api/wnioski/${id}/anuluj`, {}),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['wnioski', 'moje'] }),
  });
}

export function useUtworzTypWniosku() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ZapiszTypWniosku) => api.post<string>('/api/wnioski/typy', body),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['wnioski', 'typy'] }),
  });
}

export function useZmienTypWniosku() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...body }: ZapiszTypWniosku & { id: string }) =>
      api.put<void>(`/api/wnioski/typy/${id}`, body),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['wnioski', 'typy'] }),
  });
}
