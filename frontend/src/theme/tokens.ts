// ─── Design System Tokens ───────────────────────────────────
// Single source of truth for colors, typography, spacing, radii.
// All components should import from here instead of hardcoding values.
//
// MOTYWY: każdy kolor to `var(--wb-*, <hex light>)` — jasny motyw działa bez
// żadnego CSS (fallback), ciemny nadpisuje zmienne w html[data-theme="dark"]
// (theme/workbase.css). Dzięki temu inline styles w 139 plikach przełączają
// się automatycznie bez zmian w komponentach.

// ─── Color Palette ──────────────────────────────────────────

export const colors = {
  // ── Primary (Iris Blue) ──
  primary: {
    50: 'var(--wb-p-50, #eef3fe)',
    100: 'var(--wb-p-100, #e5ecfe)',
    200: 'var(--wb-p-200, #c9d8fc)',
    300: 'var(--wb-p-300, #a3bcf9)',
    400: 'var(--wb-p-400, #6f93f6)',
    500: 'var(--wb-p-500, #3d6df2)',
    600: 'var(--wb-p-600, #2b55d4)',
    700: 'var(--wb-p-700, #2346af)',
    800: 'var(--wb-p-800, #1e3a8e)',
    900: 'var(--wb-p-900, #1b3272)',
  },

  // ── Gray (Ink-tinted cool neutral; w dark: odwrócona skala jasności) ──
  gray: {
    50: 'var(--wb-g-50, #f6f8fc)',
    100: 'var(--wb-g-100, #eef1f8)',
    200: 'var(--wb-g-200, #e3e7f1)',
    300: 'var(--wb-g-300, #cdd3e4)',
    400: 'var(--wb-g-400, #9aa3bc)',
    500: 'var(--wb-g-500, #6b7490)',
    600: 'var(--wb-g-600, #4c5570)',
    700: 'var(--wb-g-700, #353d56)',
    800: 'var(--wb-g-800, #232941)',
    900: 'var(--wb-g-900, #14192b)',
  },

  // ── Slate (kiosk / celowo ciemne powierzchnie — statyczne) ──
  slate: {
    400: '#94a3b8',
    700: '#334155',
    800: '#1e293b',
    900: '#0f172a',
  },

  // ── Semantic ──
  success: {
    50: 'var(--wb-suc-50, #f0fdf4)',
    100: 'var(--wb-suc-100, #dcfce7)',
    200: 'var(--wb-suc-200, #bbf7d0)',
    500: 'var(--wb-suc-500, #22c55e)',
    600: 'var(--wb-suc-600, #16a34a)',
    700: 'var(--wb-suc-700, #15803d)',
    800: 'var(--wb-suc-800, #166534)',
  },
  warning: {
    50: 'var(--wb-war-50, #fffbeb)',
    100: 'var(--wb-war-100, #fef3c7)',
    200: 'var(--wb-war-200, #fde68a)',
    500: 'var(--wb-war-500, #f59e0b)',
    600: 'var(--wb-war-600, #d97706)',
    700: 'var(--wb-war-700, #b45309)',
    800: 'var(--wb-war-800, #92400e)',
  },
  danger: {
    50: 'var(--wb-dan-50, #fef2f2)',
    100: 'var(--wb-dan-100, #fee2e2)',
    200: 'var(--wb-dan-200, #fecaca)',
    400: 'var(--wb-dan-400, #f87171)',
    500: 'var(--wb-dan-500, #ef4444)',
    600: 'var(--wb-dan-600, #dc2626)',
    700: 'var(--wb-dan-700, #b91c1c)',
    800: 'var(--wb-dan-800, #991b1b)',
  },
  info: {
    50: 'var(--wb-inf-50, #eff6ff)',
    100: 'var(--wb-inf-100, #dbeafe)',
    500: 'var(--wb-inf-500, #3b82f6)',
    600: 'var(--wb-inf-600, #2563eb)',
  },
  emerald: {
    600: 'var(--wb-eme-600, #059669)',
  },

  // ── Base ──
  white: 'var(--wb-surface, #ffffff)',
  black: '#000000',
  transparent: 'transparent',
} as const;

// ─── Semantic Aliases ───────────────────────────────────────

