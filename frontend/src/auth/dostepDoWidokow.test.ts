import { describe, it, expect } from 'vitest';
import { DOSTEP_DO_WIDOKOW, uprawnieniaDlaSciezki } from './dostepDoWidokow';

describe('uprawnieniaDlaSciezki', () => {
  it('zwraca uprawnienie dla trasy statycznej', () => {
    expect(uprawnieniaDlaSciezki('/org/employees')).toEqual(['org.view']);
    expect(uprawnieniaDlaSciezki('/time/team-report')).toEqual(['time.view-team']);
  });

  it('trasa konkretna wygrywa z trasa parametryczna', () => {
    // '/org/employees/import' i '/org/employees/:id' maja te sama liczbe segmentow,
    // wiec zla kolejnosc w mapie oddalaby import kazdemu z org.view.
    expect(uprawnieniaDlaSciezki('/org/employees/import')).toEqual(['org.import']);
    expect(uprawnieniaDlaSciezki('/org/employees/019fd628-0c2d-71ad-a400-eb48e5137435')).toEqual(['org.view']);
    expect(uprawnieniaDlaSciezki('/tasks/my')).toEqual(['tasks.view']);
  });

  it('pusta lista dla pulpitu, null dla trasy spoza mapy', () => {
    expect(uprawnieniaDlaSciezki('/workspace')).toEqual([]);
    expect(uprawnieniaDlaSciezki('/nie-ma-takiej')).toBeNull();
  });

  it('ignoruje koncowy ukosnik', () => {
    expect(uprawnieniaDlaSciezki('/documents/')).toEqual(['documents.view']);
  });

  it('nie myli tras o roznej liczbie segmentow', () => {
    expect(uprawnieniaDlaSciezki('/documents/categories')).toEqual(['documents.manage']);
    expect(uprawnieniaDlaSciezki('/org')).toBeNull();
  });

  it('kazdy wpis mapy uzywa kodu w formacie modul.akcja', () => {
    for (const [, wymagane] of DOSTEP_DO_WIDOKOW) {
      for (const kod of wymagane) {
        expect(kod).toMatch(/^[a-z]+\.[a-z-]+$/);
      }
    }
  });
});
