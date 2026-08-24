import { describe, it, expect } from 'vitest';
import {
  NAGLOWKI_ROZLICZENIA,
  wierszDoArkusza,
  wierszSumy,
  type WierszRozliczenia,
} from './rozliczenieDoArkusza';

function wiersz(nadpisz: Partial<WierszRozliczenia> = {}): WierszRozliczenia {
  return {
    name: 'Jan Kowalski',
    email: 'jan@firma.pl',
    rate: 50,
    hasRate: true,
    normaH: 160,
    workedH: 168,
    regularH: 160,
    overtimeH: 8,
    vacationDays: 2,
    absenceDays: 1,
    basicPay: 8000,
    overtimePay: 600,
    totalPay: 8600,
    ...nadpisz,
  };
}

describe('rozliczenie do arkusza', () => {
  it('wiersz ma tyle kolumn co naglowek', () => {
    expect(wierszDoArkusza(wiersz())).toHaveLength(NAGLOWKI_ROZLICZENIA.length);
    expect(wierszSumy([wiersz()])).toHaveLength(NAGLOWKI_ROZLICZENIA.length);
  });

  it('godziny i kwoty ida jako liczby, nie napisy — inaczej nie da sie na nich liczyc', () => {
    const komorki = wierszDoArkusza(wiersz());

    for (const indeks of [2, 3, 4, 5, 6, 7, 8, 9, 10, 11]) {
      expect(typeof komorki[indeks]).toBe('number');
    }
  });

  it('brak stawki daje puste kwoty, a nie zera', () => {
    const komorki = wierszDoArkusza(wiersz({ hasRate: false, rate: 0, basicPay: 0, overtimePay: 0, totalPay: 0 }));

    // stawka, zasadnicze, za nadgodziny, razem
    expect(komorki[2]).toBeNull();
    expect(komorki[9]).toBeNull();
    expect(komorki[10]).toBeNull();
    expect(komorki[11]).toBeNull();
    // godziny licza sie nadal — pracownik pracowal, tylko stawki nikt nie ustawil
    expect(komorki[4]).toBe(168);
  });

  it('suma liczy godziny wszystkich, a kwoty tylko tych ze stawka', () => {
    const zeStawka = wiersz({ normaH: 160, workedH: 168, totalPay: 8600, basicPay: 8000, overtimePay: 600 });
    const bezStawki = wiersz({
      hasRate: false, rate: 0, normaH: 100, workedH: 90,
      basicPay: 0, overtimePay: 0, totalPay: 0,
    });

    const suma = wierszSumy([zeStawka, bezStawki]);

    expect(suma[0]).toBe('RAZEM');
    expect(suma[3]).toBe(260); // norma: 160 + 100
    expect(suma[4]).toBe(258); // czas pracy: 168 + 90
    expect(suma[11]).toBe(8600); // razem: tylko osoba ze stawka
  });

  it('pusta lista daje same zera, nie NaN', () => {
    const suma = wierszSumy([]);

    expect(suma[3]).toBe(0);
    expect(suma[11]).toBe(0);
  });
});
