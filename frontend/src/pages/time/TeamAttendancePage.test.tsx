// Regresja zgloszona z uzytkowania: wpisywanie godzin w raporcie zespolu
// „przeskakiwalo" — po wpisaniu godziny w polu „Do" kursor wracal do „Od",
// a kolejne cyfry kasowaly juz wpisana godzine rozpoczecia.
import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { TimeSheetPeriodDto } from '@/api/types/time';

const createMutate = vi.fn().mockResolvedValue({});
const deleteMutate = vi.fn().mockResolvedValue({});
const clockInMutate = vi.fn().mockResolvedValue({});
const clockOutMutate = vi.fn().mockResolvedValue({});

vi.mock('@/api/hooks/useTimeTracking', () => ({
  useTeamTimesheets: () => ({ data: [timesheet], isLoading: false }),
  useAnomalies: () => ({ data: [] }),
  useAdminCreateTimeEntry: () => ({ mutateAsync: createMutate, isPending: false }),
  useAdminDeleteTimeEntry: () => ({ mutateAsync: deleteMutate, isPending: false }),
  useClockIn: () => ({ mutateAsync: clockInMutate, isPending: false }),
  useClockOut: () => ({ mutateAsync: clockOutMutate, isPending: false }),
}));

vi.mock('@/api/hooks/useIam', () => ({
  useCurrentUser: () => ({ data: { permissions: ['time.edit'] } }),
}));

vi.mock('@/api/hooks/useOrganization', () => ({
  useEmployees: () => ({
    data: { items: [{ id: 'emp-1', firstName: 'Kamil', lastName: 'Kida', status: 'Active' }], total: 1 },
  }),
  useOrgUnitTree: () => ({ data: [] }),
}));

vi.mock('@tanstack/react-query', () => ({
  useQueryClient: () => ({ invalidateQueries: vi.fn() }),
}));

const timesheet: TimeSheetPeriodDto = {
  from: '2026-07-01',
  to: '2026-07-31',
  period: 'month',
  employeeId: 'emp-1',
  totalWorked: '00:00:00',
  totalBreaks: '00:00:00',
  netWorked: '00:00:00',
  daysWorked: 0,
  daysIncomplete: 0,
  days: [],
};

const { TeamAttendancePage } = await import('./TeamAttendancePage');

/** Otwiera panel edycji pierwszej komorki dnia i zwraca pola „Od" i „Do". */
async function otworzPanel() {
  const { container } = render(<TeamAttendancePage />);
  // Pierwsza komorka dnia = pierwsza <td> po kolumnie z nazwiskiem.
  const komorki = container.querySelectorAll('tbody tr td');
  fireEvent.click(komorki[1]!);
  const pola = await waitFor(() => {
    const wszystkie = screen.getAllByPlaceholderText('HH:mm');
    expect(wszystkie.length).toBeGreaterThanOrEqual(2);
    return wszystkie;
  });
  return { od: pola[0] as HTMLInputElement, doPola: pola[1] as HTMLInputElement };
}

