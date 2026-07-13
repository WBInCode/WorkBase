import { useState, useEffect, useMemo, useCallback, type ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { useTranslation } from 'react-i18next';
import { FolderTree, Users, FileUp, LogOut, Menu, X, Shield, Grid3X3, CalendarDays, UsersRound, CalendarClock, Palmtree, CalendarRange, ClipboardCheck, ListTodo, ClipboardList, LayoutDashboard, Briefcase, Clock, MoreHorizontal, FileArchive, FolderOpen, Flag, CircleDot, Coffee, Layers, Wallet, Building2, Palette, Type, Bell, AlarmClockCheck, ChevronDown, Sun, Moon, type LucideIcon } from 'lucide-react';
import { mapUserClaims } from '@/auth';
import { useFeatureFlags, useCurrentUser } from '@/api/hooks/useIam';
import { useBranding } from '@/api/hooks/useBranding';
import { ClockButton } from '@/components/TimeTracking';
import { NotificationBell } from '@/components/Notifications';
import { useIsMobile, appStorage } from '@/shared';
import { BRAND_CSS_VARS } from '@/theme/applyBranding';
import { useTenantThemeBootstrap } from '@/theme/useTenantThemeBootstrap';
import { useTheme } from '@/theme/themeMode';

// ─── Live clock (isolated!) ──────────────────────────────────
// Poprzednio useLiveClock() siedział w MainLayout i re-renderował CAŁE drzewo
// aplikacji co sekundę. Teraz tyka tylko ten mały chip.
function LiveClockChip() {
  const [now, setNow] = useState(new Date());
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);
  return (
    <div className="wb-clock-chip">
      <Clock size={13} />
      <span>{now.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}</span>
    </div>
  );
}

