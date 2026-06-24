import { useState, useMemo } from 'react';
import { ChevronLeft, ChevronRight, Clock, Coffee, Briefcase, AlertCircle, Play, Square, ChevronDown, ChevronUp } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useTimesheet, type TimesheetFilter } from '@/api/hooks/useTimeTracking';
import type { TimeSheetDayDto, TimeSheetEntryDto } from '@/api/types/time';
import { useIsMobile } from '@/shared';

type PeriodType = 'day' | 'week' | 'month';

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

function formatDate(dateStr: string): string {
  const date = new Date(dateStr + 'T00:00:00');
  return date.toLocaleDateString('pl-PL', { weekday: 'short', day: 'numeric', month: 'short' });
}

function formatDateLong(dateStr: string): string {
  const date = new Date(dateStr + 'T00:00:00');
  return date.toLocaleDateString('pl-PL', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
}

function toDateString(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function getWeekRange(date: Date): { from: string; to: string } {
  const d = new Date(date);
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day; // Monday start
  const monday = new Date(d);
  monday.setDate(d.getDate() + diff);
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  return { from: toDateString(monday), to: toDateString(sunday) };
}

function getMonthRange(date: Date): { from: string; to: string } {
  const from = new Date(date.getFullYear(), date.getMonth(), 1);
  const to = new Date(date.getFullYear(), date.getMonth() + 1, 0);
  return { from: toDateString(from), to: toDateString(to) };
}

const STATUS_STYLES: Record<string, { bg: string; text: string; label: string }> = {
  complete: { bg: '#dcfce7', text: '#166534', label: 'Kompletny' },
  approved: { bg: '#dbeafe', text: '#1e40af', label: 'Zatwierdzony' },
  incomplete: { bg: '#fef3c7', text: '#92400e', label: 'Niekompletny' },
  empty: { bg: '#f3f4f6', text: '#6b7280', label: 'Brak danych' },
};

export function TimesheetPage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const employeeId = user?.employeeId ?? '';

  const [period, setPeriod] = useState<PeriodType>('week');
  const [currentDate, setCurrentDate] = useState(() => new Date());

  const dateRange = useMemo(() => {
    switch (period) {
      case 'day':
        return { from: toDateString(currentDate), to: toDateString(currentDate) };
      case 'week':
        return getWeekRange(currentDate);
      case 'month':
        return getMonthRange(currentDate);
    }
  }, [period, currentDate]);

  const filter: TimesheetFilter = {
    employeeId,
    from: dateRange.from,
    to: dateRange.to,
    period,
  };

  const { data, isLoading, error } = useTimesheet(filter);
  const mobile = useIsMobile();

  const navigate = (direction: number) => {
    const d = new Date(currentDate);
    switch (period) {
      case 'day':
        d.setDate(d.getDate() + direction);
        break;
      case 'week':
        d.setDate(d.getDate() + direction * 7);
        break;
      case 'month':
        d.setMonth(d.getMonth() + direction);
        break;
    }
    setCurrentDate(d);
  };

  const goToToday = () => setCurrentDate(new Date());

  const periodLabel = useMemo(() => {
    switch (period) {
      case 'day':
        return formatDateLong(dateRange.from);
      case 'week':
        return `${formatDate(dateRange.from)} – ${formatDate(dateRange.to)}`;
      case 'month': {
        const d = new Date(dateRange.from + 'T00:00:00');
        return d.toLocaleDateString('pl-PL', { month: 'long', year: 'numeric' });
      }
    }
  }, [period, dateRange]);

  if (!employeeId) {
    return (
      <div style={{ padding: '32px', textAlign: 'center', color: '#6b7280' }}>
        Brak przypisanego profilu pracownika.
      </div>
    );
  }

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '1100px' }}>
      {/* Header */}
      <h1 style={{ fontSize: '22px', fontWeight: 700, color: '#111827', margin: '0 0 20px' }}>
        Karta czasu pracy
      </h1>

      {/* Controls bar */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '20px',
        flexWrap: 'wrap',
      }}>
        {/* Period switcher */}
        <div style={{ display: 'flex', borderRadius: '8px', border: '1px solid #e5e7eb', overflow: 'hidden' }}>
          {(['day', 'week', 'month'] as PeriodType[]).map((p) => (
            <button
              key={p}
              onClick={() => setPeriod(p)}
              style={{
                padding: '6px 16px', fontSize: '13px', fontWeight: period === p ? 600 : 400,
                color: period === p ? '#ffffff' : '#374151',
                backgroundColor: period === p ? '#3b82f6' : '#ffffff',
                border: 'none', cursor: 'pointer',
                borderRight: p !== 'month' ? '1px solid #e5e7eb' : undefined,
              }}
            >
              {{ day: 'Dzień', week: 'Tydzień', month: 'Miesiąc' }[p]}
            </button>
          ))}
        </div>

        {/* Navigation */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
          <NavButton onClick={() => navigate(-1)} icon={<ChevronLeft size={18} />} />
          <button
            onClick={goToToday}
            style={{
              padding: '6px 12px', fontSize: '12px', fontWeight: 500,
              color: '#3b82f6', backgroundColor: '#eff6ff',
              border: '1px solid #bfdbfe', borderRadius: '6px', cursor: 'pointer',
            }}
          >
            Dziś
          </button>
          <NavButton onClick={() => navigate(1)} icon={<ChevronRight size={18} />} />
        </div>

        {/* Period label */}
        <span style={{ fontSize: '15px', fontWeight: 600, color: '#374151', textTransform: 'capitalize' }}>
          {periodLabel}
        </span>
      </div>

      {/* Summary cards */}
      {data && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: '12px', marginBottom: '24px' }}>
          <SummaryCard icon={<Briefcase size={18} />} label="Czas pracy" value={formatDuration(data.totalWorked)} color="#3b82f6" />
          <SummaryCard icon={<Coffee size={18} />} label="Przerwy" value={formatDuration(data.totalBreaks)} color="#f59e0b" />
          <SummaryCard icon={<Clock size={18} />} label="Netto" value={formatDuration(data.netWorked)} color="#059669" />
          <SummaryCard icon={<AlertCircle size={18} />} label="Dni" value={`${data.daysWorked} / ${data.daysWorked + data.daysIncomplete}`} color="#6366f1" />
        </div>
      )}

      {/* Loading / Error */}
      {isLoading && (
        <div style={{ padding: '40px', textAlign: 'center', color: '#9ca3af' }}>
          Ładowanie danych...
        </div>
      )}

      {error && (
        <div style={{ padding: '16px', backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: '8px', color: '#dc2626', fontSize: '14px' }}>
          Błąd ładowania danych: {error.message}
        </div>
      )}

      {/* Day view */}
      {data && period === 'day' && <DayView day={data.days[0]} />}

      {/* Week view */}
      {data && period === 'week' && <WeekView days={data.days} />}

      {/* Month view */}
      {data && period === 'month' && <MonthView days={data.days} />}
    </div>
  );
}

