const API_BASE = import.meta.env.VITE_API_URL ?? '';

interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown;
}

let getAccessToken: (() => string | undefined) | null = null;

export function setTokenProvider(provider: () => string | undefined) {
  getAccessToken = provider;
}

const SCIEZKA_KREATORA = '/kreator';

/**
 * Buduje błąd z odpowiedzi i — dla firmy, która nie ukończyła kreatora pierwszego startu —
 * przenosi do kreatora.
 *
 * Serwer odpowiada wtedy 409 z `errorCode: SETUP_REQUIRED` na każdym żądaniu poza białą listą.
 * Bez tego przekierowania właściciel nowej firmy widzi rozsypaną aplikację: powłoka odpytuje
 * branding i feature flags, dostaje 409 i nie ma jak dowiedzieć się, co zrobić.
 *
 * Twarde przejście zamiast nawigacji Reactem jest celowe — ten kod działa poza drzewem
 * routera, a kreator i tak renderuje się poza MainLayout, więc nie ma czego zachowywać.
 */
async function bladZOdpowiedzi(response: Response): Promise<ApiError> {
  const tresc = await response.json().catch(() => ({ message: response.statusText }));

  const kod = (tresc as { errorCode?: string })?.errorCode;
  if (
    response.status === 409 &&
    kod === 'SETUP_REQUIRED' &&
    typeof window !== 'undefined' &&
    !window.location.pathname.startsWith(SCIEZKA_KREATORA)
  ) {
    window.location.assign(SCIEZKA_KREATORA);
  }

  return new ApiError(
    response.status,
    tresc.message ?? tresc.detail ?? tresc.title ?? response.statusText,
    tresc,
  );
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, headers: customHeaders, ...rest } = options;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...customHeaders as Record<string, string>,
  };

  const token = getAccessToken?.();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...rest,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) throw await bladZOdpowiedzi(response);

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public body?: unknown,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: 'GET' }),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body }),
  put: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
  postForm: async <T>(path: string, formData: FormData): Promise<T> => {
    const headers: Record<string, string> = {};
    const token = getAccessToken?.();
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const response = await fetch(`${API_BASE}${path}`, {
      method: 'POST',
      headers,
      body: formData,
    });

    // Backend zwraca ProblemDetails z polem `detail` — bez tego użytkownik dostawał samo
    // „Nie udało się” zamiast konkretnej przyczyny (np. niedozwolone rozszerzenie pliku).
    if (!response.ok) throw await bladZOdpowiedzi(response);

    if (response.status === 204) return undefined as T;
    return response.json() as Promise<T>;
  },
  download: async (path: string): Promise<Blob> => {
    const headers: Record<string, string> = {};
    const token = getAccessToken?.();
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const response = await fetch(`${API_BASE}${path}`, { method: 'GET', headers });
    if (!response.ok) throw await bladZOdpowiedzi(response);
    return response.blob();
  },
};
