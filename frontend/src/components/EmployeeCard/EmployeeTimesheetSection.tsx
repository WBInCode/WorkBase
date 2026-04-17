import type { TimeStatusDto, TimeSheetPeriodDto } from '@/api/types/time';

interface Props {
  timeStatus: TimeStatusDto | undefined;
  timesheet: TimeSheetPeriodDto | undefined;
  isLoading: boolean;
}

const STATUS_LABELS: Record<string, string> = {
  'not-started': 'Nie rozpoczął',
  'working': 'Pracuje',
  'on-break': 'Przerwa',
  'ended': 'Zakończył',
};

const STATUS_COLORS: Record<string, { bg: string; text: string; dot: string }> = {
  'not-started': { bg: '#f3f4f6', text: '#6b7280', dot: '#9ca3af' },
  'working': { bg: '#dcfce7', text: '#166534', dot: '#22c55e' },
  'on-break': { bg: '#fef9c3', text: '#854d0e', dot: '#f59e0b' },
  'ended': { bg: '#dbeafe', text: '#1d4ed8', dot: '#3b82f6' },
};

export function EmployeeTimesheetSection({ timeStatus, timesheet, isLoading }: Props) {
  if (isLoading) {
    return (
      <div style={cardStyle}>
        <h3 style={headingStyle}>Czas pracy</h3>
        <div style={{ color: '#9ca3af', fontSize: '14px' }}>Ładowanie...</div>
      </div>
    );
  }

  const sc = STATUS_COLORS[timeStatus?.status ?? 'not-started'];

  return (
    <div style={cardStyle}>
      <h3 style={headingStyle}>Czas pracy</h3>

      {/* Today status */}
      {timeStatus && (
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px', marginBottom: '16px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span style={{ width: '10px', height: '10px', borderRadius: '50%', backgroundColor: sc?.dot }} />
            <span style={{ padding: '3px 10px', borderRadius: '9999px', fontSize: '13px', fontWeight: 500, backgroundColor: sc?.bg, color: sc?.text }}>
              {STATUS_LABELS[timeStatus.status] ?? timeStatus.status}
            </span>
          </div>
          <div style={{ fontSize: '13px', color: '#6b7280' }}>
            Przepracowane: <strong style={{ color: '#111827' }}>{timeStatus.workedToday ?? '—'}</strong>
            {' · '}
            Przerwy: <strong style={{ color: '#111827' }}>{timeStatus.breaksToday ?? '—'}</strong>
          </div>
        </div>
      )}

      {/* Current period summary */}
      {timesheet && (
        <div>
          <div style={{ fontSize: '13px', fontWeight: 600, color: '#374151', marginBottom: '8px' }}>
            Okres: {formatDate(timesheet.from)} – {formatDate(timesheet.to)}
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '12px' }}>
            <StatBox label="Przepracowane" value={timesheet.totalWorked} />
            <StatBox label="Przerwy" value={timesheet.totalBreaks} />
            <StatBox label="Netto" value={timesheet.netWorked} />
            <StatBox label="Dni pracy" value={String(timesheet.daysWorked)} />
          </div>

          {/* Recent days */}
          {timesheet.days.length > 0 && (
            <div style={{ marginTop: '12px' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid #e5e7eb' }}>
                    <th style={thStyle}>Data</th>
                    <th style={thStyle}>Przepracowane</th>
                    <th style={thStyle}>Przerwy</th>
                    <th style={thStyle}>Netto</th>
                    <th style={thStyle}>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {timesheet.days.slice(-7).reverse().map((day) => (
                    <tr key={day.date} style={{ borderBottom: '1px solid #f3f4f6' }}>
                      <td style={tdStyle}>{formatDate(day.date)}</td>
                      <td style={tdStyle}>{day.totalWorked}</td>
                      <td style={tdStyle}>{day.totalBreaks}</td>
                      <td style={tdStyle}>{day.netWorked}</td>
                      <td style={tdStyle}>
                        <span style={{
                          padding: '1px 6px',
                          borderRadius: '4px',
                          fontSize: '11px',
                          fontWeight: 500,
                          backgroundColor: day.status === 'complete' ? '#dcfce7' : day.status === 'incomplete' ? '#fef9c3' : '#f3f4f6',
                          color: day.status === 'complete' ? '#166534' : day.status === 'incomplete' ? '#854d0e' : '#6b7280',
                        }}>
                          {day.status === 'complete' ? 'OK' : day.status === 'incomplete' ? 'Niekompletny' : day.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {!timeStatus && !timesheet && (
        <div style={{ color: '#9ca3af', fontSize: '14px' }}>Brak danych o czasie pracy.</div>
      )}
    </div>
  );
}

function StatBox({ label, value }: { label: string; value: string }) {
  return (
    <div style={{ padding: '10px 12px', backgroundColor: '#f9fafb', borderRadius: '8px', textAlign: 'center' }}>
      <div style={{ fontSize: '12px', color: '#9ca3af', marginBottom: '2px' }}>{label}</div>
      <div style={{ fontSize: '15px', fontWeight: 700, color: '#111827' }}>{value}</div>
    </div>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL');
}

const cardStyle: React.CSSProperties = {
  padding: '20px',
  border: '1px solid #e5e7eb',
  borderRadius: '12px',
  backgroundColor: '#fff',
};

const headingStyle: React.CSSProperties = {
  margin: '0 0 14px',
  fontSize: '16px',
  fontWeight: 700,
  color: '#111827',
};

const thStyle: React.CSSProperties = {
  textAlign: 'left',
  padding: '6px 8px',
  color: '#6b7280',
  fontWeight: 600,
};

const tdStyle: React.CSSProperties = {
  padding: '6px 8px',
  color: '#111827',
};
