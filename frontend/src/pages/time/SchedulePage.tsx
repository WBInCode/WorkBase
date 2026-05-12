import { useState, useMemo, useCallback } from 'react';
import {
  ChevronLeft, ChevronRight, Plus, Pencil, Trash2, Calendar, X, Zap, Building2,
} from 'lucide-react';
import TimeInput from '@/components/shared/TimeInput';
import {
  useTeamSchedules, useCreateSchedule, useUpdateSchedule, useDeleteSchedule,
  useScheduleTemplates, useGenerateBatchSchedules,
  useOrgUnitSchedule, useCreateOrgUnitSchedule, useUpdateOrgUnitSchedule, useDeleteOrgUnitSchedule,
} from '@/api/hooks/useTimeTracking';
import { useEmployees, useOrgUnitTree } from '@/api/hooks/useOrganization';
import type { ScheduleDto, ScheduleTemplateDto, DayShiftPattern, OrgUnitScheduleDto } from '@/api/types/time';
import type { EmployeeDto, OrganizationUnitTreeNode } from '@/api/types/organization';
import { useIsMobile } from '@/shared';

/* ── helpers ── */

function toDateString(d: Date): string {
  return d.toISOString().split('T')[0] ?? '';
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
  nieplanowana: { bg: '#ffedd5', border: '#fdba74', text: '#9a3412' },
};

