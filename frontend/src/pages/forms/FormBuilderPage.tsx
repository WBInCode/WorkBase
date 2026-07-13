import { useState, useCallback, useEffect } from 'react';
import {
  useFormDefinitions,
  useFormDefinition,
  useCreateFormDefinition,
  useUpdateFormDefinition,
  useDeleteFormDefinition,
  useFormSubmissions,
} from '@/api/hooks/useForms';
import type { CreateFormFieldRequest } from '@/api/types/forms';
import { FIELD_TYPES } from '@/api/types/forms';
import { useIsMobile } from '@/shared';

function emptyField(order: number): CreateFormFieldRequest {
  return { label: '', fieldType: 'text', order, isRequired: false };
}

export function FormBuilderPage() {
  const { data: definitions } = useFormDefinitions();
  const [selectedDefId, setSelectedDefId] = useState<string | null>(null);
  const { data: selectedDef } = useFormDefinition(selectedDefId);
  const { data: submissions } = useFormSubmissions(selectedDefId);
  const createMut = useCreateFormDefinition();
  const updateMut = useUpdateFormDefinition();
  const deleteMut = useDeleteFormDefinition();
  const mobile = useIsMobile();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [isPublic, setIsPublic] = useState(false);
  const [fields, setFields] = useState<CreateFormFieldRequest[]>([emptyField(0)]);
  const [tab, setTab] = useState<'builder' | 'submissions'>('builder');

  const loadDef = useCallback(() => {
    if (selectedDef) {
      setName(selectedDef.name);
      setDescription(selectedDef.description ?? '');
      setIsPublic(selectedDef.isPublic);
      setFields(
        selectedDef.fields.map((f) => ({
          label: f.label,
          fieldType: f.fieldType,
          order: f.order,
          isRequired: f.isRequired,
          placeholder: f.placeholder ?? undefined,
          validationRule: f.validationRule ?? undefined,
          optionsJson: f.optionsJson ?? undefined,
          defaultValue: f.defaultValue ?? undefined,
        })),
      );
    }
  }, [selectedDef]);

  // Load when selectedDef changes
  useEffect(() => {
    if (selectedDef) loadDef();
  }, [selectedDef, loadDef]);

  const addField = () => setFields((prev) => [...prev, emptyField(prev.length)]);

  const removeField = (idx: number) =>
    setFields((prev) => prev.filter((_, i) => i !== idx).map((f, i) => ({ ...f, order: i })));

  const updateField = (idx: number, patch: Partial<CreateFormFieldRequest>) =>
    setFields((prev) => prev.map((f, i) => (i === idx ? { ...f, ...patch } : f)));

  const moveField = (idx: number, dir: -1 | 1) => {
    const target = idx + dir;
    if (target < 0 || target >= fields.length) return;
    setFields((prev) => {
      const copy = [...prev];
      [copy[idx]!, copy[target]!] = [copy[target]!, copy[idx]!];
      return copy.map((f, i) => ({ ...f, order: i }));
    });
  };

  const handleSave = async () => {
    const body = { name, description: description || undefined, isPublic, fields };
    if (selectedDefId) {
      await updateMut.mutateAsync({ id: selectedDefId, ...body });
    } else {
      await createMut.mutateAsync(body);
    }
  };

  const handleNew = () => {
    setSelectedDefId(null);
    setName('');
    setDescription('');
    setIsPublic(false);
    setFields([emptyField(0)]);
  };

  const handleDelete = async () => {
    if (selectedDefId && confirm('Czy na pewno usunąć formularz?')) {
      await deleteMut.mutateAsync(selectedDefId);
      handleNew();
    }
  };

  return (
    <div style={{ padding: mobile ? 16 : 24, maxWidth: 1000, margin: '0 auto' }}>
      <h1 style={{ fontSize: 22, marginBottom: 16 }}>Kreator formularzy</h1>

      {/* Selector */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 16, flexWrap: 'wrap', alignItems: 'center' }}>
        <select
          value={selectedDefId ?? ''}
          onChange={(e) => {
            setSelectedDefId(e.target.value || null);
            if (e.target.value) loadDef();
          }}
          style={selectStyle}
        >
          <option value="">-- Nowy formularz --</option>
          {definitions?.map((d) => (
            <option key={d.id} value={d.id}>
              {d.name} (v{d.version})
            </option>
          ))}
        </select>
        <button onClick={handleNew} style={btnStyle}>Nowy</button>
        {selectedDefId && <button onClick={handleDelete} style={{ ...btnStyle, background: '#dc3545', color: '#fff' }}>Usuń</button>}
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 0, marginBottom: 16 }}>
        {(['builder', 'submissions'] as const).map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            style={{
              padding: '8px 16px',
              border: '1px solid #ccc',
              background: tab === t ? '#007bff' : '#fff',
              color: tab === t ? '#fff' : '#333',
              cursor: 'pointer',
              fontWeight: 600,
              borderRadius: t === 'builder' ? '4px 0 0 4px' : '0 4px 4px 0',
            }}
          >
            {t === 'builder' ? 'Kreator' : 'Zgłoszenia'}
          </button>
        ))}
      </div>

      {tab === 'builder' && (
        <>
          {/* Form meta */}
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: 12, marginBottom: 16 }}>
            <div>
              <label style={labelStyle}>Nazwa</label>
              <input value={name} onChange={(e) => setName(e.target.value)} style={inputStyle} />
            </div>
            <div>
              <label style={labelStyle}>Opis</label>
              <input value={description} onChange={(e) => setDescription(e.target.value)} style={inputStyle} />
            </div>
          </div>
          <label style={{ ...labelStyle, display: 'flex', alignItems: 'center', gap: 6, marginBottom: 16 }}>
            <input type="checkbox" checked={isPublic} onChange={(e) => setIsPublic(e.target.checked)} />
            Formularz publiczny
          </label>

          {/* Fields */}
          <h3 style={{ marginBottom: 8 }}>Pola formularza</h3>
          {fields.map((f, idx) => (
            <div
              key={idx}
              style={{
                padding: 12,
                marginBottom: 8,
                border: '1px solid #ddd',
                borderRadius: 10,
                background: '#fafafa',
              }}
            >
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
                <span style={{ fontWeight: 600, width: 24 }}>#{idx + 1}</span>
                <input
                  value={f.label}
                  onChange={(e) => updateField(idx, { label: e.target.value })}
                  placeholder="Etykieta"
                  style={{ ...inputStyle, flex: 1, minWidth: 150, marginBottom: 0 }}
                />
                <select
                  value={f.fieldType}
                  onChange={(e) => updateField(idx, { fieldType: e.target.value })}
                  style={{ ...selectStyle, width: 120 }}
                >
                  {FIELD_TYPES.map((t) => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </select>
                <label style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 12 }}>
                  <input
                    type="checkbox"
                    checked={f.isRequired}
                    onChange={(e) => updateField(idx, { isRequired: e.target.checked })}
                  />
                  Wymagane
                </label>
                <button onClick={() => moveField(idx, -1)} style={tinyBtn} disabled={idx === 0}>↑</button>
                <button onClick={() => moveField(idx, 1)} style={tinyBtn} disabled={idx === fields.length - 1}>↓</button>
                <button onClick={() => removeField(idx)} style={{ ...tinyBtn, color: '#dc3545' }}>✕</button>
              </div>
              <div style={{ display: 'flex', gap: 8, marginTop: 6, flexWrap: 'wrap' }}>
                <input
                  value={f.placeholder ?? ''}
                  onChange={(e) => updateField(idx, { placeholder: e.target.value })}
                  placeholder="Placeholder"
                  style={{ ...inputStyle, flex: 1, marginBottom: 0, fontSize: 12 }}
                />
                <input
                  value={f.defaultValue ?? ''}
                  onChange={(e) => updateField(idx, { defaultValue: e.target.value })}
                  placeholder="Wartość domyślna"
                  style={{ ...inputStyle, flex: 1, marginBottom: 0, fontSize: 12 }}
                />
                {f.fieldType === 'select' && (
                  <input
                    value={f.optionsJson ?? ''}
                    onChange={(e) => updateField(idx, { optionsJson: e.target.value })}
                    placeholder='Opcje JSON: ["A","B","C"]'
                    style={{ ...inputStyle, flex: 2, marginBottom: 0, fontSize: 12 }}
                  />
                )}
              </div>
            </div>
          ))}
          <button onClick={addField} style={{ ...btnStyle, marginTop: 8 }}>+ Dodaj pole</button>

          <div style={{ marginTop: 16 }}>
            <button onClick={handleSave} style={{ ...btnStyle, background: '#28a745', color: '#fff', padding: '10px 24px' }}>
              Zapisz formularz
            </button>
          </div>
        </>
      )}

      {tab === 'submissions' && (
        <div>
          <h3>Zgłoszenia</h3>
          {!submissions?.length && <p style={{ color: '#888' }}>Brak zgłoszeń.</p>}
          <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead>
              <tr style={{ background: '#f0f0f0' }}>
                <th style={thStyle}>ID</th>
                <th style={thStyle}>Status</th>
                <th style={thStyle}>Zgłoszono przez</th>
                <th style={thStyle}>Data</th>
              </tr>
            </thead>
            <tbody>
              {submissions?.map((s) => (
                <tr key={s.id}>
                  <td style={tdStyle}>{s.id.slice(0, 8)}…</td>
                  <td style={tdStyle}>{s.status}</td>
                  <td style={tdStyle}>{s.submittedBy ?? '—'}</td>
                  <td style={tdStyle}>{new Date(s.createdAt).toLocaleString('pl-PL')}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        </div>
      )}
    </div>
  );
}

const btnStyle: React.CSSProperties = { padding: '6px 14px', borderRadius: 4, border: '1px solid #ccc', background: 'var(--wb-panel, #fff)', cursor: 'pointer', fontWeight: 600, fontSize: 13 };
const tinyBtn: React.CSSProperties = { padding: '2px 8px', borderRadius: 3, border: '1px solid #ccc', background: 'var(--wb-panel, #fff)', cursor: 'pointer', fontSize: 12 };
const inputStyle: React.CSSProperties = { padding: '6px 10px', borderRadius: 4, border: '1px solid #ccc', fontSize: 13, width: '100%', boxSizing: 'border-box' as const, marginBottom: 4 };
const selectStyle: React.CSSProperties = { padding: '6px 10px', borderRadius: 4, border: '1px solid #ccc', fontSize: 13 };
const labelStyle: React.CSSProperties = { fontWeight: 600, fontSize: 12, marginBottom: 4, display: 'block' };
const thStyle: React.CSSProperties = { padding: '8px 12px', textAlign: 'left' as const, borderBottom: '2px solid #ddd' };
const tdStyle: React.CSSProperties = { padding: '8px 12px', borderBottom: '1px solid #eee' };
