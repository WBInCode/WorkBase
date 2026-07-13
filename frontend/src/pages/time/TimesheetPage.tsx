import { useState, useMemo } from 'react';
import { ChevronLeft, ChevronRight, Clock, Coffee, Briefcase, AlertCircle, Play, Square, ChevronDown, ChevronUp } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useTimesheet, type TimesheetFilter } from '@/api/hooks/useTimeTracking';
import type { TimeSheetDayDto, TimeSheetEntryDto } from '@/api/types/time';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

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
  complete: { bg: colors.success[100], text: colors.success[800], label: 'Kompletny' },
  approved: { bg: colors.primary[100], text: colors.primary[800], label: 'Zatwierdzony' },
  incomplete: { bg: colors.warning[100], text: colors.warning[800], label: 'Niekompletny' },
  empty: { bg: colors.gray[100], text: colors.gray[500], label: 'Brak danych' },
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
      <div style={{ padding: '32px', textAlign: 'center', color: colors.gray[500] }}>
        Brak przypisanego profilu pracownika.
      </div>
    );
  }

  return (
    <div style={{ padding: mobile ? '14px' : '24px 28px', maxWidth: '1240px', margin: '0 auto' }}>
      {/* ── Karta dowodzenia: tytuł + przełącznik okresu + nawigacja ── */}
      <div
        style={{
          backgroundColor: colors.white,
          border: `1px solid ${colors.gray[200]}`,
          borderRadius: '20px',
          boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
          padding: mobile ? '16px' : '18px 22px',
          marginBottom: '18px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '14px',
          flexWrap: 'wrap',
        }}
      >
        <div>
          <h1 style={{ fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900], margin: 0 }}>
            Karta czasu pracy
          </h1>
          <p style={{ margin: '3px 0 0', fontSize: '13.5px', fontWeight: 600, color: colors.primary[600], textTransform: 'capitalize' }}>
            {periodLabel}
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap' }}>
          {/* Period switcher — segmentowana pigułka */}
          <div style={{ display: 'flex', borderRadius: '999px', border: `1px solid ${colors.gray[200]}`, backgroundColor: colors.gray[50], padding: '3px', gap: '2px' }}>
            {(['day', 'week', 'month'] as PeriodType[]).map((p) => (
              <button
                key={p}
                onClick={() => setPeriod(p)}
                style={{
                  padding: '6px 16px', fontSize: '12.5px', fontWeight: 700, fontFamily: 'inherit',
                  color: period === p ? colors.white : colors.gray[600],
                  backgroundColor: period === p ? colors.primary[500] : 'transparent',
                  border: 'none', cursor: 'pointer', borderRadius: '999px',
                  boxShadow: period === p ? '0 4px 10px -3px rgba(61,109,242,0.5)' : 'none',
                  transition: 'background 0.15s ease, color 0.15s ease',
                }}
              >
                {{ day: 'Dzień', week: 'Tydzień', month: 'Miesiąc' }[p]}
              </button>
            ))}
          </div>

          {/* Navigation */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <NavButton onClick={() => navigate(-1)} icon={<ChevronLeft size={17} />} />
            <button
              onClick={goToToday}
              style={{
                padding: '7px 14px', fontSize: '12px', fontWeight: 700, fontFamily: 'inherit',
                color: colors.primary[600], backgroundColor: colors.primary[100],
                border: 'none', borderRadius: '999px', cursor: 'pointer',
              }}
            >
              Dziś
            </button>
            <NavButton onClick={() => navigate(1)} icon={<ChevronRight size={17} />} />
          </div>
        </div>
      </div>

      {/* Summary cards */}
      {data && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(170px, 1fr))', gap: '12px', marginBottom: '18px' }}>
          <SummaryCard icon={<Briefcase size={17} />} label="Czas pracy" value={formatDuration(data.totalWorked)} fg={colors.primary[600]} bg={colors.primary[100]} />
          <SummaryCard icon={<Coffee size={17} />} label="Przerwy" value={formatDuration(data.totalBreaks)} fg={colors.warning[700]} bg={colors.warning[100]} />
          <SummaryCard icon={<Clock size={17} />} label="Netto" value={formatDuration(data.netWorked)} fg="#047857" bg="#d1fae5" />
          <SummaryCard icon={<AlertCircle size={17} />} label="Dni przepracowane" value={`${data.daysWorked} / ${data.daysWorked + data.daysIncomplete}`} fg={colors.primary[600]} bg={colors.primary[100]} />
        </div>
      )}

      {/* Loading / Error */}
      {isLoading && (
        <div style={{ padding: '40px', textAlign: 'center', color: colors.gray[400] }}>
          Ładowanie danych...
        </div>
      )}

      {error && (
        <div style={{ padding: '16px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, borderRadius: '12px', color: colors.danger[600], fontSize: '14px' }}>
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

function SummaryCard({ icon, label, value, fg, bg }: { icon: React.ReactNode; label: string; value: string; fg: string; bg: string }) {
  return (
    <div style={{
      padding: '15px 17px', borderRadius: '18px', border: `1px solid ${colors.gray[200]}`,
      backgroundColor: colors.white,
      boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 26px -14px rgba(20,25,43,0.12)',
      display: 'flex', alignItems: 'center', gap: '12px', minWidth: 0,
    }}>
      <span style={{
        width: 38, height: 38, borderRadius: 12, backgroundColor: bg, color: fg,
        display: 'inline-flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
      }}>{icon}</span>
      <span style={{ minWidth: 0 }}>
        <span className="wb-tnum" style={{ display: 'block', fontSize: '21px', fontWeight: 800, color: colors.gray[900], letterSpacing: '-0.02em', lineHeight: 1.2 }}>
          {value}
        </span>
        <span style={{ display: 'block', fontSize: '11.5px', color: colors.gray[500], fontWeight: 600 }}>{label}</span>
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
        width: '34px', height: '34px', border: `1px solid ${colors.gray[200]}`,
        borderRadius: '999px', backgroundColor: colors.white, cursor: 'pointer', color: colors.gray[700],
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
      display: 'inline-block', padding: '2px 10px', borderRadius: '999px',
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
      <div style={{ padding: '40px', textAlign: 'center', color: colors.gray[400], border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', backgroundColor: colors.white }}>
        Brak danych za wybrany dzień.
      </div>
    );
  }

  return (
    <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', overflow: 'hidden', boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)' }}>
      <div style={{
        padding: '16px 20px', backgroundColor: colors.gray[50], borderBottom: `1px solid ${colors.gray[200]}`,
        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
      }}>
        <span style={{ fontSize: '15px', fontWeight: 600, color: colors.gray[900] }}>
          {formatDateLong(day.date)}
        </span>
        <StatusBadge status={day.status} />
      </div>

      <div style={{ padding: '20px', display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))', gap: '20px' }}>
        <DetailRow label="Czas pracy brutto" value={formatDuration(day.totalWorked)} color={colors.primary[500]} />
        <DetailRow label="Przerwy" value={formatDuration(day.totalBreaks)} color={colors.warning[500]} />
        <DetailRow label="Czas netto" value={formatDuration(day.netWorked)} color={colors.emerald[600]} />
      </div>

      {/* Timeline */}
      {day.entries && day.entries.length > 0 && (
        <div style={{ padding: '0 20px 20px' }}>
          <div style={{ fontSize: '12px', fontWeight: 600, color: colors.gray[500], textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '12px' }}>
            Rejestr zdarzeń
          </div>
          <EntryTimeline entries={day.entries} />
        </div>
      )}

      {day.note && (
        <div style={{ padding: '12px 20px', borderTop: `1px solid ${colors.gray[200]}`, fontSize: '13px', color: colors.gray[500] }}>
          <strong>Notatka:</strong> {day.note}
        </div>
      )}
    </div>
  );
}

function DetailRow({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div>
      <div style={{ fontSize: '12px', color: colors.gray[500], marginBottom: '4px' }}>{label}</div>
      <div style={{ fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color, fontVariantNumeric: 'tabular-nums' }}>{value}</div>
    </div>
  );
}

/* Week View: table with row per day */
function WeekView({ days }: { days: TimeSheetDayDto[] }) {
  const [expandedDate, setExpandedDate] = useState<string | null>(null);

  if (days.length === 0) {
    return (
      <div style={{ padding: '40px', textAlign: 'center', color: colors.gray[400], border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', backgroundColor: colors.white }}>
        Brak danych za wybrany tydzień.
      </div>
    );
  }

  return (
    <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', overflowX: 'auto', backgroundColor: colors.white, boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
        <thead>
          <tr style={{ backgroundColor: colors.gray[50] }}>
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
              <tr key={day.date} style={{ borderTop: `1px solid ${colors.gray[200]}` }}>
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
                        ? <ChevronUp size={14} color={colors.gray[400]} />
                        : <ChevronDown size={14} color={colors.gray[400]} />
                      )}
                      {formatDate(day.date)}
                    </span>
                    <span style={{ ...tdStyle, textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                      {formatDuration(day.totalWorked)}
                    </span>
                    <span style={{ ...tdStyle, textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: colors.warning[800] }}>
                      {formatDuration(day.totalBreaks)}
                    </span>
                    <span style={{
                      ...tdStyle, textAlign: 'right', fontWeight: 600, fontVariantNumeric: 'tabular-nums',
                      color: isLow ? colors.danger[600] : colors.emerald[600],
                    }}>
                      {formatDuration(day.netWorked)}
                    </span>
                    <span style={{ ...tdStyle, textAlign: 'center' }}>
                      <StatusBadge status={day.status} />
                    </span>
                  </div>
                  {isExpanded && hasEntries && (
                    <div style={{ padding: '8px 20px 14px 32px', backgroundColor: colors.gray[50], borderTop: `1px solid ${colors.gray[100]}` }}>
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
      <div style={{ padding: '40px', textAlign: 'center', color: colors.gray[400], border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', backgroundColor: colors.white }}>
        Brak danych za wybrany miesiąc.
      </div>
    );
  }

  return (
    <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', overflowX: 'auto', backgroundColor: colors.white, boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
        <thead>
          <tr style={{ backgroundColor: colors.gray[50] }}>
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
                  borderTop: `1px solid ${colors.gray[100]}`,
                  backgroundColor: isEmpty ? '#fafafa' : undefined,
                }}
              >
                <td style={{ ...tdStyle, padding: '6px 16px' }}>
                  <span style={{ fontWeight: 500, fontSize: '13px' }}>{formatDate(day.date)}</span>
                </td>
                <td style={{ ...tdStyle, padding: '6px 16px', textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: isEmpty ? colors.gray[300] : colors.gray[700] }}>
                  {formatDuration(day.totalWorked)}
                </td>
                <td style={{
                  ...tdStyle, padding: '6px 16px', textAlign: 'right', fontWeight: 600, fontVariantNumeric: 'tabular-nums',
                  color: isEmpty ? colors.gray[300] : (netMins < 480 && netMins > 0 ? colors.danger[600] : colors.emerald[600]),
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
  ClockIn: { label: 'Rozpoczęcie pracy', color: colors.emerald[600], icon: Play },
  ClockOut: { label: 'Zakończenie pracy', color: colors.danger[600], icon: Square },
  BreakStart: { label: 'Rozpoczęcie przerwy', color: colors.warning[600], icon: Coffee },
  BreakEnd: { label: 'Zakończenie przerwy', color: colors.emerald[600], icon: Coffee },
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
                border: `2px solid ${colors.white}`, boxShadow: `0 0 0 2px ${cfg.color}33`,
              }} />
              {!isLast && (
                <div style={{ width: '2px', flex: 1, backgroundColor: colors.gray[200], minHeight: '16px' }} />
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
              <div style={{ fontSize: '12px', color: colors.gray[500], marginTop: '1px' }}>
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
  color: colors.gray[500],
  textTransform: 'uppercase',
  letterSpacing: '0.05em',
};

const tdStyle: React.CSSProperties = {
  padding: '10px 16px',
  color: colors.gray[700],
};
