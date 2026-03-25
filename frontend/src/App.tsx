import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from 'react-oidc-context';
import { oidcConfig, ProtectedRoute } from '@/auth';
import { HomePage } from '@/pages/HomePage';
import { AuthCallbackPage } from '@/pages/AuthCallbackPage';

function App() {
  return (
    <AuthProvider {...oidcConfig}>
      <BrowserRouter>
        <Routes>
          <Route path="/auth/callback" element={<AuthCallbackPage />} />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <HomePage />
              </ProtectedRoute>
            }
          />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
