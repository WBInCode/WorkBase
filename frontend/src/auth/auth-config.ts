import type { AuthProviderProps } from 'react-oidc-context';
import { WebStorageStateStore } from 'oidc-client-ts';

const authority = import.meta.env.VITE_KEYCLOAK_AUTHORITY || 'http://localhost:8080/realms/workbase';
const clientId = import.meta.env.VITE_KEYCLOAK_CLIENT_ID || 'workbase-web';
const redirectUri = import.meta.env.VITE_REDIRECT_URI || `${window.location.origin}/auth/callback`;
const postLogoutRedirectUri = import.meta.env.VITE_POST_LOGOUT_REDIRECT_URI || window.location.origin;

export const oidcConfig: AuthProviderProps = {
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  post_logout_redirect_uri: postLogoutRedirectUri,
  response_type: 'code',
  scope: 'openid profile email',
  automaticSilentRenew: true,
  // sessionStorage keeps OIDC state across redirects but clears on tab close
  userStore: new WebStorageStateStore({ store: sessionStorage }),
  onSigninCallback: () => {
    // Remove OIDC state params from URL after login
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};
