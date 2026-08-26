import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertTriangle, CheckCircle2, Check, X, ExternalLink } from 'lucide-react';
import {
  useAnomalies,
  useOznaczAnomalieJakoPrzejrzana,
  useOdrzucAnomalie,
} from '@/api/hooks/useTimeTracking';
import { useEmployees } from '@/api/hooks/useOrganization';
import { useUprawnienia } from '@/auth/useUprawnienia';
import type { TimeAnomalyDto } from '@/api/types/time';
import { colors } from '@/theme/tokens';

/**
 * Rozpatrywanie anomalii czasu pracy.
 *
 * Zadanie cykliczne wykrywa je codziennie o 01:00 i robi to od początku. Backend od zawsze miał
 * `review` i `dismiss`, ale nie istniał ekran, który by je wywołał — pulpit pokazywał rosnący
 * licznik bez żadnego sposobu, żeby spadł. W chwili pisania na produkcji czekało 724 pozycje,
 * wszystkie ze statusem „New", najstarsze sprzed pięciu tygodni.
 *
 * Dwie decyzje, bo to dwie różne rzeczy: **przejrzana** (sprawa obejrzana i zamknięta) oraz
 * **odrzucona** (to nie był problem — ktoś był na urlopie, dzień wolny, pomyłka w grafiku).
 * Rozróżnienie ma sens dla firmy, która chce później policzyć, ile anomalii było realnych.
 *
 * Lista jest zawężana przez serwer do zakresu danych pytającego.
 */
const TYP_OPIS: Record<string, string> = {
  MissingClockIn: 'Brak wejścia',
  MissingClockOut: 'Brak wyjścia',
  LateArrival: 'Spóźnienie',
  WorkOnDayOff: 'Praca w dniu wolnym',
  DoubleClockIn: 'Podwójne wejście',
};

export function AnomaliePage() {
  const [odDni, setOdDni] = useState(30);
  const [pokazZamkniete, setPokazZamkniete] = useState(false);

  // Datę liczymy WEWNĄTRZ useMemo. Gdyby `new Date()` stało wyżej, powstawałoby na każdym
  // renderze i zależność zmieniałaby się bez przerwy — a to znaczy zapytanie przy każdym renderze.
  const zakres = useMemo(() => {
    const teraz = new Date();
    const od = new Date(teraz);
    od.setDate(od.getDate() - odDni);
    return { from: od.toISOString().slice(0, 10), to: teraz.toISOString().slice(0, 10) };
  }, [odDni]);

  const { data: anomalie = [], isLoading } = useAnomalies({
    ...zakres,
    status: pokazZamkniete ? undefined : 'New',
  });

  const { data: pracownicy } = useEmployees({ page: 1, pageSize: 500 });
  const nazwiska = useMemo(() => {
    const mapa = new Map<string, string>();
    for (const p of pracownicy?.items ?? []) mapa.set(p.id, `${p.firstName} ${p.lastName}`);
    return mapa;
  }, [pracownicy]);

  const { moze } = useUprawnienia();
  const mozeRozpatrywac = moze('time.manage');

  const przejrzana = useOznaczAnomalieJakoPrzejrzana();
  const odrzuc = useOdrzucAnomalie();

  const otwarte = anomalie.filter((a) => a.status === 'New');

  return (
    <div style={{ padding: '24px 28px', maxWidth: 920, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6, flexWrap: 'wrap' }}>
        <AlertTriangle size={20} style={{ color: colors.primary[600] }} />
        <h1 style={{ fontSize: 22, fontWeight: 800, margin: 0, color: colors.gray[900] }}>Anomalie</h1>

        <select
          value={odDni}
          onChange={(e) => setOdDni(Number(e.target.value))}
          style={{
            marginLeft: 'auto',
            padding: '6px 10px',
            borderRadius: 8,
            border: '1px solid var(--wb-line, #e3e7f1)',
            fontSize: 13,
          }}
        >
          <option value={7}>Ostatnie 7 dni</option>
          <option value={30}>Ostatnie 30 dni</option>
          <option value={90}>Ostatnie 90 dni</option>
        </select>
      </div>

      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 14px', fontSize: 14, maxWidth: '72ch' }}>
        Rozbieżności między grafikiem a rzeczywistą rejestracją czasu, wykrywane co noc.
        Rozpatrzenie ich tutaj sprawia, że znikają z licznika na pulpicie.
      </p>

      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 18, fontSize: 13 }}>
        <input
          type="checkbox"
          checked={pokazZamkniete}
          onChange={(e) => setPokazZamkniete(e.target.checked)}
        />
        Pokaż także rozpatrzone
      </label>

      {isLoading && <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Wczytywanie…</p>}

      {!isLoading && anomalie.length === 0 && (
        <div
          style={{
            display: 'flex',
            gap: 12,
            alignItems: 'flex-start',
            background: 'var(--wb-emr-100, #d1fae5)',
            borderRadius: 12,
            padding: '16px 18px',
          }}
        >
          <CheckCircle2 size={19} style={{ color: 'var(--wb-emr-800, #065f46)', flexShrink: 0, marginTop: 1 }} />
          <div>
            <strong style={{ fontSize: 14.5, color: colors.gray[900] }}>Nic do rozpatrzenia</strong>
            <p style={{ margin: '4px 0 0', fontSize: 13.5, color: 'var(--wb-ink-2, #6b7490)' }}>
              W wybranym okresie nie ma otwartych anomalii w Twoim zakresie danych.
            </p>
          </div>
        </div>
      )}

      {anomalie.length > 0 && (
        <>
          <p style={{ margin: '0 0 10px', fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
            Otwartych: <strong style={{ color: colors.gray[900] }}>{otwarte.length}</strong>
            {pokazZamkniete && ` · wszystkich w okresie: ${anomalie.length}`}
          </p>

          <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 10 }}>
            {anomalie.map((anomalia) => (
              <Wiersz
                key={anomalia.id}
                anomalia={anomalia}
                osoba={nazwiska.get(anomalia.employeeId) ?? '—'}
                mozeRozpatrywac={mozeRozpatrywac}
                zajete={przejrzana.isPending || odrzuc.isPending}
                onPrzejrzana={() => przejrzana.mutate(anomalia.id)}
                onOdrzuc={() => odrzuc.mutate(anomalia.id)}
              />
            ))}
          </ul>
        </>
      )}
    </div>
  );
}

