import type ExcelJS from 'exceljs';

/**
 * Wspolne elementy eksportu do Excela. Sam arkusz kazda strona buduje sama — rozni sie
 * kolumnami i ukladem — ale dobieranie biblioteki, wspolny wyglad naglowka i mechanika
 * pobierania pliku byly juz przepisane dwa razy.
 */

/** Zielen naglowka, ta sama co w raporcie czasu pracy — zeby eksporty wygladaly jak jedna rodzina. */
export const WYPELNIENIE_NAGLOWKA: ExcelJS.FillPattern = {
  type: 'pattern',
  pattern: 'solid',
  fgColor: { argb: 'FF059669' },
};

export const CZCIONKA_NAGLOWKA: Partial<ExcelJS.Font> = {
  bold: true,
  color: { argb: 'FFFFFFFF' },
  size: 11,
};

export const CIENKA_RAMKA: Partial<ExcelJS.Borders> = {
  top: { style: 'thin', color: { argb: 'FFE5E7EB' } },
  bottom: { style: 'thin', color: { argb: 'FFE5E7EB' } },
  left: { style: 'thin', color: { argb: 'FFE5E7EB' } },
  right: { style: 'thin', color: { argb: 'FFE5E7EB' } },
};

/**
 * Wczytuje ExcelJS dopiero przy eksporcie. Biblioteka wazy ~940 kB, wiec statyczny import
 * wciagalby ja do pakietu strony, ktora prawie nigdy nie eksportuje.
 */
export async function utworzSkoroszyt(): Promise<ExcelJS.Workbook> {
  const { default: ExcelJSLib } = await import('exceljs');
  const skoroszyt = new ExcelJSLib.Workbook();
  skoroszyt.creator = 'WorkBase';
  return skoroszyt;
}

/** Zapisuje skoroszyt i podsuwa go przegladarce jako pobranie. */
export async function pobierzSkoroszyt(skoroszyt: ExcelJS.Workbook, nazwaPliku: string): Promise<void> {
  const bufor = await skoroszyt.xlsx.writeBuffer();
  const blob = new Blob([bufor], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  const adres = URL.createObjectURL(blob);
  const odnosnik = document.createElement('a');
  odnosnik.href = adres;
  odnosnik.download = nazwaPliku;
  odnosnik.click();
  URL.revokeObjectURL(adres);
}
