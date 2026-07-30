import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, cleanup, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

class MockApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

const apiGet = vi.fn();
const apiPost = vi.fn();

vi.mock('@/api/client', () => ({
  api: {
    get: (url: string) => apiGet(url),
    post: (url: string, body: unknown) => apiPost(url, body),
    put: vi.fn(),
    delete: vi.fn(),
  },
  ApiError: MockApiError,
}));

const kierownik = {
  id: 'role-1',
  name: 'Kierownik',
  description: 'Zarządzanie zespołem',
  type: 'System',
  isActive: true,
  level: 30,
  permissionCount: 20,
  userCount: 0,
};

const employees = [
  { id: 'emp-1', firstName: 'Anna', lastName: 'Nowak', email: 'anna@firma.pl', userId: 'user-1', status: 'Active' },
  { id: 'emp-2', firstName: 'Piotr', lastName: 'Bez Konta', email: 'piotr@firma.pl', userId: null, status: 'Active' },
];

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <RolesPage />
    </QueryClientProvider>,
  );
}

let RolesPage: typeof import('./RolesPage').RolesPage;

beforeEach(async () => {
  ({ RolesPage } = await import('./RolesPage'));
  apiPost.mockReset().mockResolvedValue(undefined);
  apiGet.mockReset().mockImplementation((url: string) => {
    if (url === '/api/auth/me') {
      return Promise.resolve({ userId: 'me', email: 'admin@firma.pl', name: 'Admin', permissions: ['identity.assign-roles'], isAdmin: true });
    }
    if (url === '/api/iam/roles') return Promise.resolve([kierownik]);
    if (url.startsWith('/api/iam/roles/role-1/users')) return Promise.resolve([]);
    if (url.startsWith('/api/org/employees')) {
      return Promise.resolve({ items: employees, totalCount: employees.length, page: 1, pageSize: 8 });
    }
    return Promise.reject(new Error(`Unexpected URL ${url}`));
  });
});

afterEach(() => cleanup());

describe('RolesPage — przypisywanie roli', () => {
  it('pozwala przypisać rolę pracownikowi z kontem i blokuje pracownika bez konta', async () => {
    const user = userEvent.setup();
    renderPage();

    // Rola bez przypisanych osób też otwiera listę — inaczej nie da się dodać pierwszej.
    await user.click(await screen.findByTitle('Brak przypisanych — przypisz pierwszego'));
    await user.click(await screen.findByRole('button', { name: /Przypisz użytkownika/ }));
    await user.type(screen.getByPlaceholderText(/Szukaj pracownika/), 'a');

    expect(await screen.findByText('Piotr Bez Konta')).toBeInTheDocument();
    expect(screen.getByText('Brak konta')).toBeInTheDocument();

    const assignButtons = await screen.findAllByRole('button', { name: 'Przypisz' });
    expect(assignButtons).toHaveLength(1);
    await user.click(assignButtons[0]!);

    await waitFor(() =>
      expect(apiPost).toHaveBeenCalledWith('/api/iam/users/user-1/roles', { roleId: 'role-1' }),
    );
  });

  it('ukrywa przypisywanie dla roli zarządzanej przez WB Platform', async () => {
    apiGet.mockImplementation((url: string) => {
      if (url === '/api/auth/me') {
        return Promise.resolve({ userId: 'me', email: 'admin@firma.pl', name: 'Admin', permissions: ['identity.assign-roles'], isAdmin: true });
      }
      if (url === '/api/iam/roles') return Promise.resolve([{ ...kierownik, id: 'role-2', name: 'Admin' }]);
      if (url.startsWith('/api/iam/roles/role-2/users')) return Promise.resolve([]);
      return Promise.reject(new Error(`Unexpected URL ${url}`));
    });
    const user = userEvent.setup();
    renderPage();

    await user.click(await screen.findByTitle('Brak przypisanych — przypisz pierwszego'));

    expect(await screen.findByText(/Tę rolę nadaje WB Platform/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Przypisz użytkownika/ })).not.toBeInTheDocument();
  });
});
