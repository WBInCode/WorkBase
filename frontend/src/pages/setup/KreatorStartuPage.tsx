import { useMemo, useState } from 'react';
import { Check, ChevronRight, Loader2, Users, Clock, UserCheck, Sparkles } from 'lucide-react';
import {
  useStanKreatora,
  usePracownicyKreatora,
  useZapiszLudzi,
  useZapiszGodziny,
  useZapiszAkceptantow,
  useZakonczKreator,
  type NowaOsoba,
  type Zmiana,
} from '@/api/hooks/useKreatorStartu';
import { odczytajCsv, parseCsv, parsujDateZatrudnienia } from '@/utils/csvParser';
import { colors } from '@/theme/tokens';

/**
 * Kreator pierwszego startu. Projekt: docs/KONFIGURATOR-PIERWSZEGO-STARTU.md.
 *
 * Renderuje się POZA MainLayout (patrz App.tsx) i to nie jest kwestia wyglądu: powłoka
 * odpytuje branding i feature flags, których biała lista bramki nie przepuszcza, więc pod
 * blokadą sypałaby się na 409 przy każdym wejściu.
 *
 * Każde pytanie ma odpowiedź domyślną. „Dalej, Dalej, Dalej, Zacznij" daje działającą firmę
 * jednoosobową w minutę i to jest poprawny scenariusz, nie obejście.
 */
export function KreatorStartuPage() {
  const { data: stan, isLoading } = useStanKreatora();
  const [ekran, setEkran] = useState<number | null>(null);

  const ekranStartowy = useMemo(() => {
    if (!stan) return 0;
    if (!stan.aktualnyKrok) return 0;
    // Wracamy na krok NASTĘPNY po ostatnim zapisanym — stąd wznawialność.
    const indeks = stan.kroki.indexOf(stan.aktualnyKrok);
    return Math.min(indeks + 2, 4);
  }, [stan]);

  const biezacy = ekran ?? ekranStartowy;
  const idz = (nr: number) => setEkran(Math.max(0, Math.min(4, nr)));

  if (isLoading) {
    return (
      <Tlo>
        <Loader2 size={22} style={{ color: colors.primary[600], animation: 'spin 1s linear infinite' }} />
      </Tlo>
    );
  }

  return (
    <Tlo>
      <div
        style={{
          width: '100%',
          maxWidth: 680,
          background: 'var(--wb-panel, #fff)',
          border: '1px solid var(--wb-line, #e3e7f1)',
          borderRadius: 16,
          padding: '26px 28px 30px',
          boxShadow: '0 12px 40px rgba(15, 23, 42, .08)',
        }}
      >
        <Postep biezacy={biezacy} />

        {biezacy === 0 && <EkranPowitalny dalej={() => idz(1)} />}
        {biezacy === 1 && <EkranLudzie dalej={() => idz(2)} />}
        {biezacy === 2 && <EkranGodziny dalej={() => idz(3)} wstecz={() => idz(1)} />}
        {biezacy === 3 && <EkranAkceptanci dalej={() => idz(4)} wstecz={() => idz(2)} />}
        {biezacy === 4 && <EkranPodsumowanie wstecz={() => idz(3)} />}
      </div>
    </Tlo>
  );
}

function Tlo({ children }: { children: React.ReactNode }) {
  return (
    <div
      style={{
        minHeight: '100vh',
        background: 'var(--wb-bg, #f5f7fb)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '24px 16px',
      }}
    >
      {children}
    </div>
  );
}

const ETAPY = [
  { nr: 1, etykieta: 'Ludzie', Ikona: Users },
  { nr: 2, etykieta: 'Godziny', Ikona: Clock },
  { nr: 3, etykieta: 'Akceptacje', Ikona: UserCheck },
];

function Postep({ biezacy }: { biezacy: number }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 22, flexWrap: 'wrap' }}>
      {ETAPY.map(({ nr, etykieta, Ikona }) => {
        const zrobiony = biezacy > nr;
        const aktywny = biezacy === nr;
        return (
          <div key={nr} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 6,
                fontSize: 12.5,
                fontWeight: aktywny ? 700 : 500,
                padding: '5px 11px',
                borderRadius: 999,
                background: zrobiony
                  ? 'var(--wb-emr-100, #d1fae5)'
                  : aktywny
                    ? colors.primary[600]
                    : 'var(--wb-bg, #f1f4f9)',
                color: zrobiony
                  ? 'var(--wb-emr-800, #065f46)'
                  : aktywny
                    ? colors.textOnAccent
                    : 'var(--wb-ink-2, #6b7490)',
              }}
            >
              {zrobiony ? <Check size={13} /> : <Ikona size={13} />}
              {etykieta}
            </span>
            {nr < 3 && <ChevronRight size={13} style={{ color: 'var(--wb-ink-2, #9aa3b8)' }} />}
          </div>
        );
      })}
    </div>
  );
}

