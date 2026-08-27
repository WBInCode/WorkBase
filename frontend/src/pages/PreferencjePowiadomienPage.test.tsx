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
  it('bez zapisanych ustawien wszystko jest wlaczone', () => {
    render(<PreferencjePowiadomienPage />);

    const przelaczniki = screen.getAllByRole('checkbox') as HTMLInputElement[];
    expect(przelaczniki.length).toBeGreaterThan(0);
    expect(przelaczniki.every((p) => p.checked)).toBe(true);
  });

  it('wyciszona kategoria ma odznaczony przelacznik, pozostale zostaja wlaczone', () => {
    preferencje = [{ id: 'p1', category: 'task_assigned', inApp: false, email: false }];

    render(<PreferencjePowiadomienPage />);

    const przelaczniki = screen.getAllByRole('checkbox') as HTMLInputElement[];
    expect(przelaczniki.filter((p) => !p.checked)).toHaveLength(1);
  });

  it('klikniecie wlaczonej kategorii wysyla wylaczenie', () => {
    render(<PreferencjePowiadomienPage />);

    const [pierwszy] = screen.getAllByRole('checkbox');
    fireEvent.click(pierwszy!);

    expect(zapisz).toHaveBeenCalledWith({ category: 'task_assigned', inApp: false, email: false });
  });
});