// Przełącznik jasny/ciemny — w topbarze
function ThemeToggle() {
  const { theme, toggle } = useTheme();
  return (
    <button
      className="wb-icon-btn"
      onClick={toggle}
      aria-label={theme === 'dark' ? 'Przełącz na jasny motyw' : 'Przełącz na ciemny motyw'}
      title={theme === 'dark' ? 'Jasny motyw' : 'Ciemny motyw'}
    >
      {theme === 'dark' ? <Sun size={17} /> : <Moon size={17} />}
    </button>
  );
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
  operatorOnly?: boolean;
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

const adminNavItems: NavItem[] = [
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

// ─── Collapsible section state (persisted) ──────────────────

const COLLAPSE_STORE_KEY = 'wb.nav.collapsed';

function readCollapsed(): Record<string, boolean> {
  try {
    return JSON.parse(appStorage.local.get(COLLAPSE_STORE_KEY) ?? '{}') as Record<string, boolean>;
  } catch {
    return {};
  }
}

// ─── Nav item ────────────────────────────────────────────────

function NavLinkItem({ item, isActive, label }: { item: NavItem; isActive: boolean; label: string }) {
  const Icon = item.icon;
  return (
    <Link to={item.path} className={`wb-nav-item${isActive ? ' is-active' : ''}`}>
      <span className="wb-nav-ico">
        <Icon size={15} />
      </span>
      {label}
    </Link>
  );
}

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

  // Zwijane sekcje nawigacji — stan trzymany lokalnie, sekcja z aktywną trasą
  // zawsze otwarta. Sekcja admina (17 pozycji) domyślnie zwinięta.
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>(() => ({
    'nav.admin': !location.pathname.startsWith('/admin'),
    ...readCollapsed(),
  }));

  const toggleSection = useCallback((key: string) => {
    setCollapsed((prev) => {
      const next = { ...prev, [key]: !prev[key] };
      try {
        appStorage.local.set(COLLAPSE_STORE_KEY, JSON.stringify(next));
      } catch { /* private mode etc. */ }
      return next;
    });
  }, []);

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

  // Tytuł bieżącej strony do topbara (z aktywnej pozycji nawigacji)
  const pageTitle = useMemo(() => {
    const all: NavItem[] = [...navSections.flatMap((s) => s.items), ...adminNavItems];
    const match = all
      .filter((i) => (i.exact ? location.pathname === i.path : location.pathname.startsWith(i.path)))
      .sort((a, b) => b.path.length - a.path.length)[0];
    return match ? t(match.labelKey) : '';
  }, [location.pathname, t]);

  const isSectionActive = (section: NavSection) =>
    section.items.some((i) => (i.exact ? location.pathname === i.path : location.pathname.startsWith(i.path)));
  const isAdminActive = location.pathname.startsWith('/admin');

  const appName = branding?.appName ?? 'WorkBase';

  return (
    <div className="wb-shell" style={{ fontFamily: BRAND_CSS_VARS.font }}>
      {/* Mobile backdrop */}
      {isMobile && sidebarOpen && (
        <div className="wb-backdrop" onClick={() => setSidebarOpen(false)} />
      )}

      {/* Sidebar — pływająca karta */}
      <aside
        className="wb-rail"
        style={{
          width: sidebarOpen ? 248 : 0,
          margin: sidebarOpen ? '12px 0 12px 12px' : '12px 0',
          opacity: sidebarOpen ? 1 : 0,
          border: sidebarOpen ? undefined : 'none',
          ...(isMobile
            ? { position: 'fixed', top: 0, left: 0, bottom: 0, zIndex: 50, margin: sidebarOpen ? 10 : 0 }
            : {}),
        }}
      >
        {/* Logo */}
        <div className="wb-rail-head">
          {branding?.logoUrl ? (
            <img
              src={branding.logoUrl}
              alt={appName}
              style={{ width: 34, height: 34, borderRadius: 10, objectFit: 'contain', flexShrink: 0 }}
            />
          ) : (
            <div
              style={{
                width: 34,
                height: 34,
                borderRadius: 10,
                background: `linear-gradient(135deg, ${BRAND_CSS_VARS.primary}, ${BRAND_CSS_VARS.primaryHover})`,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: 16,
                fontWeight: 800,
                color: '#fff',
                flexShrink: 0,
                boxShadow: '0 6px 14px -4px rgba(61,109,242,0.5)',
              }}
            >
              {appName.charAt(0).toUpperCase()}
            </div>
          )}
          <div style={{ minWidth: 0 }}>
            <div style={{ fontSize: 15.5, fontWeight: 800, color: 'var(--wb-ink)', whiteSpace: 'nowrap', letterSpacing: '-0.02em' }}>
              {appName}
            </div>
            <div style={{ fontSize: 10, fontWeight: 600, color: '#9aa3bc', letterSpacing: '0.06em', textTransform: 'uppercase', whiteSpace: 'nowrap' }}>
              Platforma HR
            </div>
          </div>
        </div>

        {/* Nav */}
        <nav className="wb-nav-scroll" style={{ flex: 1, overflowY: 'auto', overflowX: 'hidden', padding: '2px 10px 12px' }}>
          {visibleNavSections.map((section, si) => {
            const key = section.titleKey ?? `sec-${si}`;
            const isClosed = !!section.titleKey && !!collapsed[key] && !isSectionActive(section);
            return (
              <div key={key} style={{ marginBottom: 2 }}>
                {section.titleKey && (
                  <button
                    type="button"
                    className={`wb-nav-sec${isClosed ? ' is-closed' : ''}`}
                    onClick={() => toggleSection(key)}
                    aria-expanded={!isClosed}
                  >
                    {t(section.titleKey)}
                    <ChevronDown size={12} className="wb-sec-chev" />
                  </button>
                )}
                {!isClosed && (
                  <div className="wb-nav-group">
                    {section.items.map((item) => {
                      if (item.adminOnly && !isAdmin) return null;
                      const isActive = item.exact
                        ? location.pathname === item.path
                        : location.pathname.startsWith(item.path);
                      return <NavLinkItem key={item.path} item={item} isActive={isActive} label={t(item.labelKey)} />;
                    })}
                  </div>
                )}
              </div>
            );
          })}

          {/* Admin section */}
          {isAdmin && (
            <div style={{ marginTop: 6, paddingTop: 6, borderTop: '1px solid var(--wb-line)' }}>
              <button
                type="button"
                className={`wb-nav-sec${collapsed['nav.admin'] && !isAdminActive ? ' is-closed' : ''}`}
                onClick={() => toggleSection('nav.admin')}
                aria-expanded={!(collapsed['nav.admin'] && !isAdminActive)}
              >
                {t('nav.admin')}
                <ChevronDown size={12} className="wb-sec-chev" />
              </button>
              {!(collapsed['nav.admin'] && !isAdminActive) && (
                <div className="wb-nav-group">
                  {visibleAdminNavItems.map((item) => (
                    <NavLinkItem
                      key={item.path}
                      item={item}
                      isActive={location.pathname.startsWith(item.path)}
                      label={t(item.labelKey)}
                    />
                  ))}
                </div>
              )}
            </div>
          )}
        </nav>

        {/* User */}
        {user && (
          <div className="wb-rail-foot">
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10 }}>
              <div
                style={{
                  width: 34,
                  height: 34,
                  borderRadius: '50%',
                  background: `linear-gradient(135deg, ${BRAND_CSS_VARS.primary}, ${BRAND_CSS_VARS.primaryHover})`,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 13,
                  fontWeight: 700,
                  color: '#fff',
                  flexShrink: 0,
                  boxShadow: '0 4px 10px -3px rgba(61,109,242,0.45)',
                }}
              >
                {(user.name ?? 'U').charAt(0).toUpperCase()}
              </div>
              <div style={{ minWidth: 0 }}>
                <div style={{ fontSize: 13, fontWeight: 700, color: 'var(--wb-ink)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {user.name}
                </div>
                <div style={{ fontSize: 11, color: '#9aa3bc', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
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
                padding: '8px 10px',
                fontSize: 12,
                fontWeight: 600,
                fontFamily: 'inherit',
                color: 'var(--wb-ink-2)',
                backgroundColor: 'var(--wb-panel)',
                border: '1px solid var(--wb-line)',
                borderRadius: 999,
                cursor: 'pointer',
                whiteSpace: 'nowrap',
                transition: 'all 0.15s ease',
                boxShadow: '0 1px 2px rgba(20,25,43,0.05)',
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.borderColor = 'var(--wb-dan-400, #f1c2c2)';
                e.currentTarget.style.color = 'var(--wb-dan-500, #c0392b)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.borderColor = 'var(--wb-line)';
                e.currentTarget.style.color = 'var(--wb-ink-2)';
              }}
            >
              <LogOut size={13} />
              Wyloguj
            </button>
          </div>
        )}
      </aside>

      {/* Main content */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', minWidth: 0 }}>
        {/* Topbar — szklana pigułka */}
        <header className="wb-topbar">
          <button
            className="wb-icon-btn"
            onClick={() => setSidebarOpen(!sidebarOpen)}
            aria-label={sidebarOpen ? 'Zwiń sidebar' : 'Rozwiń sidebar'}
          >
            {sidebarOpen && isMobile ? <X size={18} /> : <Menu size={18} />}
          </button>

          {pageTitle && <div className="wb-topbar-title">{pageTitle}</div>}

          <div style={{ flex: 1 }} />

          {!isMobile && <LiveClockChip />}

          <ThemeToggle />

          {user?.sub && <NotificationBell userId={user.sub} />}

          {/* Na "/workspace" akcje RCP są w hero — unikamy duplikacji w topbarze */}
          {user?.employeeId && location.pathname !== '/workspace' && <ClockButton employeeId={user.employeeId} />}
        </header>

        {/* Page content */}
        <main
          key={location.pathname}
          className="wb-page-enter"
          style={{ flex: 1, overflow: 'auto', paddingBottom: isMobile ? 84 : undefined }}
        >
          {children}
        </main>

        {/* Mobile bottom tabs — pływający dock */}
        {isMobile && (
          <nav className="wb-tabbar">
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
  icon: LucideIcon;
  label: string;
  path: string;
  currentPath: string;
}) {
  const isActive = currentPath.startsWith(path);
  return (
    <Link to={path} className={`wb-tab${isActive ? ' is-active' : ''}`}>
      <Icon size={19} />
      {label}
    </Link>
  );
}

function BottomTabMore({ onClick }: { onClick: () => void }) {
  return (
    <button onClick={onClick} className="wb-tab">
      <MoreHorizontal size={19} />
      Więcej
    </button>
  );
}
