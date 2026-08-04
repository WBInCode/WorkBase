// Kontrola kontrastu par tonalnych ciemnego motywu (WCAG 2.1).
// Czyta wartosci wprost z workbase.css, zeby nie rozjechaly sie z kodem.
import { readFileSync } from 'node:fs';

const css = readFileSync(new URL('../src/theme/workbase.css', import.meta.url), 'utf8');
const blokCiemny = css.slice(css.indexOf("html[data-theme='dark']"));
const blokJasny = css.slice(css.indexOf(':root'), css.indexOf("html[data-theme='dark']"));

function zmienne(blok) {
  const mapa = {};
  for (const m of blok.matchAll(/(--wb-[a-z0-9-]+):\s*(#[0-9a-fA-F]{3,8})\s*;/g)) mapa[m[1]] = m[2];
  return mapa;
}

function kanal(v) {
  const s = v / 255;
  return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
}

function luminancja(hex) {
  const h = hex.replace('#', '');
  const pelny = h.length === 3 ? h.split('').map((c) => c + c).join('') : h;
  const r = parseInt(pelny.slice(0, 2), 16);
  const g = parseInt(pelny.slice(2, 4), 16);
  const b = parseInt(pelny.slice(4, 6), 16);
  return 0.2126 * kanal(r) + 0.7152 * kanal(g) + 0.0722 * kanal(b);
}

function kontrast(a, b) {
  const l1 = luminancja(a);
  const l2 = luminancja(b);
  return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
}

// Pary tlo/tekst faktycznie uzywane w plakietkach i kartach.
const pary = [
  ['--wb-p-100', '--wb-p-700'], ['--wb-p-50', '--wb-p-700'],
  ['--wb-suc-100', '--wb-suc-800'], ['--wb-suc-50', '--wb-suc-700'],
  ['--wb-war-100', '--wb-war-800'], ['--wb-war-50', '--wb-war-700'],
  ['--wb-dan-100', '--wb-dan-800'], ['--wb-dan-50', '--wb-dan-700'],
  ['--wb-inf-100', '--wb-inf-600'],
  ['--wb-vio-100', '--wb-vio-800'], ['--wb-vio-50', '--wb-vio-600'],
  ['--wb-ind-50', '--wb-ind-700'], ['--wb-ind-100', '--wb-ind-700'],
  ['--wb-org-50', '--wb-org-800'], ['--wb-org-100', '--wb-org-700'],
  ['--wb-emr-100', '--wb-emr-800'], ['--wb-emr-50', '--wb-emr-700'],
  ['--wb-sky-200', '--wb-sky-700'], ['--wb-sky-50', '--wb-sky-700'],
  ['--wb-pur-50', '--wb-pur-800'], ['--wb-yel-50', '--wb-yel-800'],
  ['--wb-yel-100', '--wb-yel-800'], ['--wb-pnk-50', '--wb-pnk-600'],
  ['--wb-surface', '--wb-g-900'], ['--wb-surface', '--wb-g-500'],
  ['--wb-canvas', '--wb-g-900'], ['--wb-g-50', '--wb-g-700'],
  ['--wb-surface', '--wb-tea-700'], ['--wb-surface', '--wb-emr-500'],
];

const PROG = 4.5;
let bledy = 0;

for (const [nazwaTryb, blok] of [['jasny', zmienne(blokJasny)], ['ciemny', zmienne(blokCiemny)]]) {
  console.log(`\n== motyw ${nazwaTryb} ==`);
  for (const [tlo, tekst] of pary) {
    const a = blok[tlo];
    const b = blok[tekst];
    if (!a || !b) continue; // wartosc tylko w drugim bloku — jasny motyw bierze fallback z tokens.ts
    const k = kontrast(a, b);
    const ok = k >= PROG;
    // Jasny motyw zostaje taki, jaki byl — pilnujemy progu tam, gdzie zmiany faktycznie wprowadzamy.
    if (!ok && nazwaTryb === 'ciemny') bledy++;
    console.log(`  ${ok ? 'OK   ' : 'NISKI'} ${k.toFixed(2)}  ${tlo} ${a} / ${tekst} ${b}`);
  }
}

console.log(`\npar ciemnego motywu ponizej ${PROG}: ${bledy}`);
process.exit(bledy === 0 ? 0 : 1);
