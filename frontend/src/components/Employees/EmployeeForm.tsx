import { useState, type FormEvent } from 'react';
import { X } from 'lucide-react';
import type { CreateEmployeeRequest } from '@/api/types/organization';
import { colors } from '@/theme/tokens';

interface EmployeeFormProps {
  onSubmit: (data: CreateEmployeeRequest) => void;
  onClose: () => void;
  isSubmitting: boolean;
  error: string | null;
}

export function EmployeeForm({ onSubmit, onClose, isSubmitting, error }: EmployeeFormProps) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [employeeNumber, setEmployeeNumber] = useState('');
  const [hireDate, setHireDate] = useState(() => {
    const d = new Date();
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  });

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    onSubmit({
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      email: email.trim(),
      employeeNumber: employeeNumber.trim() || undefined,
      hireDate: new Date(hireDate ?? '').toISOString(),
    });
  };

  const isValid = firstName.trim() && lastName.trim() && email.trim() && hireDate;

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
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
      role="dialog"
      aria-modal="true"
      aria-label="Nowy pracownik"
    >
      <div
        style={{
          backgroundColor: colors.white,
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
            borderBottom: `1px solid ${colors.gray[200]}`,
          }}
        >
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600, color: colors.gray[900] }}>
            Nowy pracownik
          </h2>
          <button
            onClick={onClose}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: colors.gray[500],
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
                backgroundColor: colors.danger[50],
                border: `1px solid ${colors.danger[200]}`,
                borderRadius: '6px',
                color: colors.danger[600],
                fontSize: '13px',
                marginBottom: '16px',
              }}
            >
              {error}
            </div>
          )}

          <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
            {/* First + Last name row */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))', gap: '12px' }}>
              <FieldGroup label="Imię" required>
                <input
                  type="text"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder="Jan"
                  required
                  style={inputStyle}
                />
              </FieldGroup>
              <FieldGroup label="Nazwisko" required>
                <input
                  type="text"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder="Kowalski"
                  required
                  style={inputStyle}
                />
              </FieldGroup>
            </div>

            <FieldGroup label="Email" required>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="jan.kowalski@firma.pl"
                required
                style={inputStyle}
              />
            </FieldGroup>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))', gap: '12px' }}>
              <FieldGroup label="Numer pracownika">
                <input
                  type="text"
                  value={employeeNumber}
                  onChange={(e) => setEmployeeNumber(e.target.value)}
                  placeholder="EMP-001"
                  style={inputStyle}
                />
              </FieldGroup>
              <FieldGroup label="Data zatrudnienia" required>
                <input
                  type="date"
                  value={hireDate}
                  onChange={(e) => setHireDate(e.target.value)}
                  required
                  style={inputStyle}
                />
              </FieldGroup>
            </div>
          </div>

          {/* Actions */}
          <div
            style={{
              display: 'flex',
              justifyContent: 'flex-end',
              gap: '10px',
              marginTop: '24px',
              paddingTop: '16px',
              borderTop: `1px solid ${colors.gray[200]}`,
            }}
          >
            <button
              type="button"
              onClick={onClose}
              style={{
                padding: '8px 16px',
                fontSize: '14px',
                fontWeight: 500,
                color: colors.gray[700],
                backgroundColor: colors.white,
                border: `1px solid ${colors.gray[300]}`,
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
                color: colors.white,
                backgroundColor: (!isValid || isSubmitting) ? colors.primary[300] : colors.primary[600],
                border: 'none',
                borderRadius: '6px',
                cursor: (!isValid || isSubmitting) ? 'not-allowed' : 'pointer',
              }}
            >
              {isSubmitting ? 'Zapisywanie...' : 'Utwórz'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

/* Reusable field group */
function FieldGroup({ label, required, children }: { label: string; required?: boolean; children: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
      <label style={{ fontSize: '13px', fontWeight: 500, color: colors.gray[700] }}>
        {label}
        {required && <span style={{ color: colors.danger[600], marginLeft: '2px' }}>*</span>}
      </label>
      {children}
    </div>
  );
}

const inputStyle: React.CSSProperties = {
  padding: '8px 12px',
  fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px',
  outline: 'none',
  width: '100%',
  boxSizing: 'border-box',
};
