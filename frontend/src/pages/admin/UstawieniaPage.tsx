import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowRight, Gauge } from 'lucide-react';
import { useUprawnienia } from '@/auth/useUprawnienia';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import { useGotowosc } from '@/api/hooks/useGotowosc';
import { GOTOWOSC, GRUPY_USTAWIEN } from '@/nav/ustawienia';
import { colors } from '@/theme/tokens';

// Ta sama stala co w MainLayout — tylko operator widzi kafelek platformy.
const OPERATOR_TENANT_ID = '00000000-0000-0000-0000-000000000001';

/**
 * Przeglad ustawien: wszystkie ekrany administracyjne w jednym miejscu, pogrupowane, z opisem.
 *
 * Pasek boczny odpowiada na pytanie „gdzie to jest", ale nie „co to robi" — etykieta
 * „Nazewnictwo" albo „Moduly" niczego nie tlumaczy komus, kto wchodzi tu drugi raz w zyciu.
 * Kafelek z jednym zdaniem opisu rozwiazuje to bez rozbudowywania paska.
 *
 * Kafelki filtrujemy po tej samej mapie uprawnien, ktora steruje wejsciem na trase, wiec
 * nie da sie tu zobaczyc ekranu, na ktory i tak nie wolno wejsc.
 */
export function UstawieniaPage() {
  const { t } = useTranslation();
  const { mozeWejscNa, znane } = useUprawnienia();
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;
  const jestOperatorem = user?.tenantId === OPERATOR_TENANT_ID;
  const { data: gotowosc } = useGotowosc();

  const widoczna = (p: { path: string; operatorOnly?: boolean }) =>
    (!p.operatorOnly || jestOperatorem) && (!znane || mozeWejscNa(p.path));

  const grupy = GRUPY_USTAWIEN
    .map((g) => ({ ...g, pozycje: g.pozycje.filter(widoczna) }))
    .filter((g) => g.pozycje.length > 0);
  const gotowoscWidoczna = widoczna(GOTOWOSC);
  const brakow = gotowosc?.pozycje.length ?? 0;

  return (
    <div style={{ padding: '24px 28px', maxWidth: 1040, margin: '0 auto' }}>
      <h1 style={{ fontSize: 22, fontWeight: 800, margin: '0 0 6px', color: colors.gray[900] }}>
        Ustawienia
      </h1>
      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 22px', fontSize: 14, maxWidth: '70ch' }}>
        Wszystko, co firma może dostosować pod siebie. Każda pozycja mówi, co ustawia i co z tego
        wynika. Nic nie jest wymagane — system działa na wartościach domyślnych.
      </p>

      {gotowoscWidoczna && (
        <Link
          to={GOTOWOSC.path}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 14,
            padding: '14px 18px',
            marginBottom: 26,
            borderRadius: 14,
            textDecoration: 'none',
            color: 'inherit',
            background: brakow > 0 ? colors.warning[50] : colors.success[50],
            border: `1px solid ${brakow > 0 ? colors.warning[200] : colors.success[200]}`,
          }}
        >
          <Gauge size={20} style={{ color: brakow > 0 ? colors.warning[600] : colors.success[600], flexShrink: 0 }} />
          <div style={{ flex: 1, minWidth: 0 }}>
            <strong style={{ fontSize: 14, color: colors.gray[900] }}>{t(GOTOWOSC.labelKey)}</strong>
            <p style={{ margin: '2px 0 0', fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
              {gotowosc === undefined
                ? GOTOWOSC.opis
                : brakow === 0
                  ? 'Wszystko, co sprawdzamy, jest ustawione.'
                  : `${brakow} ${brakow === 1 ? 'rzecz jeszcze nie zadziała' : brakow < 5 ? 'rzeczy jeszcze nie zadziałają' : 'rzeczy jeszcze nie zadziała'} — zobacz, które i gdzie to ustawić.`}
            </p>
          </div>
          <ArrowRight size={16} style={{ color: colors.gray[400], flexShrink: 0 }} />
        </Link>
      )}

      {grupy.length === 0 && znane && (
        <p style={{ fontSize: 14, color: 'var(--wb-ink-2, #6b7490)' }}>
          Nie masz dostępu do żadnego ekranu ustawień. Jeśli powinieneś, poproś administratora o
          odpowiednią rolę.
        </p>
      )}

      <div style={{ display: 'grid', gap: 26 }}>
        {grupy.map((grupa) => {
          const IkonaGrupy = grupa.icon;
          return (
            <section key={grupa.id}>
              <div style={{ display: 'flex', alignItems: 'baseline', gap: 10, marginBottom: 10 }}>
                <IkonaGrupy size={16} style={{ color: colors.primary[600], alignSelf: 'center' }} />
                <h2 style={{ fontSize: 15, fontWeight: 800, margin: 0, color: colors.gray[900] }}>{grupa.tytul}</h2>
                <span style={{ fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>{grupa.opis}</span>
              </div>
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))',
                  gap: 10,
                }}
              >
                {grupa.pozycje.map((p) => {
                  const Ikona = p.icon;
                  return (
                    <Link
                      key={p.path}
                      to={p.path}
                      className="wb-ustawienia-kafelek"
                      style={{
                        display: 'flex',
                        gap: 12,
                        padding: '13px 15px',
                        borderRadius: 12,
                        textDecoration: 'none',
                        color: 'inherit',
                        background: 'var(--wb-panel, #fff)',
                        border: '1px solid var(--wb-line, #e3e7f1)',
                      }}
                    >
                      <span
                        style={{
                          width: 32,
                          height: 32,
                          borderRadius: 9,
                          display: 'inline-flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          background: colors.primary[50],
                          color: colors.primary[600],
                          flexShrink: 0,
                        }}
                      >
                        <Ikona size={16} />
                      </span>
                      <span style={{ minWidth: 0 }}>
                        <strong style={{ display: 'block', fontSize: 13.5, color: colors.gray[900] }}>
                          {t(p.labelKey)}
                        </strong>
                        <span style={{ display: 'block', fontSize: 12.5, lineHeight: 1.45, color: 'var(--wb-ink-2, #6b7490)', marginTop: 2 }}>
                          {p.opis}
                        </span>
                      </span>
                    </Link>
                  );
                })}
              </div>
            </section>
          );
        })}
      </div>
    </div>
  );
}
