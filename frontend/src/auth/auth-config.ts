import type { AuthProviderProps } from 'react-oidc-context';
import { WebStorageStateStore } from 'oidc-client-ts';

const defaultAuthority = import.meta.env.VITE_KEYCLOAK_AUTHORITY || 'http://localhost:8080/realms/workbase';
const clientId = import.meta.env.VITE_KEYCLOAK_CLIENT_ID || 'workbase-web';
const redirectUri = import.meta.env.VITE_REDIRECT_URI || `${window.location.origin}/auth/callback`;
const postLogoutRedirectUri = import.meta.env.VITE_POST_LOGOUT_REDIRECT_URI || `${window.location.origin}/logged-out`;

const REALM_STORAGE_KEY = 'wb_realm';

/**
 * Multi-realm support (docs/05-module-licensing-architecture.md §5): each company can have
 * its own dedicated Keycloak realm. Opening the app with ?realm=tenant-acme pins that realm
 * in localStorage (survives the OIDC redirect roundtrip and future visits); ?realm= (empty)
 * clears it back to the shared/default realm. The realm name only ever swaps the last path
 * segment of the configured authority — a crafted link cannot point login at a foreign
 * Keycloak server, and the backend independently validates the token's issuer against the
 * Tenant table either way.
 */
function resolveAuthority(): string {
  const params = new URLSearchParams(window.location.search);
  if (params.has('realm')) {
    const requested = (params.get('realm') ?? '').trim().toLowerCase();
    if (requested && /^[a-z0-9-]+$/.test(requested)) {
      localStorage.setItem(REALM_STORAGE_KEY, requested);
    } else {
      localStorage.removeItem(REALM_STORAGE_KEY);
    }
  }

  const realm = localStorage.getItem(REALM_STORAGE_KEY);
  if (!realm) return defaultAuthority;

  return defaultAuthority.replace(/\/realms\/[^/]+$/, `/realms/${realm}`);
}

const authority = resolveAuthority();

/**
 * Domyślny dostawca tożsamości (Identity Provider) dla logowania. Ustawiony na
 * „wb-hub" sprawia, że Keycloak od razu przekierowuje do logowania przez WB Platform
 * (Hub jako IdP) — użytkownik NIE widzi natywnego ekranu logowania Keycloak. Pusta
 * wartość (VITE_KC_IDP_HINT="") przywraca standardowy ekran Keycloak (np. do lokalnego
 * logowania administracyjnego). Konta kiosku używają osobnego przepływu i to nie dotyczy ich.
 */
const kcIdpHint = import.meta.env.VITE_KC_IDP_HINT ?? 'wb-hub';

export const oidcConfig: AuthProviderProps = {
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  post_logout_redirect_uri: postLogoutRedirectUri,
  response_type: 'code',
  scope: 'openid profile email',
  automaticSilentRenew: true,
  // kc_idp_hint kieruje Keycloak prosto do wskazanego IdP, pomijając jego ekran logowania.
  ...(kcIdpHint ? { extraQueryParams: { kc_idp_hint: kcIdpHint } } : {}),
  // sessionStorage keeps OIDC state across redirects but clears on tab close
  userStore: new WebStorageStateStore({ store: sessionStorage }),
  onSigninCallback: () => {
    // Remove OIDC state params from URL after login
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};
