import { useState } from 'react';
import { Plus, Pencil, Trash2, ChevronLeft, ChevronRight } from 'lucide-react';
import type { TimeStatusDto, TimeSheetPeriodDto, TimeSheetEntryDto } from '@/api/types/time';
import { useAdminCreateTimeEntry, useAdminUpdateTimeEntry, useAdminDeleteTimeEntry } from '@/api/hooks/useTimeTracking';

interface Props {
  timeStatus: TimeStatusDto | undefined;
  timesheet: TimeSheetPeriodDto | undefined;
  isLoading: boolean;
  employeeId?: string;
  from: string;
  to: string;
  onDateRangeChange: (from: string, to: string) => void;
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

const ENTRY_TYPE_OPTIONS = [
  { value: 'ClockIn', label: 'Rozpoczęcie pracy' },
  { value: 'ClockOut', label: 'Zakończenie pracy' },
  { value: 'BreakStart', label: 'Rozpoczęcie przerwy' },
  { value: 'BreakEnd', label: 'Zakończenie przerwy' },
];

const BREAK_TYPE_OPTIONS = [
  { value: '', label: 'Brak' },
  { value: 'Paid', label: 'Płatna' },
  { value: 'Unpaid', label: 'Bezpłatna' },
];

const ENTRY_TYPE_LABELS: Record<string, string> = {
  ClockIn: 'Rozpoczęcie pracy',
  ClockOut: 'Zakończenie pracy',
  BreakStart: 'Rozpoczęcie przerwy',
  BreakEnd: 'Zakończenie przerwy',
};

const BREAK_TYPE_LABELS: Record<string, string> = {
  Paid: 'płatna',
  Unpaid: 'bezpłatna',
};

interface EntryModalState {
  open: boolean;
  mode: 'create' | 'edit';
  entryId?: string;
  date: string;
  time: string;
  type: string;
  breakType: string;
  note: string;
}

const initialModal: EntryModalState = {
  open: false,
  mode: 'create',
  date: '',
  time: '',
  type: 'ClockIn',
  breakType: '',
  note: '',
};

export function EmployeeTimesheetSection({ timeStatus, timesheet, isLoading, employeeId, from, to, onDateRangeChange }: Props) {
  const [modal, setModal] = useState<EntryModalState>(initialModal);
  const [expandedDay, setExpandedDay] = useState<string | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);

  const createEntry = useAdminCreateTimeEntry();
  const updateEntry = useAdminUpdateTimeEntry();
  const deleteEntry = useAdminDeleteTimeEntry();

  const isAdmin = !!employeeId;

  const shiftWeek = (direction: number) => {
    const fromDate = new Date(from);
    fromDate.setDate(fromDate.getDate() + direction * 7);
    const toDate = new Date(fromDate);
    toDate.setDate(fromDate.getDate() + 6);
    onDateRangeChange(fromDate.toISOString().slice(0, 10), toDate.toISOString().slice(0, 10));
  };