/* === Sub-components === */

function SummaryCard({ icon, label, value, color }: { icon: React.ReactNode; label: string; value: string; color: string }) {
  return (
    <div style={{
      padding: '14px 16px', borderRadius: '10px', border: '1px solid #e5e7eb',
      backgroundColor: '#ffffff',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '6px' }}>
        <span style={{ color }}>{icon}</span>
        <span style={{ fontSize: '12px', color: '#6b7280', fontWeight: 500 }}>{label}</span>
      </div>
      <span style={{ fontSize: '20px', fontWeight: 700, color: '#111827', fontVariantNumeric: 'tabular-nums' }}>
        {value}
      </span>
    </div>
  );
}

function NavButton({ onClick, icon }: { onClick: () => void; icon: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      style={{
        display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
        width: '32px', height: '32px', border: '1px solid #e5e7eb',
        borderRadius: '6px', backgroundColor: '#ffffff', cursor: 'pointer', color: '#374151',
      }}
    >
      {icon}
    </button>
  );
}

function StatusBadge({ status }: { status: string }) {
  const s = STATUS_STYLES[status] ?? STATUS_STYLES.empty;
  if (!s) return null;
  return (
    <span style={{
      display: 'inline-block', padding: '2px 8px', borderRadius: '10px',
      fontSize: '11px', fontWeight: 600, backgroundColor: s.bg, color: s.text,
    }}>
      {s.label}
    </span>
  );
}

