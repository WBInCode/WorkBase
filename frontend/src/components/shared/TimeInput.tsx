import { useState, useRef, useCallback, type CSSProperties } from 'react';

interface TimeInputProps {
  value: string; // "HH:mm"
  onChange: (value: string) => void;
  style?: CSSProperties;
  disabled?: boolean;
}

export default function TimeInput({ value, onChange, style, disabled }: TimeInputProps) {
  const [display, setDisplay] = useState(value || '');
  const inputRef = useRef<HTMLInputElement>(null);

  const normalize = useCallback((raw: string): string | null => {
    const clean = raw.replace(/[^0-9:]/g, '');

    // Auto-insert colon: "08" → "08:", "0800" → "08:00"
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

    // Live update if valid
    const normalized = normalize(raw);
    if (normalized) {
      onChange(normalized);
    }
  };

  const handleBlur = () => {
    const normalized = normalize(display);
    if (normalized) {
      setDisplay(normalized);
      onChange(normalized);
    } else {
      // Revert to last valid value
      setDisplay(value || '');
    }
  };

  // Sync external value changes
  if (value !== display && value !== normalize(display)) {
    // Only sync if it's an external change, not our own edit
    if (document.activeElement !== inputRef.current) {
      if (display !== value) {
        // Will be handled on next render via useEffect alternative
      }
    }
  }

  return (
    <input
      ref={inputRef}
      type="text"
      inputMode="numeric"
      placeholder="HH:mm"
      maxLength={5}
      value={document.activeElement === inputRef.current ? display : (value || '')}
      onChange={handleChange}
      onBlur={handleBlur}
      onFocus={() => setDisplay(value || '')}
      style={style}
      disabled={disabled}
    />
  );
}
