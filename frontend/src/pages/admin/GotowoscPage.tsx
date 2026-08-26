import { Link } from 'react-router-dom';
import { CheckCircle2, AlertTriangle, Info, ArrowRight } from 'lucide-react';
import { useGotowosc, type PozycjaGotowosci } from '@/api/hooks/useGotowosc';
import { colors } from '@/theme/tokens';

/**
 * „Co jeszcze nie zadziała” — lista braków konfiguracji wyliczana z danych firmy.
 *
 * Kreator pierwszego startu zadaje trzy pytania i celowo nie pyta o resztę, więc firma po nim
 * działa, ale część funkcji jeszcze nie. Bez tego ekranu właściciel dowiaduje się o tym dopiero
 * wtedy, gdy coś nie zadziała w trakcie pracy.
 *
 * Każda pozycja mówi, CO NIE ZADZIAŁA, a nie czego brakuje: „brak stanowisk kierowniczych” nic
 * nie znaczy dla nietechnicznego właściciela, „nikt nie zobaczy danych swojego działu” znaczy.
 * Niczego nie wymuszamy — firma ma prawo świadomie zostawić każdą z tych rzeczy nieustawioną.
 */
export function GotowoscPage() {
  const { data, isLoading } = useGotowosc();

  const blokujace = data?.pozycje.filter((p) => p.waga === 'blokuje') ?? [];
  const warte = data?.pozycje.filter((p) => p.waga === 'warto') ?? [];

  return (
    <div style={{ padding: '24px 28px', maxWidth: 860, margin: '0 auto' }}>
      <h1 style={{ fontSize: 22, fontWeight: 800, margin: '0 0 6px', color: colors.gray[900] }}>
        Gotowość konfiguracji
      </h1>
      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 22px', fontSize: 14, maxWidth: '70ch' }}>
        Lista wyliczana na bieżąco z danych firmy. Mówi, co jeszcze nie zadziała i gdzie to ustawić.
        Nic tu nie jest wymagane — jeśli któraś funkcja nie jest Wam potrzebna, zostaw ją tak, jak jest.
      </p>

      {isLoading && (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Wczytywanie…</p>
      )}

      {data && data.pozycje.length === 0 && (
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
            <strong style={{ fontSize: 14.5, color: colors.gray[900] }}>Wszystko skonfigurowane</strong>
            <p style={{ margin: '4px 0 0', fontSize: 13.5, color: 'var(--wb-ink-2, #6b7490)' }}>
              Nie znaleźliśmy niczego, co blokowałoby którąkolwiek funkcję.
            </p>
          </div>
        </div>
      )}

      {blokujace.length > 0 && (
        <Sekcja
          tytul="To nie zadziała, dopóki nie ustawicie"
          opis="Funkcje wymienione niżej są w tej chwili niedostępne albo utkną w połowie."
          pozycje={blokujace}
          kolorIkony={colors.danger[600]}
          Ikona={AlertTriangle}
        />
      )}

      {warte.length > 0 && (
        <Sekcja
          tytul="Zadziała, ale w okrojonej formie"
          opis="Te rzeczy nie blokują pracy — wpływają na to, ile system policzy i pokaże."
          pozycje={warte}
          kolorIkony={colors.warning[800]}
          Ikona={Info}
        />
      )}
    </div>
  );
}

function Sekcja({
  tytul,
  opis,
  pozycje,
  kolorIkony,
  Ikona,
}: {
  tytul: string;
  opis: string;
  pozycje: PozycjaGotowosci[];
  kolorIkony: string;
  Ikona: typeof AlertTriangle;
}) {
  return (
    <section style={{ marginBottom: 26 }}>
      <h2 style={{ fontSize: 15, fontWeight: 700, margin: '0 0 2px', color: colors.gray[900] }}>{tytul}</h2>
      <p style={{ margin: '0 0 12px', fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>{opis}</p>

      <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 10 }}>
        {pozycje.map((pozycja) => (
          <li
            key={pozycja.kod}
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
            <Ikona size={17} style={{ color: kolorIkony, flexShrink: 0, marginTop: 2 }} />
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                <strong style={{ fontSize: 14, color: colors.gray[900] }}>{pozycja.tytul}</strong>
                {pozycja.liczba !== null && (
                  <span
                    style={{
                      fontSize: 11.5,
                      fontWeight: 700,
                      padding: '2px 8px',
                      borderRadius: 999,
                      background: 'var(--wb-bg, #f1f4f9)',
                      color: 'var(--wb-ink-2, #6b7490)',
                    }}
                  >
                    {pozycja.liczba}
                  </span>
                )}
              </div>
              <p style={{ margin: '4px 0 0', fontSize: 13.5, color: 'var(--wb-ink-2, #6b7490)' }}>
                {pozycja.coNieZadziala}
              </p>
            </div>
            <Link
              to={pozycja.sciezka}
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 5,
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
              Ustaw
              <ArrowRight size={13} />
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
