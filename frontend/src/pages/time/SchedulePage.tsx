import { useState, useMemo, useCallback } from 'react';
import {
  ChevronLeft, ChevronRight, Plus, Pencil, Trash2, Calendar, X,
} from 'lucide-react';
import {
  useTeamSchedules, useCreateSchedule, useUpdateSchedule, useDeleteSchedule,
  useScheduleTemplates,
} from '@/api/hooks/useTimeTracking';
import { useEmployees, useOrgUnitTree } from '@/api/hooks/useOrganization';
import type { ScheduleDto, ScheduleTemplateDto } from '@/api/types/time';
import type { EmployeeDto, OrganizationUnitTreeNode } from '@/api/types/organization';

/* ── helpers ── */

function toDateString(d: Date): string {
  return d.toISOString().split('T')[0];
}

function getWeekRange(date: Date) {
  const d = new Date(date);
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  const mon = new Date(d);
  mon.setDate(d.getDate() + diff);
  const sun = new Date(mon);
  sun.setDate(mon.getDate() + 6);
  return { from: toDateString(mon), to: toDateString(sun) };
}

function getMonthRange(date: Date) {
  const from = new Date(date.getFullYear(), date.getMonth(), 1);
  const to = new Date(date.getFullYear(), date.getMonth() + 1, 0);
  return { from: toDateString(from), to: toDateString(to) };
}

function getDatesInRange(from: string, to: string): string[] {
  const dates: string[] = [];
  const current = new Date(from + 'T00:00:00');
  const end = new Date(to + 'T00:00:00');
  while (current <= end) {
    dates.push(toDateString(current));
    current.setDate(current.getDate() + 1);
  }
  return dates;
}

function formatWeekdayShort(dateStr: string): string {
  const d = new Date(dateStr + 'T00:00:00');
  const wd = d.toLocaleDateString('pl-PL', { weekday: 'short' });
  return `${wd} ${d.getDate()}`;
}

function formatDateShort(dateStr: string): string {
  const d = new Date(dateStr + 'T00:00:00');
  return d.toLocaleDateString('pl-PL', { weekday: 'short', day: 'numeric', month: 'short' });
}

function formatTime(timeStr: string): string {
  // TimeOnly serialized as "HH:mm:ss" or "HH:mm"
  return timeStr.slice(0, 5);
}

function flattenUnits(nodes: OrganizationUnitTreeNode[]): { id: string; name: string; depth: number }[] {
  const result: { id: string; name: string; depth: number }[] = [];
  function walk(list: OrganizationUnitTreeNode[], depth: number) {
    for (const n of list) {
      result.push({ id: n.id, name: n.name, depth });
      if (n.children?.length) walk(n.children, depth + 1);
    }
  }
  walk(nodes, 0);
  return result;
}

const SHIFT_COLORS: Record<string, { bg: string; border: string; text: string }> = {
  dzienna: { bg: '#dbeafe', border: '#93c5fd', text: '#1e40af' },
  nocna: { bg: '#ede9fe', border: '#c4b5fd', text: '#5b21b6' },
  popołudniowa: { bg: '#fef3c7', border: '#fcd34d', text: '#92400e' },
};

function getShiftStyle(shiftType: string | null) {
  if (!shiftType) return { bg: '#f0fdf4', border: '#86efac', text: '#166534' };
  return SHIFT_COLORS[shiftType.toLowerCase()] ?? { bg: '#f3f4f6', border: '#d1d5db', text: '#374151' };
}

/* ── main component ── */