function Naglowek({ tytul, opis }: { tytul: string; opis?: string }) {
  return (
    <>
      <h1 style={{ fontSize: 21, fontWeight: 800, margin: '0 0 6px', color: colors.gray[900] }}>{tytul}</h1>
      {opis && (
        <p style={{ margin: '0 0 18px', fontSize: 14, color: 'var(--wb-ink-2, #6b7490)', maxWidth: '62ch' }}>
          {opis}
        </p>
      )}
    </>
  );
}

const stylPrzycisku = (glowny: boolean): React.CSSProperties => ({
  padding: '9px 18px',
  borderRadius: 10,
  border: glowny ? 'none' : '1px solid var(--wb-line, #e3e7f1)',
  background: glowny ? colors.primary[600] : 'var(--wb-panel, #fff)',
  color: glowny ? colors.textOnAccent : 'var(--wb-ink-2, #6b7490)',
  fontSize: 13.5,
  fontWeight: 600,
  cursor: 'pointer',
});

const stylPola: React.CSSProperties = {
  display: 'block',
  width: '100%',
  marginTop: 4,
  padding: '7px 9px',
  borderRadius: 8,
  border: '1px solid var(--wb-line, #e3e7f1)',
  fontFamily: 'inherit',
  fontSize: 14,
};

function Stopka({ children }: { children: React.ReactNode }) {
  return <div style={{ display: 'flex', gap: 10, marginTop: 22, flexWrap: 'wrap' }}>{children}</div>;
}

// ---------------------------------------------------------------- ekran 0

