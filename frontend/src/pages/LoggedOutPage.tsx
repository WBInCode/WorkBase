import { useAuth } from 'react-oidc-context';

/**
 * Ekran po wylogowaniu. Jest CELEM `post_logout_redirect_uri`, więc leży POZA
 * ProtectedRoute — dzięki temu użytkownik nie jest automatycznie logowany z powrotem
 * (globalny kc_idp_hint + żywa sesja Huba inaczej wciągnęłyby go od razu). Powrót
 * do aplikacji następuje dopiero po świadomym kliknięciu „Zaloguj się".
 */
export function LoggedOutPage() {
  const auth = useAuth();

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 24,
      }}
    >
      <div
        style={{
          background: 'var(--wb-panel, #fff)',
          borderRadius: 20,
          padding: '36px 40px',
          maxWidth: 420,
          width: '100%',
          textAlign: 'center',
          boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 12px 34px -14px rgba(20,25,43,0.14)',
          border: '1px solid var(--wb-line, #e3e7f1)',
        }}
      >
        <div
          style={{
            width: 52,
            height: 52,
            margin: '0 auto 18px',
            borderRadius: 16,
            display: 'grid',
            placeItems: 'center',
            background: 'linear-gradient(135deg,#34c98f,#0e9f6e)',
            color: '#fff',
            fontSize: 22,
            fontWeight: 800,
          }}
        >
          ✓
        </div>
        <div style={{ fontSize: 17, fontWeight: 800, color: 'var(--wb-ink, #14192b)', marginBottom: 8 }}>
          Wylogowano
        </div>
        <div style={{ fontSize: 13.5, color: 'var(--wb-ink-2, #6b7490)', lineHeight: 1.55, marginBottom: 22 }}>
          Twoja sesja WorkBase została zakończona. Możesz bezpiecznie zamknąć kartę
          albo zalogować się ponownie.
        </div>
        <button
          onClick={() => auth.signinRedirect()}
          style={{
            padding: '11px 26px',
            fontSize: 14,
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
          Zaloguj się ponownie
        </button>
      </div>
    </div>
  );
}
