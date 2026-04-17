import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertTriangle } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useTasks } from '@/api/hooks/useTasks';

export function MyTasksPage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const navigate = useNavigate();

  const { data: tasks = [], isLoading } = useTasks(user?.employeeId ?? null);

  const now = new Date();

  const { open, completed } = useMemo(() => {
    const o: typeof tasks = [];
    const c: typeof tasks = [];
    for (const t of tasks) {
      if (t.completedAt) c.push(t);
      else o.push(t);
    }
    return { open: o, completed: c };
  }, [tasks]);

  return (
    <div style={{ padding: '24px', maxWidth: '960px' }}>
      <div style={{ marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: '#111827' }}>Moje zadania</h1>
        <p style={{ margin: '4px 0 0', fontSize: '14px', color: '#6b7280' }}>
          Zadania przypisane do Ciebie
        </p>
      </div>

      {isLoading ? (
        <div style={{ padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '14px' }}>Ładowanie...</div>
      ) : open.length === 0 && completed.length === 0 ? (
        <div style={{
          padding: '32px', textAlign: 'center', color: '#6b7280', fontSize: '14px',
          backgroundColor: '#f9fafb', borderRadius: '10px', border: '1px dashed #d1d5db',
        }}>
          Brak przypisanych zadań.
        </div>
      ) : (
        <>
          {/* Open tasks */}
          {open.length > 0 && (
            <div style={{ marginBottom: '24px' }}>
              <h2 style={{ fontSize: '15px', fontWeight: 600, color: '#374151', marginBottom: '10px' }}>
                Otwarte ({open.length})
              </h2>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {open.map((t) => {
                  const isOverdue = t.dueDate && new Date(t.dueDate) < now;
                  return (
                    <div
                      key={t.id}
                      onClick={() => navigate(`/tasks/${t.id}`)}
                      style={{
                        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                        padding: '12px 16px', backgroundColor: '#fff', borderRadius: '8px',
                        border: isOverdue ? '1px solid #fca5a5' : '1px solid #e5e7eb',
                        cursor: 'pointer', transition: 'box-shadow 0.15s',
                      }}
                      onMouseEnter={(e) => { (e.currentTarget as HTMLElement).style.boxShadow = '0 2px 8px rgba(0,0,0,0.08)'; }}
                      onMouseLeave={(e) => { (e.currentTarget as HTMLElement).style.boxShadow = ''; }}
                    >
                      <div>
                        <div style={{ fontSize: '14px', fontWeight: 500, color: '#111827' }}>{t.title}</div>
                        <div style={{ fontSize: '12px', color: '#9ca3af', marginTop: '2px' }}>
                          <Badge label={t.statusName} color={t.statusColor ?? '#6b7280'} />
                          <span style={{ marginLeft: '8px' }}>
                            <Badge label={t.priorityName} color={t.priorityColor ?? '#6b7280'} />
                          </span>
                        </div>
                      </div>
                      <div style={{ textAlign: 'right', flexShrink: 0 }}>
                        {t.dueDate ? (
                          <span style={{
                            fontSize: '13px',
                            color: isOverdue ? '#dc2626' : '#6b7280',
                            fontWeight: isOverdue ? 600 : 400,
                          }}>
                            {isOverdue && <AlertTriangle size={12} style={{ marginRight: '4px', verticalAlign: 'middle' }} />}
                            {new Date(t.dueDate).toLocaleDateString('pl-PL')}
                          </span>
                        ) : (
                          <span style={{ fontSize: '13px', color: '#d1d5db' }}>brak terminu</span>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Completed tasks */}
          {completed.length > 0 && (
            <div>
              <h2 style={{ fontSize: '15px', fontWeight: 600, color: '#374151', marginBottom: '10px' }}>
                Ukończone ({completed.length})
              </h2>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {completed.map((t) => (
                  <div
                    key={t.id}
                    onClick={() => navigate(`/tasks/${t.id}`)}
                    style={{
                      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                      padding: '12px 16px', backgroundColor: '#f9fafb', borderRadius: '8px',
                      border: '1px solid #e5e7eb', cursor: 'pointer', opacity: 0.7,
                    }}
                  >
                    <div>
                      <div style={{ fontSize: '14px', color: '#6b7280', textDecoration: 'line-through' }}>{t.title}</div>
                    </div>
                    <div style={{ fontSize: '12px', color: '#9ca3af' }}>
                      {t.completedAt && new Date(t.completedAt).toLocaleDateString('pl-PL')}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function Badge({ label, color }: { label: string; color: string }) {
  return (
    <span style={{
      display: 'inline-block', padding: '2px 8px', borderRadius: '4px',
      fontSize: '11px', fontWeight: 500, backgroundColor: color + '20', color,
    }}>
      {label}
    </span>
  );
}
