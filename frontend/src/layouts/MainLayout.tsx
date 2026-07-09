import { useState, useEffect, type ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { useTranslation } from 'react-i18next';
import { FolderTree, Users, FileUp, LogOut, Menu, X, Shield, Grid3X3, CalendarDays, UsersRound, CalendarClock, Palmtree, CalendarRange, ClipboardCheck, ListTodo, ClipboardList, LayoutDashboard, Briefcase, Clock, MoreHorizontal, FileArchive, FolderOpen, Flag, CircleDot, Coffee, Layers, Wallet, Building2, Palette, Type, Bell, AlarmClockCheck, type LucideIcon } from 'lucide-react';
import { mapUserClaims } from '@/auth';
import { useFeatureFlags, useCurrentUser } from '@/api/hooks/useIam';
import { useBranding } from '@/api/hooks/useBranding';
import { ClockButton } from '@/components/TimeTracking';
import { NotificationBell } from '@/components/Notifications';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';
import { BRAND_CSS_VARS } from '@/theme/applyBranding';
import { useTenantThemeBootstrap } from '@/theme/useTenantThemeBootstrap';

function useLiveClock() {
  const [now, setNow] = useState(new Date());
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);
  return now;
}

interface MainLayoutProps {
  children: ReactNode;
}

interface NavItem {
  path: string;
  labelKey: string;
  icon: LucideIcon;
  exact?: boolean;
  adminOnly?: boolean;
}

interface NavSection {
  titleKey?: string;
  /** ModuleCatalog key (src/WorkBase.Shared/Modules/ModuleCatalog.cs) gating this whole section.
   * Undefined = always shown (e.g. workspace landing page, not a licensable module). */
  module?: string;
  items: NavItem[];
}

const navSections: NavSection[] = [
  {
    items: [
      { path: '/workspace', labelKey: 'nav.myDay', icon: Briefcase },
      { path: '/dashboard', labelKey: 'nav.dashboard', icon: LayoutDashboard },
    ],
  },
  {
    titleKey: 'nav.organization',
    module: 'org',
    items: [
      { path: '/org/tree', labelKey: 'nav.structure', icon: FolderTree },
      { path: '/org/employees', labelKey: 'nav.employees', icon: Users, exact: true },
      { path: '/org/employees/import', labelKey: 'nav.csvImport', icon: FileUp },
    ],
  },
  {
    titleKey: 'nav.timeTracking',
    module: 'time',
    items: [
      { path: '/time/timesheet', labelKey: 'nav.timesheet', icon: CalendarDays },
      { path: '/time/team-report', labelKey: 'nav.teamReport', icon: UsersRound },
      { path: '/time/schedule', labelKey: 'nav.schedule', icon: CalendarClock },
      { path: '/payroll', labelKey: 'nav.payroll', icon: Wallet },
    ],
  },
  {
    titleKey: 'nav.leave',
    module: 'leave',
    items: [
      { path: '/leave/request', labelKey: 'nav.requests', icon: Palmtree },
      { path: '/leave/approvals', labelKey: 'nav.approvals', icon: ClipboardCheck },
      { path: '/leave/calendar', labelKey: 'nav.calendar', icon: CalendarRange },
    ],
  },
  {
    titleKey: 'nav.tasks',
    module: 'tasks',
    items: [
      { path: '/tasks', labelKey: 'nav.allTasks', icon: ListTodo, exact: true },
      { path: '/tasks/my', labelKey: 'nav.myTasks', icon: ClipboardList },
    ],
  },
  {
    titleKey: 'nav.documents',
    module: 'documents',
    items: [
      { path: '/documents', labelKey: 'nav.files', icon: FileArchive, exact: true },
      { path: '/documents/categories', labelKey: 'nav.categories', icon: FolderOpen },
    ],
  },
];

// Tenant id of our own operator company — keep in sync with PlatformConstants.OperatorTenantId
// (src/WorkBase.Shared/Auth/PlatformConstants.cs). Only users of THIS tenant see the operator
// panel entry; the backend independently enforces the real authorization either way.
const OPERATOR_TENANT_ID = '00000000-0000-0000-0000-000000000001';

