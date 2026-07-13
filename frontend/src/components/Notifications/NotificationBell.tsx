import { useState, useRef, useEffect } from 'react';
import { Bell, BellOff, CheckCheck } from 'lucide-react';
import { useNotifications, useUnreadCount, useMarkNotificationRead } from '@/api/hooks/useNotifications';
import { colors } from '@/theme/tokens';

function formatRelative(dateStr: string): string {
  const diffMin = Math.floor((Date.now() - new Date(dateStr).getTime()) / 60000);
  if (diffMin < 1) return 'przed chwilą';
  if (diffMin < 60) return `${diffMin} min temu`;
  const diffH = Math.floor(diffMin / 60);
  if (diffH < 24) return `${diffH} godz. temu`;
  const diffD = Math.floor(diffH / 24);
  if (diffD === 1) return 'wczoraj';
  if (diffD < 7) return `${diffD} dni temu`;
  return new Date(dateStr).toLocaleDateString('pl-PL', { day: 'numeric', month: 'short' });
}

interface NotificationBellProps {
  userId: string;
}

export function NotificationBell({ userId }: NotificationBellProps) {
  const [open, setOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const { data: unreadCount } = useUnreadCount(userId);
  const { data: notifications } = useNotifications(userId, false);
  const markRead = useMarkNotificationRead();

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    if (open) document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [open]);

  const count = unreadCount ?? 0;

  return (
    <div ref={dropdownRef} style={{ position: 'relative' }}>
      <button
        onClick={() => setOpen(!open)}
        className="wb-icon-btn"
        style={{ position: 'relative' }}
        aria-label="Powiadomienia"
      >
        <Bell size={18} />
        {count > 0 && (
          <span
            style={{
              position: 'absolute',
              top: '1px',
              right: '1px',
              backgroundColor: colors.danger[500],
              color: colors.white,
              fontSize: '10px',
              fontWeight: 700,
              borderRadius: '9999px',
              minWidth: '16px',
              height: '16px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              padding: '0 4px',
              boxShadow: '0 0 0 2px var(--wb-panel, #fff)',
            }}
          >
            {count > 99 ? '99+' : count}
          </span>
        )}
      </button>

      {open && (
        <div
          style={{
            position: 'absolute',
            top: '100%',
            right: 0,
            marginTop: '10px',
            width: '360px',
            maxWidth: 'calc(100vw - 32px)',
            maxHeight: '400px',
            overflow: 'auto',
            backgroundColor: colors.white,
            border: `1px solid ${colors.gray[200]}`,
            borderRadius: '16px',
            boxShadow: '0 12px 32px -8px rgba(20,25,43,0.18), 0 0 0 1px rgba(20,25,43,0.03)',
            zIndex: 1000,
            animation: 'wb-fadeIn 0.18s cubic-bezier(0.22, 1, 0.36, 1)',
          }}
        >
          <div
            style={{
              padding: '12px 16px',
              borderBottom: `1px solid ${colors.gray[200]}`,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: '10px',
              position: 'sticky',
              top: 0,
              backgroundColor: colors.white,
              zIndex: 1,
            }}
          >
            <span style={{ fontWeight: 800, fontSize: '14px', color: colors.gray[900], letterSpacing: '-0.01em' }}>
              Powiadomienia
              {count > 0 && (
                <span style={{
                  marginLeft: 8, padding: '1px 9px', borderRadius: '999px', fontSize: '11.5px',
                  fontWeight: 700, backgroundColor: colors.primary[100], color: colors.primary[700],
                }}>
                  {count}
                </span>
              )}
            </span>
            {count > 0 && notifications && notifications.length > 0 && (
              <button
                onClick={() => {
                  notifications.filter((n) => !n.isRead).forEach((n) => markRead.mutate(n.id));
                }}
                style={{
                  display: 'inline-flex', alignItems: 'center', gap: 4,
                  padding: '4px 10px', fontSize: '11.5px', fontWeight: 700, fontFamily: 'inherit',
                  color: colors.primary[600], backgroundColor: 'transparent',
                  border: 'none', borderRadius: '999px', cursor: 'pointer',
                }}
                onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = colors.primary[50]; }}
                onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
              >
                <CheckCheck size={13} />
                Oznacz wszystkie
              </button>
            )}
          </div>

          {(!notifications || notifications.length === 0) ? (
            <div style={{
              padding: '32px 16px', textAlign: 'center', color: colors.gray[400], fontSize: '13px',
              display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px',
            }}>
              <span style={{
                width: 44, height: 44, borderRadius: '50%', backgroundColor: colors.gray[100],
                display: 'inline-flex', alignItems: 'center', justifyContent: 'center', color: colors.gray[400],
              }}>
                <BellOff size={20} />
              </span>
              <span style={{ fontWeight: 600 }}>Brak powiadomień</span>
              <span style={{ fontSize: '12px', color: colors.gray[400] }}>Wszystko na bieżąco — wróć później.</span>
            </div>
          ) : (
            notifications.map((n) => (
              <div
                key={n.id}
                onClick={() => {
                  if (!n.isRead) markRead.mutate(n.id);
                }}
                style={{
                  padding: '11px 16px',
                  borderBottom: `1px solid ${colors.gray[100]}`,
                  cursor: n.isRead ? 'default' : 'pointer',
                  backgroundColor: n.isRead ? colors.white : colors.primary[50],
                  transition: 'background-color 0.15s',
                }}
                onMouseEnter={(e) => { if (!n.isRead) e.currentTarget.style.backgroundColor = colors.primary[100]; }}
                onMouseLeave={(e) => { if (!n.isRead) e.currentTarget.style.backgroundColor = colors.primary[50]; }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <div style={{ fontWeight: n.isRead ? 500 : 700, fontSize: '13px', color: colors.gray[900] }}>
                    {n.title}
                  </div>
                  {!n.isRead && (
                    <span
                      style={{
                        width: '8px',
                        height: '8px',
                        borderRadius: '50%',
                        backgroundColor: colors.primary[500],
                        flexShrink: 0,
                        marginTop: '4px',
                        marginLeft: '8px',
                        boxShadow: '0 0 0 3px rgba(61,109,242,0.15)',
                      }}
                    />
                  )}
                </div>
                <div style={{ fontSize: '12px', color: colors.gray[500], marginTop: '2px', lineHeight: 1.45 }}>
                  {n.body.length > 100 ? n.body.slice(0, 100) + '…' : n.body}
                </div>
                <div style={{ fontSize: '11px', fontWeight: 600, color: colors.gray[400], marginTop: '4px' }}>
                  {formatRelative(n.createdAt)}
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
