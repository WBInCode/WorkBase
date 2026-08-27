// Backend zapisanych widokow mial komplet CRUD-a i ZERO trafien we froncie. Ten test pilnuje
// dwoch rzeczy, ktore latwo zepsuc przy podpinaniu: ze zapisujemy dokladnie te filtry, ktore sa
// na ekranie, i ze uszkodzony JSON nie wywala calej listy.
import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import type { ZapisanyWidok } from '@/api/hooks/useZapisaneWidoki';

const zapisz = vi.fn().mockResolvedValue({});
const usun = vi.fn();
let widoki: ZapisanyWidok[] = [];

vi.mock('@/api/hooks/useZapisaneWidoki', () => ({
  useZapisaneWidoki: () => ({ data: widoki }),
  useZapiszWidok: () => ({ mutateAsync: zapisz, isPending: false }),
  useUsunWidok: () => ({ mutate: usun }),
}));

const { ZapisaneWidoki } = await import('./ZapisaneWidoki');

function widok(nadpisania: Partial<ZapisanyWidok> = {}): ZapisanyWidok {
  return {
    id: 'w1',
    entityType: 'employees',
    name: 'Dział IT',
    filtersJson: '{"status":"Active"}',
    sortJson: '{}',
    columnsJson: null,
    isDefault: false,
    isShared: false,
    ...nadpisania,
  };
}

afterEach(() => {
  cleanup();
  zapisz.mockClear();
  usun.mockClear();
  widoki = [];
});

describe('ZapisaneWidoki', () => {
  it('zapisuje dokladnie te filtry, ktore sa na ekranie', async () => {
    render(
      <ZapisaneWidoki
        entityType="employees"
        filtry={{ status: 'Active', search: 'kowalski' }}
        onZastosuj={() => {}}
        maFiltry
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Widoki/ }));
    fireEvent.change(screen.getByLabelText('Nazwa widoku'), { target: { value: 'Aktywni Kowalscy' } });
    fireEvent.click(screen.getByRole('button', { name: /Zapisz obecne filtry/ }));

    expect(zapisz).toHaveBeenCalledWith(
      expect.objectContaining({
        entityType: 'employees',
        name: 'Aktywni Kowalscy',
        filtersJson: '{"status":"Active","search":"kowalski"}',
        isShared: false,
      }),
    );
  });

  it('wybor widoku oddaje zapisane filtry', () => {
    widoki = [widok()];
    const zastosuj = vi.fn();

    render(
      <ZapisaneWidoki entityType="employees" filtry={{}} onZastosuj={zastosuj} maFiltry={false} />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Widoki/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Dział IT' }));

    expect(zastosuj).toHaveBeenCalledWith({ status: 'Active' });
  });

  it('uszkodzony JSON nie wywala listy', () => {
    widoki = [widok({ filtersJson: '{to nie jest json' })];
    const zastosuj = vi.fn();

    render(
      <ZapisaneWidoki entityType="employees" filtry={{}} onZastosuj={zastosuj} maFiltry={false} />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Widoki/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Dział IT' }));

    expect(zastosuj).not.toHaveBeenCalled();
  });

  it('bez filtrow nie ma czego zapisac', () => {
    render(<ZapisaneWidoki entityType="employees" filtry={{}} onZastosuj={() => {}} maFiltry={false} />);

    fireEvent.click(screen.getByRole('button', { name: /Widoki/ }));

    expect(screen.queryByLabelText('Nazwa widoku')).toBeNull();
  });
});
