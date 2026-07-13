import { useState, useRef, useCallback } from 'react';
import { ArrowRight, ArrowLeft, CheckCircle2, FileSpreadsheet } from 'lucide-react';
import { parseCsv, type ParsedCsv } from '@/utils/csvParser';
import { api } from '@/api/client';
import { ApiError } from '@/api/client';
import type { CreateEmployeeRequest } from '@/api/types/organization';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

/* ─── Employee fields available for mapping ─── */
const EMPLOYEE_FIELDS = [
  { key: 'firstName', label: 'Imię', required: true },
  { key: 'lastName', label: 'Nazwisko', required: true },
  { key: 'email', label: 'Email', required: true },
  { key: 'employeeNumber', label: 'Nr pracownika', required: false },
  { key: 'hireDate', label: 'Data zatrudnienia', required: true },
] as const;

type FieldKey = (typeof EMPLOYEE_FIELDS)[number]['key'];

/* ─── Validation ─── */
interface RowValidation {
  rowIndex: number;
  errors: string[];
}

function validateRow(row: Record<string, string>, rowIndex: number): RowValidation {
  const errors: string[] = [];
  if (!row.firstName?.trim()) errors.push('Brak imienia');
  if (!row.lastName?.trim()) errors.push('Brak nazwiska');
  if (!row.email?.trim()) errors.push('Brak email');
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(row.email.trim())) errors.push('Nieprawidłowy email');
  if (!row.hireDate?.trim()) errors.push('Brak daty zatrudnienia');
  else if (isNaN(Date.parse(row.hireDate.trim()))) errors.push('Nieprawidłowy format daty');
  return { rowIndex, errors };
}

/* ─── Import result per row ─── */
interface ImportRowResult {
  rowIndex: number;
  success: boolean;
  error?: string;
}

/* ─── Steps ─── */
type Step = 'upload' | 'mapping' | 'preview' | 'importing' | 'results';

