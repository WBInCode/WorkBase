/**
 * Ekrany zespołowe pobierają dane osobno dla każdego pracownika, bo API nie ma
 * jeszcze zapytań zbiorczych. Przy kilkudziesięciu osobach `Promise.all` wypuszcza
 * wszystkie żądania naraz i kładzie backend: najpierw limit żądań, a po jego
 * podniesieniu pulę połączeń Postgresa (`53300: remaining connection slots`).
 *
 * Ten pomocnik wykonuje te same zapytania, ale kilka naraz, a nie wszystkie.
 * Kolejność wyników odpowiada kolejności wejścia.
 */
const DOMYSLNA_ROWNOLEGLOSC = 6;

export async function mapujZOgraniczeniem<TWejscie, TWynik>(
  elementy: readonly TWejscie[],
  zadanie: (element: TWejscie, indeks: number) => Promise<TWynik>,
  rownolegle: number = DOMYSLNA_ROWNOLEGLOSC,
): Promise<TWynik[]> {
  if (elementy.length === 0) return [];

  const wyniki = new Array<TWynik>(elementy.length);
  let nastepny = 0;

  async function pracownik(): Promise<void> {
    while (nastepny < elementy.length) {
      const indeks = nastepny++;
      wyniki[indeks] = await zadanie(elementy[indeks]!, indeks);
    }
  }

  const ilu = Math.max(1, Math.min(rownolegle, elementy.length));
  await Promise.all(Array.from({ length: ilu }, () => pracownik()));
  return wyniki;
}
