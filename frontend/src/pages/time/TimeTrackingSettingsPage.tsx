import { useState, useEffect } from 'react';
import { AlarmClock, RefreshCw } from 'lucide-react';
import { useTimeTrackingSettings, useUpdateTimeTrackingSettings } from '@/api/hooks/useTenantConfig';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

function timeSpanToMinutes(ts: string): number {
  const [h, m] = ts.split(':').map(Number);
  return (h || 0) * 60 + (m || 0);
}
function minutesToTimeSpan(minutes: number): string {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:00`;
}
function timeSpanToHours(ts: string): number {
  const [h] = ts.split(':').map(Number);
  return h || 0;
}
function hoursToTimeSpan(hours: number): string {
  return `${String(hours).padStart(2, '0')}:00:00`;
}

const toggleFields: { key: 'detectMissingClockOut' | 'detectLateArrival' | 'detectDoubleClockIn' | 'detectExcessiveShift' | 'detectMissingClockIn' | 'detectWorkOnDayOff'; label: string }[] = [
  { key: 'detectMissingClockIn', label: 'Brak rejestracji wejścia' },
  { key: 'detectMissingClockOut', label: 'Brak rejestracji wyjścia' },
  { key: 'detectLateArrival', label: 'Spóźnione wejście' },
  { key: 'detectDoubleClockIn', label: 'Podwójne wejście' },
  { key: 'detectExcessiveShift', label: 'Zbyt długa zmiana' },
  { key: 'detectWorkOnDayOff', label: 'Praca w dniu wolnym' },
];

export function TimeTrackingSettingsPage() {
  const { data, isLoading, error, refetch, isFetching } = useTimeTrackingSettings();
  const updateMutation = useUpdateTimeTrackingSettings();
  const mobile = useIsMobile();

  const [lateMinutes, setLateMinutes] = useState(15);
  const [excessiveHours, setExcessiveHours] = useState(12);
  const [toggles, setToggles] = useState<Record<string, boolean>>({});

  useEffect(() => {
    if (!data) return;
    setLateMinutes(timeSpanToMinutes(data.lateArrivalThreshold));
    setExcessiveHours(timeSpanToHours(data.excessiveShiftThreshold));
    setToggles({
      detectMissingClockOut: data.detectMissingClockOut,
      detectLateArrival: data.detectLateArrival,
      detectDoubleClockIn: data.detectDoubleClockIn,
      detectExcessiveShift: data.detectExcessiveShift,
      detectMissingClockIn: data.detectMissingClockIn,
      detectWorkOnDayOff: data.detectWorkOnDayOff,
    });
  }, [data]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate({
      lateArrivalThreshold: minutesToTimeSpan(lateMinutes),
      excessiveShiftThreshold: hoursToTimeSpan(excessiveHours),
      detectMissingClockOut: !!toggles.detectMissingClockOut,
      detectLateArrival: !!toggles.detectLateArrival,
      detectDoubleClockIn: !!toggles.detectDoubleClockIn,
      detectExcessiveShift: !!toggles.detectExcessiveShift,
      detectMissingClockIn: !!toggles.detectMissingClockIn,
      detectWorkOnDayOff: !!toggles.detectWorkOnDayOff,
    });
  };

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '600px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: colors.gray[900], display: 'flex', alignItems: 'center', gap: '8px' }}>
          <AlarmClock size={22} /> Ustawienia czasu pracy
        </h1>
        <button
          onClick={() => refetch()}
          style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            padding: '7px 12px', fontSize: '13px', fontWeight: 500,
            color: colors.gray[700], backgroundColor: colors.white,
            border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', cursor: 'pointer',
          }}
          title="Odśwież"
        >
          <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
        </button>
      </div>

      <p style={{ fontSize: '13px', color: colors.gray[500], marginTop: '-12px', marginBottom: '20px' }}>
        Progi i przełączniki wykrywania anomalii rejestracji czasu pracy (nocne zmiany, spóźnienia, braki wejścia/wyjścia).
      </p>

      {error && (
        <div style={{ padding: '12px 16px', marginBottom: '16px', borderRadius: '8px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px' }}>
          Błąd ładowania ustawień.
        </div>
      )}
      {updateMutation.isError && (
        <div style={{ padding: '12px 16px', marginBottom: '16px', borderRadius: '8px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px' }}>
          {updateMutation.error instanceof Error ? updateMutation.error.message : 'Błąd zapisu ustawień.'}
        </div>
      )}
      {updateMutation.isSuccess && (
        <div style={{ padding: '10px 16px', marginBottom: '16px', borderRadius: '8px', backgroundColor: colors.success[100], border: `1px solid ${colors.success[200]}`, color: colors.success[800], fontSize: '13px' }}>
          Zapisano.
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
      ) : (
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Próg spóźnienia (minuty)
              <input type="number" min={0} max={240} value={lateMinutes} onChange={(e) => setLateMinutes(Number(e.target.value))} style={inputStyle} />
            </label>
            <label style={labelStyle}>
              Próg zbyt długiej zmiany (godziny)
              <input type="number" min={4} max={24} value={excessiveHours} onChange={(e) => setExcessiveHours(Number(e.target.value))} style={inputStyle} />
            </label>
          </div>

          <div>
            <div style={{ fontSize: '13px', fontWeight: 600, color: colors.gray[700], marginBottom: '8px' }}>Wykrywane anomalie</div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {toggleFields.map(({ key, label }) => (
                <label key={key} style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', color: colors.gray[700], cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={!!toggles[key]}
                    onChange={(e) => setToggles((prev) => ({ ...prev, [key]: e.target.checked }))}
                  />
                  {label}
                </label>
              ))}
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '8px' }}>
            <button
              type="submit"
              disabled={updateMutation.isPending}
              style={{
                padding: '9px 22px', fontSize: '14px', fontWeight: 500,
                color: colors.white, backgroundColor: colors.primary[600], border: 'none',
                borderRadius: '6px', cursor: updateMutation.isPending ? 'not-allowed' : 'pointer',
                opacity: updateMutation.isPending ? 0.6 : 1,
              }}
            >
              {updateMutation.isPending ? 'Zapisywanie...' : 'Zapisz'}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

const labelStyle: React.CSSProperties = {
  display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: colors.gray[700],
};

const inputStyle: React.CSSProperties = {
  width: '100%', padding: '8px 12px', fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', boxSizing: 'border-box',
};
