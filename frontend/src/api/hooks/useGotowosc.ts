import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';

export type WagaBraku = 'blokuje' | 'warto';

export interface PozycjaGotowosci {
  kod: string;
  tytul: string;
  /** Co konkretnie nie zadziała — nie „czego brakuje”. */
  coNieZadziala: string;
  waga: WagaBraku;
  sciezka: string;
  liczba: number | null;
}

export interface Gotowosc {
  blokujace: number;
  warteUwagi: number;
  pozycje: PozycjaGotowosci[];
}

export function useGotowosc(wlaczone = true) {
  return useQuery({
    queryKey: ['konfiguracja', 'gotowosc'],
    queryFn: () => api.get<Gotowosc>('/api/konfiguracja/gotowosc'),
    enabled: wlaczone,
    // Lista zmienia się rzadko i tylko wskutek działań administratora.
    staleTime: 60_000,
  });
}
