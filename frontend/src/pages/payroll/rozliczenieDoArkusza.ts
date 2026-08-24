/**
 * Zamiana wierszy rozliczenia na zawartosc arkusza. Wydzielone z PayrollPage, zeby dalo sie
 * sprawdzic same LICZBY bez uruchamiania ExcelJS-a — to one ida do listy plac, a formatowanie
 * komorek jest tylko oprawa.
 */

export interface WierszRozliczenia {
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

export const NAGLOWKI_ROZLICZENIA = [
  'Pracownik', 'E-mail', 'Stawka [PLN/h]', 'Norma [h]', 'Czas pracy [h]',
  'Godziny zwykłe [h]', 'Nadgodziny [h]', 'Nocne [h]', 'Świąteczne [h]',
  'Urlop [dni]', 'Nieobecności [dni]',
  'Zasadnicze [PLN]', 'Za nadgodziny [PLN]', 'Dodatek nocny [PLN]',
  'Dodatek świąteczny [PLN]', 'Razem [PLN]',
] as const;

export type KomorkaArkusza = string | number | null;

/**
 * Pracownik bez ustawionej stawki dostaje w kolumnach kwotowych `null`, a nie zero.
 * Zero znaczy „policzone i wyszlo 0 zl", a brak stawki znaczy „nie ma czego liczyc" —
 * w liscie plac to roznica miedzy pozycja do wyplaty a pozycja do uzupelnienia.
 */
export function wierszDoArkusza(wiersz: WierszRozliczenia): KomorkaArkusza[] {
  return [
    wiersz.name,
    wiersz.email,
    wiersz.hasRate ? wiersz.rate : null,
    wiersz.normaH,
    wiersz.workedH,
    wiersz.regularH,
    wiersz.overtimeH,
    wiersz.nightH,
    wiersz.holidayH,
    wiersz.vacationDays,
    wiersz.absenceDays,
    wiersz.hasRate ? wiersz.basicPay : null,
    wiersz.hasRate ? wiersz.overtimePay : null,
    wiersz.hasRate ? wiersz.nightPay : null,
    wiersz.hasRate ? wiersz.holidayPay : null,
    wiersz.hasRate ? wiersz.totalPay : null,
  ];
}

/**
 * Wiersz „RAZEM". Sumujemy po tych samych wierszach, ktore trafiaja do arkusza — dzieki temu
 * suma nie moze sie rozjechac z tym, co widac wyzej (np. gdyby ktos zmienil filtrowanie).
 * Kwoty osob bez stawki nie wchodza do sumy, bo nie sa policzone.
 */
export function wierszSumy(wiersze: readonly WierszRozliczenia[]): KomorkaArkusza[] {
  const suma = (wybierz: (w: WierszRozliczenia) => number) =>
    wiersze.reduce((acc, w) => acc + wybierz(w), 0);
  const sumaKwot = (wybierz: (w: WierszRozliczenia) => number) =>
    wiersze.reduce((acc, w) => acc + (w.hasRate ? wybierz(w) : 0), 0);

  return [
    'RAZEM',
    '',
    null,
    suma((w) => w.normaH),
    suma((w) => w.workedH),
    suma((w) => w.regularH),
    suma((w) => w.overtimeH),
    suma((w) => w.nightH),
    suma((w) => w.holidayH),
    suma((w) => w.vacationDays),
    suma((w) => w.absenceDays),
    sumaKwot((w) => w.basicPay),
    sumaKwot((w) => w.overtimePay),
    sumaKwot((w) => w.nightPay),
    sumaKwot((w) => w.holidayPay),
    sumaKwot((w) => w.totalPay),
  ];
}
