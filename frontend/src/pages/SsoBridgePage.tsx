import { useEffect, useRef } from 'react';
import { useAuth } from 'react-oidc-context';

/**
 * Most SSO Hub → Keycloak. Backend (/sso/callback) zweryfikował bilet Huba i
 * utworzył/zsynchronizował konto w Keycloaku, po czym przekierował tutaj z
 * ?email=. Teraz uruchamiamy PRAWDZIWY przepływ OIDC (Authorization Code + PKCE)
 * z podpowiedzią e-maila (login_hint) — użytkownik trafia na ekran Keycloaka już
 * z uzupełnionym loginem, więc podaje tylko hasło. Jeśli ma aktywną sesję SSO w
 * Keycloaku (single sign-on), przejdzie bez żadnego ekranu.
 *
 * Uwaga: pełne „bezhasłowe" logowanie z Huba wymagałoby token-exchange po stronie
 * Keycloaka (klient poufny). Ten most daje SSO end-to-end przy publicznym kliencie
 * SPA bez osłabiania bezpieczeństwa — konto i rola pochodzą z zaufanego biletu Huba.
 */
export function SsoBridgePage() {
  const auth = useAuth();
  const started = useRef(false);

  useEffect(() => {
    if (started.current) return;
    if (auth.isLoading) return;

    started.current = true;
    const email = new URLSearchParams(window.location.search).get('email') ?? undefined;
    // Backend just refreshed tenant/user attributes from a verified HUB handoff.
    // Always run authorize again, even when react-oidc-context still holds an older
    // authenticated user, so the API receives a token with current HUB claims.
    void auth.signinRedirect({
      login_hint: email,
      redirect_uri: `${window.location.origin}/auth/callback`,
    });
  }, [auth, auth.isLoading, auth.isAuthenticated]);

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 14,
      }}
    >
      <div className="wb-spinner" />
      <div style={{ fontSize: 13, fontWeight: 600, color: '#9aa3bc' }}>Łączenie z platformą WB…</div>
    </div>
  );
}
