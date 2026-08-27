// Kluczowa wlasnosc ekranu: BRAK wpisu preferencji znaczy „wysylaj". Odwrotna domyslka
// pokazalaby wszystkim, ze maja wszystko wyciszone, i pierwsze klikniecie w przelacznik
// niczego by nie zmienilo. Ta sama umowa jest przypieta po stronie serwera
// (SzablonyPowiadomienTests.Bez_ustawionych_preferencji_powiadomienie_dochodzi).
import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import type { PreferencjaPowiadomien } from '@/api/hooks/useNotifications';

const zapisz = vi.fn();
let preferencje: PreferencjaPowiadomien[] = [];

vi.mock('@/api/hooks/useNotifications', () => ({
  usePreferencjePowiadomien: () => ({ data: preferencje, isLoading: false }),
  useZapiszPreferencje: () => ({ mutate: zapisz, isPending: false }),
}));

const { PreferencjePowiadomienPage } = await import('./PreferencjePowiadomienPage');

afterEach(() => {
  cleanup();
  zapisz.mockClear();
  preferencje = [];
});

describe('PreferencjePowiadomienPage', () => {
  const wAplikacji = () =>
    screen.getAllByLabelText(/— w aplikacji$/) as HTMLInputElement[];
  const mailem = () => screen.getAllByLabelText(/— mailem$/) as HTMLInputElement[];

  it('bez zapisanych ustawien: w aplikacji wszystko wlaczone, mailem nic', () => {
    render(<PreferencjePowiadomienPage />);

    expect(wAplikacji().length).toBeGreaterThan(0);
    expect(wAplikacji().every((p) => p.checked)).toBe(true);
    expect(mailem().some((p) => p.checked)).toBe(false);
  });

  it('wyciszona kategoria ma odznaczony przelacznik, pozostale zostaja wlaczone', () => {
    preferencje = [{ id: 'p1', category: 'task_assigned', inApp: false, email: false }];

    render(<PreferencjePowiadomienPage />);

    expect(wAplikacji().filter((p) => !p.checked)).toHaveLength(1);
  });

  it('klikniecie wlaczonej kategorii wysyla wylaczenie', () => {
    render(<PreferencjePowiadomienPage />);

    fireEvent.click(wAplikacji()[0]!);

    expect(zapisz).toHaveBeenCalledWith({ category: 'task_assigned', inApp: false, email: false });
  });

  it('wlaczenie maila nie gasi kanalu w aplikacji', () => {
    render(<PreferencjePowiadomienPage />);

    fireEvent.click(mailem()[0]!);

    expect(zapisz).toHaveBeenCalledWith({ category: 'task_assigned', inApp: true, email: true });
  });

  it('wyciszona kategoria nie pozwala wlaczyc maila', () => {
    // Serwer i tak nie wysle: wylaczony kanal w aplikacji ucisza rowniez poczte.
    preferencje = [{ id: 'p1', category: 'task_assigned', inApp: false, email: false }];

    render(<PreferencjePowiadomienPage />);

    expect(mailem()[0]!.disabled).toBe(true);
  });
});
