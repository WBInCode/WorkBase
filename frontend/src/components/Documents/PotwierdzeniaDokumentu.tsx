import { ClipboardCheck, Check } from 'lucide-react';
import {
  useDokumentyDoPotwierdzenia,
  usePotwierdzDokument,
  useRaportPotwierdzen,
  useDownloadDocument,
} from '@/api/hooks/useDocuments';
import { colors } from '@/theme/tokens';

const data = (iso: string) => new Date(iso).toLocaleDateString('pl-PL');

/**
 * Baner nad lista dokumentow: „masz N dokumentow do potwierdzenia".
 *
 * Potwierdza wylacznie sam pracownik — serwer bierze identyfikator z tokenu, wiec nie da sie
 * potwierdzic za kogos. Baner znika, gdy nie ma nic do potwierdzenia; nie zostawiamy pustego
 * pudelka „wszystko potwierdzone", bo to szum dla kogos, kto wchodzi tu po zwykly plik.
 */
export function DoPotwierdzeniaBaner() {
  const { data: lista = [] } = useDokumentyDoPotwierdzenia();
  const potwierdz = usePotwierdzDokument();
  const pobierz = useDownloadDocument();

  if (lista.length === 0) return null;

  return (
    <section
      style={{
        marginBottom: 18,
        padding: '14px 18px',
        borderRadius: 14,
        background: colors.warning[50],
        border: `1px solid ${colors.warning[200]}`,
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
        <ClipboardCheck size={17} style={{ color: colors.warning[600] }} />
        <strong style={{ fontSize: 14, color: colors.gray[900] }}>Do potwierdzenia</strong>
        <span style={{ fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)' }}>
          {lista.length === 1 ? 'Jeden dokument czeka' : `${lista.length} dokumenty czekają`}, aż potwierdzisz, że się z nimi zapoznałeś.
        </span>
      </div>
      <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'grid', gap: 6 }}>
        {lista.map((d) => (
          <li key={d.documentId} style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
            <button
              onClick={() => pobierz.mutate({ id: d.documentId, fileName: d.fileName })}
              style={{ background: 'none', border: 'none', padding: 0, fontFamily: 'inherit', fontSize: 13.5, fontWeight: 600, color: colors.primary[700], cursor: 'pointer', textDecoration: 'underline' }}
            >
              {d.fileName}
            </button>
            {d.description && <span style={{ fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)' }}>{d.description}</span>}
            <span style={{ fontSize: 12, color: 'var(--wb-ink-2, #6b7490)' }}>opublikowano {data(d.createdAt)}</span>
            <button
              onClick={() => potwierdz.mutate(d.documentId)}
              disabled={potwierdz.isPending}
              style={{
                marginLeft: 'auto', display: 'inline-flex', alignItems: 'center', gap: 5, padding: '5px 11px',
                borderRadius: 8, border: 'none', background: colors.primary[600], color: colors.textOnAccent,
                fontSize: 12.5, fontWeight: 600, fontFamily: 'inherit', cursor: 'pointer',
              }}
            >
              <Check size={13} /> Zapoznałem się
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}

/** Widok dla kadr: kto potwierdzil, kto nie i od ilu dni. Rozwijany pod wierszem dokumentu. */
export function PotwierdzeniaDokumentu({ documentId }: { documentId: string }) {
  const { data: raport, isLoading } = useRaportPotwierdzen(documentId);

  if (isLoading || !raport) {
    return <p style={{ margin: 0, fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)' }}>Wczytywanie…</p>;
  }

  const zalegli = raport.osoby.filter((o) => o.potwierdzonoDnia === null);
  const potwierdzili = raport.osoby.filter((o) => o.potwierdzonoDnia !== null);

  return (
    <div style={{ padding: '10px 12px', borderRadius: 10, background: 'var(--wb-g-50, #f7f8fb)', fontSize: 12.5 }}>
      <div style={{ display: 'flex', gap: 14, marginBottom: zalegli.length + potwierdzili.length > 0 ? 8 : 0, fontWeight: 600, color: colors.gray[900] }}>
        <span style={{ color: colors.success[700] }}>potwierdziło: {raport.potwierdzilo}</span>
        <span style={{ color: raport.czeka > 0 ? colors.warning[800] : 'var(--wb-ink-2, #6b7490)' }}>czeka: {raport.czeka}</span>
      </div>
      {zalegli.length > 0 && (
        <ul style={{ listStyle: 'none', margin: '0 0 6px', padding: 0, display: 'flex', flexWrap: 'wrap', gap: 6 }}>
          {zalegli.map((o) => (
            <li key={o.employeeId} style={{ padding: '2px 8px', borderRadius: 999, background: colors.warning[100], color: colors.warning[800] }}>
              {o.imieNazwisko} · {o.dniBezPotwierdzenia} dni
            </li>
          ))}
        </ul>
      )}
      {potwierdzili.length > 0 && (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'flex', flexWrap: 'wrap', gap: 6 }}>
          {potwierdzili.map((o) => (
            <li key={o.employeeId} style={{ padding: '2px 8px', borderRadius: 999, background: colors.success[100], color: colors.success[800] }}>
              {o.imieNazwisko} · {data(o.potwierdzonoDnia!)}
            </li>
          ))}
        </ul>
      )}
      {raport.osoby.length === 0 && (
        <span style={{ color: 'var(--wb-ink-2, #6b7490)' }}>Ten dokument nie ma adresatów.</span>
      )}
    </div>
  );
}
