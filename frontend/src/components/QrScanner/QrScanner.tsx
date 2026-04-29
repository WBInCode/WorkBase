import { useState, useEffect, useRef, useCallback } from 'react';
import { Camera, XCircle, RefreshCw } from 'lucide-react';

interface QrScannerProps {
  onScan: (value: string) => void;
  onClose: () => void;
  facingMode?: 'user' | 'environment';
}

export function QrScanner({ onScan, onClose, facingMode = 'environment' }: QrScannerProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [scanning, setScanning] = useState(false);
  const scanningRef = useRef(false);

  const startCamera = useCallback(async () => {
    try {
      setError(null);
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode, width: { ideal: 640 }, height: { ideal: 480 } },
      });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        await videoRef.current.play();
        setScanning(true);
        scanningRef.current = true;
      }
    } catch {
      setError('Brak dostępu do kamery. Sprawdź uprawnienia przeglądarki.');
    }
  }, []);

  const stopCamera = useCallback(() => {
    scanningRef.current = false;
    setScanning(false);
    if (streamRef.current) {
      streamRef.current.getTracks().forEach((t) => t.stop());
      streamRef.current = null;
    }
  }, []);

  // BarcodeDetector-based scanning loop
  useEffect(() => {
    startCamera();
    return () => stopCamera();
  }, [startCamera, stopCamera]);

  useEffect(() => {
    if (!scanning) return;

    const hasBarcodeDetector = 'BarcodeDetector' in window;
    let rafId: number;

    if (hasBarcodeDetector) {
      // Use native BarcodeDetector API (Chrome, Edge, Android)
      const detector = new (window as unknown as { BarcodeDetector: new (opts: { formats: string[] }) => { detect: (source: HTMLVideoElement) => Promise<Array<{ rawValue: string }>> } }).BarcodeDetector({ formats: ['qr_code'] });

      const tick = async () => {
        if (!scanningRef.current || !videoRef.current) return;
        try {
          const barcodes = await detector.detect(videoRef.current);
          if (barcodes.length > 0) {
            const value = barcodes[0]?.rawValue;
            if (value) {
              stopCamera();
              onScan(value);
              return;
            }
          }
        } catch {
          // frame not ready
        }
        rafId = requestAnimationFrame(tick);
      };
      rafId = requestAnimationFrame(tick);
    } else {
      // Fallback: canvas-based frame capture for manual processing
      // Without BarcodeDetector, we show a message to manually enter code
      setError('Twoja przeglądarka nie wspiera skanowania QR. Użyj Chrome lub Edge.');
    }

    return () => {
      if (rafId) cancelAnimationFrame(rafId);
    };
  }, [scanning, onScan, stopCamera]);

  return (
    <div style={overlayStyle}>
      <div style={containerStyle}>
        {/* Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600, color: '#f8fafc' }}>
            <Camera size={20} style={{ verticalAlign: 'middle', marginRight: '8px' }} />
            Skanuj kod QR
          </h2>
          <button onClick={() => { stopCamera(); onClose(); }} style={closeBtnStyle}>
            <XCircle size={22} />
          </button>
        </div>

        {/* Error */}
        {error && (
          <div style={{ padding: '12px', borderRadius: '8px', backgroundColor: 'rgba(239,68,68,0.15)', color: '#fca5a5', fontSize: '13px', marginBottom: '12px' }}>
            {error}
            <button onClick={startCamera} style={{ marginLeft: '8px', color: '#93c5fd', background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: '13px' }}>
              <RefreshCw size={12} style={{ verticalAlign: 'middle', marginRight: '4px' }} />
              Ponów
            </button>
          </div>
        )}

        {/* Video */}
        <div style={{ position: 'relative', borderRadius: '12px', overflow: 'hidden', backgroundColor: '#000', aspectRatio: '4/3' }}>
          <video
            ref={videoRef}
            style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
            playsInline
            muted
          />
          {/* Scan guide overlay */}
          <div style={{
            position: 'absolute', inset: 0,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            pointerEvents: 'none',
          }}>
            <div style={{
              width: 'min(200px, 60vw)', height: 'min(200px, 60vw)',
              border: '3px solid rgba(99,102,241,0.7)',
              borderRadius: '16px',
              boxShadow: '0 0 0 9999px rgba(0,0,0,0.4)',
            }} />
          </div>
          {scanning && (
            <div style={{
              position: 'absolute', bottom: '12px', left: '50%', transform: 'translateX(-50%)',
              padding: '6px 16px', borderRadius: '20px',
              backgroundColor: 'rgba(0,0,0,0.6)', color: '#94a3b8', fontSize: '12px', fontWeight: 500,
            }}>
              Umieść kod QR w ramce
            </div>
          )}
        </div>

        <canvas ref={canvasRef} style={{ display: 'none' }} />
      </div>
    </div>
  );
}

const overlayStyle: React.CSSProperties = {
  position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.85)',
  display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 200,
};
const containerStyle: React.CSSProperties = {
  width: '380px', maxWidth: '95vw', padding: '20px',
  backgroundColor: '#1e293b', borderRadius: '16px',
  boxShadow: '0 20px 60px rgba(0,0,0,0.5)',
};
const closeBtnStyle: React.CSSProperties = {
  background: 'none', border: 'none', cursor: 'pointer', color: '#94a3b8',
};