function EkranPowitalny({ dalej }: { dalej: () => void }) {
  return (
    <>
      <Naglowek
        tytul="Witamy w WorkBase"
        opis="Zajmie 5–10 minut. Zadamy trzy pytania, a resztę ustawimy za Ciebie. Możesz przerwać w dowolnym momencie — wrócimy tu przy następnym logowaniu."
      />
      <p style={{ fontSize: 13.5, color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 4px' }}>
        Firma jest już gotowa do pracy: typy urlopów, statusy zadań i obiegi akceptacji czekają
        z domyślnymi ustawieniami. Kreator służy do ich potwierdzenia, nie do włączenia systemu.
      </p>
      <Stopka>
        <button onClick={dalej} style={stylPrzycisku(true)}>Zaczynamy</button>
      </Stopka>
    </>
  );
}

// ---------------------------------------------------------------- ekran 1

type TrybLudzi = 'plik' | 'recznie' | 'tylkoJa';

function EkranLudzie({ dalej }: { dalej: () => void }) {
  const zapisz = useZapiszLudzi();
  const [tryb, setTryb] = useState<TrybLudzi>('plik');
  const [osoby, setOsoby] = useState<NowaOsoba[]>([]);
  const [odrzucone, setOdrzucone] = useState<string[]>([]);
  const [zaprosicTeraz, setZaprosicTeraz] = useState(false);
  const [blad, setBlad] = useState<string | null>(null);

  const wczytajPlik = async (plik: File) => {
    setBlad(null);
    setOdrzucone([]);
    try {
      const { headers, rows } = parseCsv(await odczytajCsv(plik));
      const kolumna = (...nazwy: string[]) =>
        headers.findIndex((h) => nazwy.includes(h.trim().toLowerCase().replace(/[_-]/g, ' ')));

      const iImie = kolumna('imię', 'imie', 'first name', 'firstname');
      const iNazwisko = kolumna('nazwisko', 'last name', 'lastname');
      const iEmail = kolumna('email', 'e mail', 'adres email');
      const iNumer = kolumna('numer', 'nr', 'employee number', 'numer pracownika');
      const iData = kolumna('data zatrudnienia', 'zatrudniony od', 'hire date');

      if (iImie < 0 || iNazwisko < 0 || iEmail < 0) {
        setBlad(
          'Nie znalazłem kolumn „imię", „nazwisko" i „email". Dodaj osoby ręcznie albo użyj pełnego importu w Ustawieniach po zakończeniu kreatora.',
        );
        return;
      }

      const dobre: NowaOsoba[] = [];
      const zle: string[] = [];
      rows.forEach((wiersz, i) => {
        const email = (wiersz[iEmail] ?? '').trim();
        const imie = (wiersz[iImie] ?? '').trim();
        const nazwisko = (wiersz[iNazwisko] ?? '').trim();
        if (!email || !imie || !nazwisko) {
          zle.push(`Wiersz ${i + 2}: brak imienia, nazwiska albo adresu e-mail.`);
          return;
        }
        const dataTekst = iData >= 0 ? (wiersz[iData] ?? '').trim() : '';
        const data = dataTekst ? parsujDateZatrudnienia(dataTekst) : null;
        if (dataTekst && !data) {
          zle.push(`Wiersz ${i + 2}: nie rozumiem daty „${dataTekst}".`);
          return;
        }
        dobre.push({
          imie,
          nazwisko,
          email,
          numer: iNumer >= 0 ? (wiersz[iNumer] ?? '').trim() || null : null,
          dataZatrudnienia: data ? data.toISOString() : null,
        });
      });

      setOsoby(dobre);
      setOdrzucone(zle);
    } catch {
      setBlad('Nie udało się odczytać pliku. Sprawdź, czy to CSV.');
    }
  };

  const wyslij = async (jakoTylkoJa: boolean) => {
    setBlad(null);
    try {
      await zapisz.mutateAsync({
        pracownicy: jakoTylkoJa ? [] : osoby,
        zaprosicTeraz: jakoTylkoJa ? false : zaprosicTeraz,
      });
      dalej();
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zapisać.');
    }
  };

  return (
    <>
      <Naglowek tytul="Kto tu pracuje?" opis="Możesz wgrać plik z kadr, dopisać osoby ręcznie albo zacząć w pojedynkę i dodać resztę później." />

      <div style={{ display: 'flex', gap: 8, marginBottom: 16, flexWrap: 'wrap' }}>
        {([['plik', 'Wgraj plik'], ['recznie', 'Dopisz ręcznie'], ['tylkoJa', 'Na razie tylko ja']] as const).map(
          ([wartosc, etykieta]) => (
            <button
              key={wartosc}
              onClick={() => setTryb(wartosc)}
              style={{
                ...stylPrzycisku(tryb === wartosc),
                padding: '7px 14px',
                fontSize: 13,
              }}
            >
              {etykieta}
            </button>
          ),
        )}
      </div>

      {tryb === 'plik' && (
        <div style={{ display: 'grid', gap: 10 }}>
          <input
            type="file"
            accept=".csv,text/csv"
            onChange={(e) => {
              const plik = e.target.files?.[0];
              if (plik) void wczytajPlik(plik);
            }}
            style={{ fontSize: 13.5 }}
          />
          <p style={{ margin: 0, fontSize: 12.5, color: 'var(--wb-ink-2, #6b7490)' }}>
            Potrzebne kolumny: imię, nazwisko, email. Opcjonalnie numer i data zatrudnienia.
            Pliki z Symfonii, Optimy i Excela (Windows-1250) czytamy poprawnie.
          </p>
          {osoby.length > 0 && (
            <p style={{ margin: 0, fontSize: 13.5, color: colors.gray[900] }}>
              Wejdzie <strong>{osoby.length}</strong> osób
              {odrzucone.length > 0 && <>, odpadnie <strong>{odrzucone.length}</strong></>}.
            </p>
          )}
          {odrzucone.length > 0 && (
            <ul style={{ margin: 0, paddingLeft: 18, fontSize: 12.5, color: colors.danger[600] }}>
              {odrzucone.slice(0, 6).map((powod) => <li key={powod}>{powod}</li>)}
              {odrzucone.length > 6 && <li>…i {odrzucone.length - 6} więcej</li>}
            </ul>
          )}
        </div>
      )}

      {tryb === 'recznie' && <ListaReczna osoby={osoby} onZmiana={setOsoby} />}

      {tryb === 'tylkoJa' && (
        <p style={{ fontSize: 13.5, color: 'var(--wb-ink-2, #6b7490)', margin: 0, maxWidth: '62ch' }}>
          W porządku. Firma będzie działać dla Ciebie jednego, a pracowników dodasz w Ustawieniach,
          kiedy będziesz gotowy.
        </p>
      )}

      {tryb !== 'tylkoJa' && osoby.length > 0 && (
        <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 14, fontSize: 13 }}>
          <input type="checkbox" checked={zaprosicTeraz} onChange={(e) => setZaprosicTeraz(e.target.checked)} />
          Wyślij zaproszenia do logowania od razu
          <span style={{ color: 'var(--wb-ink-2, #6b7490)' }}>
            (domyślnie nie — najpierw sprawdź listę)
          </span>
        </label>
      )}

      {blad && <p style={{ margin: '12px 0 0', fontSize: 13, color: colors.danger[600] }}>{blad}</p>}

      <Stopka>
        <button
          onClick={() => void wyslij(tryb === 'tylkoJa')}
          disabled={zapisz.isPending || (tryb !== 'tylkoJa' && osoby.length === 0)}
          style={{
            ...stylPrzycisku(true),
            opacity: zapisz.isPending || (tryb !== 'tylkoJa' && osoby.length === 0) ? 0.55 : 1,
          }}
        >
          {zapisz.isPending ? 'Zapisywanie…' : 'Dalej'}
        </button>
      </Stopka>
    </>
  );
}

