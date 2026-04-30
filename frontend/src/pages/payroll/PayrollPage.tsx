import { useMemo, useState } from 'react';
import { useEmployees } from '@/api/hooks/useOrganization';
import { useTeamTimesheets } from '@/api/hooks/useTimeTracking';

function startOfMonth(d: Date): string {
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
}

function endOfMonth(d: Date): string {
  return new Date(d.getFullYear(), d.getMonth() + 1, 0).toISOString().slice(0, 10);
}

function hoursFromTimespan(ts: string | undefined): number {
  if (!ts) return 0;
  // ASP.NET TimeSpan as string like "08:30:00" or "1.02:30:00" (days.hh:mm:ss)
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

const fmtPLN = (n: number) =>
  new Intl.NumberFormat('pl-PL', { style: 'currency', currency: 'PLN' }).format(n);

export function PayrollPage() {
  const today = new Date();
  const [from, setFrom] = useState(startOfMonth(today));
  const [to, setTo] = useState(endOfMonth(today));

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

  const rows = useMemo(() => {
    if (!timesheets) return [];
    return employees.map((emp, idx) => {
      const sheet = timesheets[idx];
      const netHours = hoursFromTimespan(sheet?.netWorked as unknown as string);
      const rate = emp.hourlyRate ?? 0;
      const earnings = netHours * rate;
      return {
        id: emp.id,
        name: `${emp.firstName} ${emp.lastName}`,
        email: emp.email,
        rate,
        hasRate: emp.hourlyRate !== null && emp.hourlyRate !== undefined,
        netHours,
        earnings,
      };
    });
  }, [employees, timesheets]);

  const total = rows.reduce((acc, r) => acc + r.earnings, 0);
  const totalHours = rows.reduce((acc, r) => acc + r.netHours, 0);

  const isLoading = loadingEmployees || loadingSheets;

  return (
    <div style={{ padding: 24 }}>
      <h1 style={{ fontSize: 28, fontWeight: 700, marginBottom: 8 }}>Wynagrodzenia</h1>
      <p style={{ color: '#64748b', marginBottom: 20 }}>
        Naliczanie wynagrodzeń brutto = przepracowane godziny netto × stawka godzinowa pracownika.
      </p>

      <div
        style={{
          display: 'flex',
          gap: 12,
          alignItems: 'end',
          marginBottom: 20,
          padding: 16,
          background: '#f8fafc',
          borderRadius: 8,
          border: '1px solid #e2e8f0',
        }}
      >
        <div>
          <label style={{ display: 'block', fontSize: 12, fontWeight: 600, marginBottom: 4 }}>
            Od
          </label>
          <input
            type="date"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            style={{ padding: '6px 10px', border: '1px solid #cbd5e1', borderRadius: 6 }}
          />
        </div>
        <div>
          <label style={{ display: 'block', fontSize: 12, fontWeight: 600, marginBottom: 4 }}>
            Do
          </label>
          <input
            type="date"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            style={{ padding: '6px 10px', border: '1px solid #cbd5e1', borderRadius: 6 }}
          />
        </div>
        <div style={{ marginLeft: 'auto', textAlign: 'right' }}>
          <div style={{ fontSize: 12, color: '#64748b' }}>Razem brutto</div>
          <div style={{ fontSize: 24, fontWeight: 700, color: '#0f172a' }}>{fmtPLN(total)}</div>
          <div style={{ fontSize: 12, color: '#64748b' }}>{totalHours.toFixed(2)} h</div>
        </div>
      </div>

      {isLoading ? (
        <div>Ładowanie…</div>
      ) : rows.length === 0 ? (
        <div style={{ color: '#64748b' }}>Brak aktywnych pracowników.</div>
      ) : (
        <div style={{ overflowX: 'auto', border: '1px solid #e2e8f0', borderRadius: 8 }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
            <thead style={{ background: '#f1f5f9' }}>
              <tr>
                <th style={th}>Pracownik</th>
                <th style={th}>Email</th>
                <th style={{ ...th, textAlign: 'right' }}>Stawka [PLN/h]</th>
                <th style={{ ...th, textAlign: 'right' }}>Godziny netto</th>
                <th style={{ ...th, textAlign: 'right' }}>Wynagrodzenie brutto</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} style={{ borderTop: '1px solid #e2e8f0' }}>
                  <td style={td}>{r.name}</td>
                  <td style={{ ...td, color: '#64748b' }}>{r.email}</td>
                  <td style={{ ...td, textAlign: 'right' }}>
                    {r.hasRate ? (
                      r.rate.toFixed(2)
                    ) : (
                      <span style={{ color: '#dc2626', fontStyle: 'italic' }}>brak</span>
                    )}
                  </td>
                  <td style={{ ...td, textAlign: 'right' }}>{r.netHours.toFixed(2)}</td>
                  <td style={{ ...td, textAlign: 'right', fontWeight: 600 }}>
                    {r.hasRate ? fmtPLN(r.earnings) : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <p style={{ marginTop: 16, fontSize: 12, color: '#94a3b8' }}>
        Stawkę godzinową można ustawić w karcie pracownika (Organizacja → Pracownicy).
      </p>
    </div>
  );
}

const th: React.CSSProperties = {
  padding: '10px 14px',
  textAlign: 'left',
  fontSize: 12,
  fontWeight: 700,
  color: '#475569',
  textTransform: 'uppercase',
  letterSpacing: '0.05em',
};

const td: React.CSSProperties = {
  padding: '10px 14px',
};
