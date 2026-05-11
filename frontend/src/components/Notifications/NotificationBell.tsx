import { useState, useRef, useEffect } from 'react';
import { Bell } from 'lucide-react';
import { useNotifications, useUnreadCount, useMarkNotificationRead } from '@/api/hooks/useNotifications';

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
    <div ref={dropdownRef} style={{ position: 'relative', marginRight: '12px' }}>
      <button
        onClick={() => setOpen(!open)}
        style={{
          position: 'relative',
          background: 'none',
          border: 'none',
          cursor: 'pointer',
          color: '#6b7280',
          padding: '4px',
          display: 'inline-flex',
        }}
        aria-label="Powiadomienia"
      >
        <Bell size={20} />
        {count > 0 && (
          <span
            style={{
              position: 'absolute',
              top: '-2px',
              right: '-4px',
              backgroundColor: '#ef4444',
              color: '#fff',
              fontSize: '10px',
              fontWeight: 700,
              borderRadius: '9999px',
              minWidth: '16px',
              height: '16px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              padding: '0 4px',
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
            marginTop: '8px',
            width: '360px',
            maxWidth: 'calc(100vw - 32px)',
            maxHeight: '400px',
            overflow: 'auto',
            backgroundColor: '#fff',
            border: '1px solid #e5e7eb',
            borderRadius: '8px',
            boxShadow: '0 10px 25px rgba(0,0,0,0.1)',
            zIndex: 1000,
          }}
        >
          <div
            style={{
              padding: '12px 16px',
              borderBottom: '1px solid #e5e7eb',
              fontWeight: 600,
              fontSize: '14px',
              color: '#1f2937',
            }}
          >
            Powiadomienia {count > 0 && `(${count} nieprzeczytanych)`}
          </div>

          {(!notifications || notifications.length === 0) ? (
            <div style={{ padding: '24px 16px', textAlign: 'center', color: '#9ca3af', fontSize: '13px' }}>
              Brak powiadomień
            </div>
          ) : (
            notifications.map((n) => (
              <div
                key={n.id}
                onClick={() => {
                  if (!n.isRead) markRead.mutate(n.id);
                }}
                style={{
                  padding: '10px 16px',
                  borderBottom: '1px solid #f3f4f6',
                  cursor: n.isRead ? 'default' : 'pointer',
                  backgroundColor: n.isRead ? '#fff' : '#eff6ff',
                  transition: 'background-color 0.15s',
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <div style={{ fontWeight: n.isRead ? 400 : 600, fontSize: '13px', color: '#1f2937' }}>
                    {n.title}
                  </div>
                  {!n.isRead && (
                    <span
                      style={{
                        width: '8px',
                        height: '8px',
                        borderRadius: '50%',
                        backgroundColor: '#3b82f6',
                        flexShrink: 0,
                        marginTop: '4px',
                        marginLeft: '8px',
                      }}
                    />
                  )}
                </div>
                <div style={{ fontSize: '12px', color: '#6b7280', marginTop: '2px' }}>
                  {n.body.length > 100 ? n.body.slice(0, 100) + '…' : n.body}
                </div>
                <div style={{ fontSize: '11px', color: '#9ca3af', marginTop: '4px' }}>
                  {new Date(n.createdAt).toLocaleString('pl-PL')}
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
