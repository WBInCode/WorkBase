// ─── Design System Tokens ───────────────────────────────────
// Single source of truth for colors, typography, spacing, radii.
// All components should import from here instead of hardcoding values.

// ─── Color Palette ──────────────────────────────────────────

export const colors = {
  // ── Primary (Blue) ──
  primary: {
    50: '#eff6ff',
    100: '#dbeafe',
    200: '#bfdbfe',
    300: '#93c5fd',
    400: '#60a5fa',
    500: '#3b82f6',
    600: '#2563eb',
    700: '#1d4ed8',
    800: '#1e40af',
    900: '#1e3a8a',
  },

  // ── Gray (Neutral) ──
  gray: {
    50: '#f9fafb',
    100: '#f3f4f6',
    200: '#e5e7eb',
    300: '#d1d5db',
    400: '#9ca3af',
    500: '#6b7280',
    600: '#4b5563',
    700: '#374151',
    800: '#1f2937',
    900: '#111827',
  },

  // ── Slate (Sidebar / Dark surfaces) ──
  slate: {
    400: '#94a3b8',
    700: '#334155',
    800: '#1e293b',
    900: '#0f172a',
  },

  // ── Semantic ──
  success: {
    50: '#f0fdf4',
    100: '#dcfce7',
    200: '#bbf7d0',
    500: '#22c55e',
    600: '#16a34a',
    700: '#15803d',
    800: '#166534',
  },
  warning: {
    50: '#fffbeb',
    100: '#fef3c7',
    200: '#fde68a',
    500: '#f59e0b',
    600: '#d97706',
    700: '#b45309',
    800: '#92400e',
  },
  danger: {
    50: '#fef2f2',
    100: '#fee2e2',
    200: '#fecaca',
    400: '#f87171',
    500: '#ef4444',
    600: '#dc2626',
    700: '#b91c1c',
    800: '#991b1b',
  },
  info: {
    50: '#eff6ff',
    100: '#dbeafe',
    500: '#3b82f6',
    600: '#2563eb',
  },
  emerald: {
    600: '#059669',
  },

  // ── Base ──
  white: '#ffffff',
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
  bgPage: colors.white,
  bgSurface: colors.white,
  bgSubtle: colors.gray[50],
  bgMuted: colors.gray[100],
  bgSidebar: colors.slate[800],
  bgHover: colors.gray[50],
  bgSelected: colors.primary[50],

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
  fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif",

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
  sm: '4px',
  md: '6px',
  lg: '8px',
  xl: '12px',
  full: '9999px',
} as const;

// ─── Shadows ────────────────────────────────────────────────

export const shadows = {
  sm: '0 1px 2px rgba(0,0,0,0.05)',
  md: '0 4px 6px rgba(0,0,0,0.07)',
  lg: '0 10px 15px rgba(0,0,0,0.1)',
  dropdown: '0 4px 12px rgba(0,0,0,0.15)',
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
