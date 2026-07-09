import { BrowserRouter, HashRouter, Routes, Route, Navigate, useNavigate, useLocation } from 'react-router-dom';
import { AuthProvider, useAuth } from 'react-oidc-context';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Suspense, lazy, useEffect } from 'react';
import { oidcConfig, ProtectedRoute } from '@/auth';
import { setTokenProvider } from '@/api/client';
import { getRouterMode } from '@/shared';
import { MainLayout } from '@/layouts/MainLayout';
import { ErrorBoundary } from '@/components/ui';
import { AuthCallbackPage } from '@/pages/AuthCallbackPage';
import { ToastProvider } from '@/components/Notifications';
import { colors } from '@/theme/tokens';

// Route-level code splitting: each page is loaded on demand instead of being
// bundled into the initial chunk. Keeps the first paint fast for a large,
// multi-module SPA (see docs/AUDIT-KNOWLEDGE-MAP.md, punkt 15).
const OrgTreePage = lazy(() => import('@/pages/organization/OrgTreePage').then((m) => ({ default: m.OrgTreePage })));
const EmployeeListPage = lazy(() => import('@/pages/organization/EmployeeListPage').then((m) => ({ default: m.EmployeeListPage })));
const CsvImportPage = lazy(() => import('@/pages/organization/CsvImportPage').then((m) => ({ default: m.CsvImportPage })));
const EmployeeCardPage = lazy(() => import('@/pages/organization/EmployeeCardPage').then((m) => ({ default: m.EmployeeCardPage })));
const RolesPage = lazy(() => import('@/pages/admin/RolesPage').then((m) => ({ default: m.RolesPage })));
const PermissionsMatrixPage = lazy(() => import('@/pages/admin/PermissionsMatrixPage').then((m) => ({ default: m.PermissionsMatrixPage })));
const FeatureFlagsPage = lazy(() => import('@/pages/admin/FeatureFlagsPage').then((m) => ({ default: m.FeatureFlagsPage })));
const PlatformTenantsPage = lazy(() => import('@/pages/admin/PlatformTenantsPage').then((m) => ({ default: m.PlatformTenantsPage })));
const LeaveTypesConfigPage = lazy(() => import('@/pages/admin/LeaveTypesConfigPage').then((m) => ({ default: m.LeaveTypesConfigPage })));
const TaskStatusConfigPage = lazy(() => import('@/pages/admin/TaskStatusConfigPage').then((m) => ({ default: m.TaskStatusConfigPage })));
const BreakPoliciesConfigPage = lazy(() => import('@/pages/admin/BreakPoliciesConfigPage').then((m) => ({ default: m.BreakPoliciesConfigPage })));
const PositionsConfigPage = lazy(() => import('@/pages/admin/PositionsConfigPage').then((m) => ({ default: m.PositionsConfigPage })));
const UnitTypesConfigPage = lazy(() => import('@/pages/admin/UnitTypesConfigPage').then((m) => ({ default: m.UnitTypesConfigPage })));
const TimesheetPage = lazy(() => import('@/pages/time/TimesheetPage').then((m) => ({ default: m.TimesheetPage })));
const TeamAttendancePage = lazy(() => import('@/pages/time/TeamAttendancePage').then((m) => ({ default: m.TeamAttendancePage })));
const SchedulePage = lazy(() => import('@/pages/time/SchedulePage').then((m) => ({ default: m.SchedulePage })));
const PayrollPage = lazy(() => import('@/pages/payroll/PayrollPage').then((m) => ({ default: m.PayrollPage })));
const LeaveRequestPage = lazy(() => import('@/pages/leave/LeaveRequestPage').then((m) => ({ default: m.LeaveRequestPage })));
const LeaveCalendarPage = lazy(() => import('@/pages/leave/LeaveCalendarPage').then((m) => ({ default: m.LeaveCalendarPage })));
const PendingApprovalsPage = lazy(() => import('@/pages/leave/PendingApprovalsPage').then((m) => ({ default: m.PendingApprovalsPage })));
const TaskListPage = lazy(() => import('@/pages/tasks/TaskListPage').then((m) => ({ default: m.TaskListPage })));
const TaskCardPage = lazy(() => import('@/pages/tasks/TaskCardPage').then((m) => ({ default: m.TaskCardPage })));
const MyTasksPage = lazy(() => import('@/pages/tasks/MyTasksPage').then((m) => ({ default: m.MyTasksPage })));
const DashboardPage = lazy(() => import('@/pages/DashboardPage').then((m) => ({ default: m.DashboardPage })));
const WorkspacePage = lazy(() => import('@/pages/WorkspacePage').then((m) => ({ default: m.WorkspacePage })));
const KioskPage = lazy(() => import('@/pages/KioskPage').then((m) => ({ default: m.KioskPage })));
const DocumentListPage = lazy(() => import('@/pages/documents/DocumentListPage').then((m) => ({ default: m.DocumentListPage })));
const DocumentCategoriesPage = lazy(() => import('@/pages/documents/DocumentCategoriesPage').then((m) => ({ default: m.DocumentCategoriesPage })));
const WorkflowBuilderPage = lazy(() => import('@/pages/workflow/WorkflowBuilderPage').then((m) => ({ default: m.WorkflowBuilderPage })));
const FormBuilderPage = lazy(() => import('@/pages/forms/FormBuilderPage').then((m) => ({ default: m.FormBuilderPage })));
const BrandingConfigPage = lazy(() => import('@/pages/admin/BrandingConfigPage').then((m) => ({ default: m.BrandingConfigPage })));
const TerminologyConfigPage = lazy(() => import('@/pages/admin/TerminologyConfigPage').then((m) => ({ default: m.TerminologyConfigPage })));
const TimeTrackingSettingsPage = lazy(() => import('@/pages/time/TimeTrackingSettingsPage').then((m) => ({ default: m.TimeTrackingSettingsPage })));
const NotificationTemplatesConfigPage = lazy(() => import('@/pages/admin/NotificationTemplatesConfigPage').then((m) => ({ default: m.NotificationTemplatesConfigPage })));
const LeavePolicyConfigPage = lazy(() => import('@/pages/admin/LeavePolicyConfigPage').then((m) => ({ default: m.LeavePolicyConfigPage })));
const EscalationRulesConfigPage = lazy(() => import('@/pages/admin/EscalationRulesConfigPage').then((m) => ({ default: m.EscalationRulesConfigPage })));
const DocumentSettingsConfigPage = lazy(() => import('@/pages/admin/DocumentSettingsConfigPage').then((m) => ({ default: m.DocumentSettingsConfigPage })));
const TaskSettingsConfigPage = lazy(() => import('@/pages/tasks/TaskSettingsConfigPage').then((m) => ({ default: m.TaskSettingsConfigPage })));

