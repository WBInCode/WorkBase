import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';

export interface PozycjaAlertu {
  id: string;
  opis: string;
}

export interface Alert {
  kod: string;
  /** `pilne` — coś stanęło albo ktoś czeka; `uwaga` — do uzupełnienia, nie blokuje. */
  waga: 'pilne' | 'uwaga';
  tytul: string;
  opis: string;
  liczba: number;
  sciezka: string | null;
  pozycje: PozycjaAlertu[];
}

export function useAlertyPulpitu() {
  return useQuery({
    queryKey: ['pulpit', 'uwaga'],
    queryFn: () => api.get<Alert[]>('/api/dashboard/uwaga'),
    // Lista zmienia sie w ciagu dnia (ktos wreszcie zarejestrowal wejscie, ktos zatwierdzil
    // wniosek), ale nie na tyle czesto, zeby odpytywac serwer przy kazdym powrocie na pulpit.
    staleTime: 120_000,
  });
}
