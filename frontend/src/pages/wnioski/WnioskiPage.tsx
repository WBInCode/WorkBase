import { useMemo, useState } from 'react';
import { FileText, Plus, X } from 'lucide-react';
import {
  useTypyWnioskow,
  useMojeWnioski,
  useZlozWniosek,
  useAnulujWniosek,
  type StatusWniosku,
  type Wniosek,
} from '@/api/hooks/useWnioski';
import { FormularzWniosku } from '@/components/Wnioski';
import { colors } from '@/theme/tokens';

const STATUS: Record<StatusWniosku, { etykieta: string; tlo: string; kolor: string }> = {
  Oczekuje: { etykieta: 'Oczekuje', tlo: colors.warning[100], kolor: colors.warning[800] },
  Zaakceptowany: { etykieta: 'Zaakceptowany', tlo: 'var(--wb-emr-100, #d1fae5)', kolor: 'var(--wb-emr-800, #065f46)' },
  Odrzucony: { etykieta: 'Odrzucony', tlo: colors.danger[50], kolor: colors.danger[600] },
  Anulowany: { etykieta: 'Wycofany', tlo: colors.gray[100], kolor: colors.gray[500] },
};

export function WnioskiPage() {
  const { data: typy = [] } = useTypyWnioskow();
  const { data: wnioski = [], isLoading } = useMojeWnioski();
  const zloz = useZlozWniosek();
  const anuluj = useAnulujWniosek();

  const [formularzOtwarty, setFormularzOtwarty] = useState(false);
  const [typId, setTypId] = useState('');
  const [wartosci, setWartosci] = useState<Record<string, string | null>>({});
  const [blad, setBlad] = useState<string | null>(null);

  const wybranyTyp = useMemo(() => typy.find((t) => t.id === typId), [typy, typId]);

  const wybierzTyp = (id: string) => {
    setTypId(id);
    setWartosci({});
    setBlad(null);
  };

  const wyslij = async () => {
    if (!wybranyTyp) return;
    setBlad(null);
    try {
      await zloz.mutateAsync({ typWnioskuId: wybranyTyp.id, wartosci });
      setFormularzOtwarty(false);
      setTypId('');
      setWartosci({});
    } catch (e) {
      // Serwer zwraca wszystkie bledy formularza naraz — pokazujemy je bez skracania,
      // zeby pracownik poprawil wszystko za jednym podejsciem.
      setBlad(e instanceof Error ? e.message : 'Nie udało się złożyć wniosku.');
    }
  };

  return (
    <div style={{ padding: '24px 28px', maxWidth: 900, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6, flexWrap: 'wrap' }}>
        <FileText size={20} style={{ color: colors.primary[600] }} />
        <h1 style={{ fontSize: 22, fontWeight: 800, margin: 0, color: colors.gray[900] }}>Wnioski</h1>

        {typy.length > 0 && (
          <button
            onClick={() => { setFormularzOtwarty((v) => !v); setBlad(null); }}
            style={{
              marginLeft: 'auto', display: 'inline-flex', alignItems: 'center', gap: 6,
              padding: '8px 14px', borderRadius: 10, border: 'none',
              background: colors.primary[600], color: colors.textOnAccent,
              fontSize: 13, fontWeight: 600, cursor: 'pointer',
            }}
          >
            {formularzOtwarty ? <X size={15} /> : <Plus size={15} />}
            {formularzOtwarty ? 'Anuluj' : 'Złóż wniosek'}
          </button>
        )}
      </div>

      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 20px', fontSize: 14, maxWidth: '70ch' }}>
        Wnioski trafiają do Twojego przełożonego tą samą drogą co wnioski urlopowe. Rodzaje wniosków
        ustala firma.
      </p>

      {typy.length === 0 && (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14, maxWidth: '70ch' }}>
          Firma nie zdefiniowała jeszcze żadnego rodzaju wniosku. Zrobi to administrator
          w ustawieniach.
        </p>
      )}

      {formularzOtwarty && (
        <div
          style={{
            background: 'var(--wb-panel, #fff)',
            border: '1px solid var(--wb-line, #e3e7f1)',
            borderRadius: 14,
            padding: '16px 18px',
            marginBottom: 18,
            display: 'grid',
            gap: 12,
          }}
        >
          <label style={{ fontSize: 13, color: colors.gray[900] }}>
            Rodzaj wniosku
            <select
              value={typId}
              onChange={(e) => wybierzTyp(e.target.value)}
              style={{
                display: 'block', width: '100%', marginTop: 4, padding: '7px 9px',
                borderRadius: 8, border: '1px solid var(--wb-line, #e3e7f1)', fontSize: 14,
              }}
            >
              <option value="">— wybierz —</option>
              {typy.map((t) => (
                <option key={t.id} value={t.id}>{t.nazwa}</option>
              ))}
            </select>
          </label>

          {wybranyTyp && (
            <>
              {wybranyTyp.opis && (
                <p style={{ margin: 0, fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
                  {wybranyTyp.opis}
                </p>
              )}

              <FormularzWniosku
                pola={wybranyTyp.pola}
                wartosci={wartosci}
                onZmiana={(kod, wartosc) => setWartosci((p) => ({ ...p, [kod]: wartosc }))}
              />

              {!wybranyTyp.wymagaAkceptacji && (
                <p style={{ margin: 0, fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)' }}>
                  Ten wniosek nie wymaga akceptacji — zostanie od razu zarejestrowany.
                </p>
              )}

              {blad && <p style={{ margin: 0, fontSize: 13, color: colors.danger[600] }}>{blad}</p>}

              <button
                onClick={wyslij}
                disabled={zloz.isPending}
                style={{
                  justifySelf: 'start', padding: '8px 16px', borderRadius: 8, border: 'none',
                  background: colors.primary[600], color: colors.textOnAccent,
                  fontSize: 13, fontWeight: 600, cursor: zloz.isPending ? 'wait' : 'pointer',
                }}
              >
                {zloz.isPending ? 'Wysyłanie…' : 'Wyślij wniosek'}
              </button>
            </>
          )}
        </div>
      )}

      {isLoading ? (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Wczytywanie…</p>
      ) : wnioski.length === 0 ? (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Nie złożyłeś jeszcze żadnego wniosku.</p>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 10 }}>
          {wnioski.map((w) => (
            <KartaWniosku key={w.id} wniosek={w} onAnuluj={() => anuluj.mutate(w.id)} />
          ))}
        </ul>
      )}
    </div>
  );
}

