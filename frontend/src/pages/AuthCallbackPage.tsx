import { useAuth } from 'react-oidc-context';
import { Navigate } from 'react-router-dom';

export function AuthCallbackPage() {
  const auth = useAuth();

  if (auth.isLoading) {
    return <div>Finalizowanie logowania...</div>;
  }

  if (auth.error) {
    return (
      <div>
        <p>Błąd logowania: {auth.error.message}</p>
        <button onClick={() => auth.signinRedirect()}>Spróbuj ponownie</button>
      </div>
    );
  }

  return <Navigate to="/" replace />;
}
