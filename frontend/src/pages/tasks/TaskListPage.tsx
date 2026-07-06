import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Search, AlertTriangle, Trash2 } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useTasks, useTaskStatuses, useTaskPriorities, useCreateTask, useDeleteTask } from '@/api/hooks/useTasks';
import { useEmployees } from '@/api/hooks/useOrganization';
import type { CreateTaskRequest } from '@/api/types/tasks';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function TaskListPage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const navigate = useNavigate();

  const { data: tasks = [], isLoading } = useTasks();
  const { data: statuses = [] } = useTaskStatuses();
  const { data: priorities = [] } = useTaskPriorities();
  const { data: employeesPage } = useEmployees({ page: 1, pageSize: 500 });
  const employees = employeesPage?.items ?? [];

  const createMutation = useCreateTask();
  const deleteMutation = useDeleteTask();
  const mobile = useIsMobile();

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(new Set());

  // Form state
  const [formTitle, setFormTitle] = useState('');
  const [formDesc, setFormDesc] = useState('');
  const [formPriority, setFormPriority] = useState('');
  const [formAssignee, setFormAssignee] = useState('');
  const [formAdditionalAssignees, setFormAdditionalAssignees] = useState<string[]>([]);
  const [formDueDate, setFormDueDate] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const now = new Date();

  const filtered = useMemo(() => {
    let result = [...tasks];
    if (search) {
      const q = search.toLowerCase();
      result = result.filter((t) => t.title.toLowerCase().includes(q));
    }
    if (statusFilter) {
      result = result.filter((t) => t.statusId === statusFilter);
    }
    if (priorityFilter) {
      result = result.filter((t) => t.priorityId === priorityFilter);
    }
    if (overdueOnly) {
      result = result.filter(
        (t) => t.dueDate && new Date(t.dueDate) < now && !t.completedAt,
      );
    }
    return result;
  }, [tasks, search, statusFilter, priorityFilter, overdueOnly]);

  const employeeMap = useMemo(() => {
    const map = new Map<string, string>();
    for (const e of employees) {
      map.set(e.id, `${e.firstName} ${e.lastName}`);
    }
    return map;
  }, [employees]);

  const handleCreate = () => {
    const missing: string[] = [];
    if (!formTitle.trim()) missing.push('Tytuł');
    if (!formPriority) missing.push('Priorytet');
    if (!formAssignee) missing.push('Przypisz do');
    if (missing.length > 0) {
      setFormError(`Uzupełnij wymagane pola: ${missing.join(', ')}.`);
      return;
    }
    setFormError(null);
    const additionalIds = formAdditionalAssignees.filter((id) => id && id !== formAssignee);
    const data: CreateTaskRequest = {
      title: formTitle,
      priorityId: formPriority,
      assigneeId: formAssignee,
      additionalAssigneeIds: additionalIds.length > 0 ? additionalIds : undefined,
      description: formDesc || undefined,
      dueDate: formDueDate || undefined,
      reporterId: user?.employeeId ?? undefined,
    };
    createMutation.mutate(data, {
      onSuccess: () => {
        setShowForm(false);
        setFormTitle('');
        setFormDesc('');
        setFormPriority('');
        setFormAssignee('');
        setFormAdditionalAssignees([]);
        setFormDueDate('');
        setFormError(null);
      },
    });
  };

  const toggleSelect = (id: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  };

  const toggleAll = () => {
    if (selected.size === filtered.length) {
      setSelected(new Set());
    } else {
      setSelected(new Set(filtered.map((t) => t.id)));
    }
  };

  const handleDeleteSingle = (id: string) => {
    if (!confirm('Czy na pewno chcesz usunąć to zadanie?')) return;
    deleteMutation.mutate(id, { onSuccess: () => setSelected((p) => { const n = new Set(p); n.delete(id); return n; }) });
  };

  const handleDeleteSelected = async () => {
    if (selected.size === 0) return;
    if (!confirm(`Czy na pewno chcesz usunąć ${selected.size} zadań?`)) return;
    const ids = [...selected];
    for (const id of ids) {
      await deleteMutation.mutateAsync(id);
    }
    setSelected(new Set());
  };

  return (
    <div style={{ padding: mobile ? '16px' : '24px', maxWidth: '1100px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: colors.gray[900] }}>Zadania</h1>
          <p style={{ margin: '4px 0 0', fontSize: '14px', color: colors.gray[500] }}>
            Wszystkie zadania w organizacji
          </p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            padding: '8px 16px', fontSize: '14px', fontWeight: 500,
            color: colors.white, backgroundColor: colors.primary[600], border: 'none',
            borderRadius: '6px', cursor: 'pointer',
          }}
        >
          <Plus size={16} /> Nowe zadanie
        </button>
      </div>

      {/* Bulk action bar */}
      {selected.size > 0 && (
        <div style={{
          display: 'flex', alignItems: 'center', gap: '12px', padding: '10px 16px',
          marginBottom: '12px', backgroundColor: colors.primary[50], borderRadius: '8px',
          border: `1px solid ${colors.primary[200]}`, fontSize: '14px', color: colors.primary[800],
        }}>
          <span style={{ fontWeight: 500 }}>Zaznaczono: {selected.size}</span>
          <button
            onClick={handleDeleteSelected}
            disabled={deleteMutation.isPending}
            style={{
              display: 'inline-flex', alignItems: 'center', gap: '4px',
              padding: '6px 12px', fontSize: '13px', fontWeight: 500,
              color: colors.white, backgroundColor: colors.danger[600], border: 'none',
              borderRadius: '6px', cursor: 'pointer',
            }}
          >
            <Trash2 size={14} /> {deleteMutation.isPending ? 'Usuwanie...' : 'Usuń zaznaczone'}
          </button>
          <button
            onClick={() => setSelected(new Set())}
            style={{
              padding: '6px 12px', fontSize: '13px', color: colors.primary[800],
              backgroundColor: 'transparent', border: `1px solid ${colors.primary[300]}`,
              borderRadius: '6px', cursor: 'pointer',
            }}
          >
            Odznacz wszystko
          </button>
        </div>
      )}

      {/* Filters */}
      <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', marginBottom: '16px' }}>
        <div style={{ position: 'relative', flex: '1 1 200px' }}>
          <Search size={16} style={{ position: 'absolute', left: '10px', top: '9px', color: colors.gray[400] }} />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Szukaj po tytule..."
            style={{
              width: '100%', padding: '8px 12px 8px 32px', fontSize: '14px',
              border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', boxSizing: 'border-box',
            }}
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          style={selectStyle}
        >
          <option value="">Wszystkie statusy</option>
          {statuses.map((s) => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>
        <select
          value={priorityFilter}
          onChange={(e) => setPriorityFilter(e.target.value)}
          style={selectStyle}
        >
          <option value="">Wszystkie priorytety</option>
          {priorities.map((p) => (
            <option key={p.id} value={p.id}>{p.name}</option>
          ))}
        </select>
        <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '14px', color: colors.gray[700], cursor: 'pointer' }}>
          <input
            type="checkbox"
            checked={overdueOnly}
            onChange={(e) => setOverdueOnly(e.target.checked)}
          />
          <AlertTriangle size={14} color={colors.danger[600]} />
          Tylko zaległe
        </label>
      </div>

      {/* Table */}
      {isLoading ? (
        <div style={{ padding: '20px', textAlign: 'center', color: colors.gray[400], fontSize: '14px' }}>Ładowanie...</div>
      ) : filtered.length === 0 ? (
        <div style={{
          padding: '32px', textAlign: 'center', color: colors.gray[500], fontSize: '14px',
          backgroundColor: colors.gray[50], borderRadius: '10px', border: `1px dashed ${colors.gray[300]}`,
        }}>
          Brak zadań spełniających kryteria.
        </div>
      ) : (
        <div style={{ backgroundColor: colors.white, borderRadius: '10px', border: `1px solid ${colors.gray[200]}`, overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50], borderBottom: `1px solid ${colors.gray[200]}` }}>
                <th style={{ ...thStyle, width: '36px', textAlign: 'center' }}>
                  <input type="checkbox" checked={filtered.length > 0 && selected.size === filtered.length} onChange={toggleAll} />
                </th>
                <th style={thStyle}>Tytuł</th>
                <th style={{ ...thStyle, textAlign: 'center' }}>Status</th>
                <th style={{ ...thStyle, textAlign: 'center' }}>Priorytet</th>
                <th style={thStyle}>Przypisane do</th>
                <th style={thStyle}>Termin</th>
                <th style={thStyle}>Utworzono</th>
                <th style={{ ...thStyle, width: '48px', textAlign: 'center' }}>Akcje</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((t) => {
                const isOverdue = t.dueDate && new Date(t.dueDate) < now && !t.completedAt;
                return (
                  <tr
                    key={t.id}
                    style={{ borderBottom: `1px solid ${colors.gray[100]}`, cursor: 'pointer', backgroundColor: selected.has(t.id) ? colors.primary[50] : '' }}
                    onMouseEnter={(e) => { if (!selected.has(t.id)) (e.currentTarget as HTMLElement).style.backgroundColor = colors.gray[50]; }}
                    onMouseLeave={(e) => { (e.currentTarget as HTMLElement).style.backgroundColor = selected.has(t.id) ? colors.primary[50] : ''; }}
                  >
                    <td style={{ ...tdStyle, textAlign: 'center' }} onClick={(e) => e.stopPropagation()}>
                      <input type="checkbox" checked={selected.has(t.id)} onChange={() => toggleSelect(t.id)} />
                    </td>
                    <td style={{ ...tdStyle, fontWeight: 500 }} onClick={() => navigate(`/tasks/${t.id}`)}>{t.title}</td>
                    <td style={{ ...tdStyle, textAlign: 'center' }} onClick={() => navigate(`/tasks/${t.id}`)}>
                      <Badge label={t.statusName} color={t.statusColor ?? colors.gray[500]} />
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'center' }} onClick={() => navigate(`/tasks/${t.id}`)}>
                      <Badge label={t.priorityName} color={t.priorityColor ?? colors.gray[500]} />
                    </td>
                    <td style={tdStyle} onClick={() => navigate(`/tasks/${t.id}`)}>
                      {employeeMap.get(t.assigneeId) ?? '—'}
                      {t.additionalAssigneeIds && t.additionalAssigneeIds.length > 0 && (
                        <span style={{ marginLeft: '6px', padding: '2px 6px', fontSize: '11px', backgroundColor: '#eef2ff', color: '#4338ca', borderRadius: '999px' }}>
                          + {t.additionalAssigneeIds.length}
                        </span>
                      )}
                    </td>
                    <td style={tdStyle} onClick={() => navigate(`/tasks/${t.id}`)}>
                      {t.dueDate ? (
                        <span style={{ color: isOverdue ? colors.danger[600] : colors.gray[700], fontWeight: isOverdue ? 600 : 400 }}>
                          {isOverdue && <AlertTriangle size={12} style={{ marginRight: '4px', verticalAlign: 'middle' }} />}
                          {new Date(t.dueDate).toLocaleString('pl-PL', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
                        </span>
                      ) : '—'}
                    </td>
                    <td style={{ ...tdStyle, color: colors.gray[400] }} onClick={() => navigate(`/tasks/${t.id}`)}>
                      {new Date(t.createdAt).toLocaleDateString('pl-PL')}
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'center' }} onClick={(e) => e.stopPropagation()}>
                      <button
                        onClick={() => handleDeleteSingle(t.id)}
                        title="Usuń zadanie"
                        style={{
                          padding: '4px', background: 'none', border: 'none',
                          cursor: 'pointer', color: colors.gray[400], borderRadius: '4px',
                        }}
                        onMouseEnter={(e) => { (e.currentTarget as HTMLElement).style.color = colors.danger[600]; }}
                        onMouseLeave={(e) => { (e.currentTarget as HTMLElement).style.color = colors.gray[400]; }}
                      >
                        <Trash2 size={16} />
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Create form modal */}
      {showForm && (
        <div style={overlayStyle} onClick={() => setShowForm(false)}>
          <div style={modalStyle} onClick={(e) => e.stopPropagation()}>
            <h2 style={{ margin: '0 0 16px', fontSize: '18px', fontWeight: 600, color: colors.gray[900] }}>Nowe zadanie</h2>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <label style={labelStyle}>
                Tytuł *
                <input value={formTitle} onChange={(e) => setFormTitle(e.target.value)} style={inputStyle} />
              </label>
              <label style={labelStyle}>
                Opis
                <textarea value={formDesc} onChange={(e) => setFormDesc(e.target.value)} rows={3} style={{ ...inputStyle, resize: 'vertical' }} />
              </label>
              <label style={labelStyle}>
                Priorytet *
                <select value={formPriority} onChange={(e) => setFormPriority(e.target.value)} style={inputStyle}>
                  <option value="">— wybierz —</option>
                  {priorities.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
                </select>
              </label>
              <label style={labelStyle}>
                Przypisz do *
                <select value={formAssignee} onChange={(e) => setFormAssignee(e.target.value)} style={inputStyle}>
                  <option value="">— wybierz —</option>
                  {employees.map((e) => <option key={e.id} value={e.id}>{e.firstName} {e.lastName}</option>)}
                </select>
              </label>
              <div style={labelStyle}>
                <span>Dodatkowe osoby (opcjonalnie)</span>
                {formAdditionalAssignees.map((aid, idx) => {
                  const used = new Set([formAssignee, ...formAdditionalAssignees.filter((_, i) => i !== idx)]);
                  return (
                    <div key={idx} style={{ display: 'flex', gap: '6px', marginTop: '6px' }}>
                      <select
                        value={aid}
                        onChange={(e) => {
                          const next = [...formAdditionalAssignees];
                          next[idx] = e.target.value;
                          setFormAdditionalAssignees(next);
                        }}
                        style={{ ...inputStyle, flex: 1 }}
                      >
                        <option value="">— wybierz —</option>
                        {employees
                          .filter((emp) => !used.has(emp.id) || emp.id === aid)
                          .map((emp) => <option key={emp.id} value={emp.id}>{emp.firstName} {emp.lastName}</option>)}
                      </select>
                      <button
                        type="button"
                        onClick={() => setFormAdditionalAssignees(formAdditionalAssignees.filter((_, i) => i !== idx))}
                        style={{ padding: '8px 10px', fontSize: '13px', color: colors.danger[600], backgroundColor: colors.white, border: `1px solid ${colors.danger[200]}`, borderRadius: '6px', cursor: 'pointer' }}
                      >
                        Usuń
                      </button>
                    </div>
                  );
                })}
                <button
                  type="button"
                  onClick={() => setFormAdditionalAssignees([...formAdditionalAssignees, ''])}
                  disabled={!formAssignee}
                  style={{
                    marginTop: '8px', alignSelf: 'flex-start',
                    padding: '6px 12px', fontSize: '13px', fontWeight: 500,
                    color: formAssignee ? colors.primary[600] : colors.gray[400],
                    backgroundColor: colors.primary[50], border: `1px solid ${colors.primary[200]}`,
                    borderRadius: '6px', cursor: formAssignee ? 'pointer' : 'not-allowed',
                  }}
                >
                  + Dodaj kolejną osobę
                </button>
              </div>
              <label style={labelStyle}>
                Termin
                <input type="datetime-local" value={formDueDate} onChange={(e) => setFormDueDate(e.target.value)} style={inputStyle} />
              </label>
            </div>
            {(formError || createMutation.error) && (
              <div style={{ marginTop: '12px', padding: '8px 12px', backgroundColor: colors.danger[50], color: colors.danger[600], fontSize: '13px', borderRadius: '6px' }}>
                {formError ?? createMutation.error?.message}
              </div>
            )}
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '20px' }}>
              <button onClick={() => setShowForm(false)} style={cancelBtnStyle}>Anuluj</button>
              <button onClick={handleCreate} disabled={createMutation.isPending} style={submitBtnStyle}>
                {createMutation.isPending ? 'Tworzenie...' : 'Utwórz'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Badge({ label, color }: { label: string; color: string }) {
  return (
    <span style={{
      display: 'inline-block', padding: '2px 10px', borderRadius: '4px',
      fontSize: '12px', fontWeight: 500, backgroundColor: color + '20', color,
    }}>
      {label}
    </span>
  );
}

const thStyle: React.CSSProperties = {
  padding: '10px 14px', textAlign: 'left', fontSize: '12px', fontWeight: 600,
  color: colors.gray[500], textTransform: 'uppercase', letterSpacing: '0.5px',
};
const tdStyle: React.CSSProperties = { padding: '10px 14px', color: colors.gray[700] };
const selectStyle: React.CSSProperties = {
  padding: '8px 12px', fontSize: '14px', border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px', backgroundColor: colors.white, cursor: 'pointer',
};
const overlayStyle: React.CSSProperties = {
  position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)',
  display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 50,
};
const modalStyle: React.CSSProperties = {
  backgroundColor: colors.white, borderRadius: '12px', padding: '24px',
  width: '100%', maxWidth: '480px', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.2)',
};
const labelStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: colors.gray[700] };
const inputStyle: React.CSSProperties = { padding: '8px 12px', fontSize: '14px', border: `1px solid ${colors.gray[300]}`, borderRadius: '6px' };
const cancelBtnStyle: React.CSSProperties = {
  padding: '8px 16px', fontSize: '14px', border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px', backgroundColor: colors.white, cursor: 'pointer',
};
const submitBtnStyle: React.CSSProperties = {
  padding: '8px 16px', fontSize: '14px', fontWeight: 500, color: colors.white,
  backgroundColor: colors.primary[600], border: 'none', borderRadius: '6px', cursor: 'pointer',
};
