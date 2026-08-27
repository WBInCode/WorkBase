import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';

export type WyzwalaczListy = 'Przyjecie' | 'Pozegnanie';
export type WykonawcaPozycji = 'Pracownik' | 'Przelozony' | 'Osoba';

export interface PozycjaListy {
  tytul: string;
  /** Termin zadania = data zdarzenia + tyle dni. 0 = tego samego dnia. */
  dniOdZdarzenia: number;
  wykonawca: WykonawcaPozycji;
  osobaId: string | null;
}

export interface ListaKontrolna {
  id: string;
  nazwa: string;
  wyzwalacz: WyzwalaczListy;
  aktywna: boolean;
  pozycje: PozycjaListy[];
}

export interface ZapiszListeBody {
  id?: string | null;
  nazwa: string;
  wyzwalacz: WyzwalaczListy;
  aktywna: boolean;
  pozycje: PozycjaListy[];
}

export const WYZWALACZ_ETYKIETA: Record<WyzwalaczListy, string> = {
  Przyjecie: 'Przyjęcie pracownika',
  Pozegnanie: 'Odejście pracownika',
};

export const WYKONAWCA_ETYKIETA: Record<WykonawcaPozycji, string> = {
  Pracownik: 'sam pracownik',
  Przelozony: 'jego przełożony',
  Osoba: 'wskazana osoba',
};

/** Wszystkie listy firmy, także wyłączone — to ekran administratora. */
export function useListyKontrolne() {
  return useQuery({
    queryKey: ['listy-kontrolne'],
    queryFn: () => api.get<ListaKontrolna[]>('/api/listy-kontrolne'),
  });
}

export function useZapiszListeKontrolna() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ZapiszListeBody) => api.post<{ id: string }>('/api/listy-kontrolne', body),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['listy-kontrolne'] }),
  });
}
