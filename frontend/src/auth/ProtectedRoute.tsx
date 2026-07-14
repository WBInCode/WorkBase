import { useAuth } from 'react-oidc-context';
import type { ReactNode } from 'react';

interface ProtectedRouteProps {
  children: ReactNode;
}

function FullScreenCentered({ children }: { children: ReactNode }) {
  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 14,
        padding: 24,
      }}
    >
      {children}
    </div>
  );
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const auth = useAuth();

  if (auth.isLoading) {
    return (
      <FullScreenCentered>
        <div className="wb-spinner" />
        <div style={{ fontSize: 13, fontWeight: 600, color: '#9aa3bc' }}>Ładowanie…</div>
      </FullScreenCentered>
    );
  }

  if (auth.error) {
    return (
      <FullScreenCentered>
        <div
          style={{
            background: 'var(--wb-panel, #fff)',
            borderRadius: 20,
            padding: '32px 36px',
            maxWidth: 420,
            textAlign: 'center',
            boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.12)',
            border: '1px solid var(--wb-line, #e3e7f1)',
          }}
        >
          <div style={{ fontSize: 15, fontWeight: 800, color: 'var(--wb-ink, #14192b)', marginBottom: 6 }}>
            Błąd autentykacji
          </div>
          <div style={{ fontSize: 13, color: 'var(--wb-ink-2, #6b7490)', marginBottom: 18, lineHeight: 1.5 }}>
            {auth.error.message}
          </div>
          <button
            onClick={() => auth.signinRedirect()}
            style={{
              padding: '10px 22px',
              fontSize: 13.5,
              fontWeight: 700,
              fontFamily: 'inherit',
              color: '#fff',
              background: '#3d6df2',
              border: 'none',
              borderRadius: 999,
              cursor: 'pointer',
              boxShadow: '0 6px 14px -4px rgba(61,109,242,0.45)',
            }}
          >
            Zaloguj ponownie
          </button>
        </div>
      </FullScreenCentered>
    );
  }

  if (!auth.isAuthenticated) {
    // Handoff SSO z Huba mógł się nie powieść (?sso_error=...) — pokaż krótką
    // informację zamiast natychmiastowego (i mylącego) ponownego redirectu.
    const ssoError = new URLSearchParams(window.location.search).get('sso_error');
    if (ssoError) {
      const messages: Record<string, string> = {
        missing_token: 'Brak biletu logowania z platformy WB.',
        invalid: 'Bilet logowania jest nieprawidłowy lub wygasł.',
        wrong_instance: 'Bilet dotyczy innej instalacji WorkBase.',
        used: 'Ten bilet logowania został już użyty.',
      };
      return (
        <FullScreenCentered>
          <div
            style={{
              background: 'var(--wb-panel, #fff)',
              borderRadius: 20,
              padding: '32px 36px',
              maxWidth: 420,
              textAlign: 'center',
              boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.12)',
              border: '1px solid var(--wb-line, #e3e7f1)',
            }}
          >
            <div style={{ fontSize: 15, fontWeight: 800, color: 'var(--wb-ink, #14192b)', marginBottom: 6 }}>
              Logowanie przez WB nieudane
            </div>
            <div style={{ fontSize: 13, color: 'var(--wb-ink-2, #6b7490)', marginBottom: 18, lineHeight: 1.5 }}>
              {messages[ssoError] ?? 'Nie udało się zalogować przez platformę WB.'} Zaloguj się bezpośrednio poniżej.
            </div>
            <button
              onClick={() => auth.signinRedirect()}
              style={{
                padding: '10px 22px',
                fontSize: 13.5,
                fontWeight: 700,
                fontFamily: 'inherit',
                color: '#fff',
                background: '#3d6df2',
                border: 'none',
                borderRadius: 999,
                cursor: 'pointer',
                boxShadow: '0 6px 14px -4px rgba(61,109,242,0.45)',
              }}
            >
              Zaloguj się
            </button>
          </div>
        </FullScreenCentered>
      );
    }

    auth.signinRedirect();
    return (
      <FullScreenCentered>
        <div className="wb-spinner" />
        <div style={{ fontSize: 13, fontWeight: 600, color: '#9aa3bc' }}>Przekierowanie do logowania…</div>
      </FullScreenCentered>
    );
  }

  return <>{children}</>;
}
