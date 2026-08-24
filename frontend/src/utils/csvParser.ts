/**
 * Minimal CSV parser — handles quoted fields, commas inside quotes,
 * and common CSV edge cases. No external dependency needed.
 */

export interface ParsedCsv {
  headers: string[];
  rows: string[][];
}

/**
 * Odczytuje plik jako tekst, dobierajac kodowanie.
 *
 * Eksporty kadrowe z Symfonii, Optimy i z Excela („Zapisz jako CSV") zapisuja sie w
 * Windows-1250, nie w UTF-8. Wczesniej plik szedl przez FileReader.readAsText bez podania
 * kodowania, czyli zawsze jako UTF-8 — polskie znaki zamienialy sie w krzaki i tak trafialy
 * do bazy. Probujemy wiec najpierw UTF-8 w trybie scislym: jesli bajty nie sa poprawnym
 * UTF-8, dekoder rzuca wyjatkiem i wtedy czytamy plik jako Windows-1250.
 */
export async function odczytajCsv(plik: Blob): Promise<string> {
  const bajty = await plik.arrayBuffer();

  try {
    return new TextDecoder('utf-8', { fatal: true }).decode(bajty);
  } catch {
    return new TextDecoder('windows-1250').decode(bajty);
  }
}

/**
 * Data zatrudnienia z pliku kadrowego.
 *
 * Wczesniej stalo tu samo `new Date(tekst)`, ktore radzi sobie wylacznie z ISO. Zapis
 * `15.03.2015` byl odrzucany, a `05/03/2015` przegladarka czytala po amerykansku jako
 * 3 MAJA zamiast 5 marca — i to bez zadnego bledu, wiec do bazy szla zla data zatrudnienia.
 *
 * Formaty z ukosnikiem i kropka czytamy po polsku (dzien pierwszy). To swiadomy wybor:
 * produkt jest polski, a plik i tak pochodzi z polskiego programu kadrowego.
 * Zwraca null, gdy nie da sie odczytac — wolimy odrzucic wiersz niz zapisac zmyslona date.
 */
export function parsujDateZatrudnienia(tekst: string): Date | null {
  const wartosc = tekst.trim();
  if (!wartosc) return null;

  // ISO: 2015-03-15 (ewentualnie z czasem) — jedyny format bez dwuznacznosci.
  const iso = /^(\d{4})-(\d{2})-(\d{2})/.exec(wartosc);
  if (iso) return zbudujDate(Number(iso[3]), Number(iso[2]), Number(iso[1]));

  // Polskie: 15.03.2015, 15-03-2015, 15/03/2015
  const polski = /^(\d{1,2})[.\-/](\d{1,2})[.\-/](\d{4})$/.exec(wartosc);
  if (polski) return zbudujDate(Number(polski[1]), Number(polski[2]), Number(polski[3]));

  return null;
}

function zbudujDate(dzien: number, miesiac: number, rok: number): Date | null {
  if (miesiac < 1 || miesiac > 12 || dzien < 1 || dzien > 31) return null;

  // Poludnie UTC, a nie polnoc: przy polnocy przesuniecie strefy potrafi cofnac date
  // o jeden dzien i pracownik dostaje zatrudnienie dzien wczesniej.
  const data = new Date(Date.UTC(rok, miesiac - 1, dzien, 12));

  // Odrzuca 31.02 — Date sam przewinolby to na 3 marca.
  if (data.getUTCMonth() !== miesiac - 1 || data.getUTCDate() !== dzien) return null;

  return data;
}

export function parseCsv(text: string): ParsedCsv {
  const lines = splitCsvLines(text.trim());
  if (lines.length === 0) return { headers: [], rows: [] };

  const headers = parseCsvLine(lines[0] ?? '');
  const rows = lines.slice(1).filter((l) => l.trim()).map(parseCsvLine);

  return { headers, rows };
}

function splitCsvLines(text: string): string[] {
  const lines: string[] = [];
  let current = '';
  let inQuotes = false;

  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (ch === '"') {
      inQuotes = !inQuotes;
      current += ch;
    } else if ((ch === '\n' || ch === '\r') && !inQuotes) {
      if (ch === '\r' && text[i + 1] === '\n') i++; // skip \r\n
      lines.push(current);
      current = '';
    } else {
      current += ch;
    }
  }
  if (current) lines.push(current);
  return lines;
}

function parseCsvLine(line: string): string[] {
  const fields: string[] = [];
  let current = '';
  let inQuotes = false;

  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (inQuotes) {
      if (ch === '"') {
        if (line[i + 1] === '"') {
          current += '"';
          i++;
        } else {
          inQuotes = false;
        }
      } else {
        current += ch;
      }
    } else {
      if (ch === '"') {
        inQuotes = true;
      } else if (ch === ',' || ch === ';') {
        fields.push(current.trim());
        current = '';
      } else {
        current += ch;
      }
    }
  }
  fields.push(current.trim());
  return fields;
}
