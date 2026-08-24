import { useMemo, useState, Fragment, useEffect, useCallback } from 'react';
import { ChevronDown, ChevronRight, Settings, Download } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { useCurrentUser } from '@/api/hooks/useIam';
import { useUprawnienia } from '@/auth/useUprawnienia';
import { useEmployees } from '@/api/hooks/useOrganization';
import { useTeamLeaveRequests } from '@/api/hooks/useLeave';
import { useRozliczenie } from '@/api/hooks/useRozliczenie';
import { usePayrollSettings, useUpdatePayrollSettings } from '@/api/hooks/usePayrollSettings';
import type { LeaveRequestDto } from '@/api/types/leave';
import { colors } from '@/theme/tokens';
import {
  utworzSkoroszyt,
  pobierzSkoroszyt,
  WYPELNIENIE_NAGLOWKA,
  CZCIONKA_NAGLOWKA,
  CIENKA_RAMKA,
} from '@/shared/arkusz';
import { NAGLOWKI_ROZLICZENIA, wierszDoArkusza, wierszSumy } from './rozliczenieDoArkusza';

const DEFAULT_OVERTIME_MULTIPLIER = 1.5;

function startOfMonth(d: Date): string {
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
}
function endOfMonth(d: Date): string {
  return new Date(d.getFullYear(), d.getMonth() + 1, 0).toISOString().slice(0, 10);
}


function fmtH(h: number): string {
  if (h <= 0) return '0h';
  const hours = Math.floor(h);
  const mins = Math.round((h - hours) * 60);
  if (mins === 0) return `${hours}h`;
  return `${hours}h ${mins}min`;
}

const fmtPLN = (n: number) =>
  new Intl.NumberFormat('pl-PL', { style: 'currency', currency: 'PLN' }).format(n);

