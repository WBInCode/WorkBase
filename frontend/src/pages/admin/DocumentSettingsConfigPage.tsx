import { useState, useEffect } from 'react';
import { FileCog, RefreshCw, X } from 'lucide-react';
import { useDocumentSettings, useUpdateDocumentSettings } from '@/api/hooks/useDocumentSettings';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function DocumentSettingsConfigPage() {
  const { data, isLoading, error, refetch, isFetching } = useDocumentSettings();
  const updateMutation = useUpdateDocumentSettings();
  const mobile = useIsMobile();

  const [maxSizeMb, setMaxSizeMb] = useState(25);
  const [extensions, setExtensions] = useState<string[]>([]);
  const [newExtension, setNewExtension] = useState('');

  useEffect(() => {
    if (!data) return;
    setMaxSizeMb(Math.round(data.maxFileSizeBytes / (1024 * 1024)));
    setExtensions(data.allowedExtensions);
  }, [data]);

  const addExtension = () => {
    let ext = newExtension.trim().toLowerCase();
    if (!ext) return;
    if (!ext.startsWith('.')) ext = `.${ext}`;
    if (!extensions.includes(ext)) setExtensions((prev) => [...prev, ext]);
    setNewExtension('');
  };

  const removeExtension = (ext: string) => {
    setExtensions((prev) => prev.filter((e) => e !== ext));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate({
      maxFileSizeBytes: Math.max(1, maxSizeMb) * 1024 * 1024,
      allowedExtensions: extensions,
    });
  };

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '640px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900], display: 'flex', alignItems: 'center', gap: '8px' }}>
          <FileCog size={22} /> Ustawienia dokumentów
        </h1>
        <button
          onClick={() => refetch()}
          style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            padding: '7px 12px', fontSize: '13px', fontWeight: 500,
            color: colors.gray[700], backgroundColor: colors.white,
            border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', cursor: 'pointer',
          }}
          title="Odśwież"
        >
          <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
        </button>
      </div>

      <p style={{ fontSize: '13px', color: colors.gray[500], marginTop: '-12px', marginBottom: '20px' }}>
        Maksymalny rozmiar i dozwolone rozszerzenia plików przesyłanych do modułu Dokumenty.
      </p>

      {error && (
        <div style={{ padding: '12px 16px', marginBottom: '16px', borderRadius: '12px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px' }}>
          Błąd ładowania ustawień.
        </div>
      )}
      {updateMutation.isError && (
        <div style={{ padding: '12px 16px', marginBottom: '16px', borderRadius: '12px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px' }}>
          {updateMutation.error instanceof Error ? updateMutation.error.message : 'Błąd zapisu ustawień.'}
        </div>
      )}
      {updateMutation.isSuccess && (
        <div style={{ padding: '10px 16px', marginBottom: '16px', borderRadius: '12px', backgroundColor: colors.success[100], border: `1px solid ${colors.success[200]}`, color: colors.success[800], fontSize: '13px' }}>
          Zapisano.
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
      ) : (
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <label style={labelStyle}>
            Maksymalny rozmiar pliku (MB)
            <input type="number" min={1} max={500} value={maxSizeMb} onChange={(e) => setMaxSizeMb(Number(e.target.value))} style={inputStyle} />
          </label>

          <div>
            <div style={{ fontSize: '13px', fontWeight: 600, color: colors.gray[700], marginBottom: '8px' }}>Dozwolone rozszerzenia</div>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px', marginBottom: '10px' }}>
              {extensions.map((ext) => (
                <span key={ext} style={{
                  display: 'inline-flex', alignItems: 'center', gap: '4px',
                  padding: '4px 8px', fontSize: '12px', borderRadius: '999px',
                  backgroundColor: colors.gray[100], color: colors.gray[700],
                }}>
                  {ext}
                  <button type="button" onClick={() => removeExtension(ext)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500], display: 'flex', padding: 0 }}>
                    <X size={12} />
                  </button>
                </span>
              ))}
              {extensions.length === 0 && (
                <span style={{ fontSize: '12px', color: colors.danger[600] }}>Brak dozwolonych rozszerzeń — dodaj przynajmniej jedno.</span>
              )}
            </div>
            <div style={{ display: 'flex', gap: '8px' }}>
              <input
                value={newExtension}
                onChange={(e) => setNewExtension(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addExtension(); } }}
                placeholder="np. .pdf"
                style={{ ...inputStyle, flex: 1 }}
              />
              <button type="button" onClick={addExtension} style={{
                padding: '8px 16px', fontSize: '13px', fontWeight: 500,
                color: colors.gray[700], backgroundColor: colors.white,
                border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', cursor: 'pointer',
              }}>
                Dodaj
              </button>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '8px' }}>
            <button
              type="submit"
              disabled={updateMutation.isPending || extensions.length === 0}
              style={{
                padding: '9px 22px', fontSize: '14px', fontWeight: 500,
                color: colors.white, backgroundColor: colors.primary[600], border: 'none',
                borderRadius: '999px', cursor: updateMutation.isPending || extensions.length === 0 ? 'not-allowed' : 'pointer',
                opacity: updateMutation.isPending || extensions.length === 0 ? 0.6 : 1,
              }}
            >
              {updateMutation.isPending ? 'Zapisywanie...' : 'Zapisz'}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

const labelStyle: React.CSSProperties = {
  display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: colors.gray[700],
};
const inputStyle: React.CSSProperties = {
  width: '100%', padding: '8px 12px', fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`, borderRadius: '10px', boxSizing: 'border-box',
};
