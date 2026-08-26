import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

describe('PWA SSO routing', () => {
  it('never handles backend routes with the SPA navigation fallback', () => {
    const config = readFileSync(resolve(process.cwd(), 'vite.config.ts'), 'utf8');

    expect(config).toContain("/^\\/sso\\/callback(?:\\/|$)/");
    expect(config).toContain("/^\\/api(?:\\/|$)/");
    expect(config).toContain("/^\\/health(?:\\/|$)/");
    expect(config).toContain("/^\\/hubs(?:\\/|$)/");
  });

  it('forces service-worker control files to revalidate', () => {
    const nginx = readFileSync(resolve(process.cwd(), 'nginx.conf'), 'utf8');

    for (const path of ['/sw.js', '/registerSW.js', '/manifest.webmanifest']) {
      expect(nginx).toContain(`location = ${path}`);
    }
    expect(nginx.match(/no-cache, no-store, must-revalidate/g)).toHaveLength(4);
  });
});

/**
 * Ikony musza byc rastrowe.
 *
 * Safari IGNORUJE apple-touch-icon w formacie SVG — Apple wymaga PNG. Front nie mial ani
 * jednego pliku PNG, wiec na iPhonie ikona po dodaniu do ekranu poczatkowego byla zastepcza.
 * Kryteria instalowalnosci w Chrome sa wobec SVG rowniez niepewne.
 */
describe('Ikony PWA', () => {
  const manifesty = ['public/manifest.webmanifest', 'public/kiosk.webmanifest'];

  it.each(manifesty)('%s deklaruje wylacznie ikony rastrowe, ktore istnieja', (sciezka) => {
    const manifest = JSON.parse(readFileSync(resolve(process.cwd(), sciezka), 'utf8')) as {
      icons: { src: string; type: string }[];
    };

    expect(manifest.icons.length).toBeGreaterThan(0);
    for (const ikona of manifest.icons) {
      expect(ikona.type, `${sciezka}: ${ikona.src}`).not.toBe('image/svg+xml');
      expect(
        existsSync(resolve(process.cwd(), 'public', ikona.src.replace(/^\//, ''))),
        `brak pliku ${ikona.src} wskazanego w ${sciezka}`,
      ).toBe(true);
    }
  });

  it('apple-touch-icon wskazuje na istniejacy plik PNG', () => {
    const html = readFileSync(resolve(process.cwd(), 'index.html'), 'utf8');
    const dopasowanie = html.match(/rel="apple-touch-icon"[^>]*href="([^"]+)"/);

    const href = dopasowanie?.[1];
    expect(href, 'brak apple-touch-icon w index.html').toBeDefined();
    if (href === undefined) return;
    expect(href.endsWith('.png'), `apple-touch-icon musi byc PNG, jest: ${href}`).toBe(true);
    expect(existsSync(resolve(process.cwd(), 'public', href.replace(/^\//, '')))).toBe(true);
  });
});
