import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';

/** Aktualny / Zbliza / Minal — liczone na serwerze wzgledem wyprzedzenia rodzaju terminu. */
export type StanTerminu = 'Aktualny' | 'Zbliza' | 'Minal';

export interface TypTerminu {
  id: string;
  kod: string;
  nazwa: string;
  opis: string | null;
  dniOstrzezenia: number;
  aktywny: boolean;
}

export interface Termin {
  id: string;
  employeeId: string;
  typTerminuId: string;
  typNazwa: string;
  waznyDo: string;
  wykonanyDnia: string | null;
  notatka: string | null;
  dokumentId: string | null;
  archiwalny: boolean;
  stan: StanTerminu;
  dniDoTerminu: number;
}

export interface WygasajacyTermin {
  id: string;
  employeeId: string;
  imieNazwisko: string;
  typNazwa: string;
  waznyDo: string;
  stan: StanTerminu;
  dniDoTerminu: number;
}

export interface ZapiszTypBody {
  id?: string | null;
  kod: string;
  nazwa: string;
  opis: string | null;
  dniOstrzezenia: number;
  aktywny: boolean;
}

export interface ZapiszTerminBody {
  id?: string | null;
  employeeId: string;
  typTerminuId: string;
  waznyDo: string;
  wykonanyDnia?: string | null;
  notatka?: string | null;
  dokumentId?: string | null;
}

/** `wszystkie` = także wyłączone rodzaje; przy dodawaniu terminu pokazujemy tylko aktywne. */
export function useTypyTerminow(wszystkie = false) {
  return useQuery({
    queryKey: ['terminy', 'typy', wszystkie],
    queryFn: () => api.get<TypTerminu[]>(`/api/terminy/typy${wszystkie ? '?wszystkie=true' : ''}`),
  });
}

/**
 * Terminy jednego pracownika. Serwer sam sprawdza zakres danych i odpowiada 403 na cudze —
 * dlatego nie ponawiamy: kolejna próba i tak dostanie to samo.
 */
export function useTerminyPracownika(employeeId: string | undefined, zArchiwalnymi = false) {
  return useQuery({
    queryKey: ['terminy', 'pracownik', employeeId, zArchiwalnymi],
    queryFn: () =>
      api.get<Termin[]>(
        `/api/terminy/pracownik/${employeeId}${zArchiwalnymi ? '?zArchiwalnymi=true' : ''}`,
      ),
    enabled: Boolean(employeeId),
    retry: false,
  });
}

/** Lista zbiorcza — serwer zawęża ją do zakresu danych pytającego. */
export function useWygasajaceTerminy(dni = 30) {
  return useQuery({
    queryKey: ['terminy', 'wygasajace', dni],
    queryFn: () => api.get<WygasajacyTermin[]>(`/api/terminy/wygasajace?dni=${dni}`),
  });
}

function useZapis<TBody, TWynik>(wywolanie: (body: TBody) => Promise<TWynik>) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: wywolanie,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['terminy'] }),
  });
}

export const useZapiszTypTerminu = () =>
  useZapis((body: ZapiszTypBody) => api.post<{ id: string }>('/api/terminy/typy', body));

export const useZapiszTermin = () =>
  useZapis((body: ZapiszTerminBody) => api.post<{ id: string }>('/api/terminy', body));

export const useOdnowTermin = () =>
  useZapis(({ id, ...body }: { id: string; nowyWaznyDo: string; wykonanyDnia?: string | null; notatka?: string | null }) =>
    api.post<{ id: string }>(`/api/terminy/${id}/odnow`, body));

export const useZarchiwizujTermin = () =>
  useZapis((id: string) => api.post<void>(`/api/terminy/${id}/archiwizuj`, {}));
