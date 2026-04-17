import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Search, AlertTriangle } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useTasks, useTaskStatuses, useTaskPriorities, useCreateTask } from '@/api/hooks/useTasks';
import { useEmployees } from '@/api/hooks/useOrganization';
import type { CreateTaskRequest } from '@/api/types/tasks';

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

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [showForm, setShowForm] = useState(false);

  // Form state
  const [formTitle, setFormTitle] = useState('');
  const [formDesc, setFormDesc] = useState('');
  const [formPriority, setFormPriority] = useState('');
  const [formAssignee, setFormAssignee] = useState('');
  const [formDueDate, setFormDueDate] = useState('');

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
    if (!formTitle || !formPriority || !formAssignee) return;
    const data: CreateTaskRequest = {
      title: formTitle,
      priorityId: formPriority,
      assigneeId: formAssignee,
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
        setFormDueDate('');
      },
    });
  };

  return (
    <div style={{ padding: '24px', maxWidth: '1100px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: '#111827' }}>Zadania</h1>
          <p style={{ margin: '4px 0 0', fontSize: '14px', color: '#6b7280' }}>
            Wszystkie zadania w organizacji
          </p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            padding: '8px 16px', fontSize: '14px', fontWeight: 500,
            color: '#fff', backgroundColor: '#2563eb', border: 'none',
            borderRadius: '6px', cursor: 'pointer',
          }}
        >
          <Plus size={16} /> Nowe zadanie
        </button>
      </div>

      {/* Filters */}
      <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', marginBottom: '16px' }}>
        <div style={{ position: 'relative', flex: '1 1 200px' }}>
          <Search size={16} style={{ position: 'absolute', left: '10px', top: '9px', color: '#9ca3af' }} />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Szukaj po tytule..."
            style={{
              width: '100%', padding: '8px 12px 8px 32px', fontSize: '14px',
              border: '1px solid #d1d5db', borderRadius: '6px', boxSizing: 'border-box',
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
        <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '14px', color: '#374151', cursor: 'pointer' }}>
          <input
            type="checkbox"
            checked={overdueOnly}
            onChange={(e) => setOverdueOnly(e.target.checked)}
          />
          <AlertTriangle size={14} color="#dc2626" />
          Tylko zaległe
        </label>
      </div>

      {/* Table */}
      {isLoading ? (
        <div style={{ padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '14px' }}>Ładowanie...</div>
      ) : filtered.length === 0 ? (
        <div style={{
          padding: '32px', textAlign: 'center', color: '#6b7280', fontSize: '14px',
          backgroundColor: '#f9fafb', borderRadius: '10px', border: '1px dashed #d1d5db',
        }}>
          Brak zadań spełniających kryteria.
        </div>
      ) : (
        <div style={{ backgroundColor: '#fff', borderRadius: '10px', border: '1px solid #e5e7eb', overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: '#f9fafb', borderBottom: '1px solid #e5e7eb' }}>
                <th style={thStyle}>Tytuł</th>
                <th style={{ ...thStyle, textAlign: 'center' }}>Status</th>
                <th style={{ ...thStyle, textAlign: 'center' }}>Priorytet</th>
                <th style={thStyle}>Przypisane do</th>
                <th style={thStyle}>Termin</th>
                <th style={thStyle}>Utworzono</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((t) => {
                const isOverdue = t.dueDate && new Date(t.dueDate) < now && !t.completedAt;
                return (
                  <tr
                    key={t.id}
                    onClick={() => navigate(`/tasks/${t.id}`)}
                    style={{ borderBottom: '1px solid #f3f4f6', cursor: 'pointer' }}
                    onMouseEnter={(e) => { (e.currentTarget as HTMLElement).style.backgroundColor = '#f9fafb'; }}
                    onMouseLeave={(e) => { (e.currentTarget as HTMLElement).style.backgroundColor = ''; }}
                  >
                    <td style={{ ...tdStyle, fontWeight: 500 }}>{t.title}</td>
                    <td style={{ ...tdStyle, textAlign: 'center' }}>
                      <Badge label={t.statusName} color={t.statusColor ?? '#6b7280'} />
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'center' }}>
                      <Badge label={t.priorityName} color={t.priorityColor ?? '#6b7280'} />
                    </td>
                    <td style={tdStyle}>{employeeMap.get(t.assigneeId) ?? '—'}</td>
                    <td style={tdStyle}>
                      {t.dueDate ? (
                        <span style={{ color: isOverdue ? '#dc2626' : '#374151', fontWeight: isOverdue ? 600 : 400 }}>
                          {isOverdue && <AlertTriangle size={12} style={{ marginRight: '4px', verticalAlign: 'middle' }} />}
                          {new Date(t.dueDate).toLocaleDateString('pl-PL')}
                        </span>
                      ) : '—'}
                    </td>
                    <td style={{ ...tdStyle, color: '#9ca3af' }}>
                      {new Date(t.createdAt).toLocaleDateString('pl-PL')}
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
            <h2 style={{ margin: '0 0 16px', fontSize: '18px', fontWeight: 600, color: '#111827' }}>Nowe zadanie</h2>
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
              <label style={labelStyle}>
                Termin
                <input type="date" value={formDueDate} onChange={(e) => setFormDueDate(e.target.value)} style={inputStyle} />
              </label>
            </div>
            {createMutation.error && (
              <div style={{ marginTop: '12px', padding: '8px 12px', backgroundColor: '#fef2f2', color: '#dc2626', fontSize: '13px', borderRadius: '6px' }}>
                {createMutation.error.message}
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
  color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.5px',
};
const tdStyle: React.CSSProperties = { padding: '10px 14px', color: '#374151' };
const selectStyle: React.CSSProperties = {
  padding: '8px 12px', fontSize: '14px', border: '1px solid #d1d5db',
  borderRadius: '6px', backgroundColor: '#fff', cursor: 'pointer',
};
const overlayStyle: React.CSSProperties = {
  position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)',
  display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 50,
};
const modalStyle: React.CSSProperties = {
  backgroundColor: '#fff', borderRadius: '12px', padding: '24px',
  width: '480px', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.2)',
};
const labelStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: '#374151' };
const inputStyle: React.CSSProperties = { padding: '8px 12px', fontSize: '14px', border: '1px solid #d1d5db', borderRadius: '6px' };
const cancelBtnStyle: React.CSSProperties = {
  padding: '8px 16px', fontSize: '14px', border: '1px solid #d1d5db',
  borderRadius: '6px', backgroundColor: '#fff', cursor: 'pointer',
};
const submitBtnStyle: React.CSSProperties = {
  padding: '8px 16px', fontSize: '14px', fontWeight: 500, color: '#fff',
  backgroundColor: '#2563eb', border: 'none', borderRadius: '6px', cursor: 'pointer',
};
