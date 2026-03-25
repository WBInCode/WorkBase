import type { AuthProviderProps } from 'react-oidc-context';
import { WebStorageStateStore, InMemoryWebStorage } from 'oidc-client-ts';

const authority = import.meta.env.VITE_KEYCLOAK_AUTHORITY || 'http://localhost:8080/realms/workbase';
const clientId = import.meta.env.VITE_KEYCLOAK_CLIENT_ID || 'workbase-web';
const redirectUri = import.meta.env.VITE_REDIRECT_URI || 'http://localhost:5173/auth/callback';
const postLogoutRedirectUri = import.meta.env.VITE_POST_LOGOUT_REDIRECT_URI || 'http://localhost:5173';

export const oidcConfig: AuthProviderProps = {
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  post_logout_redirect_uri: postLogoutRedirectUri,
  response_type: 'code',
  scope: 'openid profile email',
  automaticSilentRenew: true,
  // Store tokens in memory only — never localStorage
  userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
  onSigninCallback: () => {
    // Remove OIDC state params from URL after login
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};
