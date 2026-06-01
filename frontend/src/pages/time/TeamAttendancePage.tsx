import { useState, useMemo, useCallback } from 'react';
import { ChevronLeft, ChevronRight, Download, AlertTriangle, Users } from 'lucide-react';
import { useTeamTimesheets, useAnomalies } from '@/api/hooks/useTimeTracking';
import { useEmployees, useOrgUnitTree } from '@/api/hooks/useOrganization';
import type { TimeSheetPeriodDto, TimeAnomalyDto } from '@/api/types/time';
import type { EmployeeDto, OrganizationUnitTreeNode } from '@/api/types/organization';
import { useIsMobile } from '@/shared';

/* ── helpers ── */

function toDateString(d: Date): string {
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
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

function formatDuration(duration: string): string {
  if (!duration) return '00:00';
  const parts = duration.split(':');
  if (parts.length < 2) return '00:00';
  let hours = 0;
  const hourPart = parts[0] ?? '';
  if (hourPart.includes('.')) {
    const [days, h] = hourPart.split('.');
    hours = parseInt(days ?? '0') * 24 + parseInt(h ?? '0');
  } else {
    hours = parseInt(hourPart);
  }
  const minutes = parseInt(parts[1] ?? '0');
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;
}

function durationToMinutes(duration: string): number {
  if (!duration) return 0;
  const parts = duration.split(':');
  if (parts.length < 2) return 0;
  let hours = 0;
  const hourPart = parts[0] ?? '';
  if (hourPart.includes('.')) {
    const [days, h] = hourPart.split('.');
    hours = parseInt(days ?? '0') * 24 + parseInt(h ?? '0');
  } else {
    hours = parseInt(hourPart);
  }
  return hours * 60 + parseInt(parts[1] ?? '0');
}

function formatDateShort(dateStr: string): string {
  const d = new Date(dateStr + 'T00:00:00');
  return d.toLocaleDateString('pl-PL', { weekday: 'short', day: 'numeric', month: 'short' });
}

function formatWeekdayShort(dateStr: string): string {
  const d = new Date(dateStr + 'T00:00:00');
  const wd = d.toLocaleDateString('pl-PL', { weekday: 'short' });
  return `${wd} ${d.getDate()}`;
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

const ANOMALY_TYPE_LABELS: Record<string, string> = {
  MissingClockOut: 'Brak wyjścia',
  MissingClockIn: 'Brak wejścia',
  LateArrival: 'Spóźnienie',
  DoubleClockIn: 'Podwójne wejście',
  ExcessiveShift: 'Za długa zmiana',
};

const ANOMALY_COLORS: Record<string, string> = {
  MissingClockOut: '#dc2626',
  MissingClockIn: '#dc2626',
  LateArrival: '#f59e0b',
  DoubleClockIn: '#9333ea',
  ExcessiveShift: '#ea580c',
};

/* ── main component ── */

export function TeamAttendancePage() {
  const [currentDate, setCurrentDate] = useState(() => new Date());
  const [viewMode, setViewMode] = useState<'week' | 'month'>('week');
  const [unitId, setUnitId] = useState<string>('');
  const mobile = useIsMobile();

  const dateRange = useMemo(() => {
    return viewMode === 'week' ? getWeekRange(currentDate) : getMonthRange(currentDate);
  }, [viewMode, currentDate]);

  const periodLabel = useMemo(() => {
    if (viewMode === 'week') {
      return `${formatDateShort(dateRange.from)} – ${formatDateShort(dateRange.to)}`;
    }
    const d = new Date(dateRange.from + 'T00:00:00');
    return d.toLocaleDateString('pl-PL', { month: 'long', year: 'numeric' });
  }, [viewMode, dateRange]);

  const dates = useMemo(() => getDatesInRange(dateRange.from, dateRange.to), [dateRange]);

  /* navigation */
  const navigate = (dir: number) => {
    const d = new Date(currentDate);
    if (viewMode === 'week') {
      d.setDate(d.getDate() + dir * 7);
    } else {
      d.setDate(1);
      d.setMonth(d.getMonth() + dir);
    }
    setCurrentDate(d);
  };

  /* data fetching */
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

  const { data: timesheets, isLoading: tsLoading } = useTeamTimesheets(
    employeeIds,
    dateRange.from,
    dateRange.to,
    viewMode,
  );

  const { data: anomalies } = useAnomalies({ from: dateRange.from, to: dateRange.to });

  /* index anomalies by employeeId+date */
  const anomalyMap = useMemo(() => {
    const m = new Map<string, TimeAnomalyDto[]>();
    if (!anomalies) return m;
    for (const a of anomalies) {
      const key = `${a.employeeId}:${a.date}`;
      const arr = m.get(key);
      if (arr) arr.push(a);
      else m.set(key, [a]);
    }
    return m;
  }, [anomalies]);

  /* index timesheets by employeeId */
  const tsMap = useMemo(() => {
    const m = new Map<string, TimeSheetPeriodDto>();
    if (!timesheets) return m;
    for (const ts of timesheets) {
      m.set(ts.employeeId, ts);
    }
    return m;
  }, [timesheets]);

  /* CSV export */
  const exportCsv = useCallback(() => {
    const header = ['Pracownik', ...dates.map((d) => formatWeekdayShort(d)), 'Suma netto'];
    const rows: string[][] = [];

    for (const emp of employees) {
      const ts = tsMap.get(emp.id);
      const dayMap = new Map<string, string>();
      if (ts) {
        for (const day of ts.days) dayMap.set(day.date, day.netWorked);
      }
      const row = [`${emp.lastName} ${emp.firstName}`];
      for (const d of dates) {
        row.push(formatDuration(dayMap.get(d) ?? ''));
      }
      row.push(ts ? formatDuration(ts.netWorked) : '00:00');
      rows.push(row);
    }

    const csvContent = [header, ...rows]
      .map((r) => r.map((c) => `"${c}"`).join(';'))
      .join('\n');

    const BOM = '\uFEFF';
    const blob = new Blob([BOM + csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `raport-czasu-${dateRange.from}_${dateRange.to}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }, [employees, dates, tsMap, dateRange]);

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ fontSize: '22px', fontWeight: 700, color: '#111827', margin: 0, display: 'flex', alignItems: 'center', gap: '8px' }}>
          <Users size={22} />
          Raport czasu pracy zespołu
        </h1>
        <button
          onClick={exportCsv}
          disabled={!timesheets || employees.length === 0}
          style={{
            display: 'flex', alignItems: 'center', gap: '6px',
            padding: '8px 16px', fontSize: '13px', fontWeight: 600,
            color: '#ffffff', backgroundColor: '#059669',
            border: 'none', borderRadius: '8px', cursor: 'pointer',
            opacity: (!timesheets || employees.length === 0) ? 0.5 : 1,
          }}
        >
          <Download size={16} /> Eksport CSV
        </button>
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
            <option key={u.id} value={u.id}>
              {'  '.repeat(u.depth)}{u.name}
            </option>
          ))}
        </select>
      </div>

      {/* Legend */}
      <div style={{ display: 'flex', gap: '16px', marginBottom: '16px', fontSize: '12px', color: '#6b7280', flexWrap: 'wrap' }}>
        <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 2, backgroundColor: '#dcfce7' }} /> ≥ 8h
        </span>
        <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 2, backgroundColor: '#fef3c7' }} /> &lt; 8h
        </span>
        <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 2, backgroundColor: '#f3f4f6' }} /> Brak
        </span>
        {Object.entries(ANOMALY_TYPE_LABELS).map(([type, label]) => (
          <span key={type} style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <AlertTriangle size={12} color={ANOMALY_COLORS[type]} /> {label}
          </span>
        ))}
      </div>

      {/* Loading */}
      {tsLoading && (
        <div style={{ padding: '40px', textAlign: 'center', color: '#9ca3af' }}>
          Ładowanie danych zespołu...
        </div>
      )}

      {/* Empty */}
      {!tsLoading && employees.length === 0 && (
        <div style={{
          padding: '40px', textAlign: 'center', color: '#9ca3af',
          border: '1px solid #e5e7eb', borderRadius: '10px',
        }}>
          Brak pracowników w wybranej jednostce.
        </div>
      )}

      {/* Grid table */}
      {!tsLoading && employees.length > 0 && (
        <div style={{ overflowX: 'auto', border: '1px solid #e5e7eb', borderRadius: '10px' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px', minWidth: dates.length * 60 + 220 }}>
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
                        ...thStyle,
                        textAlign: 'center',
                        minWidth: '52px',
                        backgroundColor: isWeekend ? '#f1f5f9' : '#f9fafb',
                        fontSize: '11px',
                      }}
                    >
                      {formatWeekdayShort(d)}
                    </th>
                  );
                })}
                <th style={{ ...thStyle, textAlign: 'right', minWidth: '70px', backgroundColor: '#f0fdf4' }}>Suma</th>
              </tr>
            </thead>
            <tbody>
              {employees.map((emp) => {
                const ts = tsMap.get(emp.id);
                const dayMap = new Map<string, { netWorked: string; status: string }>();
                if (ts) {
                  for (const day of ts.days) {
                    dayMap.set(day.date, { netWorked: day.netWorked, status: day.status });
                  }
                }

                return (
                  <tr key={emp.id} style={{ borderTop: '1px solid #e5e7eb' }}>
                    <td style={{
                      ...tdStyle, fontWeight: 500, position: 'sticky', left: 0,
                      backgroundColor: '#ffffff', zIndex: 1, whiteSpace: 'nowrap',
                    }}>
                      {emp.lastName} {emp.firstName}
                    </td>
                    {dates.map((d) => {
                      const cell = dayMap.get(d);
                      const mins = cell ? durationToMinutes(cell.netWorked) : 0;
                      const cellAnomalies = anomalyMap.get(`${emp.id}:${d}`) ?? [];
                      const day = new Date(d + 'T00:00:00');
                      const isWeekend = day.getDay() === 0 || day.getDay() === 6;

                      let bg = isWeekend ? '#f8fafc' : '#ffffff';
                      if (mins >= 480) bg = '#dcfce7';
                      else if (mins > 0) bg = '#fef3c7';
                      else if (!isWeekend && !cell) bg = '#f3f4f6';

                      return (
                        <td
                          key={d}
                          title={cellAnomalies.map((a) => ANOMALY_TYPE_LABELS[a.type] ?? a.type).join(', ')}
                          style={{
                            ...tdStyle,
                            textAlign: 'center',
                            fontVariantNumeric: 'tabular-nums',
                            backgroundColor: bg,
                            padding: '4px 2px',
                            position: 'relative',
                            fontSize: '12px',
                          }}
                        >
                          {mins > 0 ? formatDuration(cell!.netWorked) : (isWeekend ? '—' : '')}
                          {cellAnomalies.length > 0 && (
                            <span style={{ position: 'absolute', top: 2, right: 2 }}>
                              <AlertTriangle
                                size={10}
                                color={ANOMALY_COLORS[cellAnomalies[0]!.type] ?? '#dc2626'}
                              />
                            </span>
                          )}
                        </td>
                      );
                    })}
                    <td style={{
                      ...tdStyle, textAlign: 'right', fontWeight: 700,
                      fontVariantNumeric: 'tabular-nums', backgroundColor: '#f0fdf4',
                      color: '#059669',
                    }}>
                      {ts ? formatDuration(ts.netWorked) : '—'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

/* ── small sub-components ── */

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
};