function KartaWniosku({ wniosek, onAnuluj }: { wniosek: Wniosek; onAnuluj: () => void }) {
  const status = STATUS[wniosek.status];
  const wpisy = Object.entries(wniosek.wartosci).filter(([, v]) => v !== null && v !== '');

  return (
    <li
      style={{
        background: 'var(--wb-panel, #fff)',
        border: '1px solid var(--wb-line, #e3e7f1)',
        borderRadius: 12,
        padding: '13px 15px',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
        <strong style={{ fontSize: 14, color: colors.gray[900] }}>{wniosek.typNazwa}</strong>
        <span
          style={{
            fontSize: 11, fontWeight: 700, padding: '2px 9px', borderRadius: 999,
            background: status.tlo, color: status.kolor,
          }}
        >
          {status.etykieta}
        </span>
        <span style={{ fontSize: 12, color: 'var(--wb-ink-2, #6b7490)' }}>
          złożony {new Date(wniosek.zlozonyO).toLocaleDateString('pl-PL')}
        </span>

        {wniosek.status === 'Oczekuje' && (
          <button
            onClick={onAnuluj}
            style={{
              marginLeft: 'auto', padding: '4px 10px', borderRadius: 8,
              border: '1px solid var(--wb-line, #e3e7f1)', background: 'var(--wb-panel, #fff)',
              color: 'var(--wb-ink-2, #6b7490)', fontSize: 12, cursor: 'pointer',
            }}
          >
            Wycofaj
          </button>
        )}
      </div>

      {wpisy.length > 0 && (
        <div style={{ marginTop: 7, fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)' }}>
          {wpisy.map(([kod, wartosc]) => `${kod}: ${wartosc}`).join(' · ')}
        </div>
      )}
    </li>
  );
}
