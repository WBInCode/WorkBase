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
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/org/tree" element={<OrgTreePage />} />
        <Route path="/org/employees" element={<EmployeeListPage />} />
        <Route path="/org/employees/import" element={<CsvImportPage />} />
        <Route path="/time/timesheet" element={<TimesheetPage />} />
        <Route path="/time/team-report" element={<TeamAttendancePage />} />
        <Route path="/time/schedule" element={<SchedulePage />} />
        <Route path="/leave/request" element={<LeaveRequestPage />} />
        <Route path="/leave/approvals" element={<PendingApprovalsPage />} />
        <Route path="/leave/calendar" element={<LeaveCalendarPage />} />
        <Route path="/tasks" element={<TaskListPage />} />
        <Route path="/tasks/my" element={<MyTasksPage />} />
        <Route path="/tasks/:id" element={<TaskCardPage />} />
        <Route path="/admin/roles" element={<RolesPage />} />
        <Route path="/admin/permissions" element={<PermissionsMatrixPage />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
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
