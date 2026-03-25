import { useAuth } from 'react-oidc-context';
import type { ReactNode } from 'react';

interface ProtectedRouteProps {
  children: ReactNode;
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const auth = useAuth();

  if (auth.isLoading) {
    return <div>Ładowanie...</div>;
  }

  if (auth.error) {
    return (
      <div>
        <p>Błąd autentykacji: {auth.error.message}</p>
        <button onClick={() => auth.signinRedirect()}>Zaloguj ponownie</button>
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    auth.signinRedirect();
    return <div>Przekierowanie do logowania...</div>;
  }

  return <>{children}</>;
}
