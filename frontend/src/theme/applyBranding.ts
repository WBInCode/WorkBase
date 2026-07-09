import { ALLOWED_BRANDING_FONTS, type BrandingDto } from '@/api/types/branding';

// CSS custom properties applied to :root at runtime from tenant branding. Components read
// them through the BRAND_CSS_VARS fallbacks below so the app looks correct even before
// branding has loaded (or when no branding row exists yet for the tenant).
const CSS_VAR_PRIMARY = '--wb-brand-primary';
const CSS_VAR_PRIMARY_HOVER = '--wb-brand-primary-hover';
const CSS_VAR_ACCENT = '--wb-brand-accent';
const CSS_VAR_FONT = '--wb-brand-font';

const HEX_COLOR_RE = /^#[0-9a-fA-F]{6}$/;

const DEFAULT_FONT_STACK = "system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif";

/** Use these in styles instead of hardcoded hex/font values to make them tenant-brandable. */
export const BRAND_CSS_VARS = {
  primary: `var(${CSS_VAR_PRIMARY}, #6366f1)`,
  primaryHover: `var(${CSS_VAR_PRIMARY_HOVER}, #8b5cf6)`,
  accent: `var(${CSS_VAR_ACCENT}, #6366f1)`,
  font: `var(${CSS_VAR_FONT}, ${DEFAULT_FONT_STACK})`,
} as const;

/**
 * Applies (or clears, on invalid/missing input) tenant branding as CSS custom properties on
 * the document root. Values are re-validated client-side even though the backend already
 * validates them — defence in depth, and guards against stale/partial cached data.
 */
export function applyBranding(branding: Partial<BrandingDto> | null | undefined): void {
  const root = document.documentElement.style;

  setOrClear(root, CSS_VAR_PRIMARY, branding?.primaryColor, HEX_COLOR_RE);
  setOrClear(root, CSS_VAR_PRIMARY_HOVER, branding?.secondaryColor, HEX_COLOR_RE);
  setOrClear(root, CSS_VAR_ACCENT, branding?.accentColor ?? branding?.primaryColor, HEX_COLOR_RE);

  const font = branding?.fontFamily;
  if (font && (ALLOWED_BRANDING_FONTS as readonly string[]).includes(font)) {
    root.setProperty(CSS_VAR_FONT, `'${font}', ${DEFAULT_FONT_STACK}`);
  } else {
    root.removeProperty(CSS_VAR_FONT);
  }

  if (branding?.appName) {
    document.title = branding.appName;
  }
}

function setOrClear(
  style: CSSStyleDeclaration,
  varName: string,
  value: string | null | undefined,
  pattern: RegExp,
): void {
  if (value && pattern.test(value)) {
    style.setProperty(varName, value);
  } else {
    style.removeProperty(varName);
  }
}
