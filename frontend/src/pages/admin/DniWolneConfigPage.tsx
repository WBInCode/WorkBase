import { useState } from 'react';
import { CalendarOff, Plus, Trash2, Sparkles } from 'lucide-react';
import {
  useDniWolne,
  useDodajDzienWolny,
  useUsunDzienWolny,
  useWstawZestawPolski,
  type DzienWolny,
} from '@/api/hooks/useDniWolne';
import { colors } from '@/theme/tokens';

const DNI_TYGODNIA = ['niedziela', 'poniedziałek', 'wtorek', 'środa', 'czwartek', 'piątek', 'sobota'];

/** Południe UTC, bo o północy strefa potrafi cofnąć datę o dzień i pokazać zły dzień tygodnia. */
function dzienTygodnia(data: string): string {
  const d = new Date(`${data}T12:00:00Z`);
  return DNI_TYGODNIA[d.getUTCDay()] ?? '';
}

/**
 * Kalendarz dni wolnych firmy.
 *
 * Dzien wolny robi dwie rzeczy: obniza norme czasu pracy w rozliczeniu i pozwala naliczyc
 * dodatek swiateczny. System nie zna z gory zadnych dat — wpisuje je firma, a podpowiedz
 * typowych polskich swiat jest osobnym przyciskiem, nie zachowaniem domyslnym.
 */
