/**
 * shared/storage — Platform-agnostic storage abstraction.
 * In browser: wraps localStorage/sessionStorage.
 * In Tauri/desktop: can be swapped for fs-based or Tauri store plugin.
 */

export interface StorageAdapter {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

class BrowserStorageAdapter implements StorageAdapter {
  constructor(private readonly store: Storage) {}
  getItem(key: string) { return this.store.getItem(key); }
  setItem(key: string, value: string) { this.store.setItem(key, value); }
  removeItem(key: string) { this.store.removeItem(key); }
}

class MemoryStorageAdapter implements StorageAdapter {
  private data = new Map<string, string>();
  getItem(key: string) { return this.data.get(key) ?? null; }
  setItem(key: string, value: string) { this.data.set(key, value); }
  removeItem(key: string) { this.data.delete(key); }
}

function createDefaultAdapter(type: 'local' | 'session'): StorageAdapter {
  if (typeof window !== 'undefined' && typeof window.localStorage !== 'undefined') {
    return new BrowserStorageAdapter(type === 'local' ? window.localStorage : window.sessionStorage);
  }
  return new MemoryStorageAdapter();
}

let _localStorage: StorageAdapter = createDefaultAdapter('local');
let _sessionStorage: StorageAdapter = createDefaultAdapter('session');

export function setStorageAdapters(local: StorageAdapter, session: StorageAdapter) {
  _localStorage = local;
  _sessionStorage = session;
}

export const appStorage = {
  local: {
    get: (key: string) => _localStorage.getItem(key),
    set: (key: string, value: string) => _localStorage.setItem(key, value),
    remove: (key: string) => _localStorage.removeItem(key),
  },
  session: {
    get: (key: string) => _sessionStorage.getItem(key),
    set: (key: string, value: string) => _sessionStorage.setItem(key, value),
    remove: (key: string) => _sessionStorage.removeItem(key),
  },
};
