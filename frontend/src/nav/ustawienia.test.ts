import { describe, expect, it } from 'vitest';
import { DOSTEP_DO_WIDOKOW, uprawnieniaDlaSciezki } from '@/auth/dostepDoWidokow';
import { GRUPY_USTAWIEN, WSZYSTKIE_POZYCJE_USTAWIEN } from './ustawienia';

// Grupy sa jedynym wejsciem do ekranow administracyjnych z paska bocznego i z przegladu.
// Ekran, ktory tu nie trafi, istnieje wylacznie pod wklejonym adresem — dokladnie tak przez
// miesiace zyl Kreator obiegow. Ta bramka oblewa, zanim ktos to zauwazy u klienta.
describe('grupy ustawien', () => {
  it('kazda trasa administracyjna z mapy dostepu ma swoje miejsce w grupach', () => {
    const wGrupach = new Set(WSZYSTKIE_POZYCJE_USTAWIEN.map((p) => p.path));
    const administracyjne = DOSTEP_DO_WIDOKOW
      .map(([sciezka]) => sciezka)
      .filter((s) => s.startsWith('/admin/') || s === '/workflow/builder');

    const brakujace = administracyjne.filter((s) => !wGrupach.has(s));
    expect(brakujace, 'ekrany poza grupami: ' + brakujace.join(', ')).toEqual([]);
  });

  it('kazda pozycja prowadzi do trasy, ktora istnieje', () => {
    for (const p of WSZYSTKIE_POZYCJE_USTAWIEN) {
      expect(uprawnieniaDlaSciezki(p.path), 'nieznana trasa: ' + p.path).not.toBeNull();
    }
  });

  it('zadna trasa nie jest w dwoch grupach', () => {
    const sciezki = WSZYSTKIE_POZYCJE_USTAWIEN.map((p) => p.path);
    expect(new Set(sciezki).size).toBe(sciezki.length);
  });

  it('kazda pozycja i grupa ma opis — to one robia z listy cos zrozumialego', () => {
    for (const g of GRUPY_USTAWIEN) {
      expect(g.opis.trim().length, 'pusta grupa ' + g.id).toBeGreaterThan(0);
      for (const p of g.pozycje) {
        expect(p.opis.trim().length, 'pusty opis ' + p.path).toBeGreaterThan(0);
      }
    }
  });
});