describe('Raport zespolu — wpisywanie godzin', () => {
  afterEach(() => {
    cleanup();
    createMutate.mockClear();
    deleteMutate.mockClear();
  });

  it('wpisanie godziny w polu „Do" nie kasuje godziny w polu „Od"', async () => {
    const { od, doPola } = await otworzPanel();

    // Prawdziwe przenoszenie fokusu: jsdom aktualizuje activeElement dopiero
    // przy .focus()/.blur(), samo fireEvent.blur go nie rusza.
    od.focus();
    fireEvent.change(od, { target: { value: '9:00' } });
    doPola.focus();
    fireEvent.change(doPola, { target: { value: '17:00' } });
    doPola.blur();

    await waitFor(() => expect(od.value).toBe('09:00'));
    expect(doPola.value).toBe('17:00');
  });

  it('kursor nie wraca do pola „Od" po zmianie w polu „Do"', async () => {
    const { od, doPola } = await otworzPanel();

    fireEvent.focus(od);
    fireEvent.change(od, { target: { value: '09:00' } });
    fireEvent.blur(od);

    doPola.focus();
    fireEvent.focus(doPola);
    fireEvent.change(doPola, { target: { value: '17' } });

    await waitFor(() => expect(document.activeElement).toBe(doPola));
    expect(document.activeElement).not.toBe(od);
  });

  it('przyjmuje same cyfry bez dwukropka', async () => {
    const { od } = await otworzPanel();

    od.focus();
    fireEvent.change(od, { target: { value: '930' } });
    od.blur();

    await waitFor(() => expect(od.value).toBe('09:30'));
  });

  it('sama godzina rozpoczecia zapisuje wejscie i zostawia dzien otwarty', async () => {
    const { od } = await otworzPanel();

    fireEvent.focus(od);
    fireEvent.change(od, { target: { value: '09:00' } });
    fireEvent.blur(od);

    fireEvent.click(screen.getByText('Zapisz'));

    await waitFor(() => expect(createMutate).toHaveBeenCalled());
    const typy = createMutate.mock.calls.map((wywolanie) => wywolanie[0].type);
    expect(typy).toContain('ClockIn');
    expect(typy).not.toContain('ClockOut');
  });

  it('sama godzina zakonczenia nie zapisuje po cichu, tylko tlumaczy dlaczego', async () => {
    const { doPola } = await otworzPanel();

    fireEvent.focus(doPola);
    fireEvent.change(doPola, { target: { value: '17:00' } });
    fireEvent.blur(doPola);

    fireEvent.click(screen.getByText('Zapisz'));

    await waitFor(() => expect(screen.getByRole('alert')).toBeTruthy());
    expect(screen.getByRole('alert').textContent).toContain('Od');
    expect(createMutate).not.toHaveBeenCalled();
  });

  it('godzina konca wczesniejsza niz poczatku jest odrzucana z komunikatem', async () => {
    const { od, doPola } = await otworzPanel();

    fireEvent.focus(od);
    fireEvent.change(od, { target: { value: '17:00' } });
    fireEvent.blur(od);
    fireEvent.focus(doPola);
    fireEvent.change(doPola, { target: { value: '09:00' } });
    fireEvent.blur(doPola);

    fireEvent.click(screen.getByText('Zapisz'));

    await waitFor(() => expect(screen.getByRole('alert')).toBeTruthy());
    expect(createMutate).not.toHaveBeenCalled();
  });

  it('poprawny zakres zapisuje wejscie i wyjscie', async () => {
    const { od, doPola } = await otworzPanel();

    fireEvent.focus(od);
    fireEvent.change(od, { target: { value: '09:00' } });
    fireEvent.blur(od);
    fireEvent.focus(doPola);
    fireEvent.change(doPola, { target: { value: '17:00' } });
    fireEvent.blur(doPola);

    fireEvent.click(screen.getByText('Zapisz'));

    await waitFor(() => expect(createMutate).toHaveBeenCalledTimes(2));
    const typy = createMutate.mock.calls.map((c) => (c[0] as { type: string }).type);
    expect(typy).toEqual(['ClockIn', 'ClockOut']);
  });

  it('blad zapisu zostawia panel otwarty z wpisanymi godzinami', async () => {
    createMutate.mockRejectedValueOnce(new Error('Serwer odmowil'));
    const { od, doPola } = await otworzPanel();

    fireEvent.focus(od);
    fireEvent.change(od, { target: { value: '09:00' } });
    fireEvent.blur(od);
    fireEvent.focus(doPola);
    fireEvent.change(doPola, { target: { value: '17:00' } });
    fireEvent.blur(doPola);

    fireEvent.click(screen.getByText('Zapisz'));

    await waitFor(() => expect(screen.getByRole('alert').textContent).toContain('Serwer odmowil'));
    expect(screen.getAllByPlaceholderText('HH:mm')[0]).toBeTruthy();
  });
});