const SOURCE_INDICATOR: Record<string, { label: string; opacity: number; borderStyle: string }> = {
  OrgUnit: { label: '● Jednostka', opacity: 0.75, borderStyle: 'dashed' },
  Individual: { label: '', opacity: 1, borderStyle: 'solid' },
  Unplanned: { label: '⚡ Nieplanowana', opacity: 1, borderStyle: 'dotted' },
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
  const [showOrgUnitPanel, setShowOrgUnitPanel] = useState(false);
  const [showGenerator, setShowGenerator] = useState(false);
  const mobile = useIsMobile();

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
    <div style={{ padding: mobile ? '16px' : '24px 32px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ fontSize: '22px', fontWeight: 700, color: '#111827', margin: 0, display: 'flex', alignItems: 'center', gap: '8px' }}>
          <Calendar size={22} />
          Grafik pracy
        </h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            onClick={() => setShowOrgUnitPanel(true)}
            style={{
              display: 'flex', alignItems: 'center', gap: '6px',
              padding: '8px 16px', fontSize: '13px', fontWeight: 600,
              color: '#7c3aed', backgroundColor: '#f5f3ff',
              border: '1px solid #c4b5fd', borderRadius: '8px', cursor: 'pointer',
            }}
          >
            <Building2 size={16} /> Grafik jednostki
          </button>
          <button
            onClick={() => setShowGenerator(true)}
            style={{
              display: 'flex', alignItems: 'center', gap: '6px',
              padding: '8px 16px', fontSize: '13px', fontWeight: 600,
              color: '#ffffff', backgroundColor: '#7c3aed',
              border: 'none', borderRadius: '8px', cursor: 'pointer',
            }}
          >
            <Zap size={16} /> Generuj grafik
          </button>
        </div>
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
        <span style={{ marginLeft: '16px', borderLeft: '1px solid #e5e7eb', paddingLeft: '16px', display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ display: 'inline-block', width: 12, height: 12, borderRadius: 3, border: '1px dashed #93c5fd', opacity: 0.75 }} />
          Z grafiku jednostki
        </span>
        <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ display: 'inline-block', width: 12, height: 12, borderRadius: 3, border: '1px dotted #fdba74', backgroundColor: '#ffedd5' }} />
          Nieplanowana
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
                      const source = SOURCE_INDICATOR[sched.source ?? 'Individual'] ?? SOURCE_INDICATOR.Individual;
                      return (
                        <td key={d} style={{ ...tdStyle, textAlign: 'center', padding: '4px', backgroundColor: isWeekend ? '#f8fafc' : undefined }}>
                          <div
                            onClick={() => openEdit(sched)}
                            style={{
                              backgroundColor: style.bg, border: `1px ${source.borderStyle} ${style.border}`,
                              borderRadius: '6px', padding: '4px 6px', cursor: 'pointer',
                              fontSize: '12px', lineHeight: '1.3', opacity: source.opacity,
                            }}
                            title={`${formatTime(sched.plannedStart)}–${formatTime(sched.plannedEnd)}${sched.shiftType ? ` (${sched.shiftType})` : ''}${source.label ? `\nŹródło: ${source.label}` : ''}\nKliknij aby edytować`}
                          >
                            <div style={{ fontWeight: 600, color: style.text }}>
                              {formatTime(sched.plannedStart)}–{formatTime(sched.plannedEnd)}
                            </div>
                            {sched.shiftType && (
                              <div style={{ fontSize: '10px', color: style.text, opacity: 0.8 }}>
                                {sched.shiftType}
                              </div>
                            )}
                            {source.label && (
                              <div style={{ fontSize: '9px', color: style.text, opacity: 0.6, marginTop: '1px' }}>
                                {source.label}
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

      {/* Generate Modal */}
      {showGenerator && (
        <GenerateScheduleModal
          flatUnits={flatUnits}
          templates={templates ?? []}
          onClose={() => setShowGenerator(false)}
        />
      )}

      {/* Org Unit Schedule Panel */}
      {showOrgUnitPanel && (
        <OrgUnitSchedulePanel
          flatUnits={flatUnits}
          selectedUnitId={unitId}
          onClose={() => setShowOrgUnitPanel(false)}
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
      onChange({ ...state, plannedStart: match[1] ?? '', plannedEnd: match[2] ?? '', shiftType: tpl.name, templateId: tpl.id });
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
            <TimeInput
              value={state.plannedStart}
              onChange={(v) => onChange({ ...state, plannedStart: v })}
              style={inputStyle}
            />
          </div>
          <div>
            <label style={labelStyle}>Koniec</label>
            <TimeInput
              value={state.plannedEnd}
              onChange={(v) => onChange({ ...state, plannedEnd: v })}
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

const WEEKDAYS = [
  { dow: 1, label: 'Poniedziałek', short: 'Pn' },
  { dow: 2, label: 'Wtorek', short: 'Wt' },
  { dow: 3, label: 'Środa', short: 'Śr' },
  { dow: 4, label: 'Czwartek', short: 'Cz' },
  { dow: 5, label: 'Piątek', short: 'Pt' },
  { dow: 6, label: 'Sobota', short: 'So' },
  { dow: 0, label: 'Niedziela', short: 'Nd' },
] as const;

interface WeekDayRow {
  enabled: boolean;
  plannedStart: string;
  plannedEnd: string;
  shiftType: string;
}

function defaultWeekRows(): Record<number, WeekDayRow> {
  const rows: Record<number, WeekDayRow> = {};
  for (const wd of WEEKDAYS) {
    rows[wd.dow] = {
      enabled: wd.dow >= 1 && wd.dow <= 5,
      plannedStart: '08:00',
      plannedEnd: '16:00',
      shiftType: '',
    };
  }
  return rows;
}

function getMonthRangeStr(d: Date): { from: string; to: string } {
  const y = d.getFullYear();
  const m = d.getMonth();
  const from = new Date(y, m, 1);
  const to = new Date(y, m + 1, 0);
  return { from: toDateString(from), to: toDateString(to) };
}

function GenerateScheduleModal({
  flatUnits,
  templates,
  onClose,
}: {
  flatUnits: { id: string; name: string; depth: number }[];
  templates: ScheduleTemplateDto[];
  onClose: () => void;
}) {
  const [genUnitId, setGenUnitId] = useState('');
  const [rangeMode, setRangeMode] = useState<'month' | 'custom'>('month');
  const [monthDate, setMonthDate] = useState(() => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  });
  const [customFrom, setCustomFrom] = useState('');
  const [customTo, setCustomTo] = useState('');
  const [weekRows, setWeekRows] = useState<Record<number, WeekDayRow>>(defaultWeekRows);
  const [overwrite, setOverwrite] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const generateMut = useGenerateBatchSchedules();

  const { data: empPage } = useEmployees({
    organizationUnitId: genUnitId || undefined,
    page: 1,
    pageSize: 500,
    status: 'Active',
  });
  const genEmployees = empPage?.items ?? [];

  const dateRange = useMemo(() => {
    if (rangeMode === 'month' && monthDate) {
      const [y, m] = monthDate.split('-').map(Number);
      if (y && m) return getMonthRangeStr(new Date(y, m - 1, 1));
    }
    if (rangeMode === 'custom' && customFrom && customTo) {
      return { from: customFrom, to: customTo };
    }
    return null;
  }, [rangeMode, monthDate, customFrom, customTo]);

  const updateRow = (dow: number, patch: Partial<WeekDayRow>) => {
    setWeekRows((prev) => {
      const old = prev[dow]!;
      return { ...prev, [dow]: { ...old, ...patch } as WeekDayRow };
    });
  };

  const applyTemplateToAll = (tpl: ScheduleTemplateDto) => {
    const match = tpl.definition.match(/^(\d{2}:\d{2})-(\d{2}:\d{2})$/);
    if (!match?.[1] || !match[2]) return;
    const start = match[1];
    const end = match[2];
    setWeekRows((prev) => {
      const next = { ...prev };
      for (const wd of WEEKDAYS) {
        const r = next[wd.dow];
        if (r && r.enabled) {
          next[wd.dow] = { ...r, plannedStart: start, plannedEnd: end, shiftType: tpl.name };
        }
      }
      return next;
    });
  };

  const handleGenerate = async () => {
    setError('');
    setSuccess('');

    if (genEmployees.length === 0) {
      setError('Brak pracowników w wybranej jednostce.');
      return;
    }
    if (!dateRange) {
      setError('Wybierz zakres dat.');
      return;
    }

    const pattern: DayShiftPattern[] = WEEKDAYS
      .filter((wd) => weekRows[wd.dow]?.enabled)
      .map((wd) => {
        const r = weekRows[wd.dow]!;
        return {
          dayOfWeek: wd.dow,
          plannedStart: r.plannedStart + ':00',
          plannedEnd: r.plannedEnd + ':00',
          shiftType: r.shiftType || undefined,
        };
      });

    if (pattern.length === 0) {
      setError('Zaznacz przynajmniej jeden dzień tygodnia.');
      return;
    }

    try {
      const result = await generateMut.mutateAsync({
        employeeIds: genEmployees.map((e) => e.id),
        from: dateRange.from,
        to: dateRange.to,
        weekPattern: pattern,
        overwrite,
      });
      setSuccess(`Wygenerowano ${result.createdCount} wpisów grafiku dla ${genEmployees.length} pracowników.`);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Wystąpił błąd');
    }
  };

  return (
    <div style={{
      position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)', zIndex: 1000,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
    }} onClick={onClose}>
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          backgroundColor: '#ffffff', borderRadius: '14px', padding: '28px',
          width: '600px', maxWidth: '95vw', maxHeight: '90vh', overflowY: 'auto',
          boxShadow: '0 20px 60px rgba(0,0,0,0.25)',
        }}
      >
        {/* Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <h2 style={{ fontSize: '20px', fontWeight: 700, color: '#111827', margin: 0, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Zap size={20} color="#7c3aed" />
            Generuj grafik pracy
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#6b7280', padding: '4px' }}>
            <X size={20} />
          </button>
        </div>

        {/* Unit selector */}
        <div style={{ marginBottom: '16px' }}>
          <label style={labelStyle}>Jednostka / zespół</label>
          <select
            value={genUnitId}
            onChange={(e) => setGenUnitId(e.target.value)}
            style={{ ...inputStyle, cursor: 'pointer' }}
          >
            <option value="">Wszystkie jednostki</option>
            {flatUnits.map((u) => (
              <option key={u.id} value={u.id}>{'  '.repeat(u.depth)}{u.name}</option>
            ))}
          </select>
          <div style={{ fontSize: '12px', color: '#6b7280', marginTop: '4px' }}>
            Pracowników: <strong>{genEmployees.length}</strong>
          </div>
        </div>

        {/* Date range */}
        <div style={{ marginBottom: '16px' }}>
          <label style={labelStyle}>Zakres dat</label>
          <div style={{ display: 'flex', gap: '8px', marginBottom: '8px' }}>
            {(['month', 'custom'] as const).map((m) => (
              <button
                key={m}
                onClick={() => setRangeMode(m)}
                style={{
                  padding: '5px 14px', fontSize: '12px', fontWeight: rangeMode === m ? 600 : 400,
                  color: rangeMode === m ? '#ffffff' : '#374151',
                  backgroundColor: rangeMode === m ? '#7c3aed' : '#f3f4f6',
                  border: '1px solid ' + (rangeMode === m ? '#7c3aed' : '#e5e7eb'),
                  borderRadius: '6px', cursor: 'pointer',
                }}
              >
                {{ month: 'Miesiąc', custom: 'Własny zakres' }[m]}
              </button>
            ))}
          </div>
          {rangeMode === 'month' && (
            <input
              type="month"
              value={monthDate}
              onChange={(e) => setMonthDate(e.target.value)}
              style={inputStyle}
            />
          )}
          {rangeMode === 'custom' && (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
              <div>
                <label style={{ ...labelStyle, fontSize: '11px' }}>Od</label>
                <input type="date" value={customFrom} onChange={(e) => setCustomFrom(e.target.value)} style={inputStyle} />
              </div>
              <div>
                <label style={{ ...labelStyle, fontSize: '11px' }}>Do</label>
                <input type="date" value={customTo} onChange={(e) => setCustomTo(e.target.value)} style={inputStyle} />
              </div>
            </div>
          )}
          {dateRange && (
            <div style={{ fontSize: '12px', color: '#6b7280', marginTop: '4px' }}>
              Okres: {dateRange.from} → {dateRange.to}
            </div>
          )}
        </div>

        {/* Quick template */}
        {templates.filter((t) => t.isActive).length > 0 && (
          <div style={{ marginBottom: '16px' }}>
            <label style={labelStyle}>Zastosuj szablon do wszystkich dni</label>
            <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap' }}>
              {templates.filter((t) => t.isActive).map((tpl) => (
                <button
                  key={tpl.id}
                  onClick={() => applyTemplateToAll(tpl)}
                  style={{
                    padding: '4px 10px', fontSize: '12px', fontWeight: 500,
                    border: '1px solid #d1d5db', borderRadius: '6px',
                    backgroundColor: '#ffffff', color: '#374151', cursor: 'pointer',
                  }}
                  title={tpl.definition}
                >
                  {tpl.name}
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Week pattern */}
        <div style={{ marginBottom: '16px' }}>
          <label style={labelStyle}>Wzorzec tygodnia</label>
          <div style={{ border: '1px solid #e5e7eb', borderRadius: '8px', overflow: 'hidden' }}>
            {WEEKDAYS.map((wd, i) => {
              const row = weekRows[wd.dow] ?? { enabled: false, plannedStart: '08:00', plannedEnd: '16:00', shiftType: '' };
              const isWeekend = wd.dow === 0 || wd.dow === 6;
              return (
                <div
                  key={wd.dow}
                  style={{
                    display: 'flex', alignItems: 'center', gap: '10px',
                    padding: '8px 12px',
                    borderTop: i > 0 ? '1px solid #f3f4f6' : undefined,
                    backgroundColor: isWeekend ? '#fafafa' : '#ffffff',
                    opacity: row.enabled ? 1 : 0.5,
                  }}
                >
                  <label style={{ display: 'flex', alignItems: 'center', gap: '6px', minWidth: '120px', cursor: 'pointer', fontSize: '13px', fontWeight: 500, color: '#374151' }}>
                    <input
                      type="checkbox"
                      checked={row.enabled}
                      onChange={(e) => updateRow(wd.dow, { enabled: e.target.checked })}
                      style={{ accentColor: '#7c3aed' }}
                    />
                    {wd.label}
                  </label>
                  <TimeInput
                    value={row.plannedStart}
                    onChange={(v) => updateRow(wd.dow, { plannedStart: v })}
                    disabled={!row.enabled}
                    style={{ ...inputStyle, width: '100px', padding: '4px 6px', fontSize: '13px' }}
                  />
                  <span style={{ color: '#9ca3af', fontSize: '13px' }}>–</span>
                  <TimeInput
                    value={row.plannedEnd}
                    onChange={(v) => updateRow(wd.dow, { plannedEnd: v })}
                    disabled={!row.enabled}
                    style={{ ...inputStyle, width: '100px', padding: '4px 6px', fontSize: '13px' }}
                  />
                  <input
                    type="text"
                    value={row.shiftType}
                    onChange={(e) => updateRow(wd.dow, { shiftType: e.target.value })}
                    disabled={!row.enabled}
                    placeholder="typ"
                    style={{ ...inputStyle, width: '100px', padding: '4px 6px', fontSize: '12px' }}
                  />
                </div>
              );
            })}
          </div>
        </div>

        {/* Overwrite */}
        <div style={{ marginBottom: '20px' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer', fontSize: '13px', color: '#374151' }}>
            <input
              type="checkbox"
              checked={overwrite}
              onChange={(e) => setOverwrite(e.target.checked)}
              style={{ accentColor: '#dc2626' }}
            />
            <span>Nadpisz istniejące wpisy grafiku</span>
          </label>
          {overwrite && (
            <div style={{ marginTop: '4px', padding: '6px 10px', backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: '6px', fontSize: '12px', color: '#dc2626' }}>
              Uwaga: istniejące grafiki w wybranym okresie zostaną zastąpione.
            </div>
          )}
        </div>

        {/* Error / Success */}
        {error && (
          <div style={{ marginBottom: '12px', padding: '8px 12px', backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: '6px', color: '#dc2626', fontSize: '13px' }}>
            {error}
          </div>
        )}
        {success && (
          <div style={{ marginBottom: '12px', padding: '8px 12px', backgroundColor: '#f0fdf4', border: '1px solid #bbf7d0', borderRadius: '6px', color: '#166534', fontSize: '13px' }}>
            {success}
          </div>
        )}

        {/* Actions */}
        <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
          <button
            onClick={onClose}
            style={{
              padding: '8px 16px', fontSize: '13px', fontWeight: 500,
              color: '#374151', backgroundColor: '#ffffff',
              border: '1px solid #e5e7eb', borderRadius: '8px', cursor: 'pointer',
            }}
          >
            {success ? 'Zamknij' : 'Anuluj'}
          </button>
          {!success && (
            <button
              onClick={handleGenerate}
              disabled={generateMut.isPending}
              style={{
                display: 'flex', alignItems: 'center', gap: '6px',
                padding: '8px 20px', fontSize: '13px', fontWeight: 600,
                color: '#ffffff', backgroundColor: '#7c3aed',
                border: 'none', borderRadius: '8px', cursor: 'pointer',
                opacity: generateMut.isPending ? 0.5 : 1,
              }}
            >
              <Zap size={14} /> {generateMut.isPending ? 'Generowanie...' : 'Generuj'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

/* ── Org Unit Schedule Panel ── */

const WEEKDAY_NAMES = ['Niedziela', 'Poniedziałek', 'Wtorek', 'Środa', 'Czwartek', 'Piątek', 'Sobota'];

function OrgUnitSchedulePanel({
  flatUnits,
  selectedUnitId,
  onClose,
}: {
  flatUnits: { id: string; name: string; depth: number }[];
  selectedUnitId: string;
  onClose: () => void;
}) {
  const [orgUnitId, setOrgUnitId] = useState(selectedUnitId);
  const [name, setName] = useState('');
  const [effectiveFrom, setEffectiveFrom] = useState(toDateString(new Date()));
  const [patterns, setPatterns] = useState<DayShiftPattern[]>([
    { dayOfWeek: 1, plannedStart: '08:00', plannedEnd: '16:00' },
    { dayOfWeek: 2, plannedStart: '08:00', plannedEnd: '16:00' },
    { dayOfWeek: 3, plannedStart: '08:00', plannedEnd: '16:00' },
    { dayOfWeek: 4, plannedStart: '08:00', plannedEnd: '16:00' },
    { dayOfWeek: 5, plannedStart: '08:00', plannedEnd: '16:00' },
  ]);

  const { data: existing } = useOrgUnitSchedule(orgUnitId || undefined);
  const createMutation = useCreateOrgUnitSchedule();
  const updateMutation = useUpdateOrgUnitSchedule();
  const deleteMutation = useDeleteOrgUnitSchedule();

  // Load existing data when org unit changes
  useMemo(() => {
    if (existing) {
      setName(existing.name);
      setEffectiveFrom(existing.effectiveFrom);
      try {
        const parsed = JSON.parse(existing.weekPattern) as DayShiftPattern[];
        if (parsed.length > 0) setPatterns(parsed);
      } catch { /* keep defaults */ }
    }
  }, [existing]);

  const updatePattern = (index: number, field: keyof DayShiftPattern, value: string | number) => {
    setPatterns(prev => prev.map((p, i) => i === index ? { ...p, [field]: value } : p));
  };

  const addDay = () => {
    const usedDays = patterns.map(p => p.dayOfWeek);
    const available = [0, 1, 2, 3, 4, 5, 6].find(d => !usedDays.includes(d));
    if (available !== undefined) {
      setPatterns(prev => [...prev, { dayOfWeek: available, plannedStart: '08:00', plannedEnd: '16:00' }]);
    }
  };

  const removeDay = (index: number) => {
    setPatterns(prev => prev.filter((_, i) => i !== index));
  };

  const handleSave = () => {
    if (!orgUnitId || !name.trim()) return;
    const weekPattern = JSON.stringify(patterns);

    if (existing) {
      updateMutation.mutate({ id: existing.id, name, weekPattern, effectiveFrom });
    } else {
      createMutation.mutate({ orgUnitId, name, weekPattern, effectiveFrom });
    }
  };

  const handleDelete = () => {
    if (existing && confirm('Usunąć grafik jednostki? Przyszłe wygenerowane wpisy zostaną usunięte.')) {
      deleteMutation.mutate(existing.id);
    }
  };

  const isSaving = createMutation.isPending || updateMutation.isPending;
  const isSuccess = createMutation.isSuccess || updateMutation.isSuccess;

  return (
    <div style={{
      position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000,
    }}>
      <div style={{
        backgroundColor: '#ffffff', borderRadius: '12px', padding: '28px',
        width: '600px', maxHeight: '85vh', overflow: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.2)',
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <h2 style={{ fontSize: '18px', fontWeight: 700, color: '#111827', margin: 0, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Building2 size={20} /> Grafik jednostki organizacyjnej
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#6b7280' }}>
            <X size={20} />
          </button>
        </div>

        {/* Org unit selector */}
        <div style={{ marginBottom: '16px' }}>
          <label style={labelStyle}>Jednostka organizacyjna</label>
          <select
            value={orgUnitId}
            onChange={(e) => setOrgUnitId(e.target.value)}
            style={{ ...inputStyle, cursor: 'pointer' }}
          >
            <option value="">Wybierz jednostkę...</option>
            {flatUnits.map(u => (
              <option key={u.id} value={u.id}>{'  '.repeat(u.depth)}{u.name}</option>
            ))}
          </select>
        </div>

        {/* Name */}
        <div style={{ marginBottom: '16px' }}>
          <label style={labelStyle}>Nazwa grafiku</label>
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="np. Grafik standardowy pon-pt"
            style={inputStyle}
          />
        </div>

        {/* Effective from */}
        <div style={{ marginBottom: '16px' }}>
          <label style={labelStyle}>Obowiązuje od</label>
          <input
            type="date"
            value={effectiveFrom}
            onChange={(e) => setEffectiveFrom(e.target.value)}
            style={inputStyle}
          />
        </div>

        {/* Week pattern */}
        <div style={{ marginBottom: '16px' }}>
          <label style={labelStyle}>Wzorzec tygodniowy</label>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {patterns.map((p, i) => (
              <div key={i} style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                <select
                  value={p.dayOfWeek}
                  onChange={(e) => updatePattern(i, 'dayOfWeek', parseInt(e.target.value))}
                  style={{ ...inputStyle, width: '140px' }}
                >
                  {[0, 1, 2, 3, 4, 5, 6].map(d => (
                    <option key={d} value={d}>{WEEKDAY_NAMES[d]}</option>
                  ))}
                </select>
                <TimeInput
                  value={p.plannedStart}
                  onChange={(v) => updatePattern(i, 'plannedStart', v)}
                  style={{ ...inputStyle, width: '110px' }}
                />
                <span style={{ color: '#6b7280' }}>–</span>
                <TimeInput
                  value={p.plannedEnd}
                  onChange={(v) => updatePattern(i, 'plannedEnd', v)}
                  style={{ ...inputStyle, width: '110px' }}
                />
                <input
                  value={p.shiftType ?? ''}
                  onChange={(e) => updatePattern(i, 'shiftType', e.target.value)}
                  placeholder="typ"
                  style={{ ...inputStyle, width: '90px' }}
                />
                <button onClick={() => removeDay(i)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#ef4444' }}>
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
            {patterns.length < 7 && (
              <button onClick={addDay} style={{
                padding: '6px 12px', fontSize: '12px', color: '#3b82f6', backgroundColor: '#eff6ff',
                border: '1px solid #bfdbfe', borderRadius: '6px', cursor: 'pointer', alignSelf: 'flex-start',
              }}>
                + Dodaj dzień
              </button>
            )}
          </div>
        </div>

        {/* Info about inheritance */}
        <div style={{
          padding: '12px', backgroundColor: '#f0f9ff', border: '1px solid #bae6fd',
          borderRadius: '8px', marginBottom: '20px', fontSize: '12px', color: '#0369a1',
        }}>
          💡 Grafik jednostki zostanie automatycznie zastosowany do wszystkich pracowników w tej jednostce.
          Indywidualne wpisy (ręczne) nie zostaną nadpisane.
          Jednostki podrzędne bez własnego grafiku odziedziczą ten wzorzec.
        </div>

        {/* Status messages */}
        {isSuccess && (
          <div style={{ padding: '10px', backgroundColor: '#f0fdf4', border: '1px solid #86efac', borderRadius: '6px', marginBottom: '16px', fontSize: '13px', color: '#166534' }}>
            ✓ Grafik zapisany. Wpisy zostaną wygenerowane automatycznie.
          </div>
        )}

        {/* Actions */}
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: '8px' }}>
          <div>
            {existing && (
              <button
                onClick={handleDelete}
                disabled={deleteMutation.isPending}
                style={{
                  padding: '10px 16px', fontSize: '13px', fontWeight: 600,
                  color: '#dc2626', backgroundColor: '#fef2f2',
                  border: '1px solid #fecaca', borderRadius: '8px', cursor: 'pointer',
                }}
              >
                <Trash2 size={14} style={{ marginRight: '4px' }} /> Usuń grafik
              </button>
            )}
          </div>
          <div style={{ display: 'flex', gap: '8px' }}>
            <button onClick={onClose} style={{
              padding: '10px 20px', fontSize: '13px', fontWeight: 500,
              color: '#374151', backgroundColor: '#ffffff',
              border: '1px solid #d1d5db', borderRadius: '8px', cursor: 'pointer',
            }}>
              Anuluj
            </button>
            <button
              onClick={handleSave}
              disabled={isSaving || !orgUnitId || !name.trim()}
              style={{
                padding: '10px 20px', fontSize: '13px', fontWeight: 600,
                color: '#ffffff', backgroundColor: isSaving ? '#9ca3af' : '#7c3aed',
                border: 'none', borderRadius: '8px', cursor: isSaving ? 'default' : 'pointer',
              }}
            >
              {existing ? 'Aktualizuj' : 'Utwórz'} grafik
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}


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
