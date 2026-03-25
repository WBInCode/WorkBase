import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';

export function HomePage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;

  const handleLogout = () => {
    auth.signoutRedirect();
  };

  return (
    <div>
      <h1>WorkBase</h1>
      {user && (
        <div>
          <p><strong>Użytkownik:</strong> {user.name} ({user.email})</p>
          <p><strong>Tenant:</strong> {user.tenantId}</p>
          <p><strong>Employee:</strong> {user.employeeId}</p>
          <p><strong>Role:</strong> {user.roles.join(', ') || '—'}</p>
          <button onClick={handleLogout}>Wyloguj</button>
        </div>
      )}
    </div>
  );
}
