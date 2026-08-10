/**
 * Jedno zrodlo prawdy dla dostepu do widokow.
 *
 * Ta sama mapa steruje kafelkami w nawigacji ORAZ wejsciem na trase, zeby nie dalo sie ominac
 * ukrytego menu wklejajac adres. Backend i tak sprawdza kazde zadanie osobno (RequirePermission),
 * to jest warstwa interfejsu: uzytkownik nie oglada ekranow, ktorych i tak nie uzyje.
 *
 * Klucze uprawnien musza istniec w slowniku (IamSeeder.CreatePermissions).
 */

/** Wystarczy JEDNO z wymienionych uprawnien. Pusta tablica = kazdy zalogowany. */
export type WymaganeUprawnienia = readonly string[];

/**
 * Trasy, na ktore wpuszczamy takze przelozonego bez odpowiedniego uprawnienia.
 * Akceptanta wniosku wyznacza relacja w strukturze (org_supervisor_relations), a nie rola —
 * osoba z rolą „Pracownik” realnie ma wnioski do rozpatrzenia. Bez tego wyjatku kolejka
 * akceptacji znikala przelozonym i caly obieg urlopowy stawal.
 */
export const WIDOKI_DLA_PRZELOZONEGO: ReadonlySet<string> = new Set([
  '/leave/approvals',
  '/time/team-report',
]);

/**
 * Wzorce tras. Segment `:cos` dopasowuje dowolna wartosc.
 * Kolejnosc ma znaczenie: pierwsze trafienie wygrywa, wiec trasy konkretne stoja przed
 * tymi z parametrem (`/org/employees/import` przed `/org/employees/:id`).
 */
export const DOSTEP_DO_WIDOKOW: ReadonlyArray<readonly [string, WymaganeUprawnienia]> = [
  // Pulpit pracownika — dostepny zawsze, to strona startowa po zalogowaniu.
  ['/workspace', []],
  ['/dashboard', ['dashboard.view']],

  ['/org/tree', ['org.view']],
  ['/org/employees/import', ['org.import']],
  ['/org/employees/:id', ['org.view']],
  ['/org/employees', ['org.view']],

  ['/time/timesheet', ['time.view']],
  ['/time/team-report', ['time.view-team']],
  ['/time/schedule', ['time.view']],
  ['/payroll', ['payroll.view']],

  ['/leave/request', ['leave.view']],
  ['/leave/approvals', ['leave.approve']],
  ['/leave/calendar', ['leave.view']],

  ['/tasks/my', ['tasks.view']],
  ['/tasks/:id', ['tasks.view']],
  ['/tasks', ['tasks.view']],

  ['/documents/categories', ['documents.manage']],
  ['/documents', ['documents.view']],

  ['/workflow/builder', ['workflow.manage']],
  ['/forms/builder', ['forms.manage']],

  // Administracja — wczesniej jeden boolean `isAdmin`. Teraz konkretne uprawnienia, wiec
  // wlasciciel moze oddac np. slowniki kadrowe kierownikowi biura bez robienia z niego admina.
  ['/admin/roles', ['identity.manage']],
  ['/admin/permissions', ['identity.manage']],
  ['/admin/feature-flags', ['identity.manage-feature-flags']],
  ['/admin/tenants', ['platform.manage-tenants']],
  ['/admin/leave-types', ['leave.manage']],
  ['/admin/leave-policies', ['leave.manage']],
  ['/admin/task-statuses', ['tasks.manage']],
  ['/admin/positions', ['org.manage']],
  ['/admin/unit-types', ['org.manage']],
  ['/admin/branding', ['config.manage']],
  ['/admin/terminology', ['config.manage']],
  ['/admin/break-policies', ['config.manage']],
  ['/admin/time-tracking-settings', ['config.manage']],
  ['/admin/notification-templates', ['config.manage']],
  ['/admin/escalation-rules', ['config.manage']],
  ['/admin/document-settings', ['config.manage']],
  ['/admin/task-settings', ['config.manage']],
];

function pasuje(wzorzec: string, sciezka: string): boolean {
  const a = wzorzec.split('/');
  const b = sciezka.split('/');
  if (a.length !== b.length) return false;
  return a.every((segment, i) => segment.startsWith(':') || segment === b[i]);
}

/** Uprawnienia wymagane dla sciezki. `null` = trasa spoza mapy (np. 404 → przekierowanie). */
export function uprawnieniaDlaSciezki(sciezka: string): WymaganeUprawnienia | null {
  const bezUkosnika = sciezka.length > 1 ? sciezka.replace(/\/+$/, '') : sciezka;
  const trafienie = DOSTEP_DO_WIDOKOW.find(([wzorzec]) => pasuje(wzorzec, bezUkosnika));
  return trafienie ? trafienie[1] : null;
}

/** Czy sciezka jest dostepna dla przelozonego mimo braku uprawnienia. */
export function dostepnaDlaPrzelozonego(sciezka: string): boolean {
  const bezUkosnika = sciezka.length > 1 ? sciezka.replace(/\/+$/, '') : sciezka;
  return WIDOKI_DLA_PRZELOZONEGO.has(bezUkosnika);
}
