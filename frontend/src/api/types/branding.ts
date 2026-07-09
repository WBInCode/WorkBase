export const ALLOWED_BRANDING_FONTS = [
  'Inter',
  'Roboto',
  'Open Sans',
  'Lato',
  'Poppins',
  'Nunito Sans',
] as const;

export type BrandingFont = (typeof ALLOWED_BRANDING_FONTS)[number];

export interface BrandingDto {
  id?: string;
  tenantId?: string;
  logoUrl: string | null;
  faviconUrl: string | null;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string | null;
  appName: string | null;
  customDomain: string | null;
  fontFamily: string | null;
  loginBackgroundUrl: string | null;
  updatedAt?: string;
}

export interface UpdateBrandingRequest {
  logoUrl: string | null;
  faviconUrl: string | null;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string | null;
  appName: string | null;
  customDomain: string | null;
  fontFamily: string | null;
  loginBackgroundUrl: string | null;
}

export type TerminologyOverrides = Record<string, string>;
