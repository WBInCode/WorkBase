import { useState } from 'react';
import { AlarmClockCheck, Plus, X } from 'lucide-react';
import {
  useTerminyPracownika,
  useTypyTerminow,
  useZapiszTermin,
  useOdnowTermin,
  type Termin,
  type StanTerminu,
} from '@/api/hooks/useTerminy';
import { useUprawnienia } from '@/auth/useUprawnienia';
import { colors } from '@/theme/tokens';

const STAN: Record<StanTerminu, { etykieta: string; tlo: string; kolor: string }> = {
  Aktualny: { etykieta: 'aktualny', tlo: 'var(--wb-emr-100, #d1fae5)', kolor: 'var(--wb-emr-800, #065f46)' },
  Zbliza: { etykieta: 'zbliża się', tlo: colors.warning[100], kolor: colors.warning[800] },
  Minal: { etykieta: 'minął', tlo: colors.danger[50], kolor: colors.danger[600] },
};

/**
 * Terminy pracownika na jego karcie: badania, BHP, uprawnienia, koniec umowy.
 *
 * Sekcja wyłącznie informuje — miniony termin niczego nie blokuje. Odnowienie zakłada NOWY
 * termin i archiwizuje poprzedni, żeby została historia; dlatego nie ma tu edycji daty „w miejscu".
 *
 * Serwer odpowiada 403 na cudzego pracownika bez `org.view-team`, więc sekcja po prostu się
 * nie wypełni — nie chowamy jej na siłę, bo własne terminy widzi każdy.
 */
