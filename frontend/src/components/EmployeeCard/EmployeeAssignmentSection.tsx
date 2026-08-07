import { useMemo, useState } from 'react';
import { Building2, UserCog } from 'lucide-react';
import type { EmployeeDetailDto, OrganizationUnitTreeNode } from '@/api/types/organization';
import {
  useAssignEmployee,
  useEmployees,
  useOrgUnitTree,
  usePositions,
  useSetSupervisor,
} from '@/api/hooks/useOrganization';
import { colors } from '@/theme/tokens';

interface Props {
  employee: EmployeeDetailDto;
  onZmiana: () => void;
}

/** Spłaszcza drzewo jednostek do listy z wcięciami, żeby zmieścić je w zwykłym <select>. */
function splaszcz(wezly: OrganizationUnitTreeNode[], poziom = 0): { id: string; etykieta: string }[] {
  return wezly.flatMap((w) => [
    { id: w.id, etykieta: `${'\u00a0\u00a0'.repeat(poziom)}${w.name}${w.code ? ` (${w.code})` : ''}` },
    ...splaszcz(w.children ?? [], poziom + 1),
  ]);
}

const polePodpis: React.CSSProperties = {
  display: 'block',
  fontSize: 12,
  fontWeight: 600,
  color: colors.gray[500],
  marginBottom: 4,
};

const poleKontrolka: React.CSSProperties = {
  width: '100%',
  minHeight: 38,
  padding: '8px 10px',
  border: `1px solid ${colors.gray[200]}`,
  borderRadius: 8,
  fontSize: 14,
  background: colors.white,
  color: colors.gray[900],
};

const przyciskGlowny: React.CSSProperties = {
  minHeight: 38,
  padding: '8px 16px',
  border: 0,
  borderRadius: 8,
  fontSize: 13,
  fontWeight: 700,
  color: colors.white,
  background: colors.primary[600],
  cursor: 'pointer',
};

/**
 * Przypisanie do jednostki i stanowiska oraz wskazanie przełożonego.
 * Bez tych dwóch rzeczy pracownik nie przejdzie procesu akceptacji wniosków —
 * wcześniej karta tylko ostrzegała o braku przełożonego, nie dając jak go ustawić.
 */
