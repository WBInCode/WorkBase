import { useRef, useState } from 'react';
import { FileArchive, Upload, Download, Trash2 } from 'lucide-react';
import {
  useDocuments,
  useUploadDocument,
  useDownloadDocument,
  useDeleteDocument,
} from '@/api/hooks/useDocuments';
import { useUprawnienia } from '@/auth/useUprawnienia';
import { colors } from '@/theme/tokens';

/**
 * Dokumenty przypięte do konkretnej rzeczy — pracownika albo zadania.
 *
 * Moduł dokumentów od początku miał do tego wszystko: kolumny `EntityType`/`EntityId`,
 * indeks złożony `(TenantId, EntityType, EntityId)`, filtrowanie w repozytorium, obsługę
 * w endpointach, a nawet gotowe parametry w hookach frontu. Brakowało **wyłącznie ekranu,
 * który te parametry poda** — `DocumentListPage` wołała je bez `entityType`/`entityId`, więc
 * cała ta warstwa leżała nieużywana.
 *
 * Skanowanie antywirusowe, limity rozmiaru i dozwolone rozszerzenia obsługuje backend —
 * ten komponent niczego nie waliduje na własną rękę, żeby nie powstała druga, rozjeżdżająca
 * się reguła.
 */
export function DokumentyEncji({
  entityType,
  entityId,
  tytul = 'Dokumenty',
}: {
  entityType: 'employee' | 'task';
  entityId: string;
  tytul?: string;
}) {
  const { data: dokumenty = [], isLoading } = useDocuments({ entityType, entityId });
  const wyslij = useUploadDocument();
  const pobierz = useDownloadDocument();
  const usun = useDeleteDocument();
  const { moze } = useUprawnienia();

  const wejscie = useRef<HTMLInputElement>(null);
  const [blad, setBlad] = useState<string | null>(null);

  const mozeDodawac = moze('documents.create');
  const mozeUsuwac = moze('documents.delete');

  const dodaj = async (plik: File) => {
    setBlad(null);
    try {
      await wyslij.mutateAsync({ file: plik, entityType, entityId });
    } catch (e) {
      // Backend odrzuca po rozszerzeniu, rozmiarze albo wyniku skanu — komunikat jest
      // konkretny, wiec pokazujemy go wprost zamiast zastepowac ogolnikiem.
      setBlad(e instanceof Error ? e.message : 'Nie udało się wysłać pliku.');
    } finally {
      if (wejscie.current) wejscie.current.value = '';
    }
  };

  return (
    <section
      style={{
        background: 'var(--wb-panel, #fff)',
        border: '1px solid var(--wb-line, #e3e7f1)',
        borderRadius: 14,
        padding: '16px 18px',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 10, flexWrap: 'wrap' }}>
        <FileArchive size={17} style={{ color: colors.primary[600] }} />
        <h2 style={{ fontSize: 15, fontWeight: 700, margin: 0, color: colors.gray[900] }}>{tytul}</h2>

        {mozeDodawac && (
          <>
            <input
              ref={wejscie}
              type="file"
              onChange={(e) => {
                const plik = e.target.files?.[0];
                if (plik) void dodaj(plik);
              }}
              style={{ display: 'none' }}
            />
            <button
              onClick={() => wejscie.current?.click()}
              disabled={wyslij.isPending}
              style={{
                marginLeft: 'auto',
                display: 'inline-flex',
                alignItems: 'center',
                gap: 5,
                padding: '5px 11px',
                borderRadius: 8,
                border: '1px solid var(--wb-line, #e3e7f1)',
                background: 'var(--wb-panel, #fff)',
                color: colors.primary[600],
                fontSize: 12.5,
                fontWeight: 600,
                cursor: wyslij.isPending ? 'wait' : 'pointer',
              }}
            >
              <Upload size={13} />
              {wyslij.isPending ? 'Wysyłanie…' : 'Dodaj plik'}
            </button>
          </>
        )}
      </div>

      {blad && <p style={{ margin: '0 0 10px', fontSize: 12.5, color: colors.danger[600] }}>{blad}</p>}

      {isLoading && <p style={stylPusty}>Wczytywanie…</p>}

      {!isLoading && dokumenty.length === 0 && (
        <p style={stylPusty}>Brak dokumentów.</p>
      )}

      {dokumenty.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 7 }}>
          {dokumenty.map((dokument) => (
            <li
              key={dokument.id}
              style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, flexWrap: 'wrap' }}
            >
              <span style={{ color: colors.gray[900], fontWeight: 500, wordBreak: 'break-all' }}>
                {dokument.fileName}
              </span>
              <span style={{ fontSize: 11.5, color: 'var(--wb-ink-2, #9aa3b8)' }}>
                {rozmiar(dokument.fileSizeBytes)}
              </span>

              <span style={{ marginLeft: 'auto', display: 'inline-flex', gap: 6 }}>
                <button
                  onClick={() => pobierz.mutate({ id: dokument.id, fileName: dokument.fileName })}
                  title="Pobierz"
                  style={stylIkony(colors.primary[600])}
                >
                  <Download size={13} />
                </button>
                {mozeUsuwac && (
                  <button
                    onClick={() => usun.mutate(dokument.id)}
                    title="Usuń"
                    style={stylIkony(colors.danger[600])}
                  >
                    <Trash2 size={13} />
                  </button>
                )}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function rozmiar(bajty: number): string {
  if (bajty < 1024) return `${bajty} B`;
  if (bajty < 1024 * 1024) return `${Math.round(bajty / 1024)} kB`;
  return `${(bajty / (1024 * 1024)).toFixed(1)} MB`;
}

const stylPusty: React.CSSProperties = {
  margin: 0,
  fontSize: 13,
  color: 'var(--wb-ink-2, #6b7490)',
};

const stylIkony = (kolor: string): React.CSSProperties => ({
  display: 'inline-flex',
  alignItems: 'center',
  padding: '4px 8px',
  borderRadius: 7,
  border: '1px solid var(--wb-line, #e3e7f1)',
  background: 'var(--wb-panel, #fff)',
  color: kolor,
  cursor: 'pointer',
});
