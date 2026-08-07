import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { ShieldOff } from 'lucide-react';
import { colors, semantic } from '@/theme/tokens';
import { useUprawnienia } from './useUprawnienia';
import type { WymaganeUprawnienia } from './dostepDoWidokow';

interface StrazWidokuProps {
  wymagane: WymaganeUprawnienia;
  children: ReactNode;
}

/**
 * Blokuje wejscie na trase, gdy uzytkownikowi brakuje uprawnienia. Ukrycie pozycji w menu
 * nie wystarcza — bez tego wystarczylo wkleic adres, zeby zobaczyc caly ekran (dane i tak
 * nie przyszly, ale UI sugerowal dostep).
 *
 * Swiadomie NIE przekierowujemy po cichu: czyste przekierowanie na /workspace wyglada jak
 * zepsuty odsylacz. Lepiej powiedziec wprost, czego brakuje.
 */
export function StrazWidoku({ wymagane, children }: StrazWidokuProps) {
  const { mozeKtorekolwiek, znane } = useUprawnienia();

  // Dopoki nie znamy uprawnien, nie pokazujemy ani ekranu, ani odmowy — inaczej przy kazdym
  // odswiezeniu mignelaby informacja o braku dostepu.
  if (!znane) return null;
  if (mozeKtorekolwiek(wymagane)) return <>{children}</>;

  return (
    <div style={{ display: 'flex', justifyContent: 'center', paddingTop: '64px' }}>
      <div
        style={{
          maxWidth: '460px', textAlign: 'center', padding: '32px',
          backgroundColor: semantic.bgSurface, border: `1px solid ${semantic.border}`,
          borderRadius: '14px',
        }}
      >
        <ShieldOff size={40} color={semantic.textMuted} />
        <h1 style={{ fontSize: '19px', fontWeight: 700, margin: '14px 0 8px', color: semantic.textPrimary }}>
          Brak dostępu do tego widoku
        </h1>
        <p style={{ fontSize: '14px', lineHeight: 1.55, color: semantic.textBody, margin: 0 }}>
          Twoja rola nie obejmuje tej części systemu. Jeśli potrzebujesz tu wejść, poproś
          administratora o nadanie uprawnienia.
        </p>
        <p style={{ fontSize: '12px', color: semantic.textMuted, margin: '12px 0 0' }}>
          Wymagane: {wymagane.join(' lub ')}
        </p>
        <Link
          to="/workspace"
          style={{
            display: 'inline-block', marginTop: '22px', padding: '9px 20px',
            fontSize: '13px', fontWeight: 700, textDecoration: 'none',
            color: colors.textOnAccent, backgroundColor: colors.primary[600], borderRadius: '999px',
          }}
        >
          Wróć do „Mój dzień”
        </Link>
      </div>
    </div>
  );
}
