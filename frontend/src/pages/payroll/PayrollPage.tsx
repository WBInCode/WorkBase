import { useMemo, useState, Fragment } from 'react';
import { ChevronDown, ChevronRight } from 'lucide-react';
import { useEmployees } from '@/api/hooks/useOrganization';
import { useTeamTimesheets, useTeamSchedulesByEmployee } from '@/api/hooks/useTimeTracking';
import { useTeamLeaveRequests } from '@/api/hooks/useLeave';
import type { ScheduleDto } from '@/api/types/time';
import type { LeaveRequestDto } from '@/api/types/leave';

const OVERTIME_MULTIPLIER = 1.5;

function startOfMonth(d: Date): string {
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
}
function endOfMonth(d: Date): string {
  return new Date(d.getFullYear(), d.getMonth() + 1, 0).toISOString().slice(0, 10);
}

function hoursFromTimespan(ts: string | undefined | null): number {
  if (!ts) return 0;
  const parts = ts.split(':');
  if (parts.length < 3) return 0;
  let days = 0;
  let hours: number;
  if (parts[0]!.includes('.')) {
    const [d, h] = parts[0]!.split('.');
    days = Number(d) || 0;
    hours = Number(h) || 0;
  } else {
    hours = Number(parts[0]) || 0;
  }
  const minutes = Number(parts[1]) || 0;
  const seconds = Number(parts[2]) || 0;
  return days * 24 + hours + minutes / 60 + seconds / 3600;
}

function fmtH(h: number): string {
  if (h <= 0) return '0h';
  const hours = Math.floor(h);
  const mins = Math.round((h - hours) * 60);
  if (mins === 0) return `${hours}h`;
  return `${hours}h ${mins}min`;
}

const fmtPLN = (n: number) =>
  new Intl.NumberFormat('pl-PL', { style: 'currency', currency: 'PLN' }).format(n);

