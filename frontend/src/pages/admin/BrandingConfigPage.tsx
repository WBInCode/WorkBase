import { useState, useEffect } from 'react';
import { Palette, RefreshCw } from 'lucide-react';
import { useBranding, useUpdateBranding } from '@/api/hooks/useBranding';
import { ALLOWED_BRANDING_FONTS } from '@/api/types/branding';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

const HEX_COLOR_RE = /^#[0-9a-fA-F]{6}$/;

export function BrandingConfigPage() {
  const { data: branding, isLoading, error, refetch, isFetching } = useBranding();
  const updateMutation = useUpdateBranding();
  const mobile = useIsMobile();

  const [appName, setAppName] = useState('');
  const [logoUrl, setLogoUrl] = useState('');
  const [faviconUrl, setFaviconUrl] = useState('');
  const [primaryColor, setPrimaryColor] = useState('#3b82f6');
  const [secondaryColor, setSecondaryColor] = useState('#1e40af');
  const [accentColor, setAccentColor] = useState('');
  const [fontFamily, setFontFamily] = useState('');
  const [loginBackgroundUrl, setLoginBackgroundUrl] = useState('');

  useEffect(() => {
    if (!branding) return;
    setAppName(branding.appName ?? '');
    setLogoUrl(branding.logoUrl ?? '');
    setFaviconUrl(branding.faviconUrl ?? '');
    setPrimaryColor(branding.primaryColor ?? '#3b82f6');
    setSecondaryColor(branding.secondaryColor ?? '#1e40af');
    setAccentColor(branding.accentColor ?? '');
    setFontFamily(branding.fontFamily ?? '');
    setLoginBackgroundUrl(branding.loginBackgroundUrl ?? '');
  }, [branding]);

  const primaryValid = HEX_COLOR_RE.test(primaryColor);
  const secondaryValid = HEX_COLOR_RE.test(secondaryColor);
  const accentValid = accentColor === '' || HEX_COLOR_RE.test(accentColor);
  const canSave = primaryValid && secondaryValid && accentValid;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSave) return;
    updateMutation.mutate({
      appName: appName || null,
      logoUrl: logoUrl || null,
      faviconUrl: faviconUrl || null,
      primaryColor,
      secondaryColor,
      accentColor: accentColor || null,
      fontFamily: fontFamily || null,
      loginBackgroundUrl: loginBackgroundUrl || null,
      customDomain: branding?.customDomain ?? null,
    });
  };

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '640px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 800, letterSpacing: '-0.02em', color: colors.gray[900], display: 'flex', alignItems: 'center', gap: '8px' }}>
          <Palette size={22} /> Branding
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
        Nazwa, logo, kolory i czcionka widoczne w aplikacji dla wszystkich użytkowników Twojej firmy.
      </p>

      {error && (
        <div style={{
          padding: '12px 16px', marginBottom: '16px', borderRadius: '12px',
          backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px',
        }}>
          Błąd ładowania brandingu.
        </div>
      )}

      {updateMutation.isError && (
        <div style={{
          padding: '12px 16px', marginBottom: '16px', borderRadius: '12px',
          backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px',
        }}>
          {updateMutation.error instanceof Error ? updateMutation.error.message : 'Błąd zapisu brandingu.'}
        </div>
      )}

      {updateMutation.isSuccess && (
        <div style={{
          padding: '10px 16px', marginBottom: '16px', borderRadius: '12px',
          backgroundColor: colors.success[100], border: `1px solid ${colors.success[200]}`, color: colors.success[800], fontSize: '13px',
        }}>
          Zapisano.
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
      ) : (
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          <label style={labelStyle}>
            Nazwa aplikacji
            <input value={appName} onChange={(e) => setAppName(e.target.value)} style={inputStyle} placeholder="np. Acme Corp" maxLength={128} />
          </label>

          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              URL logo
              <input value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} style={inputStyle} placeholder="https://..." />
            </label>
            <label style={labelStyle}>
              URL favicon
              <input value={faviconUrl} onChange={(e) => setFaviconUrl(e.target.value)} style={inputStyle} placeholder="https://..." />
            </label>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Kolor główny
              <input type="color" value={primaryValid ? primaryColor : '#3b82f6'} onChange={(e) => setPrimaryColor(e.target.value)} style={{ ...inputStyle, height: '36px', padding: '2px' }} />
            </label>
            <label style={labelStyle}>
              Kolor drugorzędny
              <input type="color" value={secondaryValid ? secondaryColor : '#1e40af'} onChange={(e) => setSecondaryColor(e.target.value)} style={{ ...inputStyle, height: '36px', padding: '2px' }} />
            </label>
            <label style={labelStyle}>
              Kolor akcentu (opcjonalnie)
              <input
                type="color"
                value={accentValid && accentColor ? accentColor : primaryColor}
                onChange={(e) => setAccentColor(e.target.value)}
                style={{ ...inputStyle, height: '36px', padding: '2px' }}
              />
            </label>
          </div>
          {accentColor && (
            <button type="button" onClick={() => setAccentColor('')} style={{ alignSelf: 'flex-start', fontSize: '12px', color: colors.primary[600], background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', padding: 0 }}>
              Wyczyść kolor akcentu
            </button>
          )}

          <label style={labelStyle}>
            Czcionka
            <select value={fontFamily} onChange={(e) => setFontFamily(e.target.value)} style={inputStyle}>
              <option value="">Domyślna systemowa</option>
              {ALLOWED_BRANDING_FONTS.map((font) => (
                <option key={font} value={font}>{font}</option>
              ))}
            </select>
          </label>

          <label style={labelStyle}>
            URL tła ekranu logowania (opcjonalnie)
            <input value={loginBackgroundUrl} onChange={(e) => setLoginBackgroundUrl(e.target.value)} style={inputStyle} placeholder="https://..." />
          </label>

          {!canSave && (
            <div style={{ fontSize: '12px', color: colors.danger[600] }}>
              Kolory muszą być w formacie #RRGGBB.
            </div>
          )}

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '8px' }}>
            <button
              type="submit"
              disabled={!canSave || updateMutation.isPending}
              style={{
                padding: '9px 22px', fontSize: '14px', fontWeight: 500,
                color: colors.white, backgroundColor: colors.primary[600], border: 'none',
                borderRadius: '999px', cursor: canSave && !updateMutation.isPending ? 'pointer' : 'not-allowed',
                opacity: !canSave || updateMutation.isPending ? 0.6 : 1,
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