export function EmployeeTerminySection({ employeeId }: { employeeId: string }) {
  const { data: terminy = [], isLoading, isError } = useTerminyPracownika(employeeId);
  const { data: typy = [] } = useTypyTerminow();
  const { moze } = useUprawnienia();
  const zapisz = useZapiszTermin();
  const odnow = useOdnowTermin();

  const [formularzOtwarty, setFormularzOtwarty] = useState(false);
  const [typId, setTypId] = useState('');
  const [waznyDo, setWaznyDo] = useState('');
  const [odnawiany, setOdnawiany] = useState<Termin | null>(null);
  const [blad, setBlad] = useState<string | null>(null);

  const mozeEdytowac = moze('org.edit');

  const wyslij = async () => {
    setBlad(null);
    try {
      if (odnawiany) {
        await odnow.mutateAsync({ id: odnawiany.id, nowyWaznyDo: waznyDo });
      } else {
        await zapisz.mutateAsync({ employeeId, typTerminuId: typId, waznyDo });
      }
      zamknij();
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zapisać.');
    }
  };

  const zamknij = () => {
    setFormularzOtwarty(false);
    setOdnawiany(null);
    setTypId('');
    setWaznyDo('');
    setBlad(null);
  };

  const kompletne = waznyDo !== '' && (odnawiany !== null || typId !== '');

  return (
    <section
      style={{
        background: 'var(--wb-panel, #fff)',
        border: '1px solid var(--wb-line, #e3e7f1)',
        borderRadius: 14,
        padding: '16px 18px',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4, flexWrap: 'wrap' }}>
        <AlarmClockCheck size={17} style={{ color: colors.primary[600] }} />
        <h2 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: colors.gray[900] }}>Terminy</h2>

        {mozeEdytowac && typy.length > 0 && (
          <button
            onClick={() => (formularzOtwarty ? zamknij() : setFormularzOtwarty(true))}
            style={{
              marginLeft: 'auto',
              display: 'inline-flex',
              alignItems: 'center',
              gap: 5,
              padding: '5px 11px',
              borderRadius: 8,
              border: '1px solid var(--wb-line, #e3e7f1)',
              background: 'var(--wb-panel, #fff)',
              color: colors.primary[600],
              fontSize: 12.5,
              fontWeight: 600,
              cursor: 'pointer',
            }}
          >
            {formularzOtwarty ? <X size={13} /> : <Plus size={13} />}
            {formularzOtwarty ? 'Anuluj' : 'Dodaj'}
          </button>
        )}
      </div>

      <p style={{ margin: '0 0 12px', fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)' }}>
        Badania, szkolenia i uprawnienia. Miniony termin nie blokuje pracy — decyzja należy do firmy.
      </p>

      {formularzOtwarty && (
        <div style={{ display: 'grid', gap: 10, marginBottom: 14 }}>
          {odnawiany ? (
            <p style={{ margin: 0, fontSize: 13, color: colors.gray[900] }}>
              Odnawiasz: <strong>{odnawiany.typNazwa}</strong>. Poprzedni wpis trafi do historii.
            </p>
          ) : (
            <label style={{ fontSize: 13, color: colors.gray[900] }}>
              Rodzaj
              <select value={typId} onChange={(e) => setTypId(e.target.value)} style={stylPola}>
                <option value="">— wybierz —</option>
                {typy.map((t) => (
                  <option key={t.id} value={t.id}>{t.nazwa}</option>
                ))}
              </select>
            </label>
          )}

          <label style={{ fontSize: 13, color: colors.gray[900] }}>
            Ważny do
            <input type="date" value={waznyDo} onChange={(e) => setWaznyDo(e.target.value)} style={stylPola} />
          </label>

          {blad && <p style={{ margin: 0, fontSize: 12.5, color: colors.danger[600] }}>{blad}</p>}

          <button
            onClick={() => void wyslij()}
            disabled={!kompletne || zapisz.isPending || odnow.isPending}
            style={{
              justifySelf: 'start',
              padding: '7px 14px',
              borderRadius: 8,
              border: 'none',
              background: colors.primary[600],
              color: colors.textOnAccent,
              fontSize: 12.5,
              fontWeight: 600,
              cursor: kompletne ? 'pointer' : 'not-allowed',
              opacity: kompletne ? 1 : 0.55,
            }}
          >
            Zapisz
          </button>
        </div>
      )}

      {isError && (
        <p style={{ margin: 0, fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
          Nie masz dostępu do terminów tej osoby.
        </p>
      )}

      {!isError && isLoading && (
        <p style={{ margin: 0, fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>Wczytywanie…</p>
      )}

      {!isError && !isLoading && terminy.length === 0 && (
        <p style={{ margin: 0, fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
          Brak wprowadzonych terminów.
        </p>
      )}

      {terminy.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 8 }}>
          {terminy.map((termin) => {
            const stan = STAN[termin.stan];
            return (
              <li
                key={termin.id}
                style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap', fontSize: 13 }}
              >
                <span style={{ color: colors.gray[900], fontWeight: 500 }}>{termin.typNazwa}</span>
                <span style={{ color: 'var(--wb-ink-2, #6b7490)' }}>
                  {new Date(termin.waznyDo).toLocaleDateString('pl-PL')}
                </span>
                <span
                  style={{
                    fontSize: 11,
                    fontWeight: 700,
                    padding: '2px 8px',
                    borderRadius: 999,
                    background: stan.tlo,
                    color: stan.kolor,
                  }}
                >
                  {stan.etykieta}
                </span>
                {mozeEdytowac && (
                  <button
                    onClick={() => {
                      setOdnawiany(termin);
                      setWaznyDo('');
                      setFormularzOtwarty(true);
                      setBlad(null);
                    }}
                    style={{
                      marginLeft: 'auto',
                      padding: '3px 9px',
                      borderRadius: 7,
                      border: '1px solid var(--wb-line, #e3e7f1)',
                      background: 'var(--wb-panel, #fff)',
                      color: 'var(--wb-ink-2, #6b7490)',
                      fontSize: 11.5,
                      cursor: 'pointer',
                    }}
                  >
                    Odnów
                  </button>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}

const stylPola: React.CSSProperties = {
  display: 'block',
  width: '100%',
  marginTop: 4,
  padding: '7px 9px',
  borderRadius: 8,
  border: '1px solid var(--wb-line, #e3e7f1)',
  fontFamily: 'inherit',
  fontSize: 14,
};
