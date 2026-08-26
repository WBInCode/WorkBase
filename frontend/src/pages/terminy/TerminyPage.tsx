import { useState } from 'react';
import { Link } from 'react-router-dom';
import { CalendarClock, AlertTriangle, Clock, CheckCircle2 } from 'lucide-react';
import { useWygasajaceTerminy, type WygasajacyTermin } from '@/api/hooks/useTerminy';
import { colors } from '@/theme/tokens';

/**
 * „Co wygasa" — badania lekarskie, szkolenia BHP, uprawnienia i końce umów.
 *
 * Ekran wyłącznie informuje. Miniony termin nie odbiera pracownikowi rejestracji czasu ani
 * składania wniosków — dopuszczenie do pracy jest decyzją pracodawcy, nie systemu. Ta zasada
 * jest tu napisana wprost, żeby nikt nie szukał blokady, której celowo nie ma.
 *
 * Lista niesie nazwiska, więc serwer zawęża ją do zakresu danych pytającego (`org.view-team`).
 */
export function TerminyPage() {
  const [dni, setDni] = useState(30);
  const { data: terminy = [], isLoading } = useWygasajaceTerminy(dni);

  const minione = terminy.filter((t) => t.stan === 'Minal');
  const zblizajace = terminy.filter((t) => t.stan === 'Zbliza');

  return (
    <div style={{ padding: '24px 28px', maxWidth: 900, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6, flexWrap: 'wrap' }}>
        <CalendarClock size={20} style={{ color: colors.primary[600] }} />
        <h1 style={{ fontSize: 22, fontWeight: 800, margin: 0, color: colors.gray[900] }}>Terminy</h1>

        <select
          value={dni}
          onChange={(e) => setDni(Number(e.target.value))}
          style={{
            marginLeft: 'auto',
            padding: '6px 10px',
            borderRadius: 8,
            border: '1px solid var(--wb-line, #e3e7f1)',
            fontSize: 13,
          }}
        >
          <option value={30}>Najbliższe 30 dni</option>
          <option value={60}>Najbliższe 60 dni</option>
          <option value={90}>Najbliższe 90 dni</option>
        </select>
      </div>

      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 20px', fontSize: 14, maxWidth: '72ch' }}>
        Badania lekarskie, szkolenia BHP, uprawnienia z datą ważności i końce umów. System
        pokazuje, co wygasa — <strong>niczego nie blokuje</strong>. Osoba z nieaktualnym badaniem
        normalnie zarejestruje czas pracy; decyzja o dopuszczeniu do pracy należy do firmy.
      </p>

      {isLoading && <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Wczytywanie…</p>}

      {!isLoading && terminy.length === 0 && (
        <div
          style={{
            display: 'flex',
            gap: 12,
            alignItems: 'flex-start',
            background: 'var(--wb-emr-100, #d1fae5)',
            borderRadius: 12,
            padding: '16px 18px',
          }}
        >
          <CheckCircle2 size={19} style={{ color: 'var(--wb-emr-800, #065f46)', flexShrink: 0, marginTop: 1 }} />
          <div>
            <strong style={{ fontSize: 14.5, color: colors.gray[900] }}>Nic nie wygasa</strong>
            <p style={{ margin: '4px 0 0', fontSize: 13.5, color: 'var(--wb-ink-2, #6b7490)' }}>
              W wybranym okresie żaden termin nie mija. Jeśli spodziewasz się tu czegoś, sprawdź,
              czy terminy są w ogóle wprowadzone na kartach pracowników.
            </p>
          </div>
        </div>
      )}

      {minione.length > 0 && (
        <Sekcja
          tytul="Termin już minął"
          opis="Te pozycje wymagają decyzji — odnowienia albo świadomego zostawienia."
          pozycje={minione}
          kolor={colors.danger[600]}
          Ikona={AlertTriangle}
        />
      )}

      {zblizajace.length > 0 && (
        <Sekcja
          tytul="Termin się zbliża"
          opis="Jest jeszcze czas, żeby umówić badanie albo szkolenie."
          pozycje={zblizajace}
          kolor={colors.warning[800]}
          Ikona={Clock}
        />
      )}
    </div>
  );
}

function Sekcja({
  tytul,
  opis,
  pozycje,
  kolor,
  Ikona,
}: {
  tytul: string;
  opis: string;
  pozycje: WygasajacyTermin[];
  kolor: string;
  Ikona: typeof AlertTriangle;
}) {
  return (
    <section style={{ marginBottom: 26 }}>
      <h2 style={{ fontSize: 15, fontWeight: 700, margin: '0 0 2px', color: colors.gray[900] }}>
        {tytul} <span style={{ color: 'var(--wb-ink-2, #6b7490)', fontWeight: 500 }}>({pozycje.length})</span>
      </h2>
      <p style={{ margin: '0 0 12px', fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>{opis}</p>

      <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 10 }}>
        {pozycje.map((pozycja) => (
          <li
            key={pozycja.id}
            style={{
              background: 'var(--wb-panel, #fff)',
              border: '1px solid var(--wb-line, #e3e7f1)',
              borderRadius: 12,
              padding: '13px 15px',
              display: 'flex',
              gap: 12,
              alignItems: 'flex-start',
            }}
          >
            <Ikona size={17} style={{ color: kolor, flexShrink: 0, marginTop: 2 }} />
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                <strong style={{ fontSize: 14, color: colors.gray[900] }}>{pozycja.imieNazwisko}</strong>
                <span style={{ fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>{pozycja.typNazwa}</span>
              </div>
              <p style={{ margin: '4px 0 0', fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
                {new Date(pozycja.waznyDo).toLocaleDateString('pl-PL')} — {opisDni(pozycja.dniDoTerminu)}
              </p>
            </div>
            <Link
              to={`/org/employees/${pozycja.employeeId}`}
              style={{
                padding: '6px 12px',
                borderRadius: 8,
                border: '1px solid var(--wb-line, #e3e7f1)',
                color: colors.primary[600],
                fontSize: 12.5,
                fontWeight: 600,
                textDecoration: 'none',
                whiteSpace: 'nowrap',
              }}
            >
              Karta
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}

function opisDni(dni: number): string {
  if (dni < 0) {
    const minelo = Math.abs(dni);
    return minelo === 1 ? 'minął wczoraj' : `minął ${minelo} dni temu`;
  }
  if (dni === 0) return 'mija dzisiaj';
  return dni === 1 ? 'zostaje 1 dzień' : `zostaje ${dni} dni`;
}
