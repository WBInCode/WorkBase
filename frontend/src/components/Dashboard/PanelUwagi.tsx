import { Link } from 'react-router-dom';
import { AlertTriangle, CircleAlert, Check, ChevronRight } from 'lucide-react';
import { useAlertyPulpitu, type Alert } from '@/api/hooks/useAlertyPulpitu';
import { colors } from '@/theme/tokens';

/**
 * „Co wymaga mojej uwagi” — lista rzeczy do zrobienia zamiast kafelkow z liczbami.
 *
 * Kolejnosc jest istotna: pilne przed uwagami. Kierownik czyta ten panel rano i ma zaczac
 * od rzeczy, ktora kogos blokuje, a nie od najliczniejszej.
 */
export function PanelUwagi() {
  const { data: alerty = [], isLoading } = useAlertyPulpitu();

  if (isLoading) return null;

  const pilne = alerty.filter((a) => a.waga === 'pilne');
  const uwagi = alerty.filter((a) => a.waga !== 'pilne');
  const posortowane = [...pilne, ...uwagi];

  return (
    <section
      style={{
        background: 'var(--wb-panel, #fff)',
        border: '1px solid var(--wb-line, #e3e7f1)',
        borderRadius: 16,
        padding: '18px 20px',
        marginBottom: 18,
      }}
    >
      <h2 style={{ fontSize: 15, fontWeight: 700, margin: '0 0 4px', color: colors.gray[900] }}>
        Co wymaga uwagi
      </h2>

      {posortowane.length === 0 ? (
        <p
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 8,
            margin: '10px 0 0',
            fontSize: 14,
            color: 'var(--wb-emr-800, #065f46)',
          }}
        >
          <Check size={16} />
          Nic nie czeka. Wszystko na bieżąco.
        </p>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: '12px 0 0', display: 'grid', gap: 10 }}>
          {posortowane.map((alert) => (
            <PozycjaListy key={alert.kod} alert={alert} />
          ))}
        </ul>
      )}
    </section>
  );
}

function PozycjaListy({ alert }: { alert: Alert }) {
  const pilne = alert.waga === 'pilne';
  const kolor = pilne ? 'var(--wb-red-600, #dc2626)' : 'var(--wb-org-600, #ea580c)';
  const tlo = pilne ? 'var(--wb-red-50, #fef2f2)' : 'var(--wb-org-50, #fff7ed)';
  const Ikona = pilne ? AlertTriangle : CircleAlert;

  const ukryte = alert.liczba - alert.pozycje.length;

  const tresc = (
    <>
      <span style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
        <Ikona size={15} style={{ color: kolor, flexShrink: 0 }} />
        <strong style={{ color: colors.gray[900], fontSize: 14 }}>{alert.tytul}</strong>
        <span
          style={{
            fontSize: 12,
            fontWeight: 700,
            padding: '1px 8px',
            borderRadius: 999,
            background: tlo,
            color: kolor,
            fontVariantNumeric: 'tabular-nums',
          }}
        >
          {alert.liczba}
        </span>
        {alert.sciezka && (
          <ChevronRight size={15} style={{ color: 'var(--wb-ink-2, #6b7490)', marginLeft: 'auto' }} />
        )}
      </span>

      <span style={{ display: 'block', fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)', marginTop: 3 }}>
        {alert.opis}
      </span>

      {alert.pozycje.length > 0 && (
        <span style={{ display: 'block', fontSize: 12.5, color: colors.gray[900], marginTop: 6 }}>
          {alert.pozycje.map((p) => p.opis).join(' · ')}
          {ukryte > 0 && (
            <span style={{ color: 'var(--wb-ink-2, #6b7490)' }}> i {ukryte} więcej</span>
          )}
        </span>
      )}
    </>
  );

  const styl = {
    display: 'block',
    padding: '11px 13px',
    borderRadius: 12,
    border: '1px solid var(--wb-line, #e3e7f1)',
    borderLeft: `3px solid ${kolor}`,
    textDecoration: 'none',
    color: 'inherit',
  } as const;

  return (
    <li>
      {alert.sciezka ? (
        <Link to={alert.sciezka} style={styl}>
          {tresc}
        </Link>
      ) : (
        <div style={styl}>{tresc}</div>
      )}
    </li>
  );
}
