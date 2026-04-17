import { useState, type FormEvent } from 'react';
import { X } from 'lucide-react';
import type { LeaveTypeDto } from '@/api/types/leave';

interface LeaveRequestFormProps {
  employeeId: string;
  leaveTypes: LeaveTypeDto[];
  onSubmit: (data: {
    leaveTypeId: string;
    startDate: string;
    endDate: string;
    totalDays: number;
    reason?: string;
  }) => void;
  onClose: () => void;
  isSubmitting: boolean;
  error: string | null;
}

function countBusinessDays(start: string, end: string): number {
  if (!start || !end) return 0;
  const s = new Date(start);
  const e = new Date(end);
  if (s > e) return 0;
  let count = 0;
  const cur = new Date(s);
  while (cur <= e) {
    const day = cur.getDay();
    if (day !== 0 && day !== 6) count++;
    cur.setDate(cur.getDate() + 1);
  }
  return count;
}

export function LeaveRequestForm({
  leaveTypes,
  onSubmit,
  onClose,
  isSubmitting,
  error,
}: LeaveRequestFormProps) {
  const [leaveTypeId, setLeaveTypeId] = useState('');
  const today = new Date().toISOString().split('T')[0];
  const [startDate, setStartDate] = useState(today);
  const [endDate, setEndDate] = useState(today);
  const [reason, setReason] = useState('');

  const totalDays = countBusinessDays(startDate, endDate);
  const selectedType = leaveTypes.find((t) => t.id === leaveTypeId);

  const isValid = leaveTypeId && startDate && endDate && totalDays > 0;

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!isValid) return;
    onSubmit({
      leaveTypeId,
      startDate: new Date(startDate).toISOString(),
      endDate: new Date(endDate).toISOString(),
      totalDays,
      reason: reason.trim() || undefined,
    });
  };

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        backgroundColor: 'rgba(0,0,0,0.4)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1000,
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
      role="dialog"
      aria-modal="true"
      aria-label="Nowy wniosek urlopowy"
    >
      <div
        style={{
          backgroundColor: '#fff',
          borderRadius: '12px',
          width: '100%',
          maxWidth: '480px',
          boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
        }}
      >
        {/* Header */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '20px 24px 16px',
            borderBottom: '1px solid #e5e7eb',
          }}
        >
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600, color: '#111827' }}>
            Nowy wniosek urlopowy
          </h2>
          <button
            onClick={onClose}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: '#6b7280',
              padding: '4px',
              display: 'inline-flex',
              borderRadius: '4px',
            }}
            aria-label="Zamknij"
          >
            <X size={20} />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} style={{ padding: '20px 24px 24px' }}>
          {error && (
            <div
              style={{
                padding: '10px 14px',
                backgroundColor: '#fef2f2',
                border: '1px solid #fecaca',
                borderRadius: '6px',
                color: '#dc2626',
                fontSize: '13px',
                marginBottom: '16px',
              }}
            >
              {error}
            </div>
          )}

          <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
            {/* Leave type */}
            <FieldGroup label="Typ nieobecności" required>
              <select
                value={leaveTypeId}
                onChange={(e) => setLeaveTypeId(e.target.value)}
                required
                style={inputStyle}
              >
                <option value="">— wybierz —</option>
                {leaveTypes.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                    {t.defaultDaysPerYear != null ? ` (${t.defaultDaysPerYear} dni/rok)` : ''}
                  </option>
                ))}
              </select>
            </FieldGroup>

            {/* Date range */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <FieldGroup label="Data od" required>
                <input
                  type="date"
                  value={startDate}
                  onChange={(e) => {
                    setStartDate(e.target.value);
                    if (e.target.value > endDate) setEndDate(e.target.value);
                  }}
                  required
                  style={inputStyle}
                />
              </FieldGroup>
              <FieldGroup label="Data do" required>
                <input
                  type="date"
                  value={endDate}
                  min={startDate}
                  onChange={(e) => setEndDate(e.target.value)}
                  required
                  style={inputStyle}
                />
              </FieldGroup>
            </div>

            {/* Computed days info */}
            {totalDays > 0 && (
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  padding: '10px 14px',
                  backgroundColor: '#f0f9ff',
                  border: '1px solid #bae6fd',
                  borderRadius: '6px',
                  fontSize: '13px',
                  color: '#0369a1',
                }}
              >
                <span style={{ fontWeight: 600 }}>{totalDays}</span>
                <span>dni roboczych</span>
                {selectedType?.requiresApproval && (
                  <span
                    style={{
                      marginLeft: 'auto',
                      fontSize: '11px',
                      backgroundColor: '#fef3c7',
                      color: '#92400e',
                      padding: '2px 8px',
                      borderRadius: '4px',
                    }}
                  >
                    Wymaga akceptacji
                  </span>
                )}
                {selectedType && !selectedType.requiresApproval && (
                  <span
                    style={{
                      marginLeft: 'auto',
                      fontSize: '11px',
                      backgroundColor: '#d1fae5',
                      color: '#065f46',
                      padding: '2px 8px',
                      borderRadius: '4px',
                    }}
                  >
                    Auto-akceptacja
                  </span>
                )}
              </div>
            )}

            {/* Reason */}
            <FieldGroup label="Komentarz">
              <textarea
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder="Opcjonalny komentarz..."
                rows={3}
                style={{ ...inputStyle, resize: 'vertical', fontFamily: 'inherit' }}
              />
            </FieldGroup>
          </div>

          {/* Actions */}
          <div
            style={{
              display: 'flex',
              justifyContent: 'flex-end',
              gap: '10px',
              marginTop: '24px',
              paddingTop: '16px',
              borderTop: '1px solid #e5e7eb',
            }}
          >
            <button
              type="button"
              onClick={onClose}
              style={{
                padding: '8px 16px',
                fontSize: '14px',
                fontWeight: 500,
                color: '#374151',
                backgroundColor: '#fff',
                border: '1px solid #d1d5db',
                borderRadius: '6px',
                cursor: 'pointer',
              }}
            >
              Anuluj
            </button>
            <button
              type="submit"
              disabled={!isValid || isSubmitting}
              style={{
                padding: '8px 20px',
                fontSize: '14px',
                fontWeight: 500,
                color: '#fff',
                backgroundColor: !isValid || isSubmitting ? '#93c5fd' : '#2563eb',
                border: 'none',
                borderRadius: '6px',
                cursor: !isValid || isSubmitting ? 'not-allowed' : 'pointer',
              }}
            >
              {isSubmitting ? 'Wysyłanie...' : 'Złóż wniosek'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function FieldGroup({
  label,
  required,
  children,
}: {
  label: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label
        style={{
          display: 'block',
          marginBottom: '4px',
          fontSize: '13px',
          fontWeight: 500,
          color: '#374151',
        }}
      >
        {label}
        {required && <span style={{ color: '#dc2626', marginLeft: '2px' }}>*</span>}
      </label>
      {children}
    </div>
  );
}

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '8px 12px',
  fontSize: '14px',
  border: '1px solid #d1d5db',
  borderRadius: '6px',
  outline: 'none',
  boxSizing: 'border-box',
};
