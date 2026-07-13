import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertTriangle } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useTasks } from '@/api/hooks/useTasks';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function MyTasksPage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const navigate = useNavigate();

  const { data: tasks = [], isLoading } = useTasks(user?.employeeId ?? null);
  const mobile = useIsMobile();

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
    <div style={{ padding: mobile ? '14px' : '24px 28px', maxWidth: '1100px', margin: '0 auto' }}>
      {/* ── Karta dowodzenia ── */}
      <div style={{
        marginBottom: '18px',
        backgroundColor: colors.white,
        border: `1px solid ${colors.gray[200]}`,
        borderRadius: '20px',
        boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
        padding: mobile ? '16px' : '18px 22px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: '12px',
        flexWrap: 'wrap',
      }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>Moje zadania</h1>
          <p style={{ margin: '3px 0 0', fontSize: '13px', color: colors.gray[500] }}>
            Zadania przypisane do Ciebie
          </p>
        </div>
        {!isLoading && (
          <div style={{ display: 'flex', gap: '8px' }}>
            <span style={{ padding: '6px 14px', borderRadius: '999px', fontSize: '12.5px', fontWeight: 700, backgroundColor: colors.primary[100], color: colors.primary[700] }}>
              {open.length} otwartych
            </span>
            <span style={{ padding: '6px 14px', borderRadius: '999px', fontSize: '12.5px', fontWeight: 700, backgroundColor: colors.success[100], color: colors.success[800] }}>
              {completed.length} ukończonych
            </span>
          </div>
        )}
      </div>

      {isLoading ? (
        <div style={{ padding: '20px', textAlign: 'center', color: colors.gray[400], fontSize: '14px' }}>Ładowanie...</div>
      ) : open.length === 0 && completed.length === 0 ? (
        <div style={{
          padding: '32px', textAlign: 'center', color: colors.gray[500], fontSize: '14px',
          backgroundColor: colors.gray[50], borderRadius: '10px', border: `1px dashed ${colors.gray[300]}`,
        }}>
          Brak przypisanych zadań.
        </div>
      ) : (
        <>
          {/* Open tasks */}
          {open.length > 0 && (
            <div style={{ marginBottom: '24px' }}>
              <h2 style={{ fontSize: '15px', fontWeight: 600, color: colors.gray[700], marginBottom: '10px' }}>
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
                        padding: '12px 16px', backgroundColor: colors.white, borderRadius: '12px',
                        border: isOverdue ? '1px solid #fca5a5' : `1px solid ${colors.gray[200]}`,
                        cursor: 'pointer', transition: 'box-shadow 0.15s',
                      }}
                      onMouseEnter={(e) => { (e.currentTarget as HTMLElement).style.boxShadow = '0 2px 8px rgba(0,0,0,0.08)'; }}
                      onMouseLeave={(e) => { (e.currentTarget as HTMLElement).style.boxShadow = ''; }}
                    >
                      <div>
                        <div style={{ fontSize: '14px', fontWeight: 500, color: colors.gray[900] }}>{t.title}</div>
                        <div style={{ fontSize: '12px', color: colors.gray[400], marginTop: '2px' }}>
                          <Badge label={t.statusName} color={t.statusColor ?? colors.gray[500]} />
                          <span style={{ marginLeft: '8px' }}>
                            <Badge label={t.priorityName} color={t.priorityColor ?? colors.gray[500]} />
                          </span>
                        </div>
                      </div>
                      <div style={{ textAlign: 'right', flexShrink: 0 }}>
                        {t.dueDate ? (
                          <span style={{
                            fontSize: '13px',
                            color: isOverdue ? colors.danger[600] : colors.gray[500],
                            fontWeight: isOverdue ? 600 : 400,
                          }}>
                            {isOverdue && <AlertTriangle size={12} style={{ marginRight: '4px', verticalAlign: 'middle' }} />}
                            {new Date(t.dueDate).toLocaleString('pl-PL', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
                          </span>
                        ) : (
                          <span style={{ fontSize: '13px', color: colors.gray[300] }}>brak terminu</span>
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
              <h2 style={{ fontSize: '15px', fontWeight: 600, color: colors.gray[700], marginBottom: '10px' }}>
                Ukończone ({completed.length})
              </h2>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {completed.map((t) => (
                  <div
                    key={t.id}
                    onClick={() => navigate(`/tasks/${t.id}`)}
                    style={{
                      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                      padding: '12px 16px', backgroundColor: colors.gray[50], borderRadius: '12px',
                      border: `1px solid ${colors.gray[200]}`, cursor: 'pointer', opacity: 0.7,
                    }}
                  >
                    <div>
                      <div style={{ fontSize: '14px', color: colors.gray[500], textDecoration: 'line-through' }}>{t.title}</div>
                    </div>
                    <div style={{ fontSize: '12px', color: colors.gray[400] }}>
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
      display: 'inline-block', padding: '2px 10px', borderRadius: '999px',
      fontSize: '11px', fontWeight: 500, backgroundColor: color + '20', color,
    }}>
      {label}
    </span>
  );
}