function daysOfApprovedLeavesInRange(
  leaves: LeaveRequestDto[],
  from: Date,
  to: Date,
): { vacationDays: number; absenceDays: number } {
  let vacation = 0;
  let absence = 0;
  for (const l of leaves) {
    if (l.status !== 'Approved' && l.status !== 'Pending') continue;
    const ls = new Date(l.startDate);
    const le = new Date(l.endDate);
    const start = ls < from ? from : ls;
    const end = le > to ? to : le;
    if (start > end) continue;
    const days =
      Math.floor((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1;
    const code = (l.leaveTypeCode ?? '').toUpperCase();
    const name = (l.leaveTypeName ?? '').toLowerCase();
    if (code.startsWith('URL') || name.includes('urlop') || name.includes('wypocz')) {
      vacation += days;
    } else {
      absence += days;
    }
  }
  return { vacationDays: vacation, absenceDays: absence };
}


interface Row {
  id: string;
  name: string;
  email: string;
  rate: number;
  hasRate: boolean;
  normaH: number;
  workedH: number;
  regularH: number;
  overtimeH: number;
  nightH: number;
  holidayH: number;
  vacationDays: number;
  absenceDays: number;
  basicPay: number;
  overtimePay: number;
  nightPay: number;
  holidayPay: number;
  totalPay: number;
}

export function PayrollPage() {
  const today = new Date();
  const [from, setFrom] = useState(startOfMonth(today));
  const [to, setTo] = useState(endOfMonth(today));
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  const auth = useAuth();
  // isAdmin is sourced from the app's own Role/Permission data, not the Keycloak "roles" claim
  // — see docs/AUDIT-KNOWLEDGE-MAP.md (role system consistency).
  const { data: currentUser } = useCurrentUser();
  const isAdmin = !!currentUser?.isAdmin;
  // Zakres zespolowy szedl wczesniej z roszczenia "roles" w tokenie Keycloaka, a rola
  // workbase-manager nigdy nie jest tam zakladana (KeycloakAdminService.CreateRealmRolesAsync).
  // Kierownik trafial przez to do galezi "tylko wlasny wiersz" i widzial ekran plac z jedna
  // pozycja. Bierzemy wiec to samo zrodlo, ktore sprawdza backend: uprawnienie payroll.view-team.
  // Szerzej niz pozwala zakres i tak nie zobaczy — stawki spoza zakresu backend zeruje
  // (EmployeeEndpoints), wiec ta zmiana nie moze niczego odslonic.
  const { moze } = useUprawnienia();
  const isDepartmentScope = !isAdmin && moze('payroll.view-team');
  const userSub = auth.user?.profile?.sub ?? null;

  const { data: payrollSettings } = usePayrollSettings();
  const overtimeMultiplier = payrollSettings?.overtimeMultiplier ?? DEFAULT_OVERTIME_MULTIPLIER;

  const { data: employeesPage, isLoading: loadingEmployees } = useEmployees({
    page: 1,
    pageSize: 200,
    status: 'Active',
  });

  const allEmployees = employeesPage?.items ?? [];
  const currentEmployee = useMemo(
    () => (userSub ? allEmployees.find((e) => e.userId === userSub) ?? null : null),
    [allEmployees, userSub],
  );

  const employees = useMemo(() => {
    if (isAdmin) return allEmployees;
    if (isDepartmentScope) {
      const unitId = currentEmployee?.primaryOrganizationUnitId;
      if (!unitId) return currentEmployee ? [currentEmployee] : [];
      return allEmployees.filter((e) => e.primaryOrganizationUnitId === unitId);
    }
    // Pracownik — tylko własny wiersz
    return currentEmployee ? [currentEmployee] : [];
  }, [allEmployees, isAdmin, isDepartmentScope, currentEmployee]);

  const employeeIds = useMemo(() => employees.map((e) => e.id), [employees]);

  // Godziny i kwoty liczy serwer: dodatek nocny wymaga wpisow czasu, a swiateczny kalendarza
  // dni wolnych — jedno i drugie jest poza zasiegiem przegladarki. Tutaj zostaje wylacznie
  // to, czego rozliczenie nie zwraca: nazwiska oraz dni urlopu i nieobecnosci.
  const { data: rozliczenie, isLoading: loadingRozliczenie } = useRozliczenie(from, to);

  const year = new Date(from).getFullYear();
  const { data: leavesByEmp, isLoading: loadingLeaves } = useTeamLeaveRequests(
    employeeIds,
    year,
  );

  const fromDate = useMemo(() => new Date(from), [from]);
  const toDate = useMemo(() => new Date(to), [to]);

  const rows: Row[] = useMemo(() => {
    if (!rozliczenie || !leavesByEmp) return [];

    const wgPracownika = new Map(rozliczenie.map((r) => [r.employeeId, r]));

    return employees.map((emp, idx) => {
      const r = wgPracownika.get(emp.id);
      const leaves = leavesByEmp[idx] ?? [];
      const { vacationDays, absenceDays } = daysOfApprovedLeavesInRange(leaves, fromDate, toDate);

      return {
        id: emp.id,
        name: `${emp.firstName} ${emp.lastName}`,
        email: emp.email,
        rate: emp.hourlyRate ?? 0,
        hasRate: emp.hourlyRate !== null && emp.hourlyRate !== undefined,
        normaH: r?.normaH ?? 0,
        workedH: r?.przepracowaneH ?? 0,
        regularH: r?.zwykleH ?? 0,
        overtimeH: r?.nadgodzinyH ?? 0,
        nightH: r?.nocneH ?? 0,
        holidayH: r?.swiateczneH ?? 0,
        vacationDays,
        absenceDays,
        basicPay: r?.zasadnicze ?? 0,
        overtimePay: r?.zaNadgodziny ?? 0,
        nightPay: r?.dodatekNocny ?? 0,
        holidayPay: r?.dodatekSwiateczny ?? 0,
        totalPay: r?.razem ?? 0,
      };
    });
  }, [employees, rozliczenie, leavesByEmp, fromDate, toDate]);

  const totals = useMemo(() => {
    return rows.reduce(
      (acc, r) => ({
        normaH: acc.normaH + r.normaH,
        workedH: acc.workedH + r.workedH,
        overtimeH: acc.overtimeH + r.overtimeH,
        basicPay: acc.basicPay + r.basicPay,
        overtimePay: acc.overtimePay + r.overtimePay,
        totalPay: acc.totalPay + r.totalPay,
      }),
      { normaH: 0, workedH: 0, overtimeH: 0, basicPay: 0, overtimePay: 0, totalPay: 0 },
    );
  }, [rows]);

  const isLoading =
    loadingEmployees || loadingRozliczenie || loadingLeaves;

  const toggle = (id: string) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  /**
   * Eksport zestawienia dla kadr. Rozbicie dzienne (pracownik x dzien) ma juz raport
   * zespolu — tutaj brakowalo tego, co idzie do listy plac: normy, czasu pracy, nadgodzin,
   * nieobecnosci i kwot. Do arkusza trafiaja LICZBY, nie sformatowane napisy, zeby dalo sie
   * na nich liczyc; formatowanie ustawiamy maska komorki.
   *
   * Wiersze sa dokladnie tym, co widac na ekranie, a widac tylko to, na co pozwala zakres
   * danych pytajacego — eksport nie omija uprawnien.
   */
  const eksportujZestawienie = useCallback(async () => {
    const skoroszyt = await utworzSkoroszyt();
    const arkusz = skoroszyt.addWorksheet('Rozliczenie');

    const naglowki = [...NAGLOWKI_ROZLICZENIA];

    const wierszNaglowka = arkusz.addRow(naglowki);
    wierszNaglowka.eachCell((komorka) => {
      komorka.fill = WYPELNIENIE_NAGLOWKA;
      komorka.font = CZCIONKA_NAGLOWKA;
      komorka.border = CIENKA_RAMKA;
      komorka.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
    });
    wierszNaglowka.height = 30;

    for (const r of rows) {
      const wiersz = arkusz.addRow(wierszDoArkusza(r));
      wiersz.eachCell((komorka) => { komorka.border = CIENKA_RAMKA; });
    }

    // Podsumowanie na dole — kadry i tak je licza recznie, wiec niech przyjdzie gotowe.
    const podsumowanie = arkusz.addRow(wierszSumy(rows));
    podsumowanie.eachCell((komorka) => {
      komorka.font = { bold: true };
      komorka.border = CIENKA_RAMKA;
    });

    arkusz.getColumn(1).width = 28;
    arkusz.getColumn(2).width = 30;
    for (let kolumna = 3; kolumna <= naglowki.length; kolumna++) arkusz.getColumn(kolumna).width = 15;

    // Godziny z dwoma miejscami, kwoty z separatorem tysiecy — inaczej arkusz pokazuje
    // 7.333333333333333 i kadrowa dostaje liczbe, ktorej nie da sie przepisac.
    // Numery kolumn odpowiadaja NAGLOWKI_ROZLICZENIA — godziny z dwoma miejscami,
    // kwoty z separatorem tysiecy.
    for (const kolumna of [4, 5, 6, 7, 8, 9]) arkusz.getColumn(kolumna).numFmt = '0.00';
    for (const kolumna of [3, 12, 13, 14, 15, 16]) arkusz.getColumn(kolumna).numFmt = '# ##0.00';

    arkusz.views = [{ state: 'frozen', ySplit: 1 }];

    await pobierzSkoroszyt(skoroszyt, `rozliczenie-${from}_${to}.xlsx`);
  }, [rows, from, to]);

  return (
    <div style={{ padding: '24px 28px', maxWidth: '1400px', margin: '0 auto' }}>
      {/* ── Karta dowodzenia: tytuł + ustawienia + zakres + statystyki ── */}
      <div
        style={{
          backgroundColor: 'var(--wb-panel, #fff)',
          border: '1px solid var(--wb-line, #e3e7f1)',
          borderRadius: 20,
          boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
          padding: '18px 22px',
          marginBottom: 18,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
          <div>
            <h1 style={{ fontSize: 22, fontWeight: 800, letterSpacing: '-0.02em', margin: 0, color: colors.gray[900] }}>Wynagrodzenia</h1>
            <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '3px 0 0', fontSize: 13 }}>
              Ewidencja czasu pracy + rozliczenie wynagrodzeń (norma z grafiku, czas pracy z kart, nadgodziny ×{overtimeMultiplier}).
            </p>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <button
              onClick={eksportujZestawienie}
              disabled={isLoading || rows.length === 0}
              title="Pobierz zestawienie do listy plac (XLSX)"
              style={{
                display: 'inline-flex', alignItems: 'center', gap: 6,
                padding: '8px 14px', borderRadius: 10,
                border: '1px solid var(--wb-line, #e3e7f1)',
                background: 'var(--wb-panel, #fff)',
                color: colors.gray[900], fontSize: 13, fontWeight: 600,
                cursor: isLoading || rows.length === 0 ? 'not-allowed' : 'pointer',
                opacity: isLoading || rows.length === 0 ? 0.5 : 1,
              }}
            >
              <Download size={15} />
              Eksport XLSX
            </button>
            {isAdmin && <PayrollSettingsButton />}
          </div>
        </div>

        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'auto auto 1fr repeat(3, auto)',
            gap: 16,
            alignItems: 'end',
            marginTop: 16,
          }}
        >
          <div>
            <label style={lblStyle}>Od</label>
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              style={inputStyle}
            />
          </div>
          <div>
            <label style={lblStyle}>Do</label>
            <input
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              style={inputStyle}
            />
          </div>
          <div />
          <Stat label="Norma" value={fmtH(totals.normaH)} />
          <Stat label="Czas pracy" value={fmtH(totals.workedH)} accent="var(--wb-tea-700, #0f766e)" />
          <Stat label="Razem brutto" value={fmtPLN(totals.totalPay)} accent={colors.primary[700]} big />
        </div>
      </div>

      {isLoading ? (
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, color: 'var(--wb-g-400, #9aa3bc)', fontSize: 14 }}>
          <div className="wb-spinner" /> Ładowanie…
        </div>
      ) : rows.length === 0 ? (
        <div style={{ color: 'var(--wb-g-500, #64748b)' }}>Brak aktywnych pracowników.</div>
      ) : (
        <div style={{ overflowX: 'auto', border: '1px solid var(--wb-line, #e3e7f1)', borderRadius: 16, backgroundColor: 'var(--wb-panel, #fff)', boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.08)' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead style={{ background: 'var(--wb-g-100, #f1f5f9)' }}>
              <tr>
                <th style={{ ...th, width: 28 }}></th>
                <th style={th}>Pracownik</th>
                <th style={{ ...th, textAlign: 'right' }}>Stawka [PLN/h]</th>
                <th style={{ ...th, textAlign: 'right' }}>Norma</th>
                <th style={{ ...th, textAlign: 'right' }}>Czas pracy</th>
                <th style={{ ...th, textAlign: 'right' }}>Nadgodziny</th>
                <th style={{ ...th, textAlign: 'right' }}>Urlop [dni]</th>
                <th style={{ ...th, textAlign: 'right' }}>Nieobec. [dni]</th>
                <th style={{ ...th, textAlign: 'right' }}>Zasadnicze</th>
                <th style={{ ...th, textAlign: 'right' }}>Za nadgodziny</th>
                <th style={{ ...th, textAlign: 'right', background: 'var(--wb-ind-100, #e0e7ff)' }}>Całkowite</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => {
                const isOpen = expanded.has(r.id);
                return (
                  <Fragment key={r.id}>
                    <tr
                      onClick={() => toggle(r.id)}
                      style={{
                        borderTop: '1px solid var(--wb-g-200, #e2e8f0)',
                        cursor: 'pointer',
                        background: isOpen ? 'var(--wb-g-50, #f8fafc)' : 'transparent',
                      }}
                    >
                      <td style={td}>
                        {isOpen ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                      </td>
                      <td style={td}>
                        <div style={{ fontWeight: 600 }}>{r.name}</div>
                        <div style={{ color: 'var(--wb-g-500, #64748b)', fontSize: 11 }}>{r.email}</div>
                      </td>
                      <td style={{ ...td, textAlign: 'right' }}>
                        {r.hasRate ? (
                          r.rate.toFixed(2)
                        ) : (
                          <span style={{ color: colors.danger[600], fontStyle: 'italic' }}>brak</span>
                        )}
                      </td>
                      <td style={{ ...td, textAlign: 'right' }}>{fmtH(r.normaH)}</td>
                      <td style={{ ...td, textAlign: 'right' }}>{fmtH(r.workedH)}</td>
                      <td
                        style={{
                          ...td,
                          textAlign: 'right',
                          color: r.overtimeH > 0 ? 'var(--wb-org-600, #ea580c)' : 'var(--wb-g-500, #64748b)',
                          fontWeight: r.overtimeH > 0 ? 600 : 400,
                        }}
                      >
                        {fmtH(r.overtimeH)}
                      </td>
                      <td style={{ ...td, textAlign: 'right' }}>{r.vacationDays || '—'}</td>
                      <td style={{ ...td, textAlign: 'right' }}>{r.absenceDays || '—'}</td>
                      <td style={{ ...td, textAlign: 'right' }}>
                        {r.hasRate ? fmtPLN(r.basicPay) : '—'}
                      </td>
                      <td style={{ ...td, textAlign: 'right' }}>
                        {r.hasRate ? fmtPLN(r.overtimePay) : '—'}
                      </td>
                      <td
                        style={{
                          ...td,
                          textAlign: 'right',
                          fontWeight: 700,
                          background: 'var(--wb-ind-50, #eef2ff)',
                        }}
                      >
                        {r.hasRate ? fmtPLN(r.totalPay) : '—'}
                      </td>
                    </tr>
                    {isOpen && (
                      <tr style={{ background: 'var(--wb-g-50, #f8fafc)' }}>
                        <td colSpan={11} style={{ padding: '12px 20px 18px' }}>
                          <DetailGrid row={r} from={fromDate} to={toDate} overtimeMultiplier={overtimeMultiplier} />
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <p style={{ marginTop: 16, fontSize: 12, color: 'var(--wb-g-400, #94a3b8)' }}>
        Norma — z grafiku pracy, pomniejszona o dni wolne oznaczone jako obniżające normę. Czas pracy — netto (po odjęciu przerw). Nadgodziny — czas pracy ponad normę. Dodatki nocny i świąteczny liczą się jako nadwyżka ponad stawkę, więc godzina nocna będąca nadgodziną nie jest płatna dwa razy. Porę nocną i mnożniki ustawiasz w ustawieniach wynagrodzeń, stawkę — w karcie pracownika.
      </p>
    </div>
  );
}

function DetailGrid({ row, from, to, overtimeMultiplier }: { row: Row; from: Date; to: Date; overtimeMultiplier: number }) {
  const days = Math.floor((to.getTime() - from.getTime()) / (1000 * 60 * 60 * 24)) + 1;
  return (
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(3, 1fr)',
        gap: 12,
        fontSize: 12,
      }}
    >
      <DetailCard title="Czas pracy">
        <DetailLine label="Norma (grafik)" value={fmtH(row.normaH)} />
        <DetailLine label="Czas pracy (netto)" value={fmtH(row.workedH)} accent="var(--wb-tea-700, #0f766e)" />
        <DetailLine label="Godziny zwykłe" value={fmtH(row.regularH)} />
        <DetailLine
          label="Nadgodziny"
          value={fmtH(row.overtimeH)}
          accent={row.overtimeH > 0 ? 'var(--wb-org-600, #ea580c)' : undefined}
        />
        <DetailLine
          label="Bilans"
          value={`${row.workedH >= row.normaH ? '+' : '−'}${fmtH(Math.abs(row.workedH - row.normaH))}`}
        />
      </DetailCard>

      <DetailCard title="Nieobecności">
        <DetailLine label="Urlop wypoczynkowy" value={`${row.vacationDays} dni`} />
        <DetailLine label="Inne nieobecności" value={`${row.absenceDays} dni`} />
        <DetailLine
          label="Razem nieobecności"
          value={`${row.vacationDays + row.absenceDays} dni`}
        />
        <DetailLine label="Okres" value={`${days} dni`} />
      </DetailCard>

      <DetailCard title="Składniki wynagrodzenia">
        <DetailLine
          label={`Zasadnicze (${row.regularH.toFixed(2)}h × ${row.rate.toFixed(2)})`}
          value={row.hasRate ? fmtPLN(row.basicPay) : '—'}
        />
        <DetailLine
          label={`Za nadgodziny (${row.overtimeH.toFixed(2)}h × ${row.rate.toFixed(2)} × ${overtimeMultiplier})`}
          value={row.hasRate ? fmtPLN(row.overtimePay) : '—'}
        />
        {row.nightH > 0 && (
          <DetailLine
            label={`Dodatek nocny (${row.nightH.toFixed(2)}h)`}
            value={row.hasRate ? fmtPLN(row.nightPay) : '—'}
          />
        )}
        {row.holidayH > 0 && (
          <DetailLine
            label={`Dodatek świąteczny (${row.holidayH.toFixed(2)}h)`}
            value={row.hasRate ? fmtPLN(row.holidayPay) : '—'}
          />
        )}
        <DetailLine
          label="Całkowite brutto"
          value={row.hasRate ? fmtPLN(row.totalPay) : '—'}
          accent={colors.primary[700]}
          bold
        />
      </DetailCard>
    </div>
  );
}

function DetailCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div
      style={{
        background: 'var(--wb-panel, #fff)',
        border: '1px solid var(--wb-line, #e2e8f0)',
        borderRadius: 10,
        padding: 12,
      }}
    >
      <div
        style={{
          fontWeight: 700,
          fontSize: 11,
          textTransform: 'uppercase',
          letterSpacing: '0.05em',
          color: 'var(--wb-g-600, #475569)',
          marginBottom: 8,
          paddingBottom: 6,
          borderBottom: '1px solid var(--wb-g-100, #f1f5f9)',
        }}
      >
        {title}
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>{children}</div>
    </div>
  );
}

function DetailLine({
  label,
  value,
  accent,
  bold,
}: {
  label: string;
  value: string;
  accent?: string;
  bold?: boolean;
}) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 8 }}>
      <span style={{ color: 'var(--wb-g-500, #64748b)' }}>{label}</span>
      <span
        style={{
          color: accent ?? colors.gray[900],
          fontWeight: bold ? 700 : 500,
          textAlign: 'right',
        }}
      >
        {value}
      </span>
    </div>
  );
}

