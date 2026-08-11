import { describe, it, expect } from 'vitest';
import { SEKCJE_POMOCY } from './tresc';
import { uprawnieniaDlaSciezki } from '@/auth/dostepDoWidokow';

const wszystkieWpisy = SEKCJE_POMOCY.flatMap((sekcja) => sekcja.wpisy);

describe('tresc pomocy', () => {
  it('identyfikatory sekcji i wpisow sa unikalne', () => {
    // Widok trzyma stan rozwiniecia po id, wiec duplikat otwieralby dwa wpisy naraz.
    const idSekcji = SEKCJE_POMOCY.map((s) => s.id);
    expect(new Set(idSekcji).size).toBe(idSekcji.length);

    const idWpisow = wszystkieWpisy.map((w) => w.id);
    expect(new Set(idWpisow).size).toBe(idWpisow.length);
  });

  it('kazdy odsylacz prowadzi do trasy, ktora istnieje', () => {
    // Literowka w sciezce dalaby odsylacz konczacy sie przekierowaniem na pulpit,
    // a w pomocy taki blad jest szczegolnie mylacy.
    for (const wpis of wszystkieWpisy) {
      if (!wpis.sciezka) continue;
      expect(uprawnieniaDlaSciezki(wpis.sciezka), `nieznana trasa w "${wpis.id}"`).not.toBeNull();
    }
  });

  it('wymagane uprawnienia uzywaja formatu modul.akcja', () => {
    // Literowka w kodzie uprawnienia ukrylaby wpis na zawsze i to bez zadnego sygnalu.
    for (const wpis of wszystkieWpisy) {
      for (const kod of wpis.wymaga ?? []) {
        expect(kod, `zly kod w "${wpis.id}"`).toMatch(/^[a-z]+\.[a-z-]+$/);
      }
    }
  });

  it('wpis dla przelozonego nie jest jednoczesnie gatowany uprawnieniem', () => {
    // Przelozony bywa zwyklym Pracownikiem, wiec dodatkowy warunek na uprawnieniu
    // schowalby mu instrukcje do ekranu, ktory realnie widzi.
    for (const wpis of wszystkieWpisy) {
      if (wpis.tylkoPrzelozony) {
        expect(wpis.wymaga, `podwojny warunek w "${wpis.id}"`).toBeUndefined();
      }
    }
  });

  it('kazdy wpis ma pytanie i co najmniej jeden akapit odpowiedzi', () => {
    for (const wpis of wszystkieWpisy) {
      expect(wpis.pytanie.trim().length, `puste pytanie w "${wpis.id}"`).toBeGreaterThan(0);
      expect(wpis.odpowiedz.length, `pusta odpowiedz w "${wpis.id}"`).toBeGreaterThan(0);
    }
  });
});
