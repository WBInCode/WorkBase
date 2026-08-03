import { useState, useRef, useCallback, useEffect, type CSSProperties } from 'react';
import { Clock } from 'lucide-react';
import { colors } from '@/theme/tokens';

interface TimeInputProps {
  value: string; // "HH:mm"
  onChange: (value: string) => void;
  style?: CSSProperties;
  disabled?: boolean;
}

// Generate time slots every 15 minutes
const TIME_SLOTS: string[] = [];
for (let h = 0; h < 24; h++) {
  for (const m of [0, 15, 30, 45]) {
    TIME_SLOTS.push(`${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`);
  }
}

export default function TimeInput({ value, onChange, style, disabled }: TimeInputProps) {
  const [display, setDisplay] = useState(value || '');
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const blurTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  // Odlozony blur wolal setState po odmontowaniu komponentu.
  useEffect(() => () => clearTimeout(blurTimerRef.current), []);

  const normalize = useCallback((raw: string): string | null => {
    const clean = raw.replace(/[^0-9:]/g, '');
    if (clean.length === 0) return null;

    let h: number, m: number;
    // Dwukropek wpisany przez uzytkownika jednoznacznie dzieli godziny od minut.
    // Bez tego „9:00" bylo czytane jako cyfry „900" -> godzina 90 -> odrzucone.
    const idx = clean.indexOf(':');
    if (idx >= 0) {
      const hh = clean.slice(0, idx);
      const mm = clean.slice(idx + 1).replace(/:/g, '').slice(0, 2);
      if (hh.length === 0) return null;
      h = parseInt(hh, 10);
      m = mm.length === 0 ? 0 : parseInt(mm.padEnd(2, '0'), 10);
    } else {
      const digits = clean.slice(0, 4);
      if (digits.length <= 2) {
        // „9" -> 09:00, „17" -> 17:00
        h = parseInt(digits, 10);
        m = 0;
      } else {
        // „930" -> 09:30 (jedna cyfra godziny), „1730" -> 17:30
        const podzial = digits.length === 3 ? 1 : 2;
        h = parseInt(digits.slice(0, podzial), 10);
        m = parseInt(digits.slice(podzial), 10);
      }
    }

    if (isNaN(h) || isNaN(m) || h > 23 || m > 59) return null;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
  }, []);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value;
    setDisplay(raw);
    const normalized = normalize(raw);
    if (normalized) onChange(normalized);
  };

  const handleBlur = () => {
    // Delay to allow click on dropdown item
    clearTimeout(blurTimerRef.current);
    blurTimerRef.current = setTimeout(() => {
      if (!containerRef.current?.contains(document.activeElement)) {
        setOpen(false);
        const normalized = normalize(display);
        if (normalized) {
          setDisplay(normalized);
          onChange(normalized);
        } else {
          setDisplay(value || '');
        }
      }
    }, 150);
  };

  const handleSelect = (slot: string) => {
    setDisplay(slot);
    onChange(slot);
    setOpen(false);
    inputRef.current?.focus();
  };

  // Scroll to current value when dropdown opens
  useEffect(() => {
    if (open && listRef.current && value) {
      const idx = TIME_SLOTS.indexOf(value);
      if (idx >= 0) {
        const item = listRef.current.children[idx] as HTMLElement;
        const centeredOffset = item.offsetTop - (listRef.current.clientHeight - item.offsetHeight) / 2;
        listRef.current.scrollTop = Math.max(0, centeredOffset);
      }
    }
  }, [open, value]);

  // Close on outside click
  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  return (
    // Korzen musi wypelniac kontener: wszystkie miejsca uzycia podaja polu
    // `width: 100%`, ale przy `inline-block` liczylo sie ono od szerokosci
    // zawartosci, wiec pola i tak nie siegaly krawedzi.
    <div ref={containerRef} style={{ position: 'relative', display: 'block', width: '100%' }}>
      <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
        <input
          ref={inputRef}
          type="text"
          inputMode="numeric"
          placeholder="HH:mm"
          maxLength={5}
          value={document.activeElement === inputRef.current ? display : (value || '')}
          onChange={handleChange}
          onBlur={handleBlur}
          onFocus={() => { setDisplay(value || ''); setOpen(true); }}
          style={{
            ...style,
            paddingRight: '32px',
          }}
          disabled={disabled}
        />
        <button
          type="button"
          tabIndex={-1}
          onClick={() => { if (!disabled) { setOpen(!open); inputRef.current?.focus(); } }}
          style={{
            position: 'absolute',
            right: '6px',
            top: '50%',
            transform: 'translateY(-50%)',
            background: 'none',
            border: 'none',
            cursor: disabled ? 'default' : 'pointer',
            padding: '2px',
            color: colors.gray[400],
            display: 'flex',
            alignItems: 'center',
          }}
        >
          <Clock size={14} />
        </button>
      </div>

      {open && !disabled && (
        <div
          ref={listRef}
          style={{
            position: 'absolute',
            top: '100%',
            left: 0,
            zIndex: 50,
            width: '100%',
            minWidth: '90px',
            maxHeight: '200px',
            overflowY: 'auto',
            background: colors.white,
            border: `1px solid ${colors.gray[200]}`,
            borderRadius: '12px',
            boxShadow: '0 4px 12px rgba(0,0,0,0.12)',
            marginTop: '4px',
          }}
        >
          {TIME_SLOTS.map((slot) => (
            <div
              key={slot}
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => handleSelect(slot)}
              style={{
                padding: '6px 12px',
                fontSize: '13px',
                cursor: 'pointer',
                background: slot === value ? '#7c3aed' : 'transparent',
                color: slot === value ? colors.white : colors.gray[700],
                fontWeight: slot === value ? 600 : 400,
              }}
              onMouseEnter={(e) => {
                if (slot !== value) {
                  e.currentTarget.style.background = colors.gray[100];
                }
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = slot === value ? '#7c3aed' : 'transparent';
              }}
            >
              {slot}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
