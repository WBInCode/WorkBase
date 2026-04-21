import { useState, useEffect, useCallback } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { useGenerateQrToken, type QrTokenDto } from '@/api/hooks/useTimeTracking';

interface KioskQrDisplayProps {
  locationId?: string;
  ttlSeconds?: number;
}

export function KioskQrDisplay({ locationId, ttlSeconds = 25 }: KioskQrDisplayProps) {
  const generateQr = useGenerateQrToken();
  const [token, setToken] = useState<QrTokenDto | null>(null);
  const [secondsLeft, setSecondsLeft] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    try {
      setError(null);
      const result = await generateQr.mutateAsync({ locationId, ttlSeconds });
      setToken(result);
      setSecondsLeft(ttlSeconds);
    } catch {
      setError('Nie udało się wygenerować kodu QR');
    }
  }, [generateQr, locationId, ttlSeconds]);

  // Generate first token on mount
  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Countdown + auto-refresh
  useEffect(() => {
    if (!token) return;
    const id = setInterval(() => {
      setSecondsLeft(prev => {
        if (prev <= 1) {
          refresh();
          return ttlSeconds;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(id);
  }, [token, ttlSeconds, refresh]);

  if (error) {
    return (
      <div style={{ textAlign: 'center', padding: '32px' }}>
        <div style={{ color: '#f87171', fontSize: '16px', marginBottom: '16px' }}>{error}</div>
        <button
          onClick={refresh}
          style={{
            padding: '10px 24px',
            borderRadius: '8px',
            border: 'none',
            backgroundColor: '#6366f1',
            color: '#fff',
            fontSize: '14px',
            cursor: 'pointer',
          }}
        >
          Ponów
        </button>
      </div>
    );
  }

  if (!token) {
    return <div style={{ color: '#94a3b8', fontSize: '16px', padding: '32px' }}>Generowanie kodu QR...</div>;
  }

  const progress = secondsLeft / ttlSeconds;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '16px' }}>
      <div style={{
        padding: '20px',
        backgroundColor: '#ffffff',
        borderRadius: '20px',
        boxShadow: '0 4px 24px rgba(99,102,241,0.15)',
        position: 'relative',
      }}>
        <QRCodeSVG
          value={token.token}
          size={220}
          level="M"
          bgColor="#ffffff"
          fgColor="#0f172a"
        />
        {/* Circular progress */}
        <svg
          width="40"
          height="40"
          viewBox="0 0 40 40"
          style={{ position: 'absolute', top: '-12px', right: '-12px' }}
        >
          <circle cx="20" cy="20" r="16" fill="#0f172a" stroke="#334155" strokeWidth="3" />
          <circle
            cx="20" cy="20" r="16"
            fill="none"
            stroke={progress > 0.3 ? '#6366f1' : '#ef4444'}
            strokeWidth="3"
            strokeDasharray={`${progress * 100.5} 100.5`}
            strokeLinecap="round"
            transform="rotate(-90 20 20)"
          />
          <text x="20" y="24" textAnchor="middle" fill="#fff" fontSize="14" fontWeight="600">
            {secondsLeft}
          </text>
        </svg>
      </div>
      <div style={{ fontSize: '14px', color: '#94a3b8' }}>
        Zeskanuj telefonem aby się odbić
      </div>
    </div>
  );
}
