import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from 'react-oidc-context';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { oidcConfig, ProtectedRoute } from '@/auth';
import { setTokenProvider } from '@/api/client';
import { MainLayout } from '@/layouts/MainLayout';
import { AuthCallbackPage } from '@/pages/AuthCallbackPage';
import { OrgTreePage } from '@/pages/organization/OrgTreePage';
import { EmployeeListPage } from '@/pages/organization/EmployeeListPage';
import { CsvImportPage } from '@/pages/organization/CsvImportPage';
import { RolesPage } from '@/pages/admin/RolesPage';
import { PermissionsMatrixPage } from '@/pages/admin/PermissionsMatrixPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});

function AppRoutes() {
  const auth = useAuth();

  // Wire API client token provider
  setTokenProvider(() => auth.user?.access_token);

  return (
    <MainLayout>
      <Routes>
        <Route path="/org/tree" element={<OrgTreePage />} />
        <Route path="/org/employees" element={<EmployeeListPage />} />
        <Route path="/org/employees/import" element={<CsvImportPage />} />
        <Route path="/admin/roles" element={<RolesPage />} />
        <Route path="/admin/permissions" element={<PermissionsMatrixPage />} />
        <Route path="*" element={<Navigate to="/org/tree" replace />} />
      </Routes>
    </MainLayout>
  );
}

function App() {
  return (
    <AuthProvider {...oidcConfig}>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <Routes>
            <Route path="/auth/callback" element={<AuthCallbackPage />} />
            <Route
              path="/*"
              element={
                <ProtectedRoute>
                  <AppRoutes />
                </ProtectedRoute>
              }
            />
          </Routes>
        </BrowserRouter>
      </QueryClientProvider>
    </AuthProvider>
  );
}

export default App;
