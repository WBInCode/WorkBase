import { useState, useMemo, useCallback, useRef, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { ChevronLeft, ChevronRight, Download, AlertTriangle } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import { useTeamTimesheets, useAnomalies, useAdminCreateTimeEntry, useAdminDeleteTimeEntry } from '@/api/hooks/useTimeTracking';
import { useEmployees, useOrgUnitTree } from '@/api/hooks/useOrganization';
import type { TimeSheetPeriodDto, TimeAnomalyDto, TimeSheetEntryDto } from '@/api/types/time';
import type { EmployeeDto, OrganizationUnitTreeNode } from '@/api/types/organization';
import { useIsMobile } from '@/shared';
import type ExcelJS from 'exceljs';
import { colors } from '@/theme/tokens';

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
  MissingClockOut: colors.danger[600],
  MissingClockIn: colors.danger[600],
  LateArrival: colors.warning[500],
  DoubleClockIn: '#9333ea',
  ExcessiveShift: '#ea580c',
};

/* ── main component ── */

interface BreakDraft {
  start: string;
  end: string;
  breakType: string;
}

interface DayEditState {
  employeeId: string;
  date: string;
  start: string;
  end: string;
  breaks: BreakDraft[];
  existingEntries: TimeSheetEntryDto[];
}

const BREAK_TYPE_OPTIONS = [
  { value: '', label: 'Bez typu' },
  { value: 'Paid', label: 'Płatna' },
  { value: 'Unpaid', label: 'Bezpłatna' },
];