function RouteLoadingFallback() {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '48px' }}>
      <div style={{ fontSize: '14px', color: colors.gray[500] }}>Ładowanie…</div>
    </div>
  );
}

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
  // Keep in sync with PlatformConstants.OperatorTenantId — operator panel is only for our own company.
  const isOperator = (auth.user?.profile?.['tenant_id'] as string | undefined) === '00000000-0000-0000-0000-000000000001';

  // Auto-redirect kiosk accounts to /kiosk
  useEffect(() => {
    if (auth.user) {
      if (roles.includes('workbase-kiosk')) {
        navigate('/kiosk', { replace: true });
      }
    }
  }, [auth.user, navigate]);

  const location = useLocation();

  return (
    <MainLayout>
      <ErrorBoundary key={location.pathname}>
      <Suspense fallback={<RouteLoadingFallback />}>
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
        <Route path="/payroll" element={<PayrollPage />} />
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
        <Route path="/admin/branding" element={isAdmin ? <BrandingConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/terminology" element={isAdmin ? <TerminologyConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/time-tracking-settings" element={isAdmin ? <TimeTrackingSettingsPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/notification-templates" element={isAdmin ? <NotificationTemplatesConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/leave-policies" element={isAdmin ? <LeavePolicyConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/escalation-rules" element={isAdmin ? <EscalationRulesConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/document-settings" element={isAdmin ? <DocumentSettingsConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/task-settings" element={isAdmin ? <TaskSettingsConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/tenants" element={isAdmin && isOperator ? <PlatformTenantsPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/leave-types" element={isAdmin ? <LeaveTypesConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/task-statuses" element={isAdmin ? <TaskStatusConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/break-policies" element={isAdmin ? <BreakPoliciesConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/positions" element={isAdmin ? <PositionsConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="/admin/unit-types" element={isAdmin ? <UnitTypesConfigPage /> : <Navigate to="/workspace" replace />} />
        <Route path="*" element={<Navigate to="/workspace" replace />} />
      </Routes>
      </Suspense>
      </ErrorBoundary>
    </MainLayout>
  );
}

const Router = getRouterMode() === 'hash' ? HashRouter : BrowserRouter;

function AppRouter({ children }: { children: React.ReactNode }) {
  return <Router>{children}</Router>;
}

function App() {
  return (
    <ErrorBoundary>
      <AuthProvider {...oidcConfig}>
        <QueryClientProvider client={queryClient}>
          <ToastProvider>
          <AppRouter>
            <Suspense fallback={<RouteLoadingFallback />}>
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
            </Suspense>
          </AppRouter>
          </ToastProvider>
        </QueryClientProvider>
      </AuthProvider>
    </ErrorBoundary>
  );
}

export default App;