export function CsvImportPage() {
  const mobile = useIsMobile();
  const [step, setStep] = useState<Step>('upload');
  const [csv, setCsv] = useState<ParsedCsv | null>(null);
  const [fileName, setFileName] = useState('');
  const [mapping, setMapping] = useState<Record<FieldKey, number | null>>({
    firstName: null,
    lastName: null,
    email: null,
    employeeNumber: null,
    hireDate: null,
  });
  const [importResults, setImportResults] = useState<ImportRowResult[]>([]);
  const [importProgress, setImportProgress] = useState(0);
  const abortRef = useRef(false);

  /* ── Upload ── */
  const handleFile = useCallback((file: File) => {
    if (!file.name.toLowerCase().endsWith('.csv')) return;
    const reader = new FileReader();
    reader.onload = (e) => {
      const text = e.target?.result;
      if (typeof text !== 'string') return;
      const parsed = parseCsv(text);
      setCsv(parsed);
      setFileName(file.name);

      // Auto-map by header names
      const autoMap: Record<FieldKey, number | null> = {
        firstName: null, lastName: null, email: null, employeeNumber: null, hireDate: null,
      };
      const aliases: Record<FieldKey, string[]> = {
        firstName: ['imię', 'imie', 'first name', 'firstname', 'first_name', 'name'],
        lastName: ['nazwisko', 'last name', 'lastname', 'last_name', 'surname'],
        email: ['email', 'e-mail', 'mail'],
        employeeNumber: ['nr pracownika', 'numer', 'employee number', 'employeenumber', 'employee_number', 'emp_no'],
        hireDate: ['data zatrudnienia', 'hire date', 'hiredate', 'hire_date', 'data'],
      };
      parsed.headers.forEach((h, idx) => {
        const lower = h.toLowerCase().trim();
        for (const [field, names] of Object.entries(aliases)) {
          if (names.includes(lower) && autoMap[field as FieldKey] === null) {
            autoMap[field as FieldKey] = idx;
          }
        }
      });
      setMapping(autoMap);
      setStep('mapping');
    };
    reader.readAsText(file);
  }, []);

  /* ── Build mapped rows ── */
  const getMappedRows = useCallback((): Record<string, string>[] => {
    if (!csv) return [];
    return csv.rows.map((row) => {
      const obj: Record<string, string> = {};
      for (const field of EMPLOYEE_FIELDS) {
        const colIdx = mapping[field.key];
        obj[field.key] = colIdx !== null && colIdx < row.length ? (row[colIdx] ?? '') : '';
      }
      return obj;
    });
  }, [csv, mapping]);

  /* ── Validation ── */
  const mappedRows = step === 'preview' || step === 'importing' || step === 'results' ? getMappedRows() : [];
  const validations = mappedRows.map((r, i) => validateRow(r, i));
  const validRows = validations.filter((v) => v.errors.length === 0);
  const invalidRows = validations.filter((v) => v.errors.length > 0);

  /* ── Import ── */
  const handleImport = useCallback(async () => {
    setStep('importing');
    setImportProgress(0);
    setImportResults([]);
    abortRef.current = false;
    const rows = getMappedRows();
    const results: ImportRowResult[] = [];

    for (let i = 0; i < rows.length; i++) {
      if (abortRef.current) break;
      const row = rows[i]!;
      const v = validateRow(row, i);
      if (v.errors.length > 0) {
        results.push({ rowIndex: i, success: false, error: v.errors.join('; ') });
        setImportProgress(i + 1);
        setImportResults([...results]);
        continue;
      }

      const req: CreateEmployeeRequest = {
        firstName: (row.firstName ?? '').trim(),
        lastName: (row.lastName ?? '').trim(),
        email: (row.email ?? '').trim(),
        employeeNumber: row.employeeNumber?.trim() || undefined,
        hireDate: new Date((row.hireDate ?? '').trim()).toISOString(),
      };

      try {
        await api.post('/api/org/employees', req);
        results.push({ rowIndex: i, success: true });
      } catch (err) {
        const msg = err instanceof ApiError ? err.message : 'Nieznany błąd';
        results.push({ rowIndex: i, success: false, error: msg });
      }
      setImportProgress(i + 1);
      setImportResults([...results]);
    }

    setImportResults(results);
    setStep('results');
  }, [getMappedRows]);

  const requiredMapped = EMPLOYEE_FIELDS.filter((f) => f.required).every((f) => mapping[f.key] !== null);

  /* ── Render ── */
  return (
    <div style={{ padding: mobile ? '14px' : '24px 28px', maxWidth: '1100px', margin: '0 auto' }}>
      {/* ── Karta dowodzenia ── */}
      <div style={{
        backgroundColor: colors.white,
        border: `1px solid ${colors.gray[200]}`,
        borderRadius: '20px',
        boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
        padding: mobile ? '16px' : '18px 22px',
        marginBottom: '18px',
      }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900] }}>
          Import pracowników z CSV
        </h1>
        <p style={{ margin: '3px 0 14px', fontSize: '13px', color: colors.gray[500] }}>
          {step === 'upload' && 'Wybierz plik CSV z danymi pracowników.'}
          {step === 'mapping' && 'Przypisz kolumny CSV do pól pracownika.'}
          {step === 'preview' && 'Sprawdź podgląd danych przed importem.'}
          {step === 'importing' && 'Importowanie w toku...'}
          {step === 'results' && 'Import zakończony.'}
        </p>

        {/* Step indicator */}
        <StepIndicator current={step} />
      </div>

      {/* ── UPLOAD ── */}
      {step === 'upload' && (
        <DropZone onFile={handleFile} />
      )}

      {/* ── MAPPING ── */}
      {step === 'mapping' && csv && (
        <div>
          <div style={{ fontSize: '13px', color: colors.gray[500], marginBottom: '12px' }}>
            Plik: <strong>{fileName}</strong> — {csv.rows.length} wierszy, {csv.headers.length} kolumn
          </div>

          <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', overflowX: 'auto', backgroundColor: colors.white, boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
              <thead>
                <tr style={{ backgroundColor: colors.gray[50] }}>
                  <th style={thStyle}>Pole pracownika</th>
                  <th style={thStyle}>Kolumna CSV</th>
                  <th style={thStyle}>Podgląd (wiersz 1)</th>
                </tr>
              </thead>
              <tbody>
                {EMPLOYEE_FIELDS.map((field) => (
                  <tr key={field.key} style={{ borderTop: `1px solid ${colors.gray[200]}` }}>
                    <td style={{ ...tdStyle, fontWeight: 500 }}>
                      {field.label}
                      {field.required && <span style={{ color: colors.danger[600], marginLeft: '2px' }}>*</span>}
                    </td>
                    <td style={tdStyle}>
                      <select
                        value={mapping[field.key] ?? ''}
                        onChange={(e) =>
                          setMapping((m) => ({
                            ...m,
                            [field.key]: e.target.value === '' ? null : Number(e.target.value),
                          }))
                        }
                        style={selectStyle}
                      >
                        <option value="">— nie mapuj —</option>
                        {csv.headers.map((h, idx) => (
                          <option key={idx} value={idx}>
                            {h || `Kolumna ${idx + 1}`}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td style={{ ...tdStyle, color: colors.gray[500], fontSize: '13px' }}>
                      {mapping[field.key] !== null && csv.rows[0]
                        ? csv.rows[0][mapping[field.key]!] ?? '—'
                        : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div style={{ display: 'flex', gap: '10px', marginTop: '20px', justifyContent: 'space-between' }}>
            <button onClick={() => { setStep('upload'); setCsv(null); }} style={secondaryBtnStyle}>
              <ArrowLeft size={16} /> Wróć
            </button>
            <button
              disabled={!requiredMapped}
              onClick={() => setStep('preview')}
              style={primaryBtnStyle(!requiredMapped)}
            >
              Dalej <ArrowRight size={16} />
            </button>
          </div>
        </div>
      )}

      {/* ── PREVIEW ── */}
      {step === 'preview' && csv && (
        <div>
          <div style={{ display: 'flex', gap: '16px', marginBottom: '16px' }}>
            <StatBadge label="Wszystkich" value={mappedRows.length} color={colors.primary[600]} />
            <StatBadge label="Poprawnych" value={validRows.length} color={colors.success[600]} />
            <StatBadge label="Z błędami" value={invalidRows.length} color={colors.danger[600]} />
          </div>

          {/* Preview table (first 50 rows) */}
          <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '16px', overflow: 'auto', maxHeight: '400px', backgroundColor: colors.white }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
              <thead>
                <tr style={{ backgroundColor: colors.gray[50], position: 'sticky', top: 0 }}>
                  <th style={thStyle}>#</th>
                  {EMPLOYEE_FIELDS.map((f) => (
                    <th key={f.key} style={thStyle}>{f.label}</th>
                  ))}
                  <th style={thStyle}>Status</th>
                </tr>
              </thead>
              <tbody>
                {mappedRows.slice(0, 50).map((row, i) => {
                  const v = validations[i]!;
                  const hasErrors = v.errors.length > 0;
                  return (
                    <tr
                      key={i}
                      style={{
                        borderTop: `1px solid ${colors.gray[200]}`,
                        backgroundColor: hasErrors ? colors.danger[50] : undefined,
                      }}
                    >
                      <td style={{ ...tdStyle, color: colors.gray[400], fontSize: '12px' }}>{i + 1}</td>
                      {EMPLOYEE_FIELDS.map((f) => (
                        <td key={f.key} style={tdStyle}>{row[f.key] || '—'}</td>
                      ))}
                      <td style={tdStyle}>
                        {hasErrors ? (
                          <span style={{ color: colors.danger[600], fontSize: '12px' }}>
                            {v!.errors.join(', ')}
                          </span>
                        ) : (
                          <CheckCircle2 size={14} style={{ color: colors.success[600] }} />
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          {mappedRows.length > 50 && (
            <div style={{ fontSize: '12px', color: colors.gray[400], marginTop: '4px' }}>
              Pokazano pierwsze 50 z {mappedRows.length} wierszy.
            </div>
          )}

          <div style={{ display: 'flex', gap: '10px', marginTop: '20px', justifyContent: 'space-between' }}>
            <button onClick={() => setStep('mapping')} style={secondaryBtnStyle}>
              <ArrowLeft size={16} /> Wróć
            </button>
            <button
              disabled={validRows.length === 0}
              onClick={handleImport}
              style={primaryBtnStyle(validRows.length === 0)}
            >
              Importuj {validRows.length} pracowników <ArrowRight size={16} />
            </button>
          </div>
        </div>
      )}

      {/* ── IMPORTING ── */}
      {step === 'importing' && (
        <div style={{ textAlign: 'center', padding: '40px 0' }}>
          <div style={{ fontSize: '15px', fontWeight: 500, color: colors.gray[900], marginBottom: '12px' }}>
            Importowanie... {importProgress} / {mappedRows.length}
          </div>
          <div
            style={{
              width: '100%',
              maxWidth: '400px',
              margin: '0 auto',
              height: '8px',
              backgroundColor: colors.gray[200],
              borderRadius: '4px',
              overflow: 'hidden',
            }}
          >
            <div
              style={{
                width: `${mappedRows.length ? (importProgress / mappedRows.length) * 100 : 0}%`,
                height: '100%',
                backgroundColor: colors.primary[600],
                transition: 'width 0.2s',
                borderRadius: '4px',
              }}
            />
          </div>
          <button
            onClick={() => { abortRef.current = true; }}
            style={{ ...secondaryBtnStyle, marginTop: '20px' }}
          >
            Przerwij
          </button>
        </div>
      )}

      {/* ── RESULTS ── */}
      {step === 'results' && (
        <div>
          {(() => {
            const succeeded = importResults.filter((r) => r.success).length;
            const failed = importResults.filter((r) => !r.success).length;
            return (
              <>
                <div style={{ display: 'flex', gap: '16px', marginBottom: '20px' }}>
                  <StatBadge label="Zaimportowano" value={succeeded} color={colors.success[600]} />
                  <StatBadge label="Błędów" value={failed} color={colors.danger[600]} />
                  <StatBadge label="Łącznie" value={importResults.length} color={colors.gray[500]} />
                </div>

                {succeeded > 0 && (
                  <div
                    style={{
                      padding: '12px 16px',
                      backgroundColor: colors.success[50],
                      border: `1px solid ${colors.success[200]}`,
                      borderRadius: '12px',
                      color: colors.success[800],
                      fontSize: '14px',
                      marginBottom: '12px',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '8px',
                    }}
                  >
                    <CheckCircle2 size={18} />
                    Pomyślnie zaimportowano {succeeded} pracowników.
                  </div>
                )}

                {/* Error rows */}
                {failed > 0 && (
                  <div style={{ border: `1px solid ${colors.danger[200]}`, borderRadius: '16px', overflow: 'auto', maxHeight: '300px', backgroundColor: colors.white }}>
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
                      <thead>
                        <tr style={{ backgroundColor: colors.danger[50] }}>
                          <th style={thStyle}>Wiersz</th>
                          <th style={thStyle}>Dane</th>
                          <th style={thStyle}>Błąd</th>
                        </tr>
                      </thead>
                      <tbody>
                        {importResults
                          .filter((r) => !r.success)
                          .map((r) => {
                            const row = mappedRows[r.rowIndex];
                            return (
                              <tr key={r.rowIndex} style={{ borderTop: `1px solid ${colors.danger[200]}` }}>
                                <td style={{ ...tdStyle, color: colors.gray[400] }}>{r.rowIndex + 1}</td>
                                <td style={tdStyle}>
                                  {row ? `${row.firstName} ${row.lastName} (${row.email})` : '—'}
                                </td>
                                <td style={{ ...tdStyle, color: colors.danger[600] }}>{r.error}</td>
                              </tr>
                            );
                          })}
                      </tbody>
                    </table>
                  </div>
                )}

                <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
                  <button
                    onClick={() => {
                      setStep('upload');
                      setCsv(null);
                      setImportResults([]);
                      setImportProgress(0);
                    }}
                    style={secondaryBtnStyle}
                  >
                    Nowy import
                  </button>
                </div>
              </>
            );
          })()}
        </div>
      )}
    </div>
  );
}

/* ═══════════════ Sub-components ═══════════════ */

function DropZone({ onFile }: { onFile: (file: File) => void }) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragOver, setDragOver] = useState(false);

  return (
    <div
      onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
      onDragLeave={() => setDragOver(false)}
      onDrop={(e) => {
        e.preventDefault();
        setDragOver(false);
        const file = e.dataTransfer.files[0];
        if (file) onFile(file);
      }}
      onClick={() => inputRef.current?.click()}
      style={{
        border: `2px dashed ${dragOver ? colors.primary[600] : colors.gray[300]}`,
        borderRadius: '16px',
        padding: '48px 32px',
        textAlign: 'center',
        cursor: 'pointer',
        backgroundColor: dragOver ? colors.primary[50] : '#fafafa',
        transition: 'all 0.15s',
      }}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') inputRef.current?.click(); }}
      aria-label="Wybierz plik CSV"
    >
      <input
        ref={inputRef}
        type="file"
        accept=".csv"
        style={{ display: 'none' }}
        onChange={(e) => {
          const file = e.target.files?.[0];
          if (file) onFile(file);
          e.target.value = '';
        }}
      />
      <FileSpreadsheet size={40} style={{ color: colors.gray[400], marginBottom: '12px' }} />
      <div style={{ fontSize: '15px', fontWeight: 500, color: colors.gray[700], marginBottom: '4px' }}>
        Przeciągnij plik CSV lub kliknij, aby wybrać
      </div>
      <div style={{ fontSize: '13px', color: colors.gray[400] }}>
        Obsługiwane: .csv (separator: przecinek lub średnik)
      </div>
    </div>
  );
}

function StepIndicator({ current }: { current: Step }) {
  const steps: { key: Step; label: string }[] = [
    { key: 'upload', label: 'Plik' },
    { key: 'mapping', label: 'Mapowanie' },
    { key: 'preview', label: 'Podgląd' },
    { key: 'importing', label: 'Import' },
    { key: 'results', label: 'Wyniki' },
  ];
  const currentIdx = steps.findIndex((s) => s.key === current);

  return (
    <div style={{ display: 'flex', gap: '4px', marginBottom: '24px', alignItems: 'center' }}>
      {steps.map((s, i) => {
        const isActive = i === currentIdx;
        const isDone = i < currentIdx;
        return (
          <div key={s.key} style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            {i > 0 && (
              <div
                style={{
                  width: '24px',
                  height: '2px',
                  backgroundColor: isDone ? colors.primary[600] : colors.gray[200],
                }}
              />
            )}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '6px',
                padding: '4px 10px',
                borderRadius: '9999px',
                fontSize: '12px',
                fontWeight: isActive ? 600 : 400,
                backgroundColor: isActive ? colors.primary[50] : isDone ? colors.success[50] : colors.gray[50],
                color: isActive ? colors.primary[600] : isDone ? colors.success[600] : colors.gray[400],
                border: `1px solid ${isActive ? colors.primary[200] : isDone ? '#bbf7d0' : colors.gray[200]}`,
              }}
            >
              {isDone ? <CheckCircle2 size={12} /> : null}
              {s.label}
            </div>
          </div>
        );
      })}
    </div>
  );
}

function StatBadge({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div
      style={{
        padding: '10px 16px',
        borderRadius: '12px',
        backgroundColor: colors.gray[50],
        border: `1px solid ${colors.gray[200]}`,
      }}
    >
      <div style={{ fontSize: '20px', fontWeight: 700, color }}>{value}</div>
      <div style={{ fontSize: '12px', color: colors.gray[500] }}>{label}</div>
    </div>
  );
}

/* ═══════════════ Styles ═══════════════ */

const thStyle: React.CSSProperties = {
  padding: '10px 14px',
  textAlign: 'left',
  fontWeight: 500,
  color: colors.gray[500],
  fontSize: '13px',
  whiteSpace: 'nowrap',
};

const tdStyle: React.CSSProperties = {
  padding: '8px 14px',
  color: colors.gray[700],
};

const selectStyle: React.CSSProperties = {
  padding: '6px 10px',
  fontSize: '13px',
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '10px',
  outline: 'none',
  backgroundColor: colors.white,
  color: colors.gray[700],
  width: '100%',
};

const secondaryBtnStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  gap: '6px',
  padding: '8px 16px',
  fontSize: '14px',
  fontWeight: 500,
  color: colors.gray[700],
  backgroundColor: colors.white,
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '10px',
  cursor: 'pointer',
};

function primaryBtnStyle(disabled: boolean): React.CSSProperties {
  return {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '6px',
    padding: '8px 20px',
    fontSize: '14px',
    fontWeight: 500,
    color: colors.white,
    backgroundColor: disabled ? colors.primary[300] : colors.primary[600],
    border: 'none',
    borderRadius: '10px',
    cursor: disabled ? 'not-allowed' : 'pointer',
  };
}
