import { BrowserRouter, HashRouter, Routes, Route, Navigate, useNavigate } from 'react-router-dom';
import { AuthProvider, useAuth } from 'react-oidc-context';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEffect } from 'react';
import { oidcConfig, ProtectedRoute } from '@/auth';
import { setTokenProvider } from '@/api/client';
import { getRouterMode } from '@/shared';
import { MainLayout } from '@/layouts/MainLayout';
import { AuthCallbackPage } from '@/pages/AuthCallbackPage';
import { OrgTreePage } from '@/pages/organization/OrgTreePage';
import { EmployeeListPage } from '@/pages/organization/EmployeeListPage';
import { CsvImportPage } from '@/pages/organization/CsvImportPage';
import { EmployeeCardPage } from '@/pages/organization/EmployeeCardPage';
import { RolesPage } from '@/pages/admin/RolesPage';
import { PermissionsMatrixPage } from '@/pages/admin/PermissionsMatrixPage';
import { FeatureFlagsPage } from '@/pages/admin/FeatureFlagsPage';
import { LeaveTypesConfigPage } from '@/pages/admin/LeaveTypesConfigPage';
import { TaskStatusConfigPage } from '@/pages/admin/TaskStatusConfigPage';
import { BreakPoliciesConfigPage } from '@/pages/admin/BreakPoliciesConfigPage';
import { PositionsConfigPage } from '@/pages/admin/PositionsConfigPage';
import { UnitTypesConfigPage } from '@/pages/admin/UnitTypesConfigPage';
import { TimesheetPage } from '@/pages/time/TimesheetPage';
import { TeamAttendancePage } from '@/pages/time/TeamAttendancePage';
import { SchedulePage } from '@/pages/time/SchedulePage';
import { LeaveRequestPage } from '@/pages/leave/LeaveRequestPage';
import { LeaveCalendarPage } from '@/pages/leave/LeaveCalendarPage';
import { PendingApprovalsPage } from '@/pages/leave/PendingApprovalsPage';
import { TaskListPage } from '@/pages/tasks/TaskListPage';
import { TaskCardPage } from '@/pages/tasks/TaskCardPage';
import { MyTasksPage } from '@/pages/tasks/MyTasksPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { WorkspacePage } from '@/pages/WorkspacePage';
import { KioskPage } from '@/pages/KioskPage';
import { DocumentListPage } from '@/pages/documents/DocumentListPage';
import { DocumentCategoriesPage } from '@/pages/documents/DocumentCategoriesPage';
import { WorkflowBuilderPage } from '@/pages/workflow/WorkflowBuilderPage';
import { FormBuilderPage } from '@/pages/forms/FormBuilderPage';
import { ToastProvider } from '@/components/Notifications';

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
  const navigate = useNavigate();

  // Wire API client token provider
  setTokenProvider(() => auth.user?.access_token);

  const roles = (auth.user?.profile?.['roles'] as string[] | undefined) ?? [];
  const isAdmin = roles.some((r) => r === 'workbase-admin' || r === 'Admin' || r === 'Super Admin');

  // Auto-redirect kiosk accounts to /kiosk
  useEffect(() => {
    if (auth.user) {
      if (roles.includes('workbase-kiosk')) {
        navigate('/kiosk', { replace: true });
      }
    }
  }, [auth.user, navigate]);

  return (
    <MainLayout>
      <Routes>
        <Route path="/workspace" element={<WorkspacePage />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/org/tree" element={<OrgTreePage />} />
        <Route path="/org/employees" element={<EmployeeListPage />} />
        <Route path="/org/employees/import" element={<CsvImportPage />} />
        <Route path="/org/employees/:id" element={<EmployeeCardPage />} />
        <Route path="/time/timesheet" element={<TimesheetPage />} />
        <Route path="/time/team-report" element={<TeamAttendancePage />} />
        <Route path="/time/schedule" element={<SchedulePage />} />
        <Route path="/leave/request" element={<LeaveRequestPage />} />
        <Route path="/leave/approvals" element={<PendingApprovalsPage />} />
        <Route path="/leave/calendar" element={<LeaveCalendarPage />} />
        <Route path="/tasks" element={<TaskListPage />} />
        <Route path="/tasks/my" element={<MyTasksPage />} />
        <Route path="/tasks/:id" element={<TaskCardPage />} />
        <Route path="/documents" element={<DocumentListPage />} />
        <Route path="/documents/categories" element={<DocumentCategoriesPage />} />
        <Route path="/workflow/builder" element={<WorkflowBuilderPage />} />
        <Route path="/forms/builder" element={<FormBuilderPage />} />
        <Route path="/admin/roles" element={isAdmin ? <RolesPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/permissions" element={isAdmin ? <PermissionsMatrixPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/feature-flags" element={isAdmin ? <FeatureFlagsPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/leave-types" element={isAdmin ? <LeaveTypesConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/task-statuses" element={isAdmin ? <TaskStatusConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/break-policies" element={isAdmin ? <BreakPoliciesConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/positions" element={isAdmin ? <PositionsConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/unit-types" element={isAdmin ? <UnitTypesConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="*" element={<Navigate to="/workspace" replace />} />
      </Routes>
    </MainLayout>
  );
}

const Router = getRouterMode() === 'hash' ? HashRouter : BrowserRouter;

function AppRouter({ children }: { children: React.ReactNode }) {
  return <Router>{children}</Router>;
}

function App() {
  return (
    <AuthProvider {...oidcConfig}>
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
        <AppRouter>
          <Routes>
            <Route path="/auth/callback" element={<AuthCallbackPage />} />
            <Route
              path="/kiosk"
              element={
                <ProtectedRoute>
                  <KioskPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/*"
              element={
                <ProtectedRoute>
                  <AppRoutes />
                </ProtectedRoute>
              }
            />
          </Routes>
        </AppRouter>
        </ToastProvider>
      </QueryClientProvider>
    </AuthProvider>
  );
}

export default App;
