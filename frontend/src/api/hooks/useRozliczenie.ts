import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';

/**
 * Rozliczenie liczone po stronie serwera.
 *
 * Wczesniej liczyla to przegladarka z sumy godzin na karcie czasu — przez co nie dalo sie
 * zastosowac dodatku nocnego (potrzebne sa wpisy, nie suma) ani swiatecznego (potrzebny
 * kalendarz dni wolnych), a filtrowanie zakresu po stronie klienta niczego nie chronilo.
 */
export interface WierszRozliczenia {
  employeeId: string;
  normaH: number;
  przepracowaneH: number;
  zwykleH: number;
  nadgodzinyH: number;
  nocneH: number;
  swiateczneH: number;
  zasadnicze: number;
  zaNadgodziny: number;
  dodatekNocny: number;
  dodatekSwiateczny: number;
  razem: number;
}

export function useRozliczenie(od: string, doDnia: string) {
  return useQuery({
    queryKey: ['rozliczenie', od, doDnia],
    queryFn: () =>
      api.get<WierszRozliczenia[]>(
        `/api/payroll/rozliczenie?od=${od}&do=${doDnia}`,
      ),
    enabled: !!od && !!doDnia,
  });
}
