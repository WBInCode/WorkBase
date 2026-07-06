import { useState, useEffect } from 'react';
import { Clock, Coffee, Play, Square } from 'lucide-react';
import type { TimeStatusDto } from '@/api/types/time';
import { colors } from '@/theme/tokens';

const BREAK_TYPE_LABELS: Record<string, string> = {
  Paid: 'Przerwa płatna',
  Unpaid: 'Przerwa bezpłatna',
};

interface Props {
  data: TimeStatusDto | undefined;
  isLoading: boolean;
}

const STATUS_MAP: Record<string, { label: string; color: string; bg: string; icon: typeof Clock }> = {
  'not-started': { label: 'Nie rozpoczęto', color: colors.gray[500], bg: colors.gray[100], icon: Play },
  'working': { label: 'W pracy', color: colors.success[600], bg: colors.success[100], icon: Clock },
  'on-break': { label: 'Przerwa', color: colors.warning[500], bg: colors.warning[100], icon: Coffee },
  'ended': { label: 'Zakończono', color: colors.gray[500], bg: colors.gray[100], icon: Square },
};

function parseDuration(duration: string): number {
  const parts = duration.split(':');
  if (parts.length < 2) return 0;
  let hours = 0, minutes = 0, seconds = 0;
  if (parts.length === 3) {
    const hourPart = parts[0] ?? '';
    if (hourPart.includes('.')) {
      const [days, h] = hourPart.split('.');
      hours = parseInt(days ?? '0') * 24 + parseInt(h ?? '0');
    } else {
      hours = parseInt(hourPart);
    }
    minutes = parseInt(parts[1] ?? '0');
    seconds = parseInt(parts[2] ?? '0');
  } else {
    minutes = parseInt(parts[0] ?? '0');
    seconds = parseInt(parts[1] ?? '0');
  }
  return hours * 3600 + minutes * 60 + seconds;
}

function formatDuration(totalSeconds: number): string {
  const s = Math.floor(totalSeconds);
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = s % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`;
}

export function MyDayOverview({ data, isLoading }: Props) {
  const [elapsed, setElapsed] = useState(0);

  const status = data?.status ?? 'not-started';
  const isWorking = status === 'working';

  useEffect(() => {
    if (!data?.workedToday) { setElapsed(0); return; }

    const base = parseDuration(data.workedToday);
    setElapsed(base);

    if (!isWorking) { return; }

    const interval = setInterval(() => setElapsed((p) => p + 1), 1000);
    return () => clearInterval(interval);
  }, [data, isWorking]);

  if (isLoading || !data) {
    return (
      <div style={cardStyle}>
        <div style={{ fontSize: '14px', fontWeight: 600, color: colors.gray[700], marginBottom: '12px' }}>Mój dzień</div>
        <div style={{ padding: '20px', textAlign: 'center', color: colors.gray[400], fontSize: '14px' }}>Ładowanie...</div>
      </div>
    );
  }

  const cfg = STATUS_MAP[data.status] ?? STATUS_MAP['not-started'];
  if (!cfg) return null;
  const Icon = cfg.icon;

  return (
    <div style={cardStyle}>
      <div style={{ fontSize: '14px', fontWeight: 600, color: colors.gray[700], marginBottom: '12px' }}>Mój dzień</div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '16px' }}>
        <div style={{
          width: '48px', height: '48px', borderRadius: '12px',
          backgroundColor: cfg.bg, display: 'flex', alignItems: 'center', justifyContent: 'center',
        }}>
          <Icon size={24} color={cfg.color} />
        </div>
        <div>
          <div style={{ fontSize: '18px', fontWeight: 700, color: cfg.color }}>
            {data.status === 'on-break' && data.currentBreakType
              ? BREAK_TYPE_LABELS[data.currentBreakType] ?? cfg.label
              : cfg.label}
          </div>
          {data.lastEntryTime && (
            <div style={{ fontSize: '12px', color: colors.gray[400] }}>
              od {new Date(data.lastEntryTime).toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' })}
            </div>
          )}
        </div>
      </div>

      <div style={{ display: 'flex', gap: '24px' }}>
        <div>
          <div style={{ fontSize: '11px', color: colors.gray[400], textTransform: 'uppercase', letterSpacing: '0.5px' }}>Przepracowano</div>
          <div style={{ fontSize: '20px', fontWeight: 600, color: colors.gray[900], fontVariantNumeric: 'tabular-nums' }}>{formatDuration(elapsed)}</div>
        </div>
        <div>
          <div style={{ fontSize: '11px', color: colors.gray[400], textTransform: 'uppercase', letterSpacing: '0.5px' }}>Przerwy</div>
          <div style={{ fontSize: '20px', fontWeight: 600, color: colors.gray[500], fontVariantNumeric: 'tabular-nums' }}>{formatDuration(parseDuration(data.breaksToday))}</div>
        </div>
      </div>
    </div>
  );
}

const cardStyle: React.CSSProperties = {
  backgroundColor: colors.white, borderRadius: '12px', border: `1px solid ${colors.gray[200]}`,
  padding: '20px', boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
};
