import { useAuth } from 'react-oidc-context';
import { Navigate } from 'react-router-dom';

export function AuthCallbackPage() {
  const auth = useAuth();

  if (auth.isLoading) {
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
        <div style={{ fontSize: 13, fontWeight: 600, color: '#9aa3bc' }}>Finalizowanie logowania…</div>
      </div>
    );
  }

  if (auth.error) {
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
            padding: '32px 36px',
            maxWidth: 420,
            textAlign: 'center',
            boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.12)',
            border: '1px solid var(--wb-line, #e3e7f1)',
          }}
        >
          <div style={{ fontSize: 15, fontWeight: 800, color: 'var(--wb-ink, #14192b)', marginBottom: 6 }}>
            Błąd logowania
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
            Spróbuj ponownie
          </button>
        </div>
      </div>
    );
  }

  return <Navigate to="/" replace />;
}
