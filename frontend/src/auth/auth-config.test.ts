import { beforeEach, describe, expect, it, vi } from 'vitest';

async function loadConfig(path: string) {
  window.history.replaceState({}, '', path);
  vi.resetModules();
  return (await import('./auth-config')).oidcConfig as {
    extraQueryParams?: Record<string, string>;
    post_logout_redirect_uri?: string;
  };
}

describe('OIDC route mode', () => {
  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
  });

  it('keeps native Keycloak login through the kiosk callback', async () => {
    const kioskConfig = await loadConfig('/kiosk?realm=');

    expect(kioskConfig.extraQueryParams).toEqual({ kc_idp_hint: '' });
    expect(kioskConfig.post_logout_redirect_uri).toBe(`${window.location.origin}/kiosk?realm=`);

    const callbackConfig = await loadConfig('/auth/callback?code=test&state=test');

    expect(callbackConfig.extraQueryParams).toEqual({ kc_idp_hint: '' });
    expect(callbackConfig.post_logout_redirect_uri).toBe(`${window.location.origin}/kiosk?realm=`);
  });

  it('uses the Hub identity provider for the regular application', async () => {
    const config = await loadConfig('/workspace');

    expect(config.extraQueryParams).toEqual({ kc_idp_hint: 'wb-hub' });
    expect(config.post_logout_redirect_uri).not.toBe(`${window.location.origin}/kiosk?realm=`);
  });
});