export function DniWolneConfigPage() {
  const biezacyRok = new Date().getFullYear();
  const [rok, setRok] = useState(biezacyRok);

  const { data: dni = [], isLoading } = useDniWolne(rok);
  const dodaj = useDodajDzienWolny(rok);
  const usun = useUsunDzienWolny(rok);
  const zestaw = useWstawZestawPolski(rok);

  const [data, setData] = useState('');
  const [nazwa, setNazwa] = useState('');
  const [obnizaNorme, setObnizaNorme] = useState(true);
  const [firmowy, setFirmowy] = useState(false);
  const [blad, setBlad] = useState<string | null>(null);
  const [komunikat, setKomunikat] = useState<string | null>(null);

  const zapisz = async () => {
    setBlad(null);
    setKomunikat(null);
    try {
      await dodaj.mutateAsync({
        data,
        nazwa: nazwa.trim(),
        rodzaj: firmowy ? 'Firmowy' : 'Swieto',
        obnizaNorme,
      });
      setData('');
      setNazwa('');
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się dodać dnia wolnego.');
    }
  };

  const wstawZestaw = async () => {
    setBlad(null);
    try {
      const wynik = await zestaw.mutateAsync();
      setKomunikat(
        wynik.dodane === 0
          ? 'Wszystkie proponowane dni były już wpisane — nic nie zmieniono.'
          : `Dodano ${wynik.dodane} dni. Istniejące wpisy zostały nietknięte.`,
      );
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się wstawić zestawu.');
    }
  };

  const moznaZapisac = data !== '' && nazwa.trim() !== '' && !dodaj.isPending;

  const pole = {
    display: 'block',
    width: '100%',
    marginTop: 4,
    padding: '7px 9px',
    borderRadius: 8,
    border: '1px solid var(--wb-line, #e3e7f1)',
  } as const;

  return (
    <div style={{ padding: '24px 28px', maxWidth: 900, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
        <CalendarOff size={20} style={{ color: colors.primary[600] }} />
        <h1 style={{ fontSize: 22, fontWeight: 800, margin: 0, color: colors.gray[900] }}>Dni wolne</h1>
      </div>
      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 20px', fontSize: 14, maxWidth: '70ch' }}>
        Dzień wolny obniża normę czasu pracy w rozliczeniu i pozwala naliczyć dodatek świąteczny za
        pracę w tym dniu. Kalendarz należy do firmy — możesz go dowolnie zmieniać.
      </p>

      <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap', marginBottom: 16 }}>
        <label style={{ fontSize: 13, color: colors.gray[900] }}>
          Rok
          <select
            value={rok}
            onChange={(e) => setRok(Number(e.target.value))}
            style={{ marginLeft: 8, padding: '6px 9px', borderRadius: 8, border: '1px solid var(--wb-line, #e3e7f1)' }}
          >
            {[biezacyRok - 1, biezacyRok, biezacyRok + 1, biezacyRok + 2].map((r) => (
              <option key={r} value={r}>{r}</option>
            ))}
          </select>
        </label>

        <button
          onClick={wstawZestaw}
          disabled={zestaw.isPending}
          title="Wstawia typowe dni ustawowo wolne w Polsce. Wpisy, które już masz, zostaną nietknięte."
          style={{
            marginLeft: 'auto',
            display: 'inline-flex',
            alignItems: 'center',
            gap: 6,
            padding: '8px 14px',
            borderRadius: 10,
            border: '1px solid var(--wb-line, #e3e7f1)',
            background: 'var(--wb-panel, #fff)',
            color: colors.gray[900],
            fontSize: 13,
            cursor: zestaw.isPending ? 'wait' : 'pointer',
          }}
        >
          <Sparkles size={15} />
          Wstaw typowe dni wolne w Polsce
        </button>
      </div>

      {komunikat && (
        <p style={{ margin: '0 0 14px', fontSize: 13, color: 'var(--wb-emr-800, #065f46)' }}>{komunikat}</p>
      )}
      {blad && <p style={{ margin: '0 0 14px', fontSize: 13, color: colors.danger[600] }}>{blad}</p>}

      <div
        style={{
          background: 'var(--wb-panel, #fff)',
          border: '1px solid var(--wb-line, #e3e7f1)',
          borderRadius: 14,
          padding: '16px 18px',
          marginBottom: 18,
          display: 'grid',
          gap: 10,
        }}
      >
        <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
          <label style={{ fontSize: 13, color: colors.gray[900], flex: '1 1 150px' }}>
            Data
            <input type="date" value={data} onChange={(e) => setData(e.target.value)} style={pole} />
          </label>
          <label style={{ fontSize: 13, color: colors.gray[900], flex: '2 1 240px' }}>
            Nazwa
            <input
              type="text"
              value={nazwa}
              maxLength={128}
              onChange={(e) => setNazwa(e.target.value)}
              placeholder="np. Dzień wolny za święto w sobotę"
              style={pole}
            />
          </label>
        </div>

        <div style={{ display: 'flex', gap: 18, flexWrap: 'wrap', fontSize: 13, color: colors.gray[900] }}>
          <label style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            <input type="checkbox" checked={obnizaNorme} onChange={(e) => setObnizaNorme(e.target.checked)} />
            Obniża normę czasu pracy
          </label>
          <label style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            <input type="checkbox" checked={firmowy} onChange={(e) => setFirmowy(e.target.checked)} />
            Dzień firmowy, nie ustawowy
          </label>
        </div>

        <button
          onClick={zapisz}
          disabled={!moznaZapisac}
          style={{
            justifySelf: 'start',
            display: 'inline-flex',
            alignItems: 'center',
            gap: 6,
            padding: '8px 16px',
            borderRadius: 8,
            border: 'none',
            background: moznaZapisac ? colors.primary[600] : colors.gray[300],
            color: colors.textOnAccent,
            fontSize: 13,
            fontWeight: 600,
            cursor: moznaZapisac ? 'pointer' : 'not-allowed',
          }}
        >
          <Plus size={15} />
          Dodaj dzień wolny
        </button>
      </div>

      {isLoading ? (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Wczytywanie…</p>
      ) : dni.length === 0 ? (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14, maxWidth: '70ch' }}>
          Brak dni wolnych w {rok}. Dopóki ich nie wpiszesz, norma czasu pracy nie uwzględnia świąt,
          a dodatek świąteczny nie ma się do czego przyłożyć.
        </p>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 8 }}>
          {dni.map((d: DzienWolny) => (
            <li
              key={d.id}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 12,
                flexWrap: 'wrap',
                padding: '10px 14px',
                borderRadius: 10,
                background: 'var(--wb-panel, #fff)',
                border: '1px solid var(--wb-line, #e3e7f1)',
                fontSize: 14,
              }}
            >
              <strong style={{ color: colors.gray[900], minWidth: 96, fontVariantNumeric: 'tabular-nums' }}>
                {d.data}
              </strong>
              <span style={{ color: 'var(--wb-ink-2, #6b7490)', minWidth: 90, fontSize: 12 }}>
                {dzienTygodnia(d.data)}
              </span>
              <span style={{ color: colors.gray[900] }}>{d.nazwa}</span>
              {d.rodzaj === 'Firmowy' && (
                <span
                  style={{
                    fontSize: 11,
                    padding: '2px 8px',
                    borderRadius: 999,
                    background: 'var(--wb-g-100, #f1f5f9)',
                    color: 'var(--wb-ink-2, #6b7490)',
                  }}
                >
                  firmowy
                </span>
              )}
              {!d.obnizaNorme && (
                <span
                  style={{
                    fontSize: 11,
                    padding: '2px 8px',
                    borderRadius: 999,
                    background: 'var(--wb-org-100, #ffedd5)',
                    color: 'var(--wb-org-600, #ea580c)',
                  }}
                >
                  nie obniża normy
                </span>
              )}
              <button
                onClick={() => usun.mutate(d.id)}
                aria-label={`Usuń dzień wolny: ${d.nazwa}`}
                title="Usuń"
                style={{
                  marginLeft: 'auto',
                  border: 'none',
                  background: 'transparent',
                  cursor: 'pointer',
                  color: 'var(--wb-ink-2, #6b7490)',
                  display: 'flex',
                  padding: 4,
                }}
              >
                <Trash2 size={15} />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