export const semantic = {
  // Text
  textPrimary: colors.gray[900],
  textSecondary: colors.gray[500],
  textMuted: colors.gray[400],
  textBody: colors.gray[700],
  textOnDark: colors.white,
  textLink: colors.primary[600],

  // Backgrounds
  bgPage: 'var(--wb-canvas, #eef1f8)',
  bgSurface: colors.white,
  bgSubtle: colors.gray[50],
  bgMuted: colors.gray[100],
  bgSidebar: colors.white,
  bgHover: colors.gray[50],
  bgSelected: colors.primary[100],

  // Borders
  border: colors.gray[200],
  borderLight: colors.gray[100],
  borderInput: colors.gray[300],
  borderFocus: colors.primary[500],

  // Interactive
  buttonPrimary: colors.primary[600],
  buttonPrimaryHover: colors.primary[700],
  buttonDanger: colors.danger[600],
  buttonDangerHover: colors.danger[700],
} as const;

// ─── Status Colors (per-domain) ─────────────────────────────

export const statusColors = {
  // Leave request statuses
  leave: {
    draft: { bg: colors.gray[100], text: colors.gray[700], border: colors.gray[300] },
    pending: { bg: colors.warning[100], text: colors.warning[800], border: colors.warning[200] },
    approved: { bg: colors.success[100], text: colors.success[800], border: colors.success[200] },
    rejected: { bg: colors.danger[50], text: colors.danger[700], border: colors.danger[200] },
    cancelled: { bg: colors.gray[100], text: colors.gray[500], border: colors.gray[300] },
  },
  // Time tracking statuses
  time: {
    'not-started': { bg: colors.gray[100], text: colors.gray[600] },
    working: { bg: colors.success[100], text: colors.success[800] },
    'on-break': { bg: colors.warning[100], text: colors.warning[800] },
    ended: { bg: colors.primary[50], text: colors.primary[700] },
  },
  // Timesheet day statuses
  timesheet: {
    complete: { bg: colors.success[100], text: colors.success[800] },
    approved: { bg: colors.primary[50], text: colors.primary[700] },
    incomplete: { bg: colors.warning[100], text: colors.warning[800] },
    empty: { bg: colors.gray[100], text: colors.gray[500] },
  },
  // Task priority
  taskPriority: {
    critical: { bg: colors.danger[100], text: colors.danger[800] },
    high: { bg: '#fff7ed', text: '#9a3412' }, // orange
    medium: { bg: colors.warning[100], text: colors.warning[800] },
    low: { bg: colors.success[100], text: colors.success[800] },
  },
  // Shift types
  shift: {
    dzienna: { bg: colors.warning[100], text: colors.warning[800] },
    nocna: { bg: '#ede9fe', text: '#5b21b6' }, // violet
    popoludniowa: { bg: colors.primary[50], text: colors.primary[700] },
  },
  // Generic
  active: { bg: colors.success[100], text: colors.success[800] },
  inactive: { bg: colors.gray[100], text: colors.gray[500] },
} as const;

// ─── Typography ─────────────────────────────────────────────

export const typography = {
  fontFamily: "'Plus Jakarta Sans', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif",

  fontSize: {
    xs: '11px',
    sm: '12px',
    base: '14px',
    md: '15px',
    lg: '16px',
    xl: '18px',
    '2xl': '20px',
    '3xl': '24px',
    '4xl': '28px',
  },

  fontWeight: {
    normal: 400 as const,
    medium: 500 as const,
    semibold: 600 as const,
    bold: 700 as const,
  },

  lineHeight: {
    tight: '1.25',
    normal: '1.5',
    relaxed: '1.75',
  },
} as const;

// ─── Spacing ────────────────────────────────────────────────

export const spacing = {
  0: '0',
  1: '4px',
  2: '8px',
  3: '12px',
  4: '16px',
  5: '20px',
  6: '24px',
  8: '32px',
  10: '40px',
  12: '48px',
  16: '64px',
} as const;

// ─── Border Radius ──────────────────────────────────────────

export const radii = {
  none: '0',
  sm: '6px',
  md: '8px',
  lg: '12px',
  xl: '16px',
  full: '9999px',
} as const;

// ─── Shadows ────────────────────────────────────────────────

export const shadows = {
  sm: '0 1px 2px rgba(20,25,43,0.04), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.85))',
  md: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.12), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
  lg: '0 12px 28px -8px rgba(20,25,43,0.14), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
  dropdown: 'var(--wb-shadow-pop, 0 12px 32px -8px rgba(20,25,43,0.18))',
} as const;

// ─── Transitions ────────────────────────────────────────────

export const transitions = {
  fast: '150ms ease',
  normal: '200ms ease',
  slow: '300ms ease',
} as const;

// ─── Breakpoints ────────────────────────────────────────────

export const breakpoints = {
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
} as const;
