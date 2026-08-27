import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';

export interface Mienie {
  id: string;
  employeeId: string;
  rodzaj: string;
  nazwa: string;
  numerSeryjny: string | null;
  wartosc: number | null;
  wydanoDnia: string;
  zwroconoDnia: string | null;
  potwierdzonoOdbior: string | null;
  notatka: string | null;
}

export interface MienieDoZwrotu {
  id: string;
  employeeId: string;
  imieNazwisko: string;
  /** „nieaktywny" albo „odchodzi dd.mm.rrrr" — po co ta osoba jest na liscie. */
  powod: string;
  rodzaj: string;
  nazwa: string;
  numerSeryjny: string | null;
  wartosc: number | null;
  wydanoDnia: string;
}

export interface WydajMienieBody {
  id?: string | null;
  employeeId: string;
  rodzaj: string;
  nazwa: string;
  wydanoDnia: string;
  numerSeryjny?: string | null;
  wartosc?: number | null;
  notatka?: string | null;
}

/** Podpowiedzi, nie slownik: firma wpisuje, co chce. */
export const TYPOWE_RODZAJE = ['Laptop', 'Telefon', 'Klucze', 'Karta dostępu', 'Odzież robocza', 'Narzędzia', 'Samochód'];

/**
 * Rzeczy jednego pracownika. Serwer odpowiada 403 na cudze poza zakresem — nie ponawiamy,
 * kolejna proba dostanie to samo.
 */
export function useMieniePracownika(employeeId: string | undefined, zeZwroconymi = false) {
  return useQuery({
    queryKey: ['mienie', 'pracownik', employeeId, zeZwroconymi],
    queryFn: () =>
      api.get<Mienie[]>(`/api/mienie/pracownik/${employeeId}${zeZwroconymi ? '?zeZwroconymi=true' : ''}`),
    enabled: Boolean(employeeId),
    retry: false,
  });
}

/** Ile rzeczy pracownik ma jeszcze oddac — do ostrzezenia przed dezaktywacja. */
export function useNiezwroconeMienie(employeeId: string | undefined, wlaczone = true) {
  return useQuery({
    queryKey: ['mienie', 'niezwrocone', employeeId],
    queryFn: () => api.get<{ liczba: number }>(`/api/mienie/pracownik/${employeeId}/niezwrocone`),
    enabled: Boolean(employeeId) && wlaczone,
    retry: false,
  });
}

/** Lista zbiorcza — serwer zaweza ja do zakresu danych pytajacego. */
export function useMienieDoZwrotu() {
  return useQuery({
    queryKey: ['mienie', 'do-zwrotu'],
    queryFn: () => api.get<MienieDoZwrotu[]>('/api/mienie/do-zwrotu'),
  });
}

function useZapis<TBody, TWynik>(wywolanie: (body: TBody) => Promise<TWynik>) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: wywolanie,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['mienie'] }),
  });
}

export const useWydajMienie = () =>
  useZapis((body: WydajMienieBody) => api.post<{ id: string }>('/api/mienie', body));

export const useZwrocMienie = () =>
  useZapis(({ id, ...body }: { id: string; zwroconoDnia: string; notatka?: string | null }) =>
    api.post<void>(`/api/mienie/${id}/zwrot`, body));

export const usePotwierdzOdbior = () =>
  useZapis((id: string) => api.post<void>(`/api/mienie/${id}/potwierdz`, {}));