/* Day View: detailed single-day info */
function DayView({ day }: { day?: TimeSheetDayDto }) {
  if (!day) {
    return (
      <div style={{ padding: '40px', textAlign: 'center', color: '#9ca3af', border: '1px solid #e5e7eb', borderRadius: '10px' }}>
        Brak danych za wybrany dzień.
      </div>
    );
  }

  return (
    <div style={{ border: '1px solid #e5e7eb', borderRadius: '10px', overflow: 'hidden' }}>
      <div style={{
        padding: '16px 20px', backgroundColor: '#f9fafb', borderBottom: '1px solid #e5e7eb',
        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
      }}>
        <span style={{ fontSize: '15px', fontWeight: 600, color: '#111827' }}>
          {formatDateLong(day.date)}
        </span>
        <StatusBadge status={day.status} />
      </div>

      <div style={{ padding: '20px', display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))', gap: '20px' }}>
        <DetailRow label="Czas pracy brutto" value={formatDuration(day.totalWorked)} color="#3b82f6" />
        <DetailRow label="Przerwy" value={formatDuration(day.totalBreaks)} color="#f59e0b" />
        <DetailRow label="Czas netto" value={formatDuration(day.netWorked)} color="#059669" />
      </div>

      {/* Timeline */}
      {day.entries && day.entries.length > 0 && (
        <div style={{ padding: '0 20px 20px' }}>
          <div style={{ fontSize: '12px', fontWeight: 600, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '12px' }}>
            Rejestr zdarzeń
          </div>
          <EntryTimeline entries={day.entries} />
        </div>
      )}

      {day.note && (
        <div style={{ padding: '12px 20px', borderTop: '1px solid #e5e7eb', fontSize: '13px', color: '#6b7280' }}>
          <strong>Notatka:</strong> {day.note}
        </div>
      )}
    </div>
  );
}

function DetailRow({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div>
      <div style={{ fontSize: '12px', color: '#6b7280', marginBottom: '4px' }}>{label}</div>
      <div style={{ fontSize: '22px', fontWeight: 700, color, fontVariantNumeric: 'tabular-nums' }}>{value}</div>
    </div>
  );
}