const adminNavItems = [
  { path: '/admin/roles', labelKey: 'nav.roles', icon: Shield },
  { path: '/admin/permissions', labelKey: 'nav.permissions', icon: Grid3X3 },
  { path: '/admin/feature-flags', labelKey: 'nav.featureFlags', icon: Flag },
  { path: '/admin/branding', labelKey: 'nav.branding', icon: Palette },
  { path: '/admin/terminology', labelKey: 'nav.terminology', icon: Type },
  { path: '/admin/tenants', labelKey: 'nav.tenantsOperator', icon: Building2, operatorOnly: true },
  { path: '/admin/leave-types', labelKey: 'nav.leaveTypes', icon: Palmtree },
  { path: '/admin/leave-policies', labelKey: 'nav.leavePolicies', icon: Palmtree },
  { path: '/admin/task-statuses', labelKey: 'nav.taskStatuses', icon: CircleDot },
  { path: '/admin/break-policies', labelKey: 'nav.breakPolicies', icon: Coffee },
  { path: '/admin/positions', labelKey: 'nav.positions', icon: Briefcase },
  { path: '/admin/unit-types', labelKey: 'nav.unitTypes', icon: Layers },
  { path: '/admin/time-tracking-settings', labelKey: 'nav.timeTrackingSettings', icon: CalendarClock },
  { path: '/admin/notification-templates', labelKey: 'nav.notificationTemplates', icon: Bell },
  { path: '/admin/escalation-rules', labelKey: 'nav.escalationRules', icon: AlarmClockCheck },
  { path: '/admin/document-settings', labelKey: 'nav.documentSettings', icon: FileArchive },
  { path: '/admin/task-settings', labelKey: 'nav.taskSettings', icon: ListTodo },
];

