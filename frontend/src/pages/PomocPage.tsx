import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ChevronDown, ArrowRight, Info, Search, X } from 'lucide-react';
import { useIsMobile } from '@/shared';
import { colors, semantic } from '@/theme/tokens';
import { useUprawnienia } from '@/auth/useUprawnienia';
import { SEKCJE_POMOCY } from '@/pomoc/tresc';
import type { SekcjaPomocy, WpisPomocy } from '@/pomoc/tresc';

const CIEN_KARTY =
  '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))';

function pasujeDoFrazy(wpis: WpisPomocy, fraza: string): boolean {
  if (!fraza) return true;
  const tekst = [wpis.pytanie, ...wpis.odpowiedz, ...(wpis.kroki ?? []), wpis.uwaga ?? '']
    .join(' ')
    .toLowerCase();
  return tekst.includes(fraza);
}

export function PomocPage() {
  const mobile = useIsMobile();
  const { moze, jestPrzelozonym, znane } = useUprawnienia();
  const [fraza, setFraza] = useState('');
  const [otwarte, setOtwarte] = useState<ReadonlySet<string>>(new Set());

  const szukane = fraza.trim().toLowerCase();

  // Dopoki nie znamy uprawnien, nie filtrujemy — inaczej lista migalaby przy kazdym wejsciu.
  const sekcje: SekcjaPomocy[] = useMemo(() => {
    return SEKCJE_POMOCY.map((sekcja) => ({
      ...sekcja,
      wpisy: sekcja.wpisy.filter((wpis) => {
        if (!pasujeDoFrazy(wpis, szukane)) return false;
        if (!znane) return true;
        if (wpis.tylkoPrzelozony && !jestPrzelozonym) return false;
        if (wpis.wymaga && !wpis.wymaga.some(moze)) return false;
        return true;
      }),
    })).filter((sekcja) => sekcja.wpisy.length > 0);
  }, [szukane, znane, jestPrzelozonym, moze]);

  const liczbaWpisow = sekcje.reduce((suma, s) => suma + s.wpisy.length, 0);

  function przelacz(id: string) {
    setOtwarte((poprzednie) => {
      const nowe = new Set(poprzednie);
      if (nowe.has(id)) nowe.delete(id);
      else nowe.add(id);
      return nowe;
    });
  }

  return (
    <div style={{ padding: mobile ? 14 : '24px 28px', maxWidth: 1100, margin: '0 auto' }}>
      <div
        style={{
          marginBottom: 18,
          backgroundColor: colors.white,
          border: `1px solid ${colors.gray[200]}`,
          borderRadius: 20,
          boxShadow: CIEN_KARTY,
          padding: mobile ? 16 : '18px 22px',
        }}
      >
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            gap: 12,
            flexWrap: 'wrap',
          }}
        >
          <div style={{ minWidth: 0 }}>
            <h1
              style={{
                fontSize: 22,
                fontWeight: 800,
                letterSpacing: '-0.02em',
                margin: 0,
                color: colors.gray[900],
              }}
            >
              Pomoc
            </h1>
            <p style={{ margin: '3px 0 0', fontSize: 13, color: colors.gray[500] }}>
              {liczbaWpisow} {liczbaWpisow === 1 ? 'odpowiedź' : 'odpowiedzi'} dopasowanych do Twoich
              uprawnień
            </p>
          </div>

          <div style={{ position: 'relative', flex: mobile ? '1 1 100%' : '0 0 300px' }}>
            <Search
              size={15}
              color={colors.gray[400]}
              style={{ position: 'absolute', left: 13, top: '50%', transform: 'translateY(-50%)' }}
            />
            <input
              value={fraza}
              onChange={(e) => setFraza(e.target.value)}
              placeholder="Szukaj, np. urlop, przerwa, hasło"
              style={{
                width: '100%',
                boxSizing: 'border-box',
                padding: '9px 34px 9px 34px',
                fontSize: 13,
                color: semantic.textPrimary,
                backgroundColor: colors.gray[50],
                border: `1px solid ${colors.gray[200]}`,
                borderRadius: 999,
                outline: 'none',
              }}
            />
            {fraza && (
              <button
                onClick={() => setFraza('')}
                aria-label="Wyczyść wyszukiwanie"
                style={{
                  position: 'absolute',
                  right: 8,
                  top: '50%',
                  transform: 'translateY(-50%)',
                  display: 'inline-flex',
                  padding: 4,
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  color: colors.gray[400],
                }}
              >
                <X size={14} />
              </button>
            )}
          </div>
        </div>
      </div>

      {sekcje.length === 0 && (
        <div
          style={{
            textAlign: 'center',
            padding: '48px 24px',
            backgroundColor: colors.white,
            border: `1px solid ${colors.gray[200]}`,
            borderRadius: 20,
            boxShadow: CIEN_KARTY,
          }}
        >
          <p style={{ margin: 0, fontSize: 14, color: semantic.textBody }}>
            Nic nie pasuje do frazy „{fraza}”.
          </p>
          <p style={{ margin: '6px 0 0', fontSize: 13, color: colors.gray[500] }}>
            Spróbuj innego słowa albo zapytaj administratora systemu w Twojej firmie.
          </p>
        </div>
      )}

      {sekcje.map((sekcja) => {
        const Ikona = sekcja.ikona;
        return (
          <section key={sekcja.id} style={{ marginBottom: 22 }}>
            <div
              style={{ display: 'flex', alignItems: 'center', gap: 10, margin: '0 0 10px 4px' }}
            >
              <span
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: 30,
                  height: 30,
                  borderRadius: 10,
                  backgroundColor: colors.primary[100],
                  color: colors.primary[600],
                }}
              >
                <Ikona size={16} />
              </span>
              <div style={{ minWidth: 0 }}>
                <h2
                  style={{
                    margin: 0,
                    fontSize: 15,
                    fontWeight: 800,
                    letterSpacing: '-0.01em',
                    color: colors.gray[900],
                  }}
                >
                  {sekcja.tytul}
                </h2>
                <p style={{ margin: '1px 0 0', fontSize: 12, color: colors.gray[500] }}>
                  {sekcja.opis}
                </p>
              </div>
            </div>

            <div
              style={{
                backgroundColor: colors.white,
                border: `1px solid ${colors.gray[200]}`,
                borderRadius: 20,
                boxShadow: CIEN_KARTY,
                overflow: 'hidden',
              }}
            >
              {sekcja.wpisy.map((wpis, indeks) => {
                const rozwiniety = otwarte.has(wpis.id) || Boolean(szukane);
                return (
                  <article
                    key={wpis.id}
                    style={{
                      borderTop: indeks === 0 ? 'none' : `1px solid ${colors.gray[100]}`,
                    }}
                  >
                    <button
                      onClick={() => przelacz(wpis.id)}
                      aria-expanded={rozwiniety}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        gap: 12,
                        width: '100%',
                        padding: mobile ? '14px 16px' : '15px 20px',
                        background: 'none',
                        border: 'none',
                        cursor: 'pointer',
                        textAlign: 'left',
                        font: 'inherit',
                      }}
                    >
                      <span
                        style={{ fontSize: 14, fontWeight: 700, color: colors.gray[900], minWidth: 0 }}
                      >
                        {wpis.pytanie}
                      </span>
                      <ChevronDown
                        size={16}
                        color={colors.gray[400]}
                        style={{
                          flexShrink: 0,
                          transform: rozwiniety ? 'rotate(180deg)' : 'none',
                          transition: 'transform 150ms ease',
                        }}
                      />
                    </button>

                    {rozwiniety && (
                      <div style={{ padding: mobile ? '0 16px 16px' : '0 20px 18px' }}>
                        {wpis.odpowiedz.map((akapit, i) => (
                          <p
                            key={i}
                            style={{
                              margin: i === 0 ? '0 0 8px' : '0 0 8px',
                              fontSize: 13.5,
                              lineHeight: 1.62,
                              color: semantic.textBody,
                            }}
                          >
                            {akapit}
                          </p>
                        ))}

                        {wpis.kroki && (
                          <ol
                            style={{
                              margin: '10px 0 0',
                              paddingLeft: 20,
                              fontSize: 13.5,
                              lineHeight: 1.62,
                              color: semantic.textBody,
                            }}
                          >
                            {wpis.kroki.map((krok, i) => (
                              <li key={i} style={{ marginBottom: 4 }}>
                                {krok}
                              </li>
                            ))}
                          </ol>
                        )}

                        {wpis.uwaga && (
                          <div
                            style={{
                              display: 'flex',
                              gap: 9,
                              marginTop: 12,
                              padding: '10px 12px',
                              backgroundColor: colors.warning[50],
                              border: `1px solid ${colors.warning[100]}`,
                              borderRadius: 12,
                            }}
                          >
                            <Info
                              size={15}
                              color={colors.warning[600]}
                              style={{ flexShrink: 0, marginTop: 1 }}
                            />
                            <p
                              style={{
                                margin: 0,
                                fontSize: 12.5,
                                lineHeight: 1.55,
                                color: colors.warning[800],
                              }}
                            >
                              {wpis.uwaga}
                            </p>
                          </div>
                        )}

                        {wpis.sciezka && (
                          <Link
                            to={wpis.sciezka}
                            style={{
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: 6,
                              marginTop: 12,
                              fontSize: 13,
                              fontWeight: 700,
                              textDecoration: 'none',
                              color: colors.primary[600],
                            }}
                          >
                            {wpis.etykietaSciezki ?? 'Przejdź'}
                            <ArrowRight size={14} />
                          </Link>
                        )}
                      </div>
                    )}
                  </article>
                );
              })}
            </div>
          </section>
        );
      })}
    </div>
  );
}
