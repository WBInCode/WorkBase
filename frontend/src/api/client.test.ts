import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { api, ApiError, setTokenProvider } from './client';

function mockFetchOnce(response: Partial<Response> & { jsonBody?: unknown }) {
  const { jsonBody, ...rest } = response;
  const fetchMock = vi.fn().mockResolvedValue({
    ok: true,
    status: 200,
    statusText: 'OK',
    json: () => Promise.resolve(jsonBody),
    ...rest,
  } as Response);
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('api client', () => {
  beforeEach(() => {
    setTokenProvider(() => undefined);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('sends Authorization header when a token provider is set', async () => {
    setTokenProvider(() => 'test-token');
    const fetchMock = mockFetchOnce({ jsonBody: { ok: true } });

    await api.get('/api/whoami');

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Record<string, string>;
    expect(headers['Authorization']).toBe('Bearer test-token');
  });

  it('omits Authorization header when no token is available', async () => {
    const fetchMock = mockFetchOnce({ jsonBody: { ok: true } });

    await api.get('/api/whoami');

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = init.headers as Record<string, string>;
    expect(headers['Authorization']).toBeUndefined();
  });

  it('serializes the request body as JSON for POST requests', async () => {
    const fetchMock = mockFetchOnce({ jsonBody: { id: '123' } });

    await api.post('/api/tasks', { title: 'Test task' });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(init.method).toBe('POST');
    expect(init.body).toBe(JSON.stringify({ title: 'Test task' }));
  });

  it('returns undefined for 204 No Content responses', async () => {
    mockFetchOnce({ status: 204, jsonBody: undefined });

    const result = await api.delete('/api/tasks/123');

    expect(result).toBeUndefined();
  });

  it('throws ApiError with the parsed message on non-2xx responses', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 403,
      statusText: 'Forbidden',
      json: () => Promise.resolve({ message: 'Brak uprawnień' }),
    } as Response);
    vi.stubGlobal('fetch', fetchMock);

    await expect(api.get('/api/secret')).rejects.toMatchObject({
      status: 403,
      message: 'Brak uprawnień',
    });
  });

  it('falls back to statusText when the error body cannot be parsed', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      statusText: 'Internal Server Error',
      json: () => Promise.reject(new Error('not json')),
    } as Response);
    vi.stubGlobal('fetch', fetchMock);

    let caught: unknown;
    try {
      await api.get('/api/broken');
    } catch (err) {
      caught = err;
    }

    expect(caught).toBeInstanceOf(ApiError);
    expect((caught as ApiError).message).toBe('Internal Server Error');
  });
});
