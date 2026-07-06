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

  const normalize = useCallback((raw: string): string | null => {
    const clean = raw.replace(/[^0-9:]/g, '');
    let digits = clean.replace(':', '');
    if (digits.length > 4) digits = digits.slice(0, 4);
    if (digits.length === 0) return null;

    let h: number, m: number;
    if (digits.length <= 2) {
      h = parseInt(digits, 10);
      m = 0;
    } else {
      h = parseInt(digits.slice(0, 2), 10);
      m = parseInt(digits.slice(2), 10);
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
    setTimeout(() => {
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
        item?.scrollIntoView({ block: 'center' });
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
    <div ref={containerRef} style={{ position: 'relative', display: 'inline-block' }}>
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
            borderRadius: '8px',
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
