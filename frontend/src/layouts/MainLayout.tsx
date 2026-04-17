import { useState, type ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { FolderTree, Users, FileUp, LogOut, Menu, X, Shield, Grid3X3, CalendarDays, UsersRound, CalendarClock, Palmtree, CalendarRange, ClipboardCheck, ListTodo, ClipboardList, LayoutDashboard, Briefcase } from 'lucide-react';
import { mapUserClaims } from '@/auth';
import { ClockButton } from '@/components/TimeTracking';

interface MainLayoutProps {
  children: ReactNode;
}

const navItems = [
  { path: '/workspace', label: 'Mój dzień', icon: Briefcase },
  { path: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { path: '/org/tree', label: 'Struktura organizacyjna', icon: FolderTree },
  { path: '/org/employees', label: 'Pracownicy', icon: Users, exact: true },
  { path: '/org/employees/import', label: 'Import CSV', icon: FileUp },
  { path: '/time/timesheet', label: 'Karta czasu pracy', icon: CalendarDays },
  { path: '/time/team-report', label: 'Raport zespołu', icon: UsersRound },
  { path: '/time/schedule', label: 'Grafik pracy', icon: CalendarClock },
  { path: '/leave/request', label: 'Urlopy', icon: Palmtree },
  { path: '/leave/approvals', label: 'Akceptacje', icon: ClipboardCheck },
  { path: '/leave/calendar', label: 'Kalendarz nieobecności', icon: CalendarRange },
  { path: '/tasks', label: 'Zadania', icon: ListTodo, exact: true },
  { path: '/tasks/my', label: 'Moje zadania', icon: ClipboardList },
];

const adminNavItems = [
  { path: '/admin/roles', label: 'Role', icon: Shield },
  { path: '/admin/permissions', label: 'Matryca uprawnień', icon: Grid3X3 },
];

export function MainLayout({ children }: MainLayoutProps) {
  const auth = useAuth();
  const location = useLocation();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const [sidebarOpen, setSidebarOpen] = useState(true);

  return (
    <div style={{ display: 'flex', height: '100vh', fontFamily: 'system-ui, -apple-system, sans-serif' }}>
      {/* Sidebar */}
      <aside
        style={{
          width: sidebarOpen ? '240px' : '0px',
          overflow: 'hidden',
          backgroundColor: '#1e293b',
          color: '#e2e8f0',
          display: 'flex',
          flexDirection: 'column',
          transition: 'width 0.2s',
          flexShrink: 0,
        }}
      >
        {/* Logo */}
        <div
          style={{
            padding: '16px 20px',
            borderBottom: '1px solid #334155',
            display: 'flex',
            alignItems: 'center',
            gap: '10px',
          }}
        >
          <span style={{ fontSize: '20px', fontWeight: 700, color: '#ffffff', whiteSpace: 'nowrap' }}>
            WorkBase
          </span>
        </div>

        {/* Nav */}
        <nav style={{ flex: 1, padding: '12px 8px' }}>
          {navItems.map((item) => {
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
                  gap: '10px',
                  padding: '10px 12px',
                  borderRadius: '6px',
                  color: isActive ? '#ffffff' : '#94a3b8',
                  backgroundColor: isActive ? '#334155' : 'transparent',
                  textDecoration: 'none',
                  fontSize: '14px',
                  fontWeight: isActive ? 600 : 400,
                  whiteSpace: 'nowrap',
                  transition: 'background-color 0.15s',
                }}
              >
                <Icon size={18} />
                {item.label}
              </Link>
            );
          })}

          {/* Admin section */}
          <div style={{ margin: '16px 0 4px', padding: '0 12px', fontSize: '11px', fontWeight: 600, color: '#64748b', textTransform: 'uppercase', letterSpacing: '0.05em', whiteSpace: 'nowrap' }}>
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
                  gap: '10px',
                  padding: '10px 12px',
                  borderRadius: '6px',
                  color: isActive ? '#ffffff' : '#94a3b8',
                  backgroundColor: isActive ? '#334155' : 'transparent',
                  textDecoration: 'none',
                  fontSize: '14px',
                  fontWeight: isActive ? 600 : 400,
                  whiteSpace: 'nowrap',
                  transition: 'background-color 0.15s',
                }}
              >
                <Icon size={18} />
                {item.label}
              </Link>
            );
          })}
        </nav>

        {/* User */}
        {user && (
          <div style={{ padding: '12px 16px', borderTop: '1px solid #334155' }}>
            <div style={{ fontSize: '13px', color: '#e2e8f0', marginBottom: '4px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {user.name}
            </div>
            <div style={{ fontSize: '11px', color: '#64748b', marginBottom: '8px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {user.email}
            </div>
            <button
              onClick={() => auth.signoutRedirect()}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '6px',
                width: '100%',
                padding: '6px 8px',
                fontSize: '13px',
                color: '#94a3b8',
                backgroundColor: 'transparent',
                border: '1px solid #334155',
                borderRadius: '4px',
                cursor: 'pointer',
                whiteSpace: 'nowrap',
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
            padding: '0 16px',
            height: '48px',
            borderBottom: '1px solid #e5e7eb',
            backgroundColor: '#ffffff',
            flexShrink: 0,
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

          {user?.employeeId && (
            <ClockButton employeeId={user.employeeId} />
          )}
        </header>

        {/* Page content */}
        <main style={{ flex: 1, overflow: 'auto', backgroundColor: '#ffffff' }}>
          {children}
        </main>
      </div>
    </div>
  );
}