function Stat({
  label,
  value,
  accent,
  big,
}: {
  label: string;
  value: string;
  accent?: string;
  big?: boolean;
}) {
  return (
    <div style={{ textAlign: 'right' }}>
      <div style={{ fontSize: 11, color: 'var(--wb-g-500, #64748b)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
        {label}
      </div>
      <div
        style={{
          fontSize: big ? 22 : 16,
          fontWeight: 700,
          color: accent ?? colors.gray[900],
        }}
      >
        {value}
      </div>
    </div>
  );
}

const th: React.CSSProperties = {
  padding: '8px 10px',
  textAlign: 'left',
  fontSize: 11,
  fontWeight: 700,
  color: 'var(--wb-g-600, #475569)',
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
  whiteSpace: 'nowrap',
};

const td: React.CSSProperties = {
  padding: '10px 10px',
  whiteSpace: 'nowrap',
};

const lblStyle: React.CSSProperties = {
  display: 'block',
  fontSize: 11,
  fontWeight: 600,
  marginBottom: 4,
  color: 'var(--wb-g-600, #475569)',
  textTransform: 'uppercase',
  letterSpacing: '0.05em',
};

const inputStyle: React.CSSProperties = {
  padding: '6px 10px',
  border: '1px solid var(--wb-g-300, #cbd5e1)',
  borderRadius: 10,
  fontSize: 13,
};

function PayrollSettingsButton() {
  const [open, setOpen] = useState(false);
  const { data, isLoading } = usePayrollSettings();
  const update = useUpdatePayrollSettings();
  const [overtime, setOvertime] = useState('1.5');
  const [night, setNight] = useState('1.2');
  const [holiday, setHoliday] = useState('2.0');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (data) {
      setOvertime(String(data.overtimeMultiplier));
      setNight(String(data.nightMultiplier));
      setHoliday(String(data.holidayMultiplier));
    }
  }, [data]);

  const save = async () => {
    setError(null);
    const ot = Number(overtime.replace(',', '.'));
    const nt = Number(night.replace(',', '.'));
    const hd = Number(holiday.replace(',', '.'));
    if (![ot, nt, hd].every((v) => Number.isFinite(v) && v >= 1 && v <= 10)) {
      setError('Każdy mnożnik musi być liczbą z zakresu 1.0 – 10.0');
      return;
    }
    try {
      await update.mutateAsync({
        overtimeMultiplier: ot,
        nightMultiplier: nt,
        holidayMultiplier: hd,
      });
      setOpen(false);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Nie udało się zapisać ustawień');
    }
  };

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 6,
          padding: '8px 14px',
          background: 'var(--wb-panel, #fff)',
          border: '1px solid var(--wb-g-300, #cbd5e1)',
          borderRadius: 10,
          fontSize: 13,
          fontWeight: 600,
          color: 'var(--wb-g-700, #334155)',
          cursor: 'pointer',
        }}
      >
        <Settings size={14} />
        Ustawienia naliczania
      </button>

      {open && (
        <div
          onClick={() => setOpen(false)}
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(20,25,43,0.45)', backdropFilter: 'blur(3px)', WebkitBackdropFilter: 'blur(3px)', animation: 'wb-backdrop-in 0.18s ease both',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
          }}
        >
          <div
            onClick={(e) => e.stopPropagation()}
            style={{
              background: 'var(--wb-panel, #fff)',
              borderRadius: 10,
              padding: 24,
              width: 460,
              maxWidth: '90vw',
              boxShadow: '0 20px 50px rgba(0,0,0,0.25)',
            }}
          >
            <h2 style={{ margin: 0, fontSize: 18, fontWeight: 700, marginBottom: 4 }}>
              Ustawienia naliczania wynagrodzeń
            </h2>
            <p style={{ margin: 0, color: 'var(--wb-g-500, #64748b)', fontSize: 12, marginBottom: 18 }}>
              Mnożniki stosowane do stawki godzinowej pracownika.
            </p>

            {isLoading ? (
              <div>Ładowanie…</div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                <SettingField
                  label="Nadgodziny (× stawka)"
                  hint="Standardowo 1.5 (50% dodatku)"
                  value={overtime}
                  onChange={setOvertime}
                />
                <SettingField
                  label="Praca w nocy (× stawka)"
                  hint="Standardowo 1.2 (20% dodatku)"
                  value={night}
                  onChange={setNight}
                />
                <SettingField
                  label="Praca w święta / niedziele (× stawka)"
                  hint="Standardowo 2.0 (100% dodatku)"
                  value={holiday}
                  onChange={setHoliday}
                />
              </div>
            )}

            {error && (
              <div
                style={{
                  marginTop: 12,
                  padding: '8px 12px',
                  background: 'var(--wb-dan-50, #fef2f2)',
                  color: 'var(--wb-dan-700, #b91c1c)',
                  border: '1px solid var(--wb-dan-200, #fecaca)',
                  borderRadius: 10,
                  fontSize: 12,
                }}
              >
                {error}
              </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 20 }}>
              <button
                type="button"
                onClick={() => setOpen(false)}
                style={{
                  padding: '8px 14px',
                  background: 'var(--wb-panel, #fff)',
                  border: '1px solid var(--wb-g-300, #cbd5e1)',
                  borderRadius: 10,
                  fontSize: 13,
                  cursor: 'pointer',
                }}
              >
                Anuluj
              </button>
              <button
                type="button"
                onClick={save}
                disabled={update.isPending}
                style={{
                  padding: '8px 14px',
                  background: colors.primary[700],
                  color: '#fff',
                  border: 'none',
                  borderRadius: 10,
                  fontSize: 13,
                  fontWeight: 600,
                  cursor: 'pointer',
                  opacity: update.isPending ? 0.6 : 1,
                }}
              >
                {update.isPending ? 'Zapisywanie…' : 'Zapisz'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

function SettingField({
  label,
  hint,
  value,
  onChange,
}: {
  label: string;
  hint: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <div>
      <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--wb-g-700, #334155)', marginBottom: 4 }}>
        {label}
      </label>
      <input
        type="number"
        step="0.01"
        min="1"
        max="10"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        style={{
          width: '100%',
          padding: '8px 10px',
          border: '1px solid var(--wb-g-300, #cbd5e1)',
          borderRadius: 10,
          fontSize: 14,
          boxSizing: 'border-box',
        }}
      />
      <div style={{ fontSize: 11, color: 'var(--wb-g-400, #94a3b8)', marginTop: 2 }}>{hint}</div>
    </div>
  );
}