/* Week View: table with row per day */
function WeekView({ days }: { days: TimeSheetDayDto[] }) {
  const [expandedDate, setExpandedDate] = useState<string | null>(null);

  if (days.length === 0) {
    return (
      <div style={{ padding: '40px', textAlign: 'center', color: '#9ca3af', border: '1px solid #e5e7eb', borderRadius: '10px' }}>
        Brak danych za wybrany tydzień.
      </div>
    );
  }

  return (
    <div style={{ border: '1px solid #e5e7eb', borderRadius: '10px', overflowX: 'auto' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
        <thead>
          <tr style={{ backgroundColor: '#f9fafb' }}>
            <th style={thStyle}>Dzień</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>Czas pracy</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>Przerwy</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>Netto</th>
            <th style={{ ...thStyle, textAlign: 'center' }}>Status</th>
          </tr>
        </thead>
        <tbody>
          {days.map((day) => {
            const netMins = durationToMinutes(day.netWorked);
            const isLow = netMins > 0 && netMins < 480;
            const hasEntries = day.entries && day.entries.length > 0;
            const isExpanded = expandedDate === day.date;
            return (
              <tr key={day.date} style={{ borderTop: '1px solid #e5e7eb' }}>
                <td colSpan={5} style={{ padding: 0 }}>
                  <div
                    onClick={() => hasEntries && setExpandedDate(isExpanded ? null : day.date)}
                    style={{
                      display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr auto',
                      alignItems: 'center', cursor: hasEntries ? 'pointer' : 'default',
                    }}
                  >
                    <span style={{ ...tdStyle, fontWeight: 500, display: 'flex', alignItems: 'center', gap: '4px' }}>
                      {hasEntries && (isExpanded
                        ? <ChevronUp size={14} color="#9ca3af" />
                        : <ChevronDown size={14} color="#9ca3af" />
                      )}
                      {formatDate(day.date)}
                    </span>
                    <span style={{ ...tdStyle, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                      {formatDuration(day.totalWorked)}
                    </span>
                    <span style={{ ...tdStyle, textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: '#92400e' }}>
                      {formatDuration(day.totalBreaks)}
                    </span>
                    <span style={{
                      ...tdStyle, textAlign: 'right', fontWeight: 600, fontVariantNumeric: 'tabular-nums',
                      color: isLow ? '#dc2626' : '#059669',
                    }}>
                      {formatDuration(day.netWorked)}
                    </span>
                    <span style={{ ...tdStyle, textAlign: 'center' }}>
                      <StatusBadge status={day.status} />
                    </span>
                  </div>
                  {isExpanded && hasEntries && (
                    <div style={{ padding: '8px 20px 14px 32px', backgroundColor: '#f9fafb', borderTop: '1px solid #f3f4f6' }}>
                      <EntryTimeline entries={day.entries} />
                    </div>
                  )}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

/* Month View: compact grid-style overview */
function MonthView({ days }: { days: TimeSheetDayDto[] }) {
  if (days.length === 0) {
    return (
      <div style={{ padding: '40px', textAlign: 'center', color: '#9ca3af', border: '1px solid #e5e7eb', borderRadius: '10px' }}>
        Brak danych za wybrany miesiąc.
      </div>
    );
  }

  return (
    <div style={{ border: '1px solid #e5e7eb', borderRadius: '10px', overflowX: 'auto' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
        <thead>
          <tr style={{ backgroundColor: '#f9fafb' }}>
            <th style={thStyle}>Data</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>Brutto</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>Netto</th>
            <th style={{ ...thStyle, textAlign: 'center' }}>Status</th>
          </tr>
        </thead>
        <tbody>
          {days.map((day) => {
            const netMins = durationToMinutes(day.netWorked);
            const isEmpty = netMins === 0;
            return (
              <tr
                key={day.date}
                style={{
                  borderTop: '1px solid #f3f4f6',
                  backgroundColor: isEmpty ? '#fafafa' : undefined,
                }}
              >
                <td style={{ ...tdStyle, padding: '6px 16px' }}>
                  <span style={{ fontWeight: 500, fontSize: '13px' }}>{formatDate(day.date)}</span>
                </td>
                <td style={{ ...tdStyle, padding: '6px 16px', textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: isEmpty ? '#d1d5db' : '#374151' }}>
                  {formatDuration(day.totalWorked)}
                </td>
                <td style={{
                  ...tdStyle, padding: '6px 16px', textAlign: 'right', fontWeight: 600, fontVariantNumeric: 'tabular-nums',
                  color: isEmpty ? '#d1d5db' : (netMins < 480 && netMins > 0 ? '#dc2626' : '#059669'),
                }}>
                  {formatDuration(day.netWorked)}
                </td>
                <td style={{ ...tdStyle, padding: '6px 16px', textAlign: 'center' }}>
                  <StatusBadge status={isEmpty ? 'empty' : day.status} />
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

const ENTRY_CONFIG: Record<string, { label: string; color: string; icon: typeof Play }> = {
  ClockIn: { label: 'Rozpoczęcie pracy', color: '#059669', icon: Play },
  ClockOut: { label: 'Zakończenie pracy', color: '#dc2626', icon: Square },
  BreakStart: { label: 'Rozpoczęcie przerwy', color: '#d97706', icon: Coffee },
  BreakEnd: { label: 'Zakończenie przerwy', color: '#059669', icon: Coffee },
};

const BREAK_TYPE_LABELS: Record<string, string> = {
  Paid: 'płatna',
  Unpaid: 'bezpłatna',
};

function formatEntryTime(isoString: string): string {
  return new Date(isoString).toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function EntryTimeline({ entries }: { entries: TimeSheetEntryDto[] }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '0' }}>
      {entries.map((entry, idx) => {
        const cfg = ENTRY_CONFIG[entry.type];
        if (!cfg) return null;
        const Icon = cfg.icon;
        const isLast = idx === entries.length - 1;
        const breakLabel = entry.breakType ? ` (${BREAK_TYPE_LABELS[entry.breakType] ?? entry.breakType})` : '';
        return (
          <div key={idx} style={{ display: 'flex', alignItems: 'stretch', gap: '12px' }}>
            {/* Timeline line + dot */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', width: '20px' }}>
              <div style={{
                width: '10px', height: '10px', borderRadius: '50%', flexShrink: 0,
                backgroundColor: cfg.color, marginTop: '5px',
                border: '2px solid #fff', boxShadow: `0 0 0 2px ${cfg.color}33`,
              }} />
              {!isLast && (
                <div style={{ width: '2px', flex: 1, backgroundColor: '#e5e7eb', minHeight: '16px' }} />
              )}
            </div>
            {/* Content */}
            <div style={{ paddingBottom: isLast ? 0 : '12px', flex: 1 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                <Icon size={13} color={cfg.color} />
                <span style={{ fontSize: '13px', fontWeight: 600, color: cfg.color }}>
                  {formatEntryTime(entry.entryTime)}
                </span>
              </div>
              <div style={{ fontSize: '12px', color: '#6b7280', marginTop: '1px' }}>
                {cfg.label}{breakLabel}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

const thStyle: React.CSSProperties = {
  padding: '10px 16px',
  textAlign: 'left',
  fontSize: '12px',
  fontWeight: 600,
  color: '#6b7280',
  textTransform: 'uppercase',
  letterSpacing: '0.05em',
};

const tdStyle: React.CSSProperties = {
  padding: '10px 16px',
  color: '#374151',
};
