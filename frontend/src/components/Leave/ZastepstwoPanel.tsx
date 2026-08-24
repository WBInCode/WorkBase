import { useState } from 'react';
import { UserCheck, X } from 'lucide-react';
import { useEmployees } from '@/api/hooks/useOrganization';
import { useZastepstwa, useWyznaczZastepstwo, useOdwolajZastepstwo } from '@/api/hooks/useZastepstwa';
import { colors } from '@/theme/tokens';

/**
 * Wyznaczanie zastepcy w akceptacji wnioskow.
 *
 * Stoi na stronie wnioskow urlopowych, bo to moment, w ktorym przelozony w ogole o tym mysli:
 * skladajac wlasny wniosek widzi, ze przez tydzien nie bedzie mial kto zatwierdzac zespolu.
 * Panel widza tylko osoby majace podwladnych — reszcie nie ma czego zastepowac.
 */
export function ZastepstwoPanel({ employeeId }: { employeeId: string }) {
  const { data: zastepstwa = [], isLoading } = useZastepstwa(employeeId);
  const { data: pracownicyStrona } = useEmployees({ page: 1, pageSize: 200, status: 'Active' });
  const wyznacz = useWyznaczZastepstwo();
  const odwolaj = useOdwolajZastepstwo(employeeId);

  const [otwarty, setOtwarty] = useState(false);
  const [zastepca, setZastepca] = useState('');
  const [od, setOd] = useState('');
  const [doDnia, setDoDnia] = useState('');
  const [powod, setPowod] = useState('');
  const [blad, setBlad] = useState<string | null>(null);

  const kandydaci = (pracownicyStrona?.items ?? []).filter((p) => p.id !== employeeId);

  const zapisz = async () => {
    setBlad(null);
    try {
      await wyznacz.mutateAsync({
        zastepowanyEmployeeId: employeeId,
        zastepcaEmployeeId: zastepca,
        odKiedy: od,
        doKiedy: doDnia,
        powod: powod.trim() || null,
      });
      setOtwarty(false);
      setZastepca(''); setOd(''); setDoDnia(''); setPowod('');
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zapisać zastępstwa.');
    }
  };

  const moznaZapisac = zastepca !== '' && od !== '' && doDnia !== '' && !wyznacz.isPending;

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
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
        <UserCheck size={17} style={{ color: colors.primary[600] }} />
        <h2 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: colors.gray[900] }}>
          Zastępstwo w akceptacji
        </h2>
        <button
          onClick={() => setOtwarty((v) => !v)}
          style={{
            marginLeft: 'auto', padding: '6px 12px', borderRadius: 8,
            border: '1px solid var(--wb-line, #e3e7f1)', background: 'var(--wb-panel, #fff)',
            color: colors.gray[900], fontSize: 13, cursor: 'pointer',
          }}
        >
          {otwarty ? 'Anuluj' : 'Wyznacz zastępcę'}
        </button>
      </div>

      <p style={{ fontSize: 13, color: 'var(--wb-ink-2, #6b7490)', margin: '6px 0 0' }}>
        Na czas Twojej nieobecności wnioski Twojego zespołu trafią do wskazanej osoby. Zastępstwo nie
        nadaje jej żadnych dodatkowych uprawnień ani dostępu do danych.
      </p>

      {otwarty && (
        <div style={{ display: 'grid', gap: 10, marginTop: 14 }}>
          <label style={{ fontSize: 13, color: colors.gray[900] }}>
            Kto zastępuje
            <select
              value={zastepca}
              onChange={(e) => setZastepca(e.target.value)}
              style={{ display: 'block', width: '100%', marginTop: 4, padding: '7px 9px', borderRadius: 8, border: '1px solid var(--wb-line, #e3e7f1)' }}
            >
              <option value="">— wybierz osobę —</option>
              {kandydaci.map((p) => (
                <option key={p.id} value={p.id}>{p.firstName} {p.lastName}</option>
              ))}
            </select>
          </label>

          <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
            <label style={{ fontSize: 13, color: colors.gray[900], flex: '1 1 140px' }}>
              Od
              <input type="date" value={od} onChange={(e) => setOd(e.target.value)}
                style={{ display: 'block', width: '100%', marginTop: 4, padding: '7px 9px', borderRadius: 8, border: '1px solid var(--wb-line, #e3e7f1)' }} />
            </label>
            <label style={{ fontSize: 13, color: colors.gray[900], flex: '1 1 140px' }}>
              Do
              <input type="date" value={doDnia} min={od || undefined} onChange={(e) => setDoDnia(e.target.value)}
                style={{ display: 'block', width: '100%', marginTop: 4, padding: '7px 9px', borderRadius: 8, border: '1px solid var(--wb-line, #e3e7f1)' }} />
            </label>
          </div>

          <label style={{ fontSize: 13, color: colors.gray[900] }}>
            Powód (opcjonalnie)
            <input type="text" value={powod} maxLength={256} onChange={(e) => setPowod(e.target.value)}
              placeholder="np. urlop wypoczynkowy"
              style={{ display: 'block', width: '100%', marginTop: 4, padding: '7px 9px', borderRadius: 8, border: '1px solid var(--wb-line, #e3e7f1)' }} />
          </label>

          {blad && (
            <p style={{ margin: 0, fontSize: 13, color: colors.danger[600] }}>{blad}</p>
          )}

          <button
            onClick={zapisz}
            disabled={!moznaZapisac}
            style={{
              justifySelf: 'start', padding: '8px 16px', borderRadius: 8, border: 'none',
              background: moznaZapisac ? colors.primary[600] : colors.gray[300],
              color: colors.textOnAccent, fontSize: 13, fontWeight: 600,
              cursor: moznaZapisac ? 'pointer' : 'not-allowed',
            }}
          >
            {wyznacz.isPending ? 'Zapisywanie…' : 'Zapisz zastępstwo'}
          </button>
        </div>
      )}

      {!isLoading && zastepstwa.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, margin: '14px 0 0', display: 'grid', gap: 8 }}>
          {zastepstwa.map((z) => (
            <li key={z.id}
              style={{
                display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap',
                padding: '9px 12px', borderRadius: 10,
                background: z.obowiazujeDzis ? 'var(--wb-emr-100, #d1fae5)' : 'var(--wb-g-50, #f8fafc)',
                fontSize: 13,
              }}
            >
              <strong style={{ color: colors.gray[900] }}>{z.zastepcaImieNazwisko}</strong>
              <span style={{ color: 'var(--wb-ink-2, #6b7490)' }}>
                {z.odKiedy} – {z.doKiedy}
                {z.powod ? ` · ${z.powod}` : ''}
              </span>
              {z.obowiazujeDzis && (
                <span style={{ fontSize: 11, fontWeight: 700, color: 'var(--wb-emr-800, #065f46)' }}>
                  obowiązuje dziś
                </span>
              )}
              <button
                onClick={() => odwolaj.mutate(z.id)}
                title="Odwołaj zastępstwo"
                aria-label={`Odwołaj zastępstwo: ${z.zastepcaImieNazwisko}`}
                style={{
                  marginLeft: 'auto', border: 'none', background: 'transparent',
                  cursor: 'pointer', color: 'var(--wb-ink-2, #6b7490)', display: 'flex', padding: 4,
                }}
              >
                <X size={15} />
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
