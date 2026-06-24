import { useState, useMemo } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useLeaveCalendar, useLeaveTypes } from '@/api/hooks/useLeave';
import { useEmployees } from '@/api/hooks/useOrganization';
import type { LeaveCalendarEntryDto } from '@/api/types/leave';
import { useIsMobile } from '@/shared';

function getMonthDays(year: number, month: number): Date[] {
  const days: Date[] = [];
  const date = new Date(year, month, 1);
  while (date.getMonth() === month) {
    days.push(new Date(date));
    date.setDate(date.getDate() + 1);
  }
  return days;
}

function isWeekend(date: Date): boolean {
  const d = date.getDay();
  return d === 0 || d === 6;
}

function formatDateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

const DAY_NAMES = ['Pn', 'Wt', 'Śr', 'Cz', 'Pt', 'So', 'Nd'];

const DEFAULT_COLORS: Record<string, string> = {
  ANNUAL: '#3b82f6',
  ON_DEMAND: '#f59e0b',
  SICK: '#ef4444',
  CHILDCARE: '#8b5cf6',
};

export function LeaveCalendarPage() {
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;

  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth());

  // Load employees from the same org
  const { data: employeesPage } = useEmployees({
    page: 1,
    pageSize: 200,
  });
  const employees = employeesPage?.items ?? [];
  const mobile = useIsMobile();

  const { data: leaveTypes = [] } = useLeaveTypes();

  const days = useMemo(() => getMonthDays(year, month), [year, month]);

  const from = `${year}-${String(month + 1).padStart(2, '0')}-01`;
  const lastDay = new Date(year, month + 1, 0).getDate();
  const to = `${year}-${String(month + 1).padStart(2, '0')}-${String(lastDay).padStart(2, '0')}`;

  const employeeIds = employees.map((e) => e.id);

  const { data: calendarEntries = [], isLoading } = useLeaveCalendar(
    employeeIds.length > 0 ? { employeeIds, from, to } : null,
  );

  // Index: employeeId -> dateKey -> entries
  const entryMap = useMemo(() => {
    const map = new Map<string, Map<string, LeaveCalendarEntryDto[]>>();
    for (const entry of calendarEntries) {
      if (!map.has(entry.employeeId)) map.set(entry.employeeId, new Map());
      const dateKey = entry.date.split('T')[0] ?? '';
      const dayMap = map.get(entry.employeeId)!;
      if (!dayMap.has(dateKey)) dayMap.set(dateKey, []);
      dayMap.get(dateKey)!.push(entry);
    }
    return map;
  }, [calendarEntries]);

  const prevMonth = () => {
    if (month === 0) {
      setMonth(11);
      setYear(year - 1);
    } else {
      setMonth(month - 1);
    }
  };

  const nextMonth = () => {
    if (month === 11) {
      setMonth(0);
      setYear(year + 1);
    } else {
      setMonth(month + 1);
    }
  };

  const goToday = () => {
    setYear(now.getFullYear());
    setMonth(now.getMonth());
  };

  const monthLabel = new Date(year, month).toLocaleDateString('pl-PL', {
    year: 'numeric',
    month: 'long',
  });

  return (
    <div style={{ padding: mobile ? '16px' : '24px' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: '20px',
        }}
      >
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: '#111827' }}>
            Kalendarz nieobecności
          </h1>
          <p style={{ margin: '4px 0 0', fontSize: '14px', color: '#6b7280' }}>
            Przegląd nieobecności zespołu
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <button onClick={prevMonth} style={navBtnStyle} aria-label="Poprzedni miesiąc">
            <ChevronLeft size={18} />
          </button>
          <span
            style={{
              minWidth: '160px',
              textAlign: 'center',
              fontSize: '15px',
              fontWeight: 600,
              color: '#111827',
              textTransform: 'capitalize',
            }}
          >
            {monthLabel}
          </span>
          <button onClick={nextMonth} style={navBtnStyle} aria-label="Następny miesiąc">
            <ChevronRight size={18} />
          </button>
          <button
            onClick={goToday}
            style={{
              padding: '6px 12px',
              fontSize: '13px',
              fontWeight: 500,
              color: '#374151',
              backgroundColor: '#fff',
              border: '1px solid #d1d5db',
              borderRadius: '6px',
              cursor: 'pointer',
              marginLeft: '4px',
            }}
          >
            Dziś
          </button>
        </div>
      </div>

      {/* Legend */}
      <div style={{ display: 'flex', gap: '16px', marginBottom: '16px', flexWrap: 'wrap' }}>
        {leaveTypes.map((t) => (
          <div key={t.id} style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <div
              style={{
                width: '12px',
                height: '12px',
                borderRadius: '3px',
                backgroundColor: t.color ?? DEFAULT_COLORS[t.code] ?? '#6b7280',
              }}
            />
            <span style={{ fontSize: '12px', color: '#6b7280' }}>{t.name}</span>
          </div>
        ))}
      </div>

      {/* Calendar grid */}
      {isLoading ? (
        <div style={{ padding: '40px', textAlign: 'center', color: '#9ca3af', fontSize: '14px' }}>
          Ładowanie kalendarza...
        </div>
      ) : employees.length === 0 ? (
        <div
          style={{
            padding: '40px',
            textAlign: 'center',
            color: '#6b7280',
            fontSize: '14px',
            backgroundColor: '#f9fafb',
            borderRadius: '10px',
            border: '1px dashed #d1d5db',
          }}
        >
          Brak pracowników do wyświetlenia.
        </div>
      ) : (
        <div
          style={{
            overflowX: 'auto',
            border: '1px solid #e5e7eb',
            borderRadius: '10px',
            backgroundColor: '#fff',
          }}
        >
          <table style={{ borderCollapse: 'collapse', width: '100%', minWidth: `${days.length * 36 + 200}px` }}>
            <thead>
              <tr style={{ borderBottom: '1px solid #e5e7eb' }}>
                <th
                  style={{
                    position: 'sticky',
                    left: 0,
                    zIndex: 2,
                    backgroundColor: '#f9fafb',
                    padding: '8px 14px',
                    textAlign: 'left',
                    fontSize: '12px',
                    fontWeight: 600,
                    color: '#6b7280',
                    minWidth: '180px',
                    borderRight: '1px solid #e5e7eb',
                  }}
                >
                  Pracownik
                </th>
                {days.map((d) => {
                  const we = isWeekend(d);
                  const dayOfWeek = d.getDay();
                  const dayIndex = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
                  const isToday = formatDateKey(d) === formatDateKey(now);
                  return (
                    <th
                      key={formatDateKey(d)}
                      style={{
                        padding: '4px 2px',
                        textAlign: 'center',
                        fontSize: '10px',
                        fontWeight: 500,
                        color: we ? '#d1d5db' : isToday ? '#2563eb' : '#9ca3af',
                        backgroundColor: isToday ? '#eff6ff' : we ? '#fafafa' : '#f9fafb',
                        minWidth: '32px',
                        borderBottom: '1px solid #e5e7eb',
                      }}
                    >
                      <div>{DAY_NAMES[dayIndex]}</div>
                      <div style={{ fontSize: '12px', fontWeight: 600, color: we ? '#d1d5db' : isToday ? '#2563eb' : '#6b7280' }}>
                        {d.getDate()}
                      </div>
                    </th>
                  );
                })}
              </tr>
            </thead>
            <tbody>
              {employees.map((emp) => {
                const empDayMap = entryMap.get(emp.id);
                const isCurrentUser = emp.id === user?.employeeId;
                return (
                  <tr
                    key={emp.id}
                    style={{
                      borderBottom: '1px solid #f3f4f6',
                      backgroundColor: isCurrentUser ? '#fefce8' : undefined,
                    }}
                  >
                    <td
                      style={{
                        position: 'sticky',
                        left: 0,
                        zIndex: 1,
                        backgroundColor: isCurrentUser ? '#fefce8' : '#fff',
                        padding: '6px 14px',
                        fontSize: '13px',
                        fontWeight: isCurrentUser ? 600 : 400,
                        color: '#374151',
                        borderRight: '1px solid #e5e7eb',
                        whiteSpace: 'nowrap',
                      }}
                    >
                      {emp.firstName} {emp.lastName}
                    </td>
                    {days.map((d) => {
                      const dateKey = formatDateKey(d);
                      const we = isWeekend(d);
                      const entries = empDayMap?.get(dateKey);
                      const isToday = dateKey === formatDateKey(now);

                      let cellBg = we ? '#fafafa' : isToday ? '#eff6ff' : 'transparent';
                      let cellColor: string | undefined;
                      let title = '';

                      if (entries && entries.length > 0) {
                        const e = entries[0]!;
                        cellColor = e.leaveTypeColor ?? DEFAULT_COLORS[e.leaveTypeCode] ?? '#6b7280';
                        cellBg = `${cellColor}33`;
                        title = `${e.leaveTypeName}${e.dayFraction < 1 ? ` (${Math.round(e.dayFraction * 100)}%)` : ''}`;
                      }

                      return (
                        <td
                          key={dateKey}
                          title={title}
                          style={{
                            padding: '4px 2px',
                            textAlign: 'center',
                            backgroundColor: cellBg,
                            minWidth: '32px',
                            height: '32px',
                          }}
                        >
                          {entries && entries.length > 0 && (
                            <div
                              style={{
                                width: '20px',
                                height: '20px',
                                borderRadius: '4px',
                                backgroundColor: cellColor,
                                margin: '0 auto',
                                opacity: entries[0]!.dayFraction < 1 ? 0.5 : 0.85,
                              }}
                            />
                          )}
                        </td>
                      );
                    })}
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

const navBtnStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  width: '32px',
  height: '32px',
  borderRadius: '6px',
  border: '1px solid #d1d5db',
  backgroundColor: '#fff',
  cursor: 'pointer',
  color: '#374151',
};
