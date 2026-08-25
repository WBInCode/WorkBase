import type { PoleWniosku } from '@/api/hooks/useWnioski';
import { colors } from '@/theme/tokens';

/**
 * Formularz generowany z definicji pol ustalonej przez administratora.
 *
 * Wartosci trzymane sa jako tekst niezaleznie od typu pola — tak samo jak w bazie. Zamiana
 * na liczbe czy date nalezy do serwera, ktory i tak musi to sprawdzic; robienie tego dwa razy
 * roznymi sposobami skonczyloby sie rozjazdem miedzy tym, co widzi uzytkownik, a tym, co
 * zapisze aplikacja.
 */
export function FormularzWniosku({
  pola,
  wartosci,
  onZmiana,
}: {
  pola: PoleWniosku[];
  wartosci: Record<string, string | null>;
  onZmiana: (kod: string, wartosc: string) => void;
}) {
  const styl = {
    display: 'block',
    width: '100%',
    marginTop: 4,
    padding: '7px 9px',
    borderRadius: 8,
    border: '1px solid var(--wb-line, #e3e7f1)',
    fontFamily: 'inherit',
    fontSize: 14,
  } as const;

  return (
    <div style={{ display: 'grid', gap: 12 }}>
      {pola.map((pole) => {
        const wartosc = wartosci[pole.kod] ?? '';
        const etykieta = (
          <>
            {pole.etykieta}
            {pole.wymagane && <span style={{ color: colors.danger[600] }}> *</span>}
          </>
        );

        return (
          <label key={pole.kod} style={{ fontSize: 13, color: colors.gray[900] }}>
            {pole.typ === 'TakNie' ? (
              <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                <input
                  type="checkbox"
                  checked={wartosc === 'true'}
                  onChange={(e) => onZmiana(pole.kod, e.target.checked ? 'true' : 'false')}
                />
                {etykieta}
              </span>
            ) : (
              <>
                {etykieta}
                {pole.typ === 'Wielolinijkowy' && (
                  <textarea
                    rows={3}
                    value={wartosc}
                    onChange={(e) => onZmiana(pole.kod, e.target.value)}
                    placeholder={pole.podpowiedz ?? undefined}
                    style={{ ...styl, resize: 'vertical' }}
                  />
                )}
                {pole.typ === 'Wybor' && (
                  <select
                    value={wartosc}
                    onChange={(e) => onZmiana(pole.kod, e.target.value)}
                    style={styl}
                  >
                    <option value="">— wybierz —</option>
                    {(pole.opcje ?? []).map((opcja) => (
                      <option key={opcja} value={opcja}>{opcja}</option>
                    ))}
                  </select>
                )}
                {(pole.typ === 'Tekst' || pole.typ === 'Liczba' || pole.typ === 'Data') && (
                  <input
                    type={pole.typ === 'Data' ? 'date' : pole.typ === 'Liczba' ? 'number' : 'text'}
                    step={pole.typ === 'Liczba' ? 'any' : undefined}
                    value={wartosc}
                    onChange={(e) => onZmiana(pole.kod, e.target.value)}
                    placeholder={pole.podpowiedz ?? undefined}
                    style={styl}
                  />
                )}
              </>
            )}
          </label>
        );
      })}
    </div>
  );
}
