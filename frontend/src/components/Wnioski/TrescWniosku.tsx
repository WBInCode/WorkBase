import { useWniosekDoDecyzji } from '@/api/hooks/useWnioski';
import { colors } from '@/theme/tokens';

/**
 * Treść wniosku pokazywana akceptantowi przed decyzją.
 *
 * Kolejka akceptacji pokazywała dotąd wyłącznie pasek „zatwierdź / odrzuć" — decydowało się
 * na ślepo, bez ani jednego pola z wypełnionego formularza.
 *
 * Pola przychodzą z serwera już złączone z definicją rodzaju, więc widać także te zostawione
 * puste: akceptant ma zobaczyć formularz tak, jak wyglądał u wnioskodawcy, a brak odpowiedzi
 * bywa równie istotny co odpowiedź.
 */
export function TrescWniosku({ wniosekId }: { wniosekId: string }) {
  const { data, isLoading, isError } = useWniosekDoDecyzji(wniosekId);

  if (isLoading) {
    return <p style={stylKomunikatu}>Wczytywanie treści wniosku…</p>;
  }

  if (isError || !data) {
    return <p style={stylKomunikatu}>Nie udało się wczytać treści wniosku.</p>;
  }

  return (
    <div
      style={{
        background: 'var(--wb-bg, #f5f7fb)',
        borderRadius: 10,
        padding: '11px 13px',
        marginBottom: 10,
      }}
    >
      <div style={{ fontSize: 13, fontWeight: 700, color: colors.gray[900], marginBottom: 6 }}>
        {data.typNazwa}
      </div>

      {data.pozycje.length === 0 ? (
        <p style={{ ...stylKomunikatu, margin: 0 }}>Ten rodzaj wniosku nie ma żadnych pól.</p>
      ) : (
        <dl style={{ margin: 0, display: 'grid', gap: 5 }}>
          {data.pozycje.map((pozycja) => (
            <div key={pozycja.etykieta} style={{ display: 'flex', gap: 8, fontSize: 12.5 }}>
              <dt style={{ color: 'var(--wb-ink-2, #6b7490)', minWidth: 120 }}>{pozycja.etykieta}</dt>
              <dd style={{ margin: 0, color: colors.gray[900], fontWeight: 500 }}>
                {sformatuj(pozycja.wartosc)}
              </dd>
            </div>
          ))}
        </dl>
      )}
    </div>
  );
}

/** Pole zostawione puste pokazujemy wprost, a nie jako pustą linijkę. */
function sformatuj(wartosc: string | null): string {
  if (wartosc === null || wartosc.trim() === '') return '— nie wypełniono —';
  if (wartosc === 'true') return 'tak';
  if (wartosc === 'false') return 'nie';
  return wartosc;
}

const stylKomunikatu: React.CSSProperties = {
  fontSize: 12.5,
  color: 'var(--wb-ink-2, #6b7490)',
  marginBottom: 10,
};
