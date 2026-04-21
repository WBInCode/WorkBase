import { useState, useEffect, type ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { FolderTree, Users, FileUp, LogOut, Menu, X, Shield, Grid3X3, CalendarDays, UsersRound, CalendarClock, Palmtree, CalendarRange, ClipboardCheck, ListTodo, ClipboardList, LayoutDashboard, Briefcase, Clock, MoreHorizontal, FileArchive, FolderOpen, Flag, CircleDot, type LucideIcon } from 'lucide-react';
import { mapUserClaims } from '@/auth';
import { ClockButton } from '@/components/TimeTracking';
import { NotificationBell } from '@/components/Notifications';

function useIsMobile(breakpoint = 768) {
  const [isMobile, setIsMobile] = useState(window.innerWidth < breakpoint);
  useEffect(() => {
    const handler = () => setIsMobile(window.innerWidth < breakpoint);
    window.addEventListener('resize', handler);
    return () => window.removeEventListener('resize', handler);
  }, [breakpoint]);
  return isMobile;
}

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
  label: string;
  icon: LucideIcon;
  exact?: boolean;
}

interface NavSection {
  title?: string;
  items: NavItem[];
}

const navSections: NavSection[] = [
  {
    items: [
      { path: '/workspace', label: 'Mój dzień', icon: Briefcase },
      { path: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
    ],
  },
  {
    title: 'Organizacja',
    items: [
      { path: '/org/tree', label: 'Struktura', icon: FolderTree },
      { path: '/org/employees', label: 'Pracownicy', icon: Users, exact: true },
      { path: '/org/employees/import', label: 'Import CSV', icon: FileUp },
    ],
  },
  {
    title: 'Czas pracy',
    items: [
      { path: '/time/timesheet', label: 'Karta czasu', icon: CalendarDays },
      { path: '/time/team-report', label: 'Raport zespołu', icon: UsersRound },
      { path: '/time/schedule', label: 'Grafik pracy', icon: CalendarClock },
    ],
  },
  {
    title: 'Urlopy',
    items: [
      { path: '/leave/request', label: 'Wnioski', icon: Palmtree },
      { path: '/leave/approvals', label: 'Akceptacje', icon: ClipboardCheck },
      { path: '/leave/calendar', label: 'Kalendarz', icon: CalendarRange },
    ],
  },
  {
    title: 'Zadania',
    items: [
      { path: '/tasks', label: 'Wszystkie', icon: ListTodo, exact: true },
      { path: '/tasks/my', label: 'Moje zadania', icon: ClipboardList },
    ],
  },
  {
    title: 'Dokumenty',
    items: [
      { path: '/documents', label: 'Pliki', icon: FileArchive, exact: true },
      { path: '/documents/categories', label: 'Kategorie', icon: FolderOpen },
    ],
  },
];

const adminNavItems = [
  { path: '/admin/roles', label: 'Role', icon: Shield },
  { path: '/admin/permissions', label: 'Matryca uprawnień', icon: Grid3X3 },
  { path: '/admin/feature-flags', label: 'Flagi funkcjonalności', icon: Flag },
  { path: '/admin/leave-types', label: 'Typy urlopów', icon: Palmtree },
  { path: '/admin/task-statuses', label: 'Statusy zadań', icon: CircleDot },
];

export function MainLayout({ children }: MainLayoutProps) {
  const auth = useAuth();
  const location = useLocation();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const isMobile = useIsMobile();
  const [sidebarOpen, setSidebarOpen] = useState(!isMobile);
  const now = useLiveClock();

  useEffect(() => {
    setSidebarOpen(!isMobile);
  }, [isMobile]);

  // Close sidebar on navigation in mobile
  useEffect(() => {
    if (isMobile) setSidebarOpen(false);
  }, [location.pathname, isMobile]);

  return (
    <div style={{ display: 'flex', height: '100vh', fontFamily: 'system-ui, -apple-system, sans-serif', background: '#0f172a' }}>
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
          background: 'linear-gradient(180deg, #0f172a 0%, #1e293b 100%)',
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
          <div style={{
            width: 32,
            height: 32,
            borderRadius: 8,
            background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: 16,
            fontWeight: 800,
            color: '#fff',
            flexShrink: 0,
          }}>
            W
          </div>
          <span style={{ fontSize: 18, fontWeight: 700, color: '#f8fafc', whiteSpace: 'nowrap', letterSpacing: '-0.02em' }}>
            WorkBase
          </span>
        </div>

        {/* Nav */}
        <nav style={{ flex: 1, overflowY: 'auto', overflowX: 'hidden', padding: '4px 10px 12px' }}>
          {navSections.map((section, si) => (
            <div key={si} style={{ marginBottom: 4 }}>
              {section.title && (
                <div style={{
                  padding: '12px 10px 6px',
                  fontSize: 10,
                  fontWeight: 700,
                  color: '#64748b',
                  textTransform: 'uppercase',
                  letterSpacing: '0.08em',
                  whiteSpace: 'nowrap',
                }}>
                  {section.title}
                </div>
              )}
              {section.items.map((item) => {
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
                      color: isActive ? '#f8fafc' : '#94a3b8',
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
                      if (!isActive) e.currentTarget.style.color = '#94a3b8';
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
                        background: 'linear-gradient(180deg, #6366f1, #8b5cf6)',
                      }} />
                    )}
                    <Icon size={17} style={{ opacity: isActive ? 1 : 0.7, flexShrink: 0 }} />
                    {item.label}
                  </Link>
                );
              })}
            </div>
          ))}

          {/* Admin section */}
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
              Administracja
            </div>
            {adminNavItems.map((item) => {
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
                    color: isActive ? '#f8fafc' : '#94a3b8',
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
                    if (!isActive) e.currentTarget.style.color = '#94a3b8';
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
                      background: 'linear-gradient(180deg, #6366f1, #8b5cf6)',
                    }} />
                  )}
                  <Icon size={17} style={{ opacity: isActive ? 1 : 0.7, flexShrink: 0 }} />
                  {item.label}
                </Link>
              );
            })}
          </div>
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
                background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: 13,
                fontWeight: 700,
                color: '#fff',
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
                color: '#94a3b8',
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
                e.currentTarget.style.color = '#94a3b8';
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
            borderBottom: '1px solid #e5e7eb',
            backgroundColor: '#ffffff',
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
              color: '#6b7280',
              padding: '4px',
              display: 'inline-flex',
            }}
            aria-label={sidebarOpen ? 'Zwiń sidebar' : 'Rozwiń sidebar'}
          >
            {sidebarOpen ? <X size={20} /> : <Menu size={20} />}
          </button>

          <div style={{ flex: 1 }} />

          {!isMobile && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: '#6b7280', fontSize: '13px', marginRight: '12px' }}>
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
        <main style={{ flex: 1, overflow: 'auto', backgroundColor: '#f8fafc', paddingBottom: isMobile ? '60px' : undefined }}>
          {children}
        </main>

        {/* Mobile bottom tabs */}
        {isMobile && (
          <nav style={{
            position: 'fixed', bottom: 0, left: 0, right: 0,
            height: '56px',
            backgroundColor: '#fff',
            borderTop: '1px solid #e5e7eb',
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
        color: isActive ? '#2563eb' : '#9ca3af',
        fontSize: '11px', fontWeight: isActive ? 600 : 400,
        padding: '4px 12px',
      }}
    >
      <Icon size={20} color={isActive ? '#2563eb' : '#9ca3af'} />
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
        color: '#9ca3af', fontSize: '11px', padding: '4px 12px',
      }}
    >
      <MoreHorizontal size={20} color="#9ca3af" />
      Więcej
    </button>
  );
}
