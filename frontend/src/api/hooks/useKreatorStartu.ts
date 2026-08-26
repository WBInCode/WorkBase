import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';

export type KrokKreatora = 'ludzie' | 'godziny' | 'akceptanci' | 'urlop';

export interface StanKreatora {
  wymagana: boolean;
  ukonczona: boolean;
  ukonczonaO: string | null;
  aktualnyKrok: KrokKreatora | null;
  pominieteKroki: KrokKreatora[];
  kroki: KrokKreatora[];
}

export interface OsobaKreatora {
  id: string;
  imie: string;
  nazwisko: string;
  email: string;
}

export interface NowaOsoba {
  imie: string;
  nazwisko: string;
  email: string;
  numer?: string | null;
  dataZatrudnienia?: string | null;
}

export interface WynikDodaniaLudzi {
  dodani: number;
  pominieci: number;
  bledy: string[];
}

export interface Zmiana {
  nazwa: string;
  od: string;
  do: string;
}

/**
 * Stan kreatora. `retry: false`, bo ta trasa jest na białej liście bramki — jeśli odpowie
 * błędem, ponawianie i tak nic nie da, a kreator ma pokazać komunikat, nie kręcić się w kółko.
 */
export function useStanKreatora() {
  return useQuery({
    queryKey: ['kreator', 'stan'],
    queryFn: () => api.get<StanKreatora>('/api/setup/state'),
    retry: false,
  });
}

export function usePracownicyKreatora(wlaczone = true) {
  return useQuery({
    queryKey: ['kreator', 'pracownicy'],
    queryFn: () => api.get<OsobaKreatora[]>('/api/setup/employees'),
    enabled: wlaczone,
    retry: false,
  });
}

function useKrok<TBody, TWynik>(sciezka: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: TBody) => api.post<TWynik>(sciezka, body),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['kreator'] });
    },
  });
}

export const useZapiszLudzi = () =>
  useKrok<{ pracownicy: NowaOsoba[]; zaprosicTeraz: boolean }, WynikDodaniaLudzi>(
    '/api/setup/employees',
  );

export const useZapiszGodziny = () =>
  useKrok<
    { zmiany: Zmiana[]; dniTygodnia: number[]; minutPrzerwy: number; przerwaPlatna: boolean },
    { szablonow: number; przerwaMinut: number }
  >('/api/setup/working-hours');

export const useZapiszAkceptantow = () =>
  useKrok<
    { akceptantId: string | null; pracownicyIds: string[] },
    { ustawione: number; bledy: string[] }
  >('/api/setup/approvals');

export function useWymiarUrlopu(wlaczone = true) {
  return useQuery({
    queryKey: ['kreator', 'urlop'],
    queryFn: () => api.get<{ dniUrlopu: number | null }>('/api/setup/leave'),
    enabled: wlaczone,
    retry: false,
  });
}

export const useZapiszUrlop = () =>
  useKrok<{ dniUrlopu: number }, { dniUrlopu: number | null; pominiety: boolean }>('/api/setup/leave');

export function useZakonczKreator() {
  return useMutation({
    mutationFn: () => api.post<void>('/api/setup/complete', {}),
  });
}
