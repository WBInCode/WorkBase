import { QRCodeSVG } from 'qrcode.react';
import { colors } from '@/theme/tokens';

interface MyQrBadgeProps {
  employeeId: string;
  employeeName: string;
  employeeNumber?: string;
}

export function MyQrBadge({ employeeId, employeeName, employeeNumber }: MyQrBadgeProps) {
  // The QR encodes the employee number (for kiosk scanner) or employee ID as fallback
  const qrValue = employeeNumber || employeeId;

  return (
    <div style={{
      backgroundColor: colors.white,
      borderRadius: '12px',
      border: `1px solid ${colors.gray[200]}`,
      padding: '20px',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '16px' }}>
        <span style={{ fontSize: '16px' }}>🪪</span>
        <h3 style={{ margin: 0, fontSize: '15px', fontWeight: 600, color: colors.gray[900] }}>
          Moja karta QR
        </h3>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '20px', flexWrap: 'wrap' }}>
        <div style={{
          padding: '12px',
          backgroundColor: '#f8fafc',
          borderRadius: '12px',
          border: '1px solid #e2e8f0',
          flexShrink: 0,
        }}>
          <QRCodeSVG
            value={qrValue}
            size={120}
            level="M"
            bgColor="#f8fafc"
            fgColor={colors.slate[900]}
          />
        </div>

        <div>
          <div style={{ fontSize: '16px', fontWeight: 600, color: colors.gray[900], marginBottom: '4px' }}>
            {employeeName}
          </div>
          {employeeNumber && (
            <div style={{ fontSize: '13px', color: colors.gray[500], marginBottom: '12px' }}>
              Nr: {employeeNumber}
            </div>
          )}
          <div style={{ fontSize: '12px', color: colors.gray[400], lineHeight: 1.5 }}>
            Pokaż ten kod na kiosku<br />aby szybko się odbić
          </div>
        </div>
      </div>
    </div>
  );
}
