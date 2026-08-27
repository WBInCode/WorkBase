import { Bell } from 'lucide-react';
import {
  usePreferencjePowiadomien,
  useZapiszPreferencje,
} from '@/api/hooks/useNotifications';
import { colors } from '@/theme/tokens';

/**
 * Które powiadomienia chcę dostawać.
 *
 * Encja, repozytorium i endpointy istniały od dawna, ale `SendAsync` nigdy ich nie czytał —
 * system wysyłał wszystko wszystkim niezależnie od ustawień, a ekranu do ich zmiany nie było.
 *
 * Ustawienia są **osobiste**: serwer wyprowadza konto z tokenu, więc nie da się zapytać
 * o cudze ani ich zmienić. Wcześniej identyfikator szedł od klienta i dało się wyciszyć
 * powiadomienia komuś innemu.
 *
 * Brak wiersza znaczy „wysyłaj" — dlatego przełącznik domyślnie jest włączony, a wyłączenie
 * dopiero tworzy wpis.
 */
const KATEGORIE: { kod: string; nazwa: string; opis: string }[] = [
  {
    kod: 'task_assigned',
    nazwa: 'Przydzielone zadanie',
    opis: 'Ktoś przypisał Ci zadanie albo przepisał je na Ciebie.',
  },
  {
    kod: 'task_overdue',
    nazwa: 'Zadanie po terminie',
    opis: 'Twoje zadanie przekroczyło termin. Raz na zadanie, nie codziennie.',
  },
  {
    kod: 'anomaly_detected',
    nazwa: 'Anomalia czasu pracy',
    opis: 'Rozbieżność między grafikiem a rejestracją u osoby z Twojego zespołu.',
  },
  {
    kod: 'termin_zbliza',
    nazwa: 'Zbliżający się termin',
    opis: 'Badania, szkolenie BHP, uprawnienie albo koniec umowy dobiegają końca.',
  },
  {
    kod: 'termin_minal',
    nazwa: 'Termin minął',
    opis: 'Termin z powyższych upłynął.',
  },
  {
    kod: 'escalation',
    nazwa: 'Wniosek czeka za długo',
    opis: 'Sprawa stoi u Ciebie dłużej, niż ustaliła firma.',
  },
];

export function PreferencjePowiadomienPage() {
  const { data: preferencje = [], isLoading } = usePreferencjePowiadomien();
  const zapisz = useZapiszPreferencje();

  const wlaczona = (kod: string) => {
    const wpis = preferencje.find((p) => p.category === kod);
    // Brak wpisu = wysylamy. Odwrotna domyslka uciszylaby powiadomienia wszystkim,
    // ktorzy nigdy tu nie zajrzeli.
    return wpis ? wpis.inApp : true;
  };

  const przelacz = (kod: string) => {
    const wpis = preferencje.find((p) => p.category === kod);
    zapisz.mutate({
      category: kod,
      inApp: wpis ? !wpis.inApp : false,
      email: wpis?.email ?? false,
    });
  };

  return (
    <div style={{ padding: '24px 28px', maxWidth: 760, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
        <Bell size={20} style={{ color: colors.primary[600] }} />
        <h1 style={{ fontSize: 22, fontWeight: 800, margin: 0, color: colors.gray[900] }}>
          Moje powiadomienia
        </h1>
      </div>

      <p style={{ color: 'var(--wb-ink-2, #6b7490)', margin: '0 0 22px', fontSize: 14, maxWidth: '70ch' }}>
        Wyłącz to, czego nie chcesz dostawać. Ustawienia są Twoje — nikt inny ich nie widzi
        ani nie zmienia. Treść samych powiadomień ustala firma w Ustawieniach.
      </p>

      {isLoading ? (
        <p style={{ color: 'var(--wb-ink-2, #6b7490)', fontSize: 14 }}>Wczytywanie…</p>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 10 }}>
          {KATEGORIE.map((kategoria) => {
            const wlaczone = wlaczona(kategoria.kod);
            return (
              <li
                key={kategoria.kod}
                style={{
                  background: 'var(--wb-panel, #fff)',
                  border: '1px solid var(--wb-line, #e3e7f1)',
                  borderRadius: 12,
                  padding: '13px 15px',
                  display: 'flex',
                  gap: 12,
                  alignItems: 'flex-start',
                }}
              >
                <div style={{ flex: 1, minWidth: 0 }}>
                  <strong style={{ fontSize: 14, color: colors.gray[900] }}>{kategoria.nazwa}</strong>
                  <p style={{ margin: '3px 0 0', fontSize: 13, color: 'var(--wb-ink-2, #6b7490)' }}>
                    {kategoria.opis}
                  </p>
                </div>

                <label style={{ display: 'inline-flex', alignItems: 'center', gap: 7, fontSize: 12.5, whiteSpace: 'nowrap' }}>
                  <input
                    type="checkbox"
                    checked={wlaczone}
                    disabled={zapisz.isPending}
                    onChange={() => przelacz(kategoria.kod)}
                  />
                  <span style={{ color: wlaczone ? colors.gray[900] : 'var(--wb-ink-2, #9aa3b8)' }}>
                    {wlaczone ? 'dostaję' : 'wyciszone'}
                  </span>
                </label>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