export function SchedulePage() {
  const [viewMode, setViewMode] = useState<'week' | 'month'>('week');
  const [currentDate, setCurrentDate] = useState(() => new Date());
  const [unitId, setUnitId] = useState('');
  const [modal, setModal] = useState<ModalState | null>(null);

  const dateRange = useMemo(() => {
    return viewMode === 'week' ? getWeekRange(currentDate) : getMonthRange(currentDate);
  }, [viewMode, currentDate]);

  const dates = useMemo(() => getDatesInRange(dateRange.from, dateRange.to), [dateRange]);

  const periodLabel = useMemo(() => {
    if (viewMode === 'week') {
      return `${formatDateShort(dateRange.from)} – ${formatDateShort(dateRange.to)}`;
    }
    const d = new Date(dateRange.from + 'T00:00:00');
    return d.toLocaleDateString('pl-PL', { month: 'long', year: 'numeric' });
  }, [viewMode, dateRange]);

  const navigate = (dir: number) => {
    const d = new Date(currentDate);
    if (viewMode === 'week') d.setDate(d.getDate() + dir * 7);
    else d.setMonth(d.getMonth() + dir);
    setCurrentDate(d);
  };

  /* data */
  const { data: tree } = useOrgUnitTree();
  const flatUnits = useMemo(() => (tree ? flattenUnits(tree) : []), [tree]);

  const { data: employeePage } = useEmployees({
    organizationUnitId: unitId || undefined,
    page: 1,
    pageSize: 200,
    status: 'Active',
  });
  const employees: EmployeeDto[] = employeePage?.items ?? [];
  const employeeIds = useMemo(() => employees.map((e) => e.id), [employees]);

  const { data: schedules, isLoading } = useTeamSchedules(employeeIds, dateRange.from, dateRange.to);
  const { data: templates } = useScheduleTemplates();

  /* index schedules by employeeId:date */
  const scheduleMap = useMemo(() => {
    const m = new Map<string, ScheduleDto>();
    if (!schedules) return m;
    for (const s of schedules) {
      m.set(`${s.employeeId}:${s.date}`, s);
    }
    return m;
  }, [schedules]);

  /* modal handlers */
  const openAdd = (employeeId: string, date: string) => {
    setModal({ mode: 'add', employeeId, date, plannedStart: '08:00', plannedEnd: '16:00', shiftType: '', templateId: '' });
  };

  const openEdit = (schedule: ScheduleDto) => {
    setModal({
      mode: 'edit',
      scheduleId: schedule.id,
      employeeId: schedule.employeeId,
      date: schedule.date,
      plannedStart: formatTime(schedule.plannedStart),
      plannedEnd: formatTime(schedule.plannedEnd),
      shiftType: schedule.shiftType ?? '',
      templateId: schedule.templateId ?? '',
    });
  };

  return (
    <div style={{ padding: '24px 32px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ fontSize: '22px', fontWeight: 700, color: '#111827', margin: 0, display: 'flex', alignItems: 'center', gap: '8px' }}>
          <Calendar size={22} />
          Grafik pracy
        </h1>
      </div>

      {/* Controls */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '20px', flexWrap: 'wrap' }}>
        {/* View mode */}
        <div style={{ display: 'flex', borderRadius: '8px', border: '1px solid #e5e7eb', overflow: 'hidden' }}>
          {(['week', 'month'] as const).map((m) => (
            <button
              key={m}
              onClick={() => setViewMode(m)}
              style={{
                padding: '6px 16px', fontSize: '13px', fontWeight: viewMode === m ? 600 : 400,
                color: viewMode === m ? '#ffffff' : '#374151',
                backgroundColor: viewMode === m ? '#3b82f6' : '#ffffff',
                border: 'none', cursor: 'pointer',
                borderRight: m === 'week' ? '1px solid #e5e7eb' : undefined,
              }}
            >
              {{ week: 'Tydzień', month: 'Miesiąc' }[m]}
            </button>
          ))}
        </div>

        {/* Nav */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <NavBtn onClick={() => navigate(-1)}><ChevronLeft size={18} /></NavBtn>
          <button
            onClick={() => setCurrentDate(new Date())}
            style={{
              padding: '6px 12px', fontSize: '12px', fontWeight: 500,
              color: '#3b82f6', backgroundColor: '#eff6ff',
              border: '1px solid #bfdbfe', borderRadius: '6px', cursor: 'pointer',
            }}
          >
            Dziś
          </button>
          <NavBtn onClick={() => navigate(1)}><ChevronRight size={18} /></NavBtn>
        </div>

        <span style={{ fontSize: '15px', fontWeight: 600, color: '#374151', textTransform: 'capitalize' }}>
          {periodLabel}
        </span>

        {/* Unit filter */}
        <select
          value={unitId}
          onChange={(e) => setUnitId(e.target.value)}
          style={{
            marginLeft: 'auto', padding: '6px 10px', fontSize: '13px',
            border: '1px solid #e5e7eb', borderRadius: '6px', color: '#374151',
            backgroundColor: '#ffffff', minWidth: '200px',
          }}
        >
          <option value="">Wszystkie jednostki</option>
          {flatUnits.map((u) => (
            <option key={u.id} value={u.id}>{'  '.repeat(u.depth)}{u.name}</option>
          ))}
        </select>
      </div>

      {/* Legend */}
      <div style={{ display: 'flex', gap: '16px', marginBottom: '16px', fontSize: '12px', color: '#6b7280', flexWrap: 'wrap' }}>
        {Object.entries(SHIFT_COLORS).map(([type, c]) => (
          <span key={type} style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <span style={{ display: 'inline-block', width: 12, height: 12, borderRadius: 3, backgroundColor: c.bg, border: `1px solid ${c.border}` }} />
            {type.charAt(0).toUpperCase() + type.slice(1)}
          </span>
        ))}
        <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ display: 'inline-block', width: 12, height: 12, borderRadius: 3, backgroundColor: '#f0fdf4', border: '1px solid #86efac' }} />
          Inna / brak typu
        </span>
      </div>

      {/* Loading */}
      {isLoading && (
        <div style={{ padding: '40px', textAlign: 'center', color: '#9ca3af' }}>Ładowanie grafiku...</div>
      )}

      {/* Empty */}
      {!isLoading && employees.length === 0 && (
        <div style={{ padding: '40px', textAlign: 'center', color: '#9ca3af', border: '1px solid #e5e7eb', borderRadius: '10px' }}>
          Brak pracowników w wybranej jednostce.
        </div>
      )}

      {/* Grid */}
      {!isLoading && employees.length > 0 && (
        <div style={{ overflowX: 'auto', border: '1px solid #e5e7eb', borderRadius: '10px' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px', minWidth: dates.length * 90 + 200 }}>
            <thead>
              <tr style={{ backgroundColor: '#f9fafb' }}>
                <th style={{ ...thStyle, position: 'sticky', left: 0, backgroundColor: '#f9fafb', zIndex: 1, minWidth: '180px' }}>
                  Pracownik
                </th>
                {dates.map((d) => {
                  const day = new Date(d + 'T00:00:00');
                  const isWeekend = day.getDay() === 0 || day.getDay() === 6;
                  return (
                    <th
                      key={d}
                      style={{
                        ...thStyle, textAlign: 'center', minWidth: '80px',
                        backgroundColor: isWeekend ? '#f1f5f9' : '#f9fafb', fontSize: '11px',
                      }}
                    >
                      {formatWeekdayShort(d)}
                    </th>
                  );
                })}
              </tr>
            </thead>
            <tbody>
              {employees.map((emp) => (
                <tr key={emp.id} style={{ borderTop: '1px solid #e5e7eb' }}>
                  <td style={{
                    ...tdStyle, fontWeight: 500, position: 'sticky', left: 0,
                    backgroundColor: '#ffffff', zIndex: 1, whiteSpace: 'nowrap',
                  }}>
                    {emp.lastName} {emp.firstName}
                  </td>
                  {dates.map((d) => {
                    const sched = scheduleMap.get(`${emp.id}:${d}`);
                    const day = new Date(d + 'T00:00:00');
                    const isWeekend = day.getDay() === 0 || day.getDay() === 6;

                    if (sched) {
                      const style = getShiftStyle(sched.shiftType);
                      return (
                        <td key={d} style={{ ...tdStyle, textAlign: 'center', padding: '4px', backgroundColor: isWeekend ? '#f8fafc' : undefined }}>
                          <div
                            onClick={() => openEdit(sched)}
                            style={{
                              backgroundColor: style.bg, border: `1px solid ${style.border}`,
                              borderRadius: '6px', padding: '4px 6px', cursor: 'pointer',
                              fontSize: '12px', lineHeight: '1.3',
                            }}
                            title={`${formatTime(sched.plannedStart)}–${formatTime(sched.plannedEnd)}${sched.shiftType ? ` (${sched.shiftType})` : ''}\nKliknij aby edytować`}
                          >
                            <div style={{ fontWeight: 600, color: style.text }}>
                              {formatTime(sched.plannedStart)}–{formatTime(sched.plannedEnd)}
                            </div>
                            {sched.shiftType && (
                              <div style={{ fontSize: '10px', color: style.text, opacity: 0.8 }}>
                                {sched.shiftType}
                              </div>
                            )}
                          </div>
                        </td>
                      );
                    }

                    return (
                      <td key={d} style={{ ...tdStyle, textAlign: 'center', padding: '4px', backgroundColor: isWeekend ? '#f8fafc' : undefined }}>
                        <button
                          onClick={() => openAdd(emp.id, d)}
                          style={{
                            width: '100%', height: '36px', border: '1px dashed #d1d5db',
                            borderRadius: '6px', backgroundColor: 'transparent', cursor: 'pointer',
                            display: 'flex', alignItems: 'center', justifyContent: 'center',
                            color: '#9ca3af', fontSize: '14px',
                          }}
                          title="Dodaj zmianę"
                        >
                          <Plus size={14} />
                        </button>
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal */}
      {modal && (
        <ScheduleModal
          state={modal}
          templates={templates ?? []}
          employeeName={getEmployeeName(employees, modal.employeeId)}
          onChange={setModal}
          onClose={() => setModal(null)}
        />
      )}
    </div>
  );
}

/* ── types ── */

interface ModalState {
  mode: 'add' | 'edit';
  scheduleId?: string;
  employeeId: string;
  date: string;
  plannedStart: string;
  plannedEnd: string;
  shiftType: string;
  templateId: string;
}

function getEmployeeName(employees: EmployeeDto[], id: string): string {
  const emp = employees.find((e) => e.id === id);
  return emp ? `${emp.firstName} ${emp.lastName}` : '';
}

/* ── modal ── */

function ScheduleModal({
  state,
  templates,
  employeeName,
  onChange,
  onClose,
}: {
  state: ModalState;
  templates: ScheduleTemplateDto[];
  employeeName: string;
  onChange: (s: ModalState) => void;
  onClose: () => void;
}) {
  const createMut = useCreateSchedule();
  const updateMut = useUpdateSchedule();
  const deleteMut = useDeleteSchedule();

  const [error, setError] = useState('');
  const saving = createMut.isPending || updateMut.isPending;

  const handleSave = useCallback(async () => {
    setError('');
    try {
      if (state.mode === 'add') {
        await createMut.mutateAsync({
          employeeId: state.employeeId,
          date: state.date,
          plannedStart: state.plannedStart + ':00',
          plannedEnd: state.plannedEnd + ':00',
          shiftType: state.shiftType || undefined,
          templateId: state.templateId || undefined,
        });
      } else {
        await updateMut.mutateAsync({
          id: state.scheduleId!,
          plannedStart: state.plannedStart + ':00',
          plannedEnd: state.plannedEnd + ':00',
          shiftType: state.shiftType || undefined,
        });
      }
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Wystąpił błąd');
    }
  }, [state, createMut, updateMut, onClose]);

  const handleDelete = useCallback(async () => {
    if (!state.scheduleId) return;
    setError('');
    try {
      await deleteMut.mutateAsync(state.scheduleId);
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Wystąpił błąd');
    }
  }, [state.scheduleId, deleteMut, onClose]);

  const applyTemplate = (tpl: ScheduleTemplateDto) => {
    // definition format: "HH:mm-HH:mm" e.g. "06:00-14:00"
    const match = tpl.definition.match(/^(\d{2}:\d{2})-(\d{2}:\d{2})$/);
    if (match) {
      onChange({ ...state, plannedStart: match[1], plannedEnd: match[2], shiftType: tpl.name, templateId: tpl.id });
    }
  };

  const dateLabel = new Date(state.date + 'T00:00:00').toLocaleDateString('pl-PL', {
    weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
  });

  return (
    <div style={{
      position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)', zIndex: 1000,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
    }} onClick={onClose}>
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          backgroundColor: '#ffffff', borderRadius: '12px', padding: '24px',
          width: '420px', maxWidth: '95vw', boxShadow: '0 20px 60px rgba(0,0,0,0.2)',
        }}
      >
        {/* Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <h2 style={{ fontSize: '18px', fontWeight: 700, color: '#111827', margin: 0 }}>
            {state.mode === 'add' ? 'Dodaj zmianę' : 'Edytuj zmianę'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#6b7280', padding: '4px' }}>
            <X size={20} />
          </button>
        </div>

        {/* Info */}
        <div style={{ marginBottom: '16px', fontSize: '14px', color: '#374151' }}>
          <div><strong>Pracownik:</strong> {employeeName}</div>
          <div style={{ textTransform: 'capitalize' }}><strong>Data:</strong> {dateLabel}</div>
        </div>

        {/* Templates */}
        {templates.length > 0 && (
          <div style={{ marginBottom: '16px' }}>
            <label style={labelStyle}>Szablon</label>
            <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap' }}>
              {templates.filter((t) => t.isActive).map((tpl) => (
                <button
                  key={tpl.id}
                  onClick={() => applyTemplate(tpl)}
                  style={{
                    padding: '4px 10px', fontSize: '12px', fontWeight: 500,
                    border: '1px solid #d1d5db', borderRadius: '6px',
                    backgroundColor: state.templateId === tpl.id ? '#eff6ff' : '#ffffff',
                    color: state.templateId === tpl.id ? '#3b82f6' : '#374151',
                    cursor: 'pointer',
                  }}
                  title={tpl.description ?? tpl.definition}
                >
                  {tpl.name}
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Time inputs */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '16px' }}>
          <div>
            <label style={labelStyle}>Początek</label>
            <input
              type="time"
              value={state.plannedStart}
              onChange={(e) => onChange({ ...state, plannedStart: e.target.value })}
              style={inputStyle}
            />
          </div>
          <div>
            <label style={labelStyle}>Koniec</label>
            <input
              type="time"
              value={state.plannedEnd}
              onChange={(e) => onChange({ ...state, plannedEnd: e.target.value })}
              style={inputStyle}
            />
          </div>
        </div>

        {/* Shift type */}
        <div style={{ marginBottom: '20px' }}>
          <label style={labelStyle}>Typ zmiany (opcjonalnie)</label>
          <input
            type="text"
            value={state.shiftType}
            onChange={(e) => onChange({ ...state, shiftType: e.target.value })}
            placeholder="np. dzienna, nocna, popołudniowa"
            style={inputStyle}
          />
        </div>

        {/* Error */}
        {error && (
          <div style={{ marginBottom: '12px', padding: '8px 12px', backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: '6px', color: '#dc2626', fontSize: '13px' }}>
            {error}
          </div>
        )}

        {/* Actions */}
        <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
          {state.mode === 'edit' && (
            <button
              onClick={handleDelete}
              disabled={deleteMut.isPending}
              style={{
                display: 'flex', alignItems: 'center', gap: '4px',
                padding: '8px 14px', fontSize: '13px', fontWeight: 600,
                color: '#dc2626', backgroundColor: '#fef2f2',
                border: '1px solid #fecaca', borderRadius: '8px', cursor: 'pointer',
                marginRight: 'auto', opacity: deleteMut.isPending ? 0.5 : 1,
              }}
            >
              <Trash2 size={14} /> Usuń
            </button>
          )}
          <button
            onClick={onClose}
            style={{
              padding: '8px 16px', fontSize: '13px', fontWeight: 500,
              color: '#374151', backgroundColor: '#ffffff',
              border: '1px solid #e5e7eb', borderRadius: '8px', cursor: 'pointer',
            }}
          >
            Anuluj
          </button>
          <button
            onClick={handleSave}
            disabled={saving}
            style={{
              display: 'flex', alignItems: 'center', gap: '4px',
              padding: '8px 16px', fontSize: '13px', fontWeight: 600,
              color: '#ffffff', backgroundColor: '#3b82f6',
              border: 'none', borderRadius: '8px', cursor: 'pointer',
              opacity: saving ? 0.5 : 1,
            }}
          >
            {state.mode === 'add' ? <><Plus size={14} /> Dodaj</> : <><Pencil size={14} /> Zapisz</>}
          </button>
        </div>
      </div>
    </div>
  );
}

/* ── small components ── */

function NavBtn({ onClick, children }: { onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      style={{
        display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
        width: '32px', height: '32px', border: '1px solid #e5e7eb',
        borderRadius: '6px', backgroundColor: '#ffffff', cursor: 'pointer', color: '#374151',
      }}
    >
      {children}
    </button>
  );
}

/* ── styles ── */

const thStyle: React.CSSProperties = {
  padding: '10px 8px',
  textAlign: 'left',
  fontSize: '12px',
  fontWeight: 600,
  color: '#6b7280',
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
};

const tdStyle: React.CSSProperties = {
  padding: '8px',
  color: '#374151',
  verticalAlign: 'top',
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontSize: '12px',
  fontWeight: 600,
  color: '#374151',
  marginBottom: '4px',
};

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '8px 10px',
  fontSize: '14px',
  border: '1px solid #d1d5db',
  borderRadius: '6px',
  color: '#111827',
  boxSizing: 'border-box',
};
