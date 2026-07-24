import { readFileSync } from 'node:fs';
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