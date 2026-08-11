import { useCallback } from 'react';
import { useCurrentUser } from '@/api/hooks/useIam';
import { dostepnaDlaPrzelozonego, uprawnieniaDlaSciezki } from './dostepDoWidokow';
import type { WymaganeUprawnienia } from './dostepDoWidokow';

/**
 * Uprawnienia zalogowanego uzytkownika z GET /api/auth/me — tego samego zrodla, ktore backend
 * sprawdza w RequirePermission. Nie czytac roli z tokenu Keycloaka: nadanie roli na ekranie
 * Role nie zmienia niczego w Keycloaku i oba zrodla sie rozjezdzaja.
 */
export function useUprawnienia() {
  const { data: currentUser, isLoading } = useCurrentUser();
  const kody = currentUser?.permissions;
  const jestPrzelozonym = currentUser?.isSupervisor ?? false;

  const moze = useCallback(
    (kod: string) => kody?.includes(kod) ?? false,
    [kody],
  );

  /** Pusta lista wymagan = kazdy zalogowany. Niepusta = wystarczy jedno z uprawnien. */
  const mozeKtorekolwiek = useCallback(
    (wymagane: WymaganeUprawnienia) =>
      wymagane.length === 0 || wymagane.some((kod) => kody?.includes(kod) ?? false),
    [kody],
  );

  const mozeWejscNa = useCallback(
    (sciezka: string) => {
      const wymagane = uprawnieniaDlaSciezki(sciezka) ?? [];
      if (wymagane.length === 0) return true;
      if (wymagane.some((kod) => kody?.includes(kod) ?? false)) return true;
      return jestPrzelozonym && dostepnaDlaPrzelozonego(sciezka);
    },
    [kody, jestPrzelozonym],
  );

  return {
    moze,
    mozeKtorekolwiek,
    /** Czy wolno wejsc na trase: uprawnienie albo wyjatek dla przelozonego. */
    mozeWejscNa,
    jestPrzelozonym,
    /** Dopoki false, nie wiadomo jeszcze czego uzytkownikowi wolno — nie chowac na sile UI. */
    znane: !isLoading && kody !== undefined,
  };
}