function daysOfApprovedLeavesInRange(
  leaves: LeaveRequestDto[],
  from: Date,
  to: Date,
): { vacationDays: number; absenceDays: number } {
  let vacation = 0;
  let absence = 0;
  for (const l of leaves) {
    if (l.status !== 'Approved' && l.status !== 'Pending') continue;
    const ls = new Date(l.startDate);
    const le = new Date(l.endDate);
    const start = ls < from ? from : ls;
    const end = le > to ? to : le;
    if (start > end) continue;
    const days =
      Math.floor((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1;
    const code = (l.leaveTypeCode ?? '').toUpperCase();
    const name = (l.leaveTypeName ?? '').toLowerCase();
    if (code.startsWith('URL') || name.includes('urlop') || name.includes('wypocz')) {
      vacation += days;
    } else {
      absence += days;
    }
  }
  return { vacationDays: vacation, absenceDays: absence };
}

function plannedHoursInRange(schedules: ScheduleDto[], from: Date, to: Date): number {
  let total = 0;
  for (const s of schedules) {
    const d = new Date(s.date);
    if (d < from || d > to) continue;
    total += hoursFromTimespan(s.plannedDuration);
  }
  return total;
}

interface Row {
  id: string;
  name: string;
  email: string;
  rate: number;
  hasRate: boolean;
  normaH: number;
  workedH: number;
  regularH: number;
  overtimeH: number;
  vacationDays: number;
  absenceDays: number;
  basicPay: number;
  overtimePay: number;
  totalPay: number;
}

export function PayrollPage() {
  const today = new Date();
  const [from, setFrom] = useState(startOfMonth(today));
  const [to, setTo] = useState(endOfMonth(today));
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  const { data: employeesPage, isLoading: loadingEmployees } = useEmployees({
    page: 1,
    pageSize: 200,
    status: 'Active',
  });

  const employees = employeesPage?.items ?? [];
  const employeeIds = useMemo(() => employees.map((e) => e.id), [employees]);

  const { data: timesheets, isLoading: loadingSheets } = useTeamTimesheets(
    employeeIds,
    from,
    to,
    'month',
  );
  const { data: schedulesByEmp, isLoading: loadingSchedules } =
    useTeamSchedulesByEmployee(employeeIds, from, to);
  const year = new Date(from).getFullYear();
  const { data: leavesByEmp, isLoading: loadingLeaves } = useTeamLeaveRequests(
    employeeIds,
    year,
  );

  const fromDate = useMemo(() => new Date(from), [from]);
  const toDate = useMemo(() => new Date(to), [to]);

  const rows: Row[] = useMemo(() => {
    if (!timesheets || !schedulesByEmp || !leavesByEmp) return [];
    return employees.map((emp, idx) => {
      const sheet = timesheets[idx];
      const schedules = schedulesByEmp[idx] ?? [];
      const leaves = leavesByEmp[idx] ?? [];

      const workedH = hoursFromTimespan(sheet?.netWorked as unknown as string);
      const normaH = plannedHoursInRange(schedules, fromDate, toDate);
      const regularH = normaH > 0 ? Math.min(workedH, normaH) : workedH;
      const overtimeH = normaH > 0 ? Math.max(workedH - normaH, 0) : 0;

      const { vacationDays, absenceDays } = daysOfApprovedLeavesInRange(
        leaves,
        fromDate,
        toDate,
      );

      const rate = emp.hourlyRate ?? 0;
      const basicPay = rate * regularH;
      const overtimePay = rate * OVERTIME_MULTIPLIER * overtimeH;
      const totalPay = basicPay + overtimePay;

      return {
        id: emp.id,
        name: `${emp.firstName} ${emp.lastName}`,
        email: emp.email,
        rate,
        hasRate: emp.hourlyRate !== null && emp.hourlyRate !== undefined,
        normaH,
        workedH,
        regularH,
        overtimeH,
        vacationDays,
        absenceDays,
        basicPay,
        overtimePay,
        totalPay,
      };
    });
  }, [employees, timesheets, schedulesByEmp, leavesByEmp, fromDate, toDate]);

  const totals = useMemo(() => {
    return rows.reduce(
      (acc, r) => ({
        normaH: acc.normaH + r.normaH,
        workedH: acc.workedH + r.workedH,
        overtimeH: acc.overtimeH + r.overtimeH,
        basicPay: acc.basicPay + r.basicPay,
        overtimePay: acc.overtimePay + r.overtimePay,
        totalPay: acc.totalPay + r.totalPay,
      }),
      { normaH: 0, workedH: 0, overtimeH: 0, basicPay: 0, overtimePay: 0, totalPay: 0 },
    );
  }, [rows]);

  const isLoading =
    loadingEmployees || loadingSheets || loadingSchedules || loadingLeaves;

  const toggle = (id: string) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  return (
    <div style={{ padding: 24 }}>
      <h1 style={{ fontSize: 28, fontWeight: 700, marginBottom: 4 }}>Wynagrodzenia</h1>
      <p style={{ color: '#64748b', marginBottom: 20, fontSize: 13 }}>
        Ewidencja czasu pracy + rozliczenie wynagrodzeń (norma z grafiku, czas pracy z kart, nadgodziny ×{OVERTIME_MULTIPLIER}).
      </p>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'auto auto 1fr repeat(3, auto)',
          gap: 16,
          alignItems: 'end',
          marginBottom: 20,
          padding: 16,
          background: '#f8fafc',
          borderRadius: 8,
          border: '1px solid #e2e8f0',
        }}
      >
        <div>
          <label style={lblStyle}>Od</label>
          <input
            type="date"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            style={inputStyle}
          />
        </div>
        <div>
          <label style={lblStyle}>Do</label>
          <input
            type="date"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            style={inputStyle}
          />
        </div>
        <div />
        <Stat label="Norma" value={fmtH(totals.normaH)} />
        <Stat label="Czas pracy" value={fmtH(totals.workedH)} accent="#0f766e" />
        <Stat label="Razem brutto" value={fmtPLN(totals.totalPay)} accent="#1d4ed8" big />
      </div>

      {isLoading ? (
        <div>Ładowanie…</div>
      ) : rows.length === 0 ? (
        <div style={{ color: '#64748b' }}>Brak aktywnych pracowników.</div>
      ) : (
        <div style={{ overflowX: 'auto', border: '1px solid #e2e8f0', borderRadius: 8 }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead style={{ background: '#f1f5f9' }}>
              <tr>
                <th style={{ ...th, width: 28 }}></th>
                <th style={th}>Pracownik</th>
                <th style={{ ...th, textAlign: 'right' }}>Stawka [PLN/h]</th>
                <th style={{ ...th, textAlign: 'right' }}>Norma</th>
                <th style={{ ...th, textAlign: 'right' }}>Czas pracy</th>
                <th style={{ ...th, textAlign: 'right' }}>Nadgodziny</th>
                <th style={{ ...th, textAlign: 'right' }}>Urlop [dni]</th>
                <th style={{ ...th, textAlign: 'right' }}>Nieobec. [dni]</th>
                <th style={{ ...th, textAlign: 'right' }}>Zasadnicze</th>
                <th style={{ ...th, textAlign: 'right' }}>Za nadgodziny</th>
                <th style={{ ...th, textAlign: 'right', background: '#e0e7ff' }}>Całkowite</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => {
                const isOpen = expanded.has(r.id);
                return (
                  <Fragment key={r.id}>
                    <tr
                      onClick={() => toggle(r.id)}
                      style={{
                        borderTop: '1px solid #e2e8f0',
                        cursor: 'pointer',
                        background: isOpen ? '#f8fafc' : 'transparent',
                      }}
                    >
                      <td style={td}>
                        {isOpen ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                      </td>
                      <td style={td}>
                        <div style={{ fontWeight: 600 }}>{r.name}</div>
                        <div style={{ color: '#64748b', fontSize: 11 }}>{r.email}</div>
                      </td>
                      <td style={{ ...td, textAlign: 'right' }}>
                        {r.hasRate ? (
                          r.rate.toFixed(2)
                        ) : (
                          <span style={{ color: '#dc2626', fontStyle: 'italic' }}>brak</span>
                        )}
                      </td>
                      <td style={{ ...td, textAlign: 'right' }}>{fmtH(r.normaH)}</td>
                      <td style={{ ...td, textAlign: 'right' }}>{fmtH(r.workedH)}</td>
                      <td
                        style={{
                          ...td,
                          textAlign: 'right',
                          color: r.overtimeH > 0 ? '#ea580c' : '#64748b',
                          fontWeight: r.overtimeH > 0 ? 600 : 400,
                        }}
                      >
                        {fmtH(r.overtimeH)}
                      </td>
                      <td style={{ ...td, textAlign: 'right' }}>{r.vacationDays || '—'}</td>
                      <td style={{ ...td, textAlign: 'right' }}>{r.absenceDays || '—'}</td>
                      <td style={{ ...td, textAlign: 'right' }}>
                        {r.hasRate ? fmtPLN(r.basicPay) : '—'}
                      </td>
                      <td style={{ ...td, textAlign: 'right' }}>
                        {r.hasRate ? fmtPLN(r.overtimePay) : '—'}
                      </td>
                      <td
                        style={{
                          ...td,
                          textAlign: 'right',
                          fontWeight: 700,
                          background: '#eef2ff',
                        }}
                      >
                        {r.hasRate ? fmtPLN(r.totalPay) : '—'}
                      </td>
                    </tr>
                    {isOpen && (
                      <tr style={{ background: '#f8fafc' }}>
                        <td colSpan={11} style={{ padding: '12px 20px 18px' }}>
                          <DetailGrid row={r} from={fromDate} to={toDate} />
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <p style={{ marginTop: 16, fontSize: 12, color: '#94a3b8' }}>
        Norma — z grafiku pracy. Czas pracy — netto z karty czasu (po odjęciu przerw). Nadgodziny — czas pracy ponad normę. Stawkę ustawiasz w karcie pracownika.
      </p>
    </div>
  );
}

function DetailGrid({ row, from, to }: { row: Row; from: Date; to: Date }) {
  const days = Math.floor((to.getTime() - from.getTime()) / (1000 * 60 * 60 * 24)) + 1;
  return (
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(3, 1fr)',
        gap: 12,
        fontSize: 12,
      }}
    >
      <DetailCard title="Czas pracy">
        <DetailLine label="Norma (grafik)" value={fmtH(row.normaH)} />
        <DetailLine label="Czas pracy (netto)" value={fmtH(row.workedH)} accent="#0f766e" />
        <DetailLine label="Godziny zwykłe" value={fmtH(row.regularH)} />
        <DetailLine
          label="Nadgodziny"
          value={fmtH(row.overtimeH)}
          accent={row.overtimeH > 0 ? '#ea580c' : undefined}
        />
        <DetailLine
          label="Bilans"
          value={`${row.workedH >= row.normaH ? '+' : '−'}${fmtH(Math.abs(row.workedH - row.normaH))}`}
        />
      </DetailCard>

      <DetailCard title="Nieobecności">
        <DetailLine label="Urlop wypoczynkowy" value={`${row.vacationDays} dni`} />
        <DetailLine label="Inne nieobecności" value={`${row.absenceDays} dni`} />
        <DetailLine
          label="Razem nieobecności"
          value={`${row.vacationDays + row.absenceDays} dni`}
        />
        <DetailLine label="Okres" value={`${days} dni`} />
      </DetailCard>

      <DetailCard title="Składniki wynagrodzenia">
        <DetailLine
          label={`Zasadnicze (${row.regularH.toFixed(2)}h × ${row.rate.toFixed(2)})`}
          value={row.hasRate ? fmtPLN(row.basicPay) : '—'}
        />
        <DetailLine
          label={`Za nadgodziny (${row.overtimeH.toFixed(2)}h × ${row.rate.toFixed(2)} × ${OVERTIME_MULTIPLIER})`}
          value={row.hasRate ? fmtPLN(row.overtimePay) : '—'}
        />
        <DetailLine
          label="Całkowite brutto"
          value={row.hasRate ? fmtPLN(row.totalPay) : '—'}
          accent="#1d4ed8"
          bold
        />
      </DetailCard>
    </div>
  );
}

function DetailCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div
      style={{
        background: '#fff',
        border: '1px solid #e2e8f0',
        borderRadius: 6,
        padding: 12,
      }}
    >
      <div
        style={{
          fontWeight: 700,
          fontSize: 11,
          textTransform: 'uppercase',
          letterSpacing: '0.05em',
          color: '#475569',
          marginBottom: 8,
          paddingBottom: 6,
          borderBottom: '1px solid #f1f5f9',
        }}
      >
        {title}
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>{children}</div>
    </div>
  );
}

function DetailLine({
  label,
  value,
  accent,
  bold,
}: {
  label: string;
  value: string;
  accent?: string;
  bold?: boolean;
}) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 8 }}>
      <span style={{ color: '#64748b' }}>{label}</span>
      <span
        style={{
          color: accent ?? '#0f172a',
          fontWeight: bold ? 700 : 500,
          textAlign: 'right',
        }}
      >
        {value}
      </span>
    </div>
  );
}

function Stat({
  label,
  value,
  accent,
  big,
}: {
  label: string;
  value: string;
  accent?: string;
  big?: boolean;
}) {
  return (
    <div style={{ textAlign: 'right' }}>
      <div style={{ fontSize: 11, color: '#64748b', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
        {label}
      </div>
      <div
        style={{
          fontSize: big ? 22 : 16,
          fontWeight: 700,
          color: accent ?? '#0f172a',
        }}
      >
        {value}
      </div>
    </div>
  );
}

const th: React.CSSProperties = {
  padding: '8px 10px',
  textAlign: 'left',
  fontSize: 11,
  fontWeight: 700,
  color: '#475569',
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
  whiteSpace: 'nowrap',
};

const td: React.CSSProperties = {
  padding: '10px 10px',
  whiteSpace: 'nowrap',
};

const lblStyle: React.CSSProperties = {
  display: 'block',
  fontSize: 11,
  fontWeight: 600,
  marginBottom: 4,
  color: '#475569',
  textTransform: 'uppercase',
  letterSpacing: '0.05em',
};

const inputStyle: React.CSSProperties = {
  padding: '6px 10px',
  border: '1px solid #cbd5e1',
  borderRadius: 6,
  fontSize: 13,
};