export function EmployeeAssignmentSection({ employee, onZmiana }: Props) {
  const { data: drzewo = [] } = useOrgUnitTree();
  const { data: stanowiska = [] } = usePositions();
  const { data: lista } = useEmployees({ page: 1, pageSize: 300, status: 'Active' });

  const przypisanie = employee.assignments.find((a) => a.isPrimary) ?? employee.assignments[0];

  const [jednostka, setJednostka] = useState(przypisanie?.organizationUnitId ?? '');
  const [stanowisko, setStanowisko] = useState(przypisanie?.positionId ?? '');
  const [przelozony, setPrzelozony] = useState(employee.supervisor?.employeeId ?? '');
  const [komunikat, setKomunikat] = useState<{ typ: 'ok' | 'blad'; tresc: string } | null>(null);

  const przypisz = useAssignEmployee();
  const ustawPrzelozonego = useSetSupervisor();

  const jednostki = useMemo(() => splaszcz(drzewo), [drzewo]);
  const kandydaci = useMemo(
    () => (lista?.items ?? []).filter((e) => e.id !== employee.id),
    [lista, employee.id],
  );
  const aktywneStanowiska = useMemo(() => stanowiska.filter((p) => p.isActive), [stanowiska]);

  function pokaz(typ: 'ok' | 'blad', tresc: string) {
    setKomunikat({ typ, tresc });
    setTimeout(() => setKomunikat(null), 4000);
  }

  function zapiszPrzypisanie() {
    if (!jednostka || !stanowisko) return;
    przypisz.mutate(
      {
        employeeId: employee.id,
        organizationUnitId: jednostka,
        positionId: stanowisko,
        isPrimary: true,
        startDate: new Date().toISOString().slice(0, 10),
      },
      {
        onSuccess: () => { pokaz('ok', 'Przypisanie zapisane.'); onZmiana(); },
        onError: (e: Error) => pokaz('blad', e.message || 'Nie udało się zapisać przypisania.'),
      },
    );
  }

  function zapiszPrzelozonego() {
    if (!przelozony) return;
    ustawPrzelozonego.mutate(
      { employeeId: employee.id, supervisorEmployeeId: przelozony },
      {
        onSuccess: () => { pokaz('ok', 'Przełożony ustawiony.'); onZmiana(); },
        onError: (e: Error) => pokaz('blad', e.message || 'Nie udało się ustawić przełożonego.'),
      },
    );
  }

  const zmienionePrzypisanie =
    jednostka !== (przypisanie?.organizationUnitId ?? '') || stanowisko !== (przypisanie?.positionId ?? '');

  return (
    <div
      style={{
        padding: 20,
        background: colors.white,
        border: `1px solid ${colors.gray[200]}`,
        borderRadius: 12,
      }}
    >
      <h3 style={{ margin: '0 0 4px', fontSize: 15, fontWeight: 700, color: colors.gray[900], display: 'flex', alignItems: 'center', gap: 8 }}>
        <Building2 size={16} /> Miejsce w organizacji
      </h3>
      <p style={{ margin: '0 0 16px', fontSize: 13, color: colors.gray[500] }}>
        Jednostka i stanowisko decydują o widoczności danych, a przełożony o tym, kto zatwierdza wnioski.
      </p>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 12, marginBottom: 12 }}>
        <div>
          <label style={polePodpis} htmlFor="wybor-jednostki">Jednostka</label>
          <select id="wybor-jednostki" style={poleKontrolka} value={jednostka} onChange={(e) => setJednostka(e.target.value)}>
            <option value="">— wybierz —</option>
            {jednostki.map((j) => (
              <option key={j.id} value={j.id}>{j.etykieta}</option>
            ))}
          </select>
        </div>
        <div>
          <label style={polePodpis} htmlFor="wybor-stanowiska">Stanowisko</label>
          <select id="wybor-stanowiska" style={poleKontrolka} value={stanowisko} onChange={(e) => setStanowisko(e.target.value)}>
            <option value="">— wybierz —</option>
            {aktywneStanowiska.map((p) => (
              <option key={p.id} value={p.id}>{p.name}{p.isManagerial ? ' (kierownicze)' : ''}</option>
            ))}
          </select>
        </div>
      </div>

      <button
        type="button"
        style={{ ...przyciskGlowny, opacity: !jednostka || !stanowisko || !zmienionePrzypisanie ? 0.5 : 1 }}
        disabled={!jednostka || !stanowisko || !zmienionePrzypisanie || przypisz.isPending}
        onClick={zapiszPrzypisanie}
      >
        {przypisz.isPending ? 'Zapisywanie…' : 'Zapisz przypisanie'}
      </button>

      <hr style={{ margin: '20px 0', border: 0, borderTop: `1px solid ${colors.gray[100]}` }} />

      <h3 style={{ margin: '0 0 12px', fontSize: 15, fontWeight: 700, color: colors.gray[900], display: 'flex', alignItems: 'center', gap: 8 }}>
        <UserCog size={16} /> Przełożony
      </h3>
      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'flex-end' }}>
        <div style={{ flex: '1 1 220px', minWidth: 0 }}>
          <label style={polePodpis} htmlFor="wybor-przelozonego">Akceptuje wnioski tego pracownika</label>
          <select id="wybor-przelozonego" style={poleKontrolka} value={przelozony} onChange={(e) => setPrzelozony(e.target.value)}>
            <option value="">— wybierz —</option>
            {kandydaci.map((k) => (
              <option key={k.id} value={k.id}>{k.firstName} {k.lastName}</option>
            ))}
          </select>
        </div>
        <button
          type="button"
          style={{ ...przyciskGlowny, opacity: !przelozony || przelozony === (employee.supervisor?.employeeId ?? '') ? 0.5 : 1 }}
          disabled={!przelozony || przelozony === (employee.supervisor?.employeeId ?? '') || ustawPrzelozonego.isPending}
          onClick={zapiszPrzelozonego}
        >
          {ustawPrzelozonego.isPending ? 'Zapisywanie…' : 'Zapisz przełożonego'}
        </button>
      </div>

      {komunikat && (
        <div
          role="status"
          style={{
            marginTop: 14,
            padding: '8px 12px',
            borderRadius: 8,
            fontSize: 13,
            background: komunikat.typ === 'ok' ? colors.success[100] : colors.danger[50],
            color: komunikat.typ === 'ok' ? colors.success[800] : colors.danger[600],
          }}
        >
          {komunikat.tresc}
        </div>
      )}
    </div>
  );
}