function ListaReczna({ osoby, onZmiana }: { osoby: NowaOsoba[]; onZmiana: (o: NowaOsoba[]) => void }) {
  const zmien = (i: number, pole: keyof NowaOsoba, wartosc: string) =>
    onZmiana(osoby.map((o, idx) => (idx === i ? { ...o, [pole]: wartosc } : o)));

  return (
    <div style={{ display: 'grid', gap: 10 }}>
      {osoby.map((osoba, i) => (
        <div key={i} style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))', gap: 8 }}>
          <input placeholder="Imię" value={osoba.imie} onChange={(e) => zmien(i, 'imie', e.target.value)} style={stylPola} />
          <input placeholder="Nazwisko" value={osoba.nazwisko} onChange={(e) => zmien(i, 'nazwisko', e.target.value)} style={stylPola} />
          <input placeholder="E-mail" value={osoba.email} onChange={(e) => zmien(i, 'email', e.target.value)} style={stylPola} />
        </div>
      ))}
      <button
        onClick={() => onZmiana([...osoby, { imie: '', nazwisko: '', email: '' }])}
        style={{ ...stylPrzycisku(false), justifySelf: 'start', padding: '6px 12px', fontSize: 13 }}
      >
        Dodaj osobę
      </button>
    </div>
  );
}

// ---------------------------------------------------------------- ekran 2

const DNI = ['Pn', 'Wt', 'Śr', 'Cz', 'Pt', 'So', 'Nd'];

