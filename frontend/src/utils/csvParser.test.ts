import { describe, it, expect } from 'vitest';
import { parseCsv, parsujDateZatrudnienia, odczytajCsv } from './csvParser';

describe('parseCsv — pliki z prawdziwych programow kadrowych', () => {
  it('czyta srednik jako separator i konce linii z Windows', () => {
    const plik = 'Imię;Nazwisko;Email\r\nAnna;Kowalska;anna@firma.pl\r\nJan;Nowak;jan@firma.pl\r\n';

    const wynik = parseCsv(plik);

    expect(wynik.headers).toEqual(['Imię', 'Nazwisko', 'Email']);
    expect(wynik.rows).toHaveLength(2);
    expect(wynik.rows[1]).toEqual(['Jan', 'Nowak', 'jan@firma.pl']);
  });

  it('nie rozcina pola w cudzyslowie, nawet gdy jest w nim separator', () => {
    const plik = 'Nazwisko;Stanowisko\n"Kowalska-Nowak";"Kierownik, dzial handlowy"';

    const wynik = parseCsv(plik);

    expect(wynik.rows[0]).toEqual(['Kowalska-Nowak', 'Kierownik, dzial handlowy']);
  });

  it('rozumie podwojony cudzyslow w srodku pola', () => {
    const wynik = parseCsv('Nazwa\n"Firma ""ABC"" sp. z o.o."');

    expect(wynik.rows[0]?.[0]).toBe('Firma "ABC" sp. z o.o.');
  });

  it('pomija puste wiersze na koncu pliku', () => {
    const wynik = parseCsv('Imię;Nazwisko\nAnna;Kowalska\n\n\n');

    expect(wynik.rows).toHaveLength(1);
  });

  it('znacznik BOM z Excela nie psuje pierwszego naglowka', () => {
    // Bez tego pierwsza kolumna nazywa sie "<BOM>Imię" i automatyczne mapowanie jej nie znajduje.
    const wynik = parseCsv('﻿' + 'Imię;Nazwisko\nAnna;Kowalska');

    expect(wynik.headers[0]).toBe('Imię');
  });
});

describe('odczytajCsv — kodowanie', () => {
  it('czyta plik zapisany w Windows-1250 bez krzakow', async () => {
    // "Łukasz Żółw" w Windows-1250: Ł=0xA3, ó=0xF3, ł=0xB3, Ż=0xAF, ó=0xF3, ł=0xB3, w
    const bajty = new Uint8Array([
      0xA3, 0x75, 0x6B, 0x61, 0x73, 0x7A, 0x20, // "Łukasz "
      0xAF, 0xF3, 0xB3, 0x77,                   // "Żółw"
    ]);

    const tekst = await odczytajCsv(new Blob([bajty]));

    expect(tekst).toBe('Łukasz Żółw');
  });

  it('poprawny UTF-8 czyta jako UTF-8', async () => {
    const tekst = await odczytajCsv(new Blob([new TextEncoder().encode('Łukasz Żółw')]));

    expect(tekst).toBe('Łukasz Żółw');
  });
});

describe('parsujDateZatrudnienia', () => {
  it('czyta ISO', () => {
    expect(parsujDateZatrudnienia('2015-03-15')?.toISOString().slice(0, 10)).toBe('2015-03-15');
  });

  it('czyta polski zapis z kropka, myslnikiem i ukosnikiem', () => {
    for (const zapis of ['15.03.2015', '15-03-2015', '15/03/2015']) {
      expect(parsujDateZatrudnienia(zapis)?.toISOString().slice(0, 10)).toBe('2015-03-15');
    }
  });

  it('dwuznaczna date czyta po polsku, nie po amerykansku', () => {
    // Wczesniej new Date('05/03/2015') dawalo 3 MAJA — bez zadnego bledu.
    expect(parsujDateZatrudnienia('05/03/2015')?.toISOString().slice(0, 10)).toBe('2015-03-05');
  });

  it('jednocyfrowy dzien i miesiac tez przechodzi', () => {
    expect(parsujDateZatrudnienia('5.3.2015')?.toISOString().slice(0, 10)).toBe('2015-03-05');
  });

  it('nie przewija nieistniejacej daty na nastepny miesiac', () => {
    // Date sam zamienilby 31.02 na 3 marca i nikt by sie nie zorientowal.
    expect(parsujDateZatrudnienia('31.02.2015')).toBeNull();
  });

  it('odrzuca smieci zamiast zmyslac date', () => {
    for (const zapis of ['', '  ', 'brak', '2015', '15.03', '32.01.2015', '15.13.2015']) {
      expect(parsujDateZatrudnienia(zapis)).toBeNull();
    }
  });

  it('data nie cofa sie o dzien przez strefe czasowa', () => {
    // Budowanie o polnocy potrafi dac 14.03 w strefie ujemnej.
    expect(parsujDateZatrudnienia('15.03.2015')?.getUTCDate()).toBe(15);
  });
});