function Wiersz({
  anomalia,
  osoba,
  mozeRozpatrywac,
  zajete,
  onPrzejrzana,
  onOdrzuc,
}: {
  anomalia: TimeAnomalyDto;
  osoba: string;
  mozeRozpatrywac: boolean;
  zajete: boolean;
  onPrzejrzana: () => void;
  onOdrzuc: () => void;
}) {
  const otwarta = anomalia.status === 'New';

  return (
    <li
      style={{
        background: 'var(--wb-panel, #fff)',
        border: '1px solid var(--wb-line, #e3e7f1)',
        borderRadius: 12,
        padding: '13px 15px',
        display: 'flex',
        gap: 12,
        alignItems: 'flex-start',
        opacity: otwarta ? 1 : 0.6,
      }}
    >
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
          <strong style={{ fontSize: 14, color: colors.gray[900] }}>{osoba}</strong>
          <span style={{ fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
            {TYP_OPIS[anomalia.type] ?? anomalia.type}
          </span>
          <span style={{ fontSize: 12.5, color: 'var(--wb-ink-2, #9aa3b8)' }}>
            {new Date(anomalia.date).toLocaleDateString('pl-PL')}
          </span>
          {!otwarta && (
            <span
              style={{
                fontSize: 11,
                fontWeight: 700,
                padding: '2px 8px',
                borderRadius: 999,
                background: 'var(--wb-bg, #f1f4f9)',
                color: 'var(--wb-ink-2, #6b7490)',
              }}
            >
              {anomalia.status === 'Dismissed' ? 'odrzucona' : 'przejrzana'}
            </span>
          )}
        </div>
        {anomalia.description && (
          <p style={{ margin: '4px 0 0', fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
            {anomalia.description}
          </p>
        )}
      </div>

      <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
        {/* Poprawienie ewidencji to osobna czynnosc na karcie czasu — stad odsylacz,
            a nie edycja wpisu w tym miejscu. */}
        <Link
          to={`/time/timesheet?employeeId=${anomalia.employeeId}&date=${anomalia.date}`}
          title="Otwórz kartę czasu"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 4,
            padding: '5px 10px',
            borderRadius: 8,
            border: '1px solid var(--wb-line, #e3e7f1)',
            color: colors.primary[600],
            fontSize: 12,
            fontWeight: 600,
            textDecoration: 'none',
            whiteSpace: 'nowrap',
          }}
        >
          Karta czasu
          <ExternalLink size={12} />
        </Link>

        {mozeRozpatrywac && otwarta && (
          <>
            <button
              onClick={onPrzejrzana}
              disabled={zajete}
              title="Sprawa obejrzana i zamknięta"
              style={stylAkcji(colors.primary[600])}
            >
              <Check size={13} />
              Przejrzane
            </button>
            <button
              onClick={onOdrzuc}
              disabled={zajete}
              title="To nie był problem — urlop, dzień wolny, pomyłka w grafiku"
              style={stylAkcji('var(--wb-ink-2, #6b7490)')}
            >
              <X size={13} />
              To nie problem
            </button>
          </>
        )}
      </div>
    </li>
  );
}

const stylAkcji = (kolor: string): React.CSSProperties => ({
  display: 'inline-flex',
  alignItems: 'center',
  gap: 4,
  padding: '5px 10px',
  borderRadius: 8,
  border: '1px solid var(--wb-line, #e3e7f1)',
  background: 'var(--wb-panel, #fff)',
  color: kolor,
  fontSize: 12,
  fontWeight: 600,
  cursor: 'pointer',
  whiteSpace: 'nowrap',
});