function EkranGodziny({ dalej, wstecz }: { dalej: () => void; wstecz: () => void }) {
  const zapisz = useZapiszGodziny();
  const [zmianowo, setZmianowo] = useState(false);
  const [dni, setDni] = useState<number[]>([1, 2, 3, 4, 5]);
  const [minutPrzerwy, setMinutPrzerwy] = useState(30);
  const [zmiany, setZmiany] = useState<Zmiana[]>([
    { nazwa: 'Podstawowa 8:00-16:00', od: '08:00', do: '16:00' },
  ]);
  const [blad, setBlad] = useState<string | null>(null);

  const przelaczDzien = (nr: number) =>
    setDni((p) => (p.includes(nr) ? p.filter((d) => d !== nr) : [...p, nr].sort()));

  const wyslij = async () => {
    setBlad(null);
    try {
      await zapisz.mutateAsync({
        zmiany: zmiany.map((z) => ({ nazwa: z.nazwa, od: `${z.od}:00`, do: `${z.do}:00` })),
        dniTygodnia: dni,
        minutPrzerwy,
        przerwaPlatna: false,
      });
      dalej();
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zapisać.');
    }
  };

  return (
    <>
      <Naglowek tytul="W jakich godzinach pracujecie?" opis="To ustawia normę dobową i szablon grafiku. Wszystko da się później zmienić w Ustawieniach." />

      <div style={{ display: 'grid', gap: 14 }}>
        <div>
          <span style={{ fontSize: 13, color: colors.gray[900] }}>Dni robocze</span>
          <div style={{ display: 'flex', gap: 6, marginTop: 6, flexWrap: 'wrap' }}>
            {DNI.map((etykieta, i) => {
              const nr = i + 1;
              const wybrany = dni.includes(nr);
              return (
                <button
                  key={nr}
                  onClick={() => przelaczDzien(nr)}
                  style={{
                    ...stylPrzycisku(wybrany),
                    padding: '6px 12px',
                    fontSize: 13,
                    minWidth: 46,
                  }}
                >
                  {etykieta}
                </button>
              );
            })}
          </div>
        </div>

        <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13.5 }}>
          <input
            type="checkbox"
            checked={zmianowo}
            onChange={(e) => {
              setZmianowo(e.target.checked);
              setZmiany(
                e.target.checked
                  ? [
                      { nazwa: 'I zmiana 6:00-14:00', od: '06:00', do: '14:00' },
                      { nazwa: 'II zmiana 14:00-22:00', od: '14:00', do: '22:00' },
                    ]
                  : [{ nazwa: 'Podstawowa 8:00-16:00', od: '08:00', do: '16:00' }],
              );
            }}
          />
          Pracujemy na zmiany
        </label>

        {zmiany.map((zmiana, i) => (
          <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 110px 110px', gap: 8, alignItems: 'end' }}>
            <label style={{ fontSize: 13, color: colors.gray[900] }}>
              Nazwa
              <input
                value={zmiana.nazwa}
                onChange={(e) => setZmiany(zmiany.map((z, idx) => (idx === i ? { ...z, nazwa: e.target.value } : z)))}
                style={stylPola}
              />
            </label>
            <label style={{ fontSize: 13, color: colors.gray[900] }}>
              Od
              <input
                type="time"
                value={zmiana.od}
                onChange={(e) => setZmiany(zmiany.map((z, idx) => (idx === i ? { ...z, od: e.target.value } : z)))}
                style={stylPola}
              />
            </label>
            <label style={{ fontSize: 13, color: colors.gray[900] }}>
              Do
              <input
                type="time"
                value={zmiana.do}
                onChange={(e) => setZmiany(zmiany.map((z, idx) => (idx === i ? { ...z, do: e.target.value } : z)))}
                style={stylPola}
              />
            </label>
          </div>
        ))}

        {zmianowo && zmiany.length < 3 && (
          <button
            onClick={() => setZmiany([...zmiany, { nazwa: 'III zmiana 22:00-06:00', od: '22:00', do: '23:59' }])}
            style={{ ...stylPrzycisku(false), justifySelf: 'start', padding: '6px 12px', fontSize: 13 }}
          >
            Dodaj zmianę
          </button>
        )}

        <label style={{ fontSize: 13, color: colors.gray[900], maxWidth: 220 }}>
          Przerwa niepłatna (minuty)
          <input
            type="number"
            min={0}
            max={120}
            value={minutPrzerwy}
            onChange={(e) => setMinutPrzerwy(Number(e.target.value))}
            style={stylPola}
          />
        </label>
      </div>

      {blad && <p style={{ margin: '12px 0 0', fontSize: 13, color: colors.danger[600] }}>{blad}</p>}

      <Stopka>
        <button onClick={() => void wyslij()} disabled={zapisz.isPending} style={stylPrzycisku(true)}>
          {zapisz.isPending ? 'Zapisywanie…' : 'Dalej'}
        </button>
        <button onClick={wstecz} style={stylPrzycisku(false)}>Wstecz</button>
      </Stopka>
    </>
  );
}

// ---------------------------------------------------------------- ekran 3