export function TeamAttendancePage() {
  const [currentDate, setCurrentDate] = useState(() => new Date());
  const [viewMode, setViewMode] = useState<'week' | 'month'>('week');
  const [unitId, setUnitId] = useState<string>('');
  const mobile = useIsMobile();
  const queryClient = useQueryClient();

  /* panel edycji dnia (od / do / przerwy) — renderowany w portalu, żeby nie był przycinany przez overflow tabeli */
  const [editState, setEditState] = useState<DayEditState | null>(null);
  const [panelPos, setPanelPos] = useState<{ top: number; left: number } | null>(null);
  const [savingCell, setSavingCell] = useState<string | null>(null);
  const startInputRef = useRef<HTMLInputElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const createEntry = useAdminCreateTimeEntry();
  const deleteEntry = useAdminDeleteTimeEntry();

  useEffect(() => {
    if (editState) startInputRef.current?.focus();
  }, [editState]);

  useEffect(() => {
    if (!editState) return;
    const onDocClick = (e: MouseEvent) => {
      if (panelRef.current && !panelRef.current.contains(e.target as Node)) {
        setEditState(null);
        setPanelPos(null);
      }
    };
    document.addEventListener('mousedown', onDocClick);
    return () => document.removeEventListener('mousedown', onDocClick);
  }, [editState]);

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

  const refreshTeamTimesheets = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ['time', 'team-timesheets'] });
    queryClient.invalidateQueries({ queryKey: ['time', 'anomalies'] });
  }, [queryClient]);

  const startCellEdit = useCallback((employeeId: string, date: string, existingEntries: TimeSheetEntryDto[], anchor: HTMLElement) => {
    const rect = anchor.getBoundingClientRect();
    setPanelPos({ top: rect.bottom + 4, left: rect.left });
    const fmt = (iso: string) => {
      const d = new Date(iso);
      return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
    };
    const sorted = [...existingEntries].sort((a, b) => a.entryTime.localeCompare(b.entryTime));
    const clockIn = sorted.find((e) => e.type === 'ClockIn');
    const clockOut = [...sorted].reverse().find((e) => e.type === 'ClockOut');
    const breaks: BreakDraft[] = [];
    let pendingStart: TimeSheetEntryDto | null = null;
    for (const entry of sorted) {
      if (entry.type === 'BreakStart') pendingStart = entry;
      else if (entry.type === 'BreakEnd' && pendingStart) {
        breaks.push({ start: fmt(pendingStart.entryTime), end: fmt(entry.entryTime), breakType: entry.breakType ?? '' });
        pendingStart = null;
      }
    }
    setEditState({
      employeeId,
      date,
      start: clockIn ? fmt(clockIn.entryTime) : '',
      end: clockOut ? fmt(clockOut.entryTime) : '',
      breaks,
      existingEntries,
    });
  }, []);

  const cancelCellEdit = useCallback(() => {
    setEditState(null);
    setPanelPos(null);
  }, []);

  const addBreakRow = useCallback(() => {
    setEditState((prev) => (prev ? { ...prev, breaks: [...prev.breaks, { start: '', end: '', breakType: '' }] } : prev));
  }, []);

  const removeBreakRow = useCallback((idx: number) => {
    setEditState((prev) => (prev ? { ...prev, breaks: prev.breaks.filter((_, i) => i !== idx) } : prev));
  }, []);

  const updateBreakRow = useCallback((idx: number, field: keyof BreakDraft, value: string) => {
    setEditState((prev) => {
      if (!prev) return prev;
      const breaks = prev.breaks.map((b, i) => (i === idx ? { ...b, [field]: value } : b));
      return { ...prev, breaks };
    });
  }, []);

  /** Zapisuje panel (od / do / przerwy) dla danego pracownika/dnia jako zestaw wpisów. */
  const saveCellEdit = useCallback(async () => {
    if (!editState) return;
    const { employeeId, date, start, end, breaks, existingEntries } = editState;
    const toMinutes = (t: string) => {
      const m = t.match(/^(\d{1,2}):(\d{2})$/);
      if (!m) return null;
      return parseInt(m[1]!, 10) * 60 + parseInt(m[2]!, 10);
    };
    const startMin = start ? toMinutes(start) : null;
    const endMin = end ? toMinutes(end) : null;
    // wymagamy pełnego zakresu od-do, albo całkowicie puste pole (czyszczenie dnia)
    if ((start || end) && (startMin === null || endMin === null || endMin <= startMin)) return;
    const validBreaks = breaks
      .map((b) => ({ ...b, startMin: toMinutes(b.start), endMin: toMinutes(b.end) }))
      .filter((b) => b.startMin !== null && b.endMin !== null && b.endMin! > b.startMin!);

    const cellKey = `${employeeId}:${date}`;
    setSavingCell(cellKey);
    try {
      // usuń istniejące wpisy tego dnia, żeby uniknąć duplikatów
      for (const entry of existingEntries) {
        // eslint-disable-next-line no-await-in-loop
        await deleteEntry.mutateAsync(entry.id);
      }
      if (startMin !== null && endMin !== null) {
        const toIso = (minutes: number) => {
          const d = new Date(`${date}T00:00:00`);
          d.setMinutes(minutes);
          return d.toISOString();
        };
        await createEntry.mutateAsync({
          employeeId,
          entryTime: toIso(startMin),
          type: 'ClockIn',
          note: 'Edycja z raportu zespołu',
        });
        for (const b of validBreaks) {
          // eslint-disable-next-line no-await-in-loop
          await createEntry.mutateAsync({
            employeeId,
            entryTime: toIso(b.startMin!),
            type: 'BreakStart',
            breakType: b.breakType || undefined,
            note: 'Edycja z raportu zespołu',
          });
          // eslint-disable-next-line no-await-in-loop
          await createEntry.mutateAsync({
            employeeId,
            entryTime: toIso(b.endMin!),
            type: 'BreakEnd',
            breakType: b.breakType || undefined,
            note: 'Edycja z raportu zespołu',
          });
        }
        await createEntry.mutateAsync({
          employeeId,
          entryTime: toIso(endMin),
          type: 'ClockOut',
          note: 'Edycja z raportu zespołu',
        });
      }
      refreshTeamTimesheets();
    } finally {
      setSavingCell(null);
      setEditState(null);
      setPanelPos(null);
    }
  }, [editState, createEntry, deleteEntry, refreshTeamTimesheets]);

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

  /* Excel export */
  const exportExcel = useCallback(async () => {
    // Loaded on demand — exceljs is a large dependency, kept out of the main
    // and route bundles until the user actually triggers an export.
    const { default: ExcelJSLib } = await import('exceljs');
    const wb = new ExcelJSLib.Workbook();
    wb.creator = 'WorkBase';
    const ws = wb.addWorksheet('Raport czasu pracy');

    /* ── colours ── */
    const headerFill: ExcelJS.FillPattern = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF059669' } };
    const headerFont: Partial<ExcelJS.Font> = { bold: true, color: { argb: 'FFFFFFFF' }, size: 11 };
    const weekendFill: ExcelJS.FillPattern = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF1F5F9' } };
    const greenFill: ExcelJS.FillPattern = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFDCFCE7' } };
    const yellowFill: ExcelJS.FillPattern = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFEF3C7' } };
    const sumFill: ExcelJS.FillPattern = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF0FDF4' } };
    const thinBorder: Partial<ExcelJS.Borders> = {
      top: { style: 'thin', color: { argb: 'FFE5E7EB' } },
      bottom: { style: 'thin', color: { argb: 'FFE5E7EB' } },
      left: { style: 'thin', color: { argb: 'FFE5E7EB' } },
      right: { style: 'thin', color: { argb: 'FFE5E7EB' } },
    };

    /* ── header row ── */
    const headerValues = ['Pracownik', ...dates.map((d) => formatWeekdayShort(d)), 'Suma netto'];
    const headerRow = ws.addRow(headerValues);
    headerRow.eachCell((cell) => {
      cell.fill = headerFill;
      cell.font = headerFont;
      cell.alignment = { horizontal: 'center', vertical: 'middle' };
      cell.border = thinBorder;
    });
    // first cell (Pracownik) left-aligned
    headerRow.getCell(1).alignment = { horizontal: 'left', vertical: 'middle' };
    headerRow.height = 24;

    /* ── detect weekend columns (1-indexed, col 1 = Pracownik) ── */
    const weekendCols = new Set<number>();
    dates.forEach((d, i) => {
      const day = new Date(d + 'T00:00:00');
      if (day.getDay() === 0 || day.getDay() === 6) weekendCols.add(i + 2); // +2: 1-based + skip col A
    });

    /* ── data rows ── */
    for (const emp of employees) {
      const ts = tsMap.get(emp.id);
      const dayMap = new Map<string, string>();
      if (ts) {
        for (const day of ts.days) dayMap.set(day.date, day.netWorked);
      }

      const rowValues: (string | number)[] = [`${emp.lastName} ${emp.firstName}`];
      for (const d of dates) {
        rowValues.push(formatDuration(dayMap.get(d) ?? ''));
      }
      const sumMinutes = ts ? durationToMinutes(ts.netWorked) : 0;
      const sumHours = sumMinutes / 60;
      rowValues.push(sumHours);

      const row = ws.addRow(rowValues);

      /* employee name styling */
      const nameCell = row.getCell(1);
      nameCell.font = { bold: true, size: 11 };
      nameCell.border = thinBorder;

      /* day cells styling */
      dates.forEach((d, i) => {
        const colIdx = i + 2;
        const cell = row.getCell(colIdx);
        cell.alignment = { horizontal: 'center', vertical: 'middle' };
        cell.border = thinBorder;

        const mins = durationToMinutes(dayMap.get(d) ?? '');
        const isWeekend = weekendCols.has(colIdx);

        if (mins >= 480) {
          cell.fill = greenFill;
        } else if (mins > 0) {
          cell.fill = yellowFill;
        } else if (isWeekend) {
          cell.fill = weekendFill;
          cell.value = '—';
        }
      });

      /* sum cell styling */
      const sumCell = row.getCell(headerValues.length);
      sumCell.fill = sumFill;
      sumCell.font = { bold: true, size: 11, color: { argb: 'FF059669' } };
      sumCell.alignment = { horizontal: 'right', vertical: 'middle' };
      sumCell.border = thinBorder;
      sumCell.numFmt = '0.00';
    }

    /* ── column widths ── */
    ws.getColumn(1).width = 28; // Pracownik
    for (let i = 2; i <= dates.length + 1; i++) ws.getColumn(i).width = 10;
    ws.getColumn(headerValues.length).width = 14; // Suma

    /* ── freeze first row + first column ── */
    ws.views = [{ state: 'frozen', xSplit: 1, ySplit: 1 }];

    /* ── download ── */
    const buffer = await wb.xlsx.writeBuffer();
    const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `raport-czasu-${dateRange.from}_${dateRange.to}.xlsx`;
    a.click();
    URL.revokeObjectURL(url);
  }, [employees, dates, tsMap, dateRange]);

  return (
    <div style={{ padding: mobile ? '14px' : '24px 28px', maxWidth: '1400px', margin: '0 auto' }}>
      {/* ── Karta dowodzenia: tytuł + eksport + kontrolki ── */}
      <div
        style={{
          backgroundColor: colors.white,
          border: `1px solid ${colors.gray[200]}`,
          borderRadius: '20px',
          boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
          padding: mobile ? '16px' : '18px 22px',
          marginBottom: '18px',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '12px', flexWrap: 'wrap' }}>
          <div>
            <h1 style={{ fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900], margin: 0 }}>
              Raport czasu pracy zespołu
            </h1>
            <p style={{ margin: '3px 0 0', fontSize: '13.5px', fontWeight: 600, color: colors.primary[600], textTransform: 'capitalize' }}>
              {periodLabel}
            </p>
          </div>
          <button
            onClick={exportExcel}
            disabled={!timesheets || employees.length === 0}
            style={{
              display: 'flex', alignItems: 'center', gap: '6px',
              padding: '9px 18px', fontSize: '13px', fontWeight: 700, fontFamily: 'inherit',
              color: colors.white, backgroundColor: colors.emerald[600],
              border: 'none', borderRadius: '999px', cursor: 'pointer',
              opacity: (!timesheets || employees.length === 0) ? 0.5 : 1,
              boxShadow: (!timesheets || employees.length === 0) ? 'none' : '0 6px 14px -4px rgba(5,150,105,0.45)',
            }}
          >
            <Download size={15} /> Eksport Excel
          </button>
        </div>

        {/* Kontrolki */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginTop: '14px', flexWrap: 'wrap' }}>
          {/* View mode — segmentowana pigułka */}
          <div style={{ display: 'flex', borderRadius: '999px', border: `1px solid ${colors.gray[200]}`, backgroundColor: colors.gray[50], padding: '3px', gap: '2px' }}>
            {(['week', 'month'] as const).map((m) => (
              <button
                key={m}
                onClick={() => setViewMode(m)}
                style={{
                  padding: '6px 16px', fontSize: '12.5px', fontWeight: 700, fontFamily: 'inherit',
                  color: viewMode === m ? colors.white : colors.gray[600],
                  backgroundColor: viewMode === m ? colors.primary[500] : 'transparent',
                  border: 'none', cursor: 'pointer', borderRadius: '999px',
                  boxShadow: viewMode === m ? '0 4px 10px -3px rgba(61,109,242,0.5)' : 'none',
                  transition: 'background 0.15s ease, color 0.15s ease',
                }}
              >
                {{ week: 'Tydzień', month: 'Miesiąc' }[m]}
              </button>
            ))}
          </div>

          {/* Nav */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <NavBtn onClick={() => navigate(-1)}><ChevronLeft size={17} /></NavBtn>
            <button
              onClick={() => setCurrentDate(new Date())}
              style={{
                padding: '7px 14px', fontSize: '12px', fontWeight: 700, fontFamily: 'inherit',
                color: colors.primary[600], backgroundColor: colors.primary[100],
                border: 'none', borderRadius: '999px', cursor: 'pointer',
              }}
            >
              Dziś
            </button>
            <NavBtn onClick={() => navigate(1)}><ChevronRight size={17} /></NavBtn>
          </div>

          {/* Unit filter */}
          <select
            value={unitId}
            onChange={(e) => setUnitId(e.target.value)}
            style={{
              marginLeft: 'auto', padding: '8px 14px', fontSize: '13px', fontFamily: 'inherit',
              border: `1px solid ${colors.gray[300]}`, borderRadius: '999px', color: colors.gray[700],
              backgroundColor: colors.gray[50], minWidth: '200px', cursor: 'pointer',
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
      </div>

      {/* Legend */}
      <div style={{ display: 'flex', gap: '16px', marginBottom: '16px', fontSize: '12px', color: colors.gray[500], flexWrap: 'wrap' }}>
        <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 2, backgroundColor: colors.success[100] }} /> ≥ 8h
        </span>
        <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 2, backgroundColor: colors.warning[100] }} /> &lt; 8h
        </span>
        <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 2, backgroundColor: colors.gray[100] }} /> Brak
        </span>
        {Object.entries(ANOMALY_TYPE_LABELS).map(([type, label]) => (
          <span key={type} style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <AlertTriangle size={12} color={ANOMALY_COLORS[type]} /> {label}
          </span>
        ))}
      </div>

      {/* Loading */}
      {tsLoading && (
        <div style={{ padding: '40px', textAlign: 'center', color: colors.gray[400] }}>
          Ładowanie danych zespołu...
        </div>
      )}

      {/* Empty */}
      {!tsLoading && employees.length === 0 && (
        <div style={{
          padding: '40px', textAlign: 'center', color: colors.gray[400],
          border: `1px solid ${colors.gray[200]}`, borderRadius: '10px',
        }}>
          Brak pracowników w wybranej jednostce.
        </div>
      )}

      {/* Grid table */}
      {!tsLoading && employees.length > 0 && (
        <div style={{ overflowX: 'auto', border: `1px solid ${colors.gray[200]}`, borderRadius: '10px' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px', minWidth: dates.length * 60 + 220 }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50] }}>
                <th style={{ ...thStyle, position: 'sticky', left: 0, backgroundColor: colors.gray[50], zIndex: 1, minWidth: '180px' }}>
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
                        backgroundColor: isWeekend ? '#f1f5f9' : colors.gray[50],
                        fontSize: '11px',
                      }}
                    >
                      {formatWeekdayShort(d)}
                    </th>
                  );
                })}
                <th style={{ ...thStyle, textAlign: 'right', minWidth: '70px', backgroundColor: colors.success[50] }}>Suma</th>
              </tr>
            </thead>
            <tbody>
              {employees.map((emp) => {
                const ts = tsMap.get(emp.id);
                const dayMap = new Map<string, { netWorked: string; status: string; entries: TimeSheetEntryDto[] }>();
                if (ts) {
                  for (const day of ts.days) {
                    dayMap.set(day.date, { netWorked: day.netWorked, status: day.status, entries: day.entries ?? [] });
                  }
                }

                return (
                  <tr key={emp.id} style={{ borderTop: `1px solid ${colors.gray[200]}` }}>
                    <td style={{
                      ...tdStyle, fontWeight: 500, position: 'sticky', left: 0,
                      backgroundColor: colors.white, zIndex: 1, whiteSpace: 'nowrap',
                    }}>
                      {emp.lastName} {emp.firstName}
                    </td>
                    {dates.map((d) => {
                      const cell = dayMap.get(d);
                      const mins = cell ? durationToMinutes(cell.netWorked) : 0;
                      const cellAnomalies = anomalyMap.get(`${emp.id}:${d}`) ?? [];
                      const day = new Date(d + 'T00:00:00');
                      const isWeekend = day.getDay() === 0 || day.getDay() === 6;
                      const isEditing = editState?.employeeId === emp.id && editState?.date === d;
                      const isSaving = savingCell === `${emp.id}:${d}`;

                      let bg = isWeekend ? '#f8fafc' : colors.white;
                      if (mins >= 480) bg = colors.success[100];
                      else if (mins > 0) bg = colors.warning[100];
                      else if (!isWeekend && !cell) bg = colors.gray[100];

                      return (
                        <td
                          key={d}
                          title={cellAnomalies.map((a) => ANOMALY_TYPE_LABELS[a.type] ?? a.type).join(', ')}
                          onClick={(e) => {
                            if (!isEditing && !isSaving) startCellEdit(emp.id, d, cell?.entries ?? [], e.currentTarget);
                          }}
                          style={{
                            ...tdStyle,
                            textAlign: 'center',
                            fontVariantNumeric: 'tabular-nums',
                            backgroundColor: isEditing ? colors.primary[50] : bg,
                            padding: '4px 2px',
                            position: 'relative',
                            fontSize: '12px',
                            cursor: isSaving ? 'wait' : 'pointer',
                          }}
                        >
                          {isSaving ? '…' : (mins > 0 ? formatDuration(cell!.netWorked) : (isWeekend ? '—' : ''))}
                          {!isEditing && cellAnomalies.length > 0 && (
                            <span style={{ position: 'absolute', top: 2, right: 2 }}>
                              <AlertTriangle
                                size={10}
                                color={ANOMALY_COLORS[cellAnomalies[0]!.type] ?? colors.danger[600]}
                              />
                            </span>
                          )}
                        </td>
                      );
                    })}
                    <td style={{
                      ...tdStyle, textAlign: 'right', fontWeight: 700,
                      fontVariantNumeric: 'tabular-nums', backgroundColor: colors.success[50],
                      color: colors.emerald[600],
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

      {/* Panel edycji dnia — portal do document.body, żeby nie był przycinany przez overflow tabeli */}
      {editState && panelPos && createPortal(
        <div
          ref={panelRef}
          onClick={(e) => e.stopPropagation()}
          onKeyDown={(e) => { if (e.key === 'Escape') cancelCellEdit(); }}
          style={{
            position: 'fixed',
            top: panelPos.top,
            left: panelPos.left,
            zIndex: 1000,
            width: '280px',
            boxSizing: 'border-box',
            backgroundColor: colors.white,
            border: `1px solid ${colors.gray[200]}`,
            borderRadius: '12px',
            boxShadow: '0 10px 30px -8px rgba(20,25,43,0.35)',
            padding: '12px',
            textAlign: 'left',
            fontSize: '13px',
          }}
        >
          <div style={{ display: 'flex', gap: '8px', marginBottom: '10px' }}>
            <label style={{ flex: 1, fontSize: '11px', fontWeight: 600, color: colors.gray[500] }}>
              Od
              <input
                ref={startInputRef}
                type="time"
                value={editState.start}
                onChange={(e) => setEditState((prev) => (prev ? { ...prev, start: e.target.value } : prev))}
                style={timeInputStyle}
              />
            </label>
            <label style={{ flex: 1, fontSize: '11px', fontWeight: 600, color: colors.gray[500] }}>
              Do
              <input
                type="time"
                value={editState.end}
                onChange={(e) => setEditState((prev) => (prev ? { ...prev, end: e.target.value } : prev))}
                style={timeInputStyle}
              />
            </label>
          </div>

          {editState.breaks.map((b, idx) => (
            <div key={idx} style={{ marginBottom: '8px', padding: '6px', backgroundColor: colors.gray[50], borderRadius: '8px' }}>
              <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                <input
                  type="time"
                  value={b.start}
                  onChange={(e) => updateBreakRow(idx, 'start', e.target.value)}
                  style={{ ...timeInputStyle, marginTop: 0, flex: 1 }}
                />
                <span style={{ color: colors.gray[400], fontSize: '11px' }}>–</span>
                <input
                  type="time"
                  value={b.end}
                  onChange={(e) => updateBreakRow(idx, 'end', e.target.value)}
                  style={{ ...timeInputStyle, marginTop: 0, flex: 1 }}
                />
                <button
                  onClick={() => removeBreakRow(idx)}
                  title="Usuń przerwę"
                  style={{
                    border: 'none', background: 'none', cursor: 'pointer',
                    color: colors.danger[500], fontSize: '14px', padding: '0 2px',
                  }}
                >
                  ×
                </button>
              </div>
              <select
                value={b.breakType}
                onChange={(e) => updateBreakRow(idx, 'breakType', e.target.value)}
                style={{
                  width: '100%', marginTop: '4px', fontSize: '11px', fontFamily: 'inherit',
                  padding: '3px 4px', border: `1px solid ${colors.gray[200]}`, borderRadius: '6px',
                  color: colors.gray[600], backgroundColor: colors.white,
                }}
              >
                {BREAK_TYPE_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
          ))}

          <button
            onClick={addBreakRow}
            style={{
              display: 'block', width: '100%', textAlign: 'left',
              border: 'none', background: 'none', cursor: 'pointer',
              color: colors.primary[600], fontSize: '12px', fontWeight: 600,
              padding: '4px 0', marginBottom: '8px',
            }}
          >
            + Dodaj przerwę
          </button>

          <div style={{ display: 'flex', gap: '8px', marginTop: '4px' }}>
            <button
              onClick={() => saveCellEdit()}
              style={{
                flex: 1, padding: '7px 0', fontSize: '12px', fontWeight: 700, fontFamily: 'inherit',
                color: colors.white, backgroundColor: colors.primary[500],
                border: 'none', borderRadius: '8px', cursor: 'pointer',
              }}
            >
              Zapisz
            </button>
            <button
              onClick={cancelCellEdit}
              style={{
                flex: 1, padding: '7px 0', fontSize: '12px', fontWeight: 700, fontFamily: 'inherit',
                color: colors.gray[600], backgroundColor: colors.gray[100],
                border: 'none', borderRadius: '8px', cursor: 'pointer',
              }}
            >
              Anuluj
            </button>
          </div>
        </div>,
        document.body,
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
        width: '32px', height: '32px', border: `1px solid ${colors.gray[200]}`,
        borderRadius: '10px', backgroundColor: colors.white, cursor: 'pointer', color: colors.gray[700],
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
  color: colors.gray[500],
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
};

const tdStyle: React.CSSProperties = {
  padding: '8px',
  color: colors.gray[700],
};

const timeInputStyle: React.CSSProperties = {
  display: 'block',
  width: '100%',
  boxSizing: 'border-box',
  marginTop: '4px',
  fontSize: '13px',
  fontFamily: 'inherit',
  padding: '5px 6px',
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px',
  color: colors.gray[800],
};