export function MainLayout({ children }: MainLayoutProps) {
  const auth = useAuth();
  const location = useLocation();
  const { t } = useTranslation();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  // isAdmin is sourced from the app's own Role/Permission data (GET /api/auth/me), NOT the
  // Keycloak "roles" claim — see docs/AUDIT-KNOWLEDGE-MAP.md (role system consistency).
  const { data: currentUser } = useCurrentUser();
  const isAdmin = !!currentUser?.isAdmin;
  const isOperator = user?.tenantId === OPERATOR_TENANT_ID;
  const visibleAdminNavItems = adminNavItems.filter((item) => !item.operatorOnly || isOperator);
  const isMobile = useIsMobile();
  const [sidebarOpen, setSidebarOpen] = useState(!isMobile);
  const now = useLiveClock();

  // Applies tenant branding (colors/font) as CSS vars and merges terminology overrides into
  // i18next — see docs/AUDIT-KNOWLEDGE-MAP.md (module/branding/terminology configuration).
  useTenantThemeBootstrap();
  const { data: branding } = useBranding();

  // Feature flags for the current tenant — hides nav sections for modules the platform
  // operator has disabled (see docs/05-module-licensing-architecture.md). Fail-open: a
  // section stays visible unless we have a definite "disabled" answer (still loading, or no
  // flag row at all = shown), matching the backend's TenantBehavior fail-open default.
  const { data: featureFlags } = useFeatureFlags();
  const isModuleEnabled = (moduleKey?: string) => {
    if (!moduleKey) return true;
    const flag = featureFlags?.find((f) => f.module === moduleKey);
    return flag?.isEnabled !== false;
  };
  const visibleNavSections = navSections
    .filter((section) => isModuleEnabled(section.module))
    .filter((section) => section.items.length > 0);

  useEffect(() => {
    setSidebarOpen(!isMobile);
  }, [isMobile]);

  // Close sidebar on navigation in mobile
  useEffect(() => {
    if (isMobile) setSidebarOpen(false);
  }, [location.pathname, isMobile]);

  return (
    <div style={{ display: 'flex', height: '100vh', fontFamily: BRAND_CSS_VARS.font, background: colors.slate[900] }}>
      {/* Mobile backdrop */}
      {isMobile && sidebarOpen && (
        <div
          onClick={() => setSidebarOpen(false)}
          style={{
            position: 'fixed',
            inset: 0,
            backgroundColor: 'rgba(0,0,0,0.4)',
            zIndex: 40,
          }}
        />
      )}

      {/* Sidebar */}
      <aside
        style={{
          width: sidebarOpen ? '250px' : '0px',
          overflow: 'hidden',
          background: `linear-gradient(180deg, ${colors.slate[900]} 0%, ${colors.slate[800]} 100%)`,
          color: '#e2e8f0',
          display: 'flex',
          flexDirection: 'column',
          transition: 'width 0.25s cubic-bezier(0.4, 0, 0.2, 1)',
          flexShrink: 0,
          ...(isMobile ? { position: 'fixed', top: 0, left: 0, bottom: 0, zIndex: 50 } : {}),
        }}
      >
        {/* Logo */}
        <div
          style={{
            padding: '20px 20px 16px',
            display: 'flex',
            alignItems: 'center',
            gap: '10px',
          }}
        >
          {branding?.logoUrl ? (
            <img
              src={branding.logoUrl}
              alt={branding.appName ?? 'Logo'}
              style={{ width: 32, height: 32, borderRadius: 8, objectFit: 'contain', flexShrink: 0 }}
            />
          ) : (
            <div style={{
              width: 32,
              height: 32,
              borderRadius: 8,
              background: `linear-gradient(135deg, ${BRAND_CSS_VARS.primary}, ${BRAND_CSS_VARS.primaryHover})`,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: 16,
              fontWeight: 800,
              color: colors.white,
              flexShrink: 0,
            }}>
              {(branding?.appName ?? 'WorkBase').charAt(0).toUpperCase()}
            </div>
          )}
          <span style={{ fontSize: 18, fontWeight: 700, color: '#f8fafc', whiteSpace: 'nowrap', letterSpacing: '-0.02em' }}>
            {branding?.appName ?? 'WorkBase'}
          </span>
        </div>

        {/* Nav */}
        <nav
          className="wb-nav-scroll"
          style={{ flex: 1, overflowY: 'auto', overflowX: 'hidden', padding: '4px 10px 12px' }}
        >
          {visibleNavSections.map((section, si) => (
            <div key={si} style={{ marginBottom: 4 }}>
              {section.titleKey && (
                <div style={{
                  padding: '12px 10px 6px',
                  fontSize: 10,
                  fontWeight: 700,
                  color: '#64748b',
                  textTransform: 'uppercase',
                  letterSpacing: '0.08em',
                  whiteSpace: 'nowrap',
                }}>
                  {t(section.titleKey)}
                </div>
              )}
              {section.items.map((item) => {
                if (item.adminOnly && !isAdmin) return null;
                const isActive = item.exact
                  ? location.pathname === item.path
                  : location.pathname.startsWith(item.path);
                const Icon = item.icon;
                return (
                  <Link
                    key={item.path}
                    to={item.path}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 10,
                      padding: '8px 10px',
                      borderRadius: 8,
                  color: isActive ? '#f8fafc' : colors.slate[400],
                      background: isActive
                        ? 'linear-gradient(90deg, rgba(99,102,241,0.2), rgba(99,102,241,0.05))'
                        : 'transparent',
                      textDecoration: 'none',
                      fontSize: 13,
                      fontWeight: isActive ? 600 : 400,
                      whiteSpace: 'nowrap',
                      transition: 'all 0.15s ease',
                      position: 'relative',
                      marginBottom: 1,
                    }}
                    onMouseEnter={(e) => {
                      if (!isActive) e.currentTarget.style.background = 'rgba(99,102,241,0.08)';
                      if (!isActive) e.currentTarget.style.color = '#cbd5e1';
                    }}
                    onMouseLeave={(e) => {
                      if (!isActive) e.currentTarget.style.background = 'transparent';
                      if (!isActive) e.currentTarget.style.color = colors.slate[400];
                    }}
                  >
                    {isActive && (
                      <div style={{
                        position: 'absolute',
                        left: 0,
                        top: '20%',
                        bottom: '20%',
                        width: 3,
                        borderRadius: 3,
                        background: `linear-gradient(180deg, ${BRAND_CSS_VARS.primary}, ${BRAND_CSS_VARS.primaryHover})`,
                      }} />
                    )}
                    <Icon size={17} style={{ opacity: isActive ? 1 : 0.7, flexShrink: 0 }} />
                    {t(item.labelKey)}
                  </Link>
                );
              })}
            </div>
          ))}

          {/* Admin section */}
          {isAdmin && (
          <div style={{
            marginTop: 8,
            paddingTop: 8,
            borderTop: '1px solid rgba(100,116,139,0.2)',
          }}>
            <div style={{
              padding: '4px 10px 6px',
              fontSize: 10,
              fontWeight: 700,
              color: '#64748b',
              textTransform: 'uppercase',
              letterSpacing: '0.08em',
              whiteSpace: 'nowrap',
            }}>
              {t('nav.admin')}
            </div>
            {visibleAdminNavItems.map((item) => {
              const isActive = location.pathname.startsWith(item.path);
              const Icon = item.icon;
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 10,
                    padding: '8px 10px',
                    borderRadius: 8,
                  color: isActive ? '#f8fafc' : colors.slate[400],
                    background: isActive
                      ? 'linear-gradient(90deg, rgba(99,102,241,0.2), rgba(99,102,241,0.05))'
                      : 'transparent',
                    textDecoration: 'none',
                    fontSize: 13,
                    fontWeight: isActive ? 600 : 400,
                    whiteSpace: 'nowrap',
                    transition: 'all 0.15s ease',
                    position: 'relative',
                    marginBottom: 1,
                  }}
                  onMouseEnter={(e) => {
                    if (!isActive) e.currentTarget.style.background = 'rgba(99,102,241,0.08)';
                    if (!isActive) e.currentTarget.style.color = '#cbd5e1';
                  }}
                  onMouseLeave={(e) => {
                    if (!isActive) e.currentTarget.style.background = 'transparent';
                    if (!isActive) e.currentTarget.style.color = colors.slate[400];
                  }}
                >
                  {isActive && (
                    <div style={{
                      position: 'absolute',
                      left: 0,
                      top: '20%',
                      bottom: '20%',
                      width: 3,
                      borderRadius: 3,
                      background: `linear-gradient(180deg, ${BRAND_CSS_VARS.primary}, ${BRAND_CSS_VARS.primaryHover})`,
                    }} />
                  )}
                  <Icon size={17} style={{ opacity: isActive ? 1 : 0.7, flexShrink: 0 }} />
                  {t(item.labelKey)}
                </Link>
              );
            })}
          </div>
          )}
        </nav>

        {/* User */}
        {user && (
          <div style={{
            padding: '14px 16px',
            borderTop: '1px solid rgba(100,116,139,0.2)',
            background: 'rgba(15,23,42,0.5)',
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10 }}>
              <div style={{
                width: 34,
                height: 34,
                borderRadius: '50%',
                background: `linear-gradient(135deg, ${BRAND_CSS_VARS.primary}, ${BRAND_CSS_VARS.primaryHover})`,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: 13,
                fontWeight: 700,
                color: colors.white,
                flexShrink: 0,
              }}>
                {(user.name ?? 'U').charAt(0).toUpperCase()}
              </div>
              <div style={{ minWidth: 0 }}>
                <div style={{ fontSize: 13, fontWeight: 600, color: '#f1f5f9', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {user.name}
                </div>
                <div style={{ fontSize: 11, color: '#64748b', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {user.email}
                </div>
              </div>
            </div>
            <button
              onClick={() => auth.signoutRedirect()}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: 6,
                width: '100%',
                padding: '7px 8px',
                fontSize: 12,
                fontWeight: 500,
                color: colors.slate[400],
                backgroundColor: 'rgba(51,65,85,0.4)',
                border: '1px solid rgba(100,116,139,0.25)',
                borderRadius: 6,
                cursor: 'pointer',
                whiteSpace: 'nowrap',
                transition: 'all 0.15s ease',
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.backgroundColor = 'rgba(51,65,85,0.7)';
                e.currentTarget.style.color = '#cbd5e1';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor = 'rgba(51,65,85,0.4)';
                e.currentTarget.style.color = colors.slate[400];
              }}
            >
              <LogOut size={14} />
              Wyloguj
            </button>
          </div>
        )}
      </aside>

      {/* Main content */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        {/* Topbar */}
        <header
          style={{
            display: 'flex',
            alignItems: 'center',
            padding: '0 20px',
            height: '52px',
            borderBottom: `1px solid ${colors.gray[200]}`,
            backgroundColor: colors.white,
            flexShrink: 0,
            boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
          }}
        >
          <button
            onClick={() => setSidebarOpen(!sidebarOpen)}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: colors.gray[500],
              padding: '4px',
              display: 'inline-flex',
            }}
            aria-label={sidebarOpen ? 'Zwiń sidebar' : 'Rozwiń sidebar'}
          >
            {sidebarOpen ? <X size={20} /> : <Menu size={20} />}
          </button>

          <div style={{ flex: 1 }} />

          {!isMobile && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: colors.gray[500], fontSize: '13px', marginRight: '12px' }}>
              <Clock size={14} />
              <span style={{ fontVariantNumeric: 'tabular-nums' }}>
                {now.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
              </span>
            </div>
          )}

          {user?.sub && (
            <NotificationBell userId={user.sub} />
          )}

          {user?.employeeId && (
            <ClockButton employeeId={user.employeeId} />
          )}
        </header>

        {/* Page content */}
        <main style={{ flex: 1, overflow: 'auto', backgroundColor: colors.gray[50], paddingBottom: isMobile ? '60px' : undefined }}>
          {children}
        </main>

        {/* Mobile bottom tabs */}
        {isMobile && (
          <nav style={{
            position: 'fixed', bottom: 0, left: 0, right: 0,
            height: '56px',
            backgroundColor: colors.white,
            borderTop: `1px solid ${colors.gray[200]}`,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-around',
            zIndex: 50,
          }}>
            <BottomTab icon={Briefcase} label="Mój dzień" path="/workspace" currentPath={location.pathname} />
            <BottomTab icon={ListTodo} label="Zadania" path="/tasks" currentPath={location.pathname} />
            <BottomTab icon={Palmtree} label="Wnioski" path="/leave/request" currentPath={location.pathname} />
            <BottomTabMore onClick={() => setSidebarOpen(true)} />
          </nav>
        )}
      </div>
    </div>
  );
}

function BottomTab({ icon: Icon, label, path, currentPath }: {
  icon: React.ComponentType<{ size?: number; color?: string }>;
  label: string;
  path: string;
  currentPath: string;
}) {
  const isActive = currentPath.startsWith(path);
  return (
    <Link
      to={path}
      style={{
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '2px',
        textDecoration: 'none',
        color: isActive ? colors.primary[600] : colors.gray[400],
        fontSize: '11px', fontWeight: isActive ? 600 : 400,
        padding: '4px 12px',
      }}
    >
      <Icon size={20} color={isActive ? colors.primary[600] : colors.gray[400]} />
      {label}
    </Link>
  );
}

function BottomTabMore({ onClick }: { onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      style={{
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '2px',
        background: 'none', border: 'none', cursor: 'pointer',
        color: colors.gray[400], fontSize: '11px', padding: '4px 12px',
      }}
    >
      <MoreHorizontal size={20} color={colors.gray[400]} />
      Więcej
    </button>
  );
}
