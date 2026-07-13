import { useState, useEffect, type ReactNode } from 'react';
import { Clock, Coffee, Play, Square, CalendarDays } from 'lucide-react';
import type { TimeStatusDto } from '@/api/types/time';
import { colors } from '@/theme/tokens';

const BREAK_TYPE_LABELS: Record<string, string> = {
  Paid: 'Przerwa płatna',
  Unpaid: 'Przerwa bezpłatna',
};

interface Props {
  data: TimeStatusDto | undefined;
  isLoading: boolean;
  greeting: string;
  name: string;
  /** Slot na ClockButton (akcje wejścia/wyjścia/przerwy) — logika zostaje w jednym miejscu. */
  actions?: ReactNode;
}

const STATUS_MAP: Record<string, { label: string; color: string; bg: string; icon: typeof Clock }> = {
  'not-started': { label: 'Nie rozpoczęto', color: colors.gray[500], bg: colors.gray[100], icon: Play },
  'working': { label: 'W pracy', color: colors.success[600], bg: colors.success[100], icon: Clock },
  'on-break': { label: 'Przerwa', color: colors.warning[600], bg: colors.warning[100], icon: Coffee },
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

/**
 * Hero "Mój dzień" — pełnoszerokościowa karta otwierająca workspace:
 * powitanie + data po lewej, wielki licznik czasu pracy + status + akcje po prawej.
 */
export function MyDayOverview({ data, isLoading, greeting, name, actions }: Props) {
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

  const cfg = STATUS_MAP[status] ?? STATUS_MAP['not-started']!;
  const Icon = cfg.icon;
  const dateLabel = new Date().toLocaleDateString('pl-PL', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });

  const statusLabel = data?.status === 'on-break' && data.currentBreakType
    ? BREAK_TYPE_LABELS[data.currentBreakType] ?? cfg.label
    : cfg.label;

  return (
    <section
      style={{
        position: 'relative',
        overflow: 'hidden',
        backgroundColor: colors.white,
        borderRadius: '24px',
        border: `1px solid ${colors.gray[200]}`,
        boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 14px 40px -14px rgba(20,25,43,0.14), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
        padding: 'clamp(20px, 3vw, 30px)',
      }}
    >
      {/* Dekoracyjne bloby */}
      <div aria-hidden style={{
        position: 'absolute', top: -80, right: -60, width: 280, height: 280, borderRadius: '50%',
        background: 'radial-gradient(circle, rgba(61,109,242,0.10), transparent 65%)',
        pointerEvents: 'none',
      }} />
      <div aria-hidden style={{
        position: 'absolute', bottom: -100, right: 180, width: 240, height: 240, borderRadius: '50%',
        background: 'radial-gradient(circle, rgba(139,92,246,0.07), transparent 65%)',
        pointerEvents: 'none',
      }} />

      <div style={{
        position: 'relative',
        display: 'flex',
        flexWrap: 'wrap',
        alignItems: 'center',
        gap: '20px 32px',
        justifyContent: 'space-between',
      }}>
        {/* Powitanie */}
        <div style={{ minWidth: 220 }}>
          <h1 style={{ margin: 0, fontSize: 'clamp(21px, 2.4vw, 26px)', fontWeight: 800, color: colors.gray[900], letterSpacing: '-0.02em' }}>
            {greeting}, {name}
          </h1>
          <p style={{ margin: '6px 0 0', fontSize: '13.5px', color: colors.gray[500], display: 'flex', alignItems: 'center', gap: 6 }}>
            <CalendarDays size={14} />
            {dateLabel}
          </p>
          <div style={{
            display: 'inline-flex', alignItems: 'center', gap: 8, marginTop: 14,
            padding: '6px 14px 6px 8px', borderRadius: 999, backgroundColor: cfg.bg,
          }}>
            <span style={{
              width: 26, height: 26, borderRadius: '50%', backgroundColor: colors.white,
              display: 'inline-flex', alignItems: 'center', justifyContent: 'center', color: cfg.color,
              boxShadow: '0 1px 3px rgba(20,25,43,0.12)',
            }}>
              <Icon size={14} />
            </span>
            <span style={{ fontSize: 13, fontWeight: 700, color: cfg.color }}>
              {isLoading ? 'Ładowanie…' : statusLabel}
            </span>
            {data?.lastEntryTime && (
              <span style={{ fontSize: 12, color: cfg.color, opacity: 0.75 }}>
                od {new Date(data.lastEntryTime).toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' })}
              </span>
            )}
          </div>
        </div>

        {/* Licznik + akcje */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '28px', flexWrap: 'wrap' }}>
          <div style={{ display: 'flex', gap: 28 }}>
            <div>
              <div style={{ fontSize: 11, fontWeight: 700, color: colors.gray[400], textTransform: 'uppercase', letterSpacing: '0.07em' }}>
                Przepracowano
              </div>
              <div className="wb-tnum" style={{
                fontSize: 'clamp(30px, 3.6vw, 40px)', fontWeight: 800, letterSpacing: '-0.02em',
                color: isWorking ? colors.gray[900] : colors.gray[600], lineHeight: 1.15,
              }}>
                {formatDuration(elapsed)}
              </div>
            </div>
            <div style={{ paddingTop: 2 }}>
              <div style={{ fontSize: 11, fontWeight: 700, color: colors.gray[400], textTransform: 'uppercase', letterSpacing: '0.07em' }}>
                Przerwy
              </div>
              <div className="wb-tnum" style={{ fontSize: 'clamp(20px, 2.4vw, 26px)', fontWeight: 700, color: colors.gray[500], lineHeight: 1.4 }}>
                {formatDuration(parseDuration(data?.breaksToday ?? '0:0'))}
              </div>
            </div>
          </div>

          {actions && <div style={{ flexShrink: 0 }}>{actions}</div>}
        </div>
      </div>
    </section>
  );
}