  const goToCurrentWeek = () => {
    const now = new Date();
    const monday = new Date(now);
    monday.setDate(now.getDate() - ((now.getDay() + 6) % 7));
    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);
    onDateRangeChange(monday.toISOString().slice(0, 10), sunday.toISOString().slice(0, 10));
  };

  const openCreate = (date: string) => {
    setModal({
      open: true,
      mode: 'create',
      date,
      time: '08:00',
      type: 'ClockIn',
      breakType: '',
      note: '',
    });
  };

  const openEdit = (entry: TimeSheetEntryDto, date: string) => {
    const dt = new Date(entry.entryTime);
    setModal({
      open: true,
      mode: 'edit',
      entryId: entry.id,
      date,
      time: `${String(dt.getHours()).padStart(2, '0')}:${String(dt.getMinutes()).padStart(2, '0')}`,
      type: entry.type,
      breakType: entry.breakType ?? '',
      note: '',
    });
  };

  const handleSave = () => {
    if (!employeeId) return;
    // Build a local Date from date+time, then convert to ISO (UTC) string
    const localDate = new Date(`${modal.date}T${modal.time}:00`);
    const entryTime = localDate.toISOString();

    if (modal.mode === 'create') {
      createEntry.mutate({
        employeeId,
        entryTime,
        type: modal.type,
        breakType: modal.breakType || undefined,
        note: modal.note || undefined,
      }, { onSuccess: () => setModal(initialModal) });
    } else if (modal.entryId) {
      updateEntry.mutate({
        id: modal.entryId,
        entryTime,
        type: modal.type,
        breakType: modal.breakType || undefined,
        note: modal.note || undefined,
      }, { onSuccess: () => setModal(initialModal) });
    }
  };

  const handleDelete = (entryId: string) => {
    deleteEntry.mutate(entryId, {
      onSuccess: () => setDeleteConfirm(null),
    });
  };

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

      {/* Date range picker */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '16px', flexWrap: 'wrap',
      }}>
        <button onClick={() => shiftWeek(-1)} title="Poprzedni tydzień" style={navBtnStyle}>
          <ChevronLeft size={16} />
        </button>
        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
          <input
            type="date"
            value={from}
            onChange={(e) => onDateRangeChange(e.target.value, to)}
            style={dateInputStyle}
          />
          <span style={{ color: '#6b7280', fontSize: '13px' }}>–</span>
          <input
            type="date"
            value={to}
            onChange={(e) => onDateRangeChange(from, e.target.value)}
            style={dateInputStyle}
          />
        </div>
        <button onClick={() => shiftWeek(1)} title="Następny tydzień" style={navBtnStyle}>
          <ChevronRight size={16} />
        </button>
        <button onClick={goToCurrentWeek} style={presetBtnStyle}>
          Bieżący tydzień
        </button>
      </div>

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
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '12px' }}>
            <StatBox label="Przepracowane" value={timesheet.totalWorked} />
            <StatBox label="Przerwy" value={timesheet.totalBreaks} />
            <StatBox label="Netto" value={timesheet.netWorked} />
            <StatBox label="Dni pracy" value={String(timesheet.daysWorked)} />
          </div>

          {/* Days with entries */}
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
                    {isAdmin && <th style={thStyle}></th>}
                  </tr>
                </thead>
                <tbody>
                  {[...timesheet.days].reverse().map((day) => {
                    const isExpanded = expandedDay === day.date;
                    const hasEntries = day.entries && day.entries.length > 0;
                    return (
                      <tr key={day.date} style={{ borderBottom: '1px solid #f3f4f6' }}>
                        <td colSpan={isAdmin ? 6 : 5} style={{ padding: 0 }}>
                          <div
                            style={{
                              display: 'grid',
                              gridTemplateColumns: isAdmin ? '1fr 1fr 1fr 1fr auto auto' : '1fr 1fr 1fr 1fr auto',
                              alignItems: 'center',
                              cursor: (isAdmin || hasEntries) ? 'pointer' : 'default',
                            }}
                            onClick={() => setExpandedDay(isExpanded ? null : day.date)}
                          >
                            <span style={tdStyle}>{formatDate(day.date)}</span>
                            <span style={tdStyle}>{day.totalWorked}</span>
                            <span style={tdStyle}>{day.totalBreaks}</span>
                            <span style={tdStyle}>{day.netWorked}</span>
                            <span style={tdStyle}>
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
                            </span>
                            {isAdmin && (
                              <span style={{ ...tdStyle, textAlign: 'right' }}>
                                <button
                                  onClick={(e) => { e.stopPropagation(); openCreate(day.date); }}
                                  title="Dodaj wpis"
                                  style={iconBtnStyle}
                                >
                                  <Plus size={14} />
                                </button>
                              </span>
                            )}
                          </div>

                          {/* Expanded entries */}
                          {isExpanded && hasEntries && (
                            <div style={{ padding: '8px 16px 12px 24px', backgroundColor: '#f9fafb', borderTop: '1px solid #f3f4f6' }}>
                              <div style={{ fontSize: '11px', fontWeight: 600, color: '#6b7280', textTransform: 'uppercase', marginBottom: '8px' }}>
                                Rejestr zdarzeń
                              </div>
                              {day.entries.map((entry) => {
                                const typeLabel = ENTRY_TYPE_LABELS[entry.type] ?? entry.type;
                                const breakLabel = entry.breakType ? ` (${BREAK_TYPE_LABELS[entry.breakType] ?? entry.breakType})` : '';
                                const time = new Date(entry.entryTime).toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' });
                                return (
                                  <div
                                    key={entry.id}
                                    style={{
                                      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                                      padding: '4px 0', fontSize: '13px',
                                    }}
                                  >
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                                      <span style={{
                                        width: '8px', height: '8px', borderRadius: '50%', flexShrink: 0,
                                        backgroundColor: entry.type === 'ClockIn' ? '#22c55e'
                                          : entry.type === 'ClockOut' ? '#ef4444'
                                          : entry.type === 'BreakStart' ? '#f59e0b'
                                          : '#22c55e',
                                      }} />
                                      <span style={{ fontWeight: 600, color: '#374151', fontVariantNumeric: 'tabular-nums' }}>{time}</span>
                                      <span style={{ color: '#6b7280' }}>{typeLabel}{breakLabel}</span>
                                    </div>
                                    {isAdmin && (
                                      <div style={{ display: 'flex', gap: '4px' }}>
                                        <button
                                          onClick={() => openEdit(entry, day.date)}
                                          title="Edytuj"
                                          style={iconBtnSmStyle}
                                        >
                                          <Pencil size={12} />
                                        </button>
                                        {deleteConfirm === entry.id ? (
                                          <div style={{ display: 'flex', gap: '4px', alignItems: 'center' }}>
                                            <button
                                              onClick={() => handleDelete(entry.id)}
                                              style={{ ...iconBtnSmStyle, color: '#dc2626', backgroundColor: '#fef2f2' }}
                                            >
                                              ✓
                                            </button>
                                            <button
                                              onClick={() => setDeleteConfirm(null)}
                                              style={iconBtnSmStyle}
                                            >
                                              ✕
                                            </button>
                                          </div>
                                        ) : (
                                          <button
                                            onClick={() => setDeleteConfirm(entry.id)}
                                            title="Usuń"
                                            style={{ ...iconBtnSmStyle, color: '#dc2626' }}
                                          >
                                            <Trash2 size={12} />
                                          </button>
                                        )}
                                      </div>
                                    )}
                                  </div>
                                );
                              })}
                            </div>
                          )}

                          {/* Expanded but no entries — only for admin */}
                          {isExpanded && !hasEntries && isAdmin && (
                            <div style={{ padding: '12px 16px', backgroundColor: '#f9fafb', borderTop: '1px solid #f3f4f6', color: '#9ca3af', fontSize: '13px' }}>
                              Brak wpisów dla tego dnia.
                            </div>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {!timeStatus && !timesheet && (
        <div style={{ color: '#9ca3af', fontSize: '14px' }}>Brak danych o czasie pracy.</div>
      )}

      {/* Entry Modal */}
      {modal.open && (
        <div style={{
          position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.3)',
          display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000,
        }}
          onClick={() => setModal(initialModal)}
        >
          <div
            style={{
              backgroundColor: '#fff', borderRadius: '12px', padding: '24px',
              width: '400px', maxWidth: '90vw', boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <h3 style={{ margin: 0, fontSize: '16px', fontWeight: 700, color: '#111827' }}>
                {modal.mode === 'create' ? 'Dodaj wpis' : 'Edytuj wpis'}
              </h3>
              <button onClick={() => setModal(initialModal)} style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: '#6b7280' }}>✕</button>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div>
                <label style={labelStyle}>Data</label>
                <input
                  type="date"
                  value={modal.date}
                  onChange={(e) => setModal({ ...modal, date: e.target.value })}
                  style={inputStyle}
                />
              </div>

              <div>
                <label style={labelStyle}>Godzina</label>
                <input
                  type="time"
                  value={modal.time}
                  onChange={(e) => setModal({ ...modal, time: e.target.value })}
                  style={inputStyle}
                />
              </div>

              <div>
                <label style={labelStyle}>Typ wpisu</label>
                <select
                  value={modal.type}
                  onChange={(e) => setModal({ ...modal, type: e.target.value })}
                  style={inputStyle}
                >
                  {ENTRY_TYPE_OPTIONS.map((o) => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>

              {(modal.type === 'BreakStart') && (
                <div>
                  <label style={labelStyle}>Typ przerwy</label>
                  <select
                    value={modal.breakType}
                    onChange={(e) => setModal({ ...modal, breakType: e.target.value })}
                    style={inputStyle}
                  >
                    {BREAK_TYPE_OPTIONS.map((o) => (
                      <option key={o.value} value={o.value}>{o.label}</option>
                    ))}
                  </select>
                </div>
              )}

              <div>
                <label style={labelStyle}>Notatka (opcjonalnie)</label>
                <input
                  type="text"
                  value={modal.note}
                  onChange={(e) => setModal({ ...modal, note: e.target.value })}
                  placeholder="np. korekta ręczna"
                  style={inputStyle}
                />
              </div>
            </div>

            {(createEntry.error || updateEntry.error) && (
              <div style={{ marginTop: '12px', padding: '8px 12px', backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: '6px', color: '#dc2626', fontSize: '13px' }}>
                {(createEntry.error as Error)?.message || (updateEntry.error as Error)?.message || 'Wystąpił błąd'}
              </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '20px' }}>
              <button
                onClick={() => setModal(initialModal)}
                style={{
                  padding: '8px 16px', fontSize: '13px', fontWeight: 500,
                  color: '#374151', backgroundColor: '#fff',
                  border: '1px solid #d1d5db', borderRadius: '8px', cursor: 'pointer',
                }}
              >
                Anuluj
              </button>
              <button
                onClick={handleSave}
                disabled={createEntry.isPending || updateEntry.isPending}
                style={{
                  padding: '8px 16px', fontSize: '13px', fontWeight: 600,
                  color: '#fff', backgroundColor: '#3b82f6',
                  border: 'none', borderRadius: '8px', cursor: 'pointer',
                  opacity: (createEntry.isPending || updateEntry.isPending) ? 0.6 : 1,
                }}
              >
                {(createEntry.isPending || updateEntry.isPending) ? 'Zapisywanie...' : 'Zapisz'}
              </button>
            </div>
          </div>
        </div>
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

const iconBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '28px', height: '28px', border: '1px solid #e5e7eb',
  borderRadius: '6px', backgroundColor: '#fff', cursor: 'pointer', color: '#3b82f6',
};

const iconBtnSmStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '24px', height: '24px', border: '1px solid #e5e7eb',
  borderRadius: '4px', backgroundColor: '#fff', cursor: 'pointer', color: '#6b7280',
  padding: 0,
};

const labelStyle: React.CSSProperties = {
  display: 'block', fontSize: '12px', fontWeight: 600, color: '#374151', marginBottom: '4px',
};

const inputStyle: React.CSSProperties = {
  width: '100%', padding: '8px 12px', fontSize: '14px',
  border: '1px solid #d1d5db', borderRadius: '8px',
  color: '#111827', backgroundColor: '#fff',
  boxSizing: 'border-box',
};

const navBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '32px', height: '32px', border: '1px solid #d1d5db',
  borderRadius: '6px', backgroundColor: '#fff', cursor: 'pointer', color: '#374151',
  padding: 0, flexShrink: 0,
};

const dateInputStyle: React.CSSProperties = {
  padding: '6px 10px', fontSize: '13px',
  border: '1px solid #d1d5db', borderRadius: '6px',
  color: '#111827', backgroundColor: '#fff',
};

const presetBtnStyle: React.CSSProperties = {
  padding: '6px 12px', fontSize: '12px', fontWeight: 500,
  color: '#3b82f6', backgroundColor: '#eff6ff',
  border: '1px solid #bfdbfe', borderRadius: '6px', cursor: 'pointer',
  whiteSpace: 'nowrap',
};