function EkranAkceptanci({ dalej, wstecz }: { dalej: () => void; wstecz: () => void }) {
  const { data: osoby = [], isLoading } = usePracownicyKreatora();
  const zapisz = useZapiszAkceptantow();
  const [akceptantId, setAkceptantId] = useState('');
  const [blad, setBlad] = useState<string | null>(null);

  const wyslij = async (pomin: boolean) => {
    setBlad(null);
    try {
      await zapisz.mutateAsync({
        akceptantId: pomin ? null : akceptantId || null,
        pracownicyIds: pomin ? [] : osoby.map((o) => o.id),
      });
      dalej();
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zapisać.');
    }
  };

  return (
    <>
      <Naglowek
        tytul="Kto akceptuje wnioski?"
        opis="Wnioski urlopowe i pozostałe trafiają do przełożonego. Na start wystarczy jedna osoba dla wszystkich — hierarchię rozbudujesz później."
      />

      {isLoading ? (
        <p style={{ fontSize: 13.5, color: 'var(--wb-ink-2, #6b7490)' }}>Wczytywanie…</p>
      ) : osoby.length === 0 ? (
        <p style={{ fontSize: 13.5, color: 'var(--wb-ink-2, #6b7490)', maxWidth: '62ch' }}>
          W firmie nie ma jeszcze innych osób, więc nie ma komu ustawiać przełożonego. Ten krok
          możesz pominąć i wrócić do niego, gdy dodasz pracowników.
        </p>
      ) : (
        <label style={{ fontSize: 13, color: colors.gray[900], display: 'block', maxWidth: 360 }}>
          Wnioski wszystkich trafiają do
          <select value={akceptantId} onChange={(e) => setAkceptantId(e.target.value)} style={stylPola}>
            <option value="">— wybierz osobę —</option>
            {osoby.map((o) => (
              <option key={o.id} value={o.id}>{o.imie} {o.nazwisko}</option>
            ))}
          </select>
        </label>
      )}

      {blad && <p style={{ margin: '12px 0 0', fontSize: 13, color: colors.danger[600] }}>{blad}</p>}

      <Stopka>
        <button
          onClick={() => void wyslij(false)}
          disabled={zapisz.isPending || osoby.length === 0 || !akceptantId}
          style={{
            ...stylPrzycisku(true),
            opacity: zapisz.isPending || osoby.length === 0 || !akceptantId ? 0.55 : 1,
          }}
        >
          {zapisz.isPending ? 'Zapisywanie…' : 'Dalej'}
        </button>
        <button onClick={() => void wyslij(true)} style={stylPrzycisku(false)}>Pomiń na razie</button>
        <button onClick={wstecz} style={stylPrzycisku(false)}>Wstecz</button>
      </Stopka>
    </>
  );
}

// ---------------------------------------------------------------- ekran 4

/**
 * Ten ekran jest ważniejszy, niż wygląda. Nietechniczny właściciel nie wie, czego nie wie —
 * lista „ustawiliśmy za Ciebie" buduje zaufanie i uczy, że te rzeczy w ogóle są konfigurowalne.
 * Bez niej za trzy miesiące przychodzi zgłoszenie „a da się zmienić liczbę dni urlopu?".
 */
const USTAWIONE_ZA_CIEBIE = [
  'Urlop wypoczynkowy, na żądanie, zwolnienie lekarskie i opieka nad dzieckiem',
  'Statusy i priorytety zadań',
  'Obieg akceptacji wniosków urlopowych i wniosków ogólnych',
  'Struktura firmy z jednostką główną',
  'Role: administrator, kierownik, pracownik, kadry',
];

function EkranPodsumowanie({ wstecz }: { wstecz: () => void }) {
  const zakoncz = useZakonczKreator();
  const [blad, setBlad] = useState<string | null>(null);

  const zacznij = async () => {
    setBlad(null);
    try {
      await zakoncz.mutateAsync();
      // Twarde przejście: powłoka aplikacji nie była dotąd montowana pod blokadą, więc
      // najprościej wejść do niej od zera, z czystym stanem zapytań.
      window.location.assign('/');
    } catch (e) {
      setBlad(e instanceof Error ? e.message : 'Nie udało się zakończyć konfiguracji.');
    }
  };

  return (
    <>
      <Naglowek tytul="Gotowe" opis="Firma jest skonfigurowana. Poniższe rzeczy ustawiliśmy za Ciebie — każdą zmienisz w Ustawieniach." />

      <ul style={{ margin: '0 0 4px', padding: 0, listStyle: 'none', display: 'grid', gap: 8 }}>
        {USTAWIONE_ZA_CIEBIE.map((pozycja) => (
          <li key={pozycja} style={{ display: 'flex', gap: 8, alignItems: 'flex-start', fontSize: 13.5 }}>
            <Check size={15} style={{ color: 'var(--wb-emr-800, #065f46)', flexShrink: 0, marginTop: 2 }} />
            <span style={{ color: colors.gray[900] }}>{pozycja}</span>
          </li>
        ))}
      </ul>

      {blad && <p style={{ margin: '12px 0 0', fontSize: 13, color: colors.danger[600] }}>{blad}</p>}

      <Stopka>
        <button
          onClick={() => void zacznij()}
          disabled={zakoncz.isPending}
          style={{ ...stylPrzycisku(true), display: 'inline-flex', alignItems: 'center', gap: 7 }}
        >
          <Sparkles size={15} />
          {zakoncz.isPending ? 'Kończenie…' : 'Zacznij pracę'}
        </button>
        <button onClick={wstecz} style={stylPrzycisku(false)}>Wstecz</button>
      </Stopka>
    </>
  );
}
