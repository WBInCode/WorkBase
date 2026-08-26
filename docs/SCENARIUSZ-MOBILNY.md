# Ścieżka mobilna pracownika — scenariusz do przejechania

Do wykonania na **realnym Androidzie i realnym iPhonie**. Zwężenie okna przeglądarki niczego nie dowodzi: responsywność jest tu realizowana w JavaScripcie (`useIsMobile` w 38 plikach), a nie w CSS, więc zachowanie na urządzeniu może się różnić od zachowania w DevTools.

Adres: **https://workbase.wb-partners.pl**

Pracownik produkcyjny albo terenowy generuje wpisy czasu pracy i nie zaloguje się z komputera. To jest ta ścieżka.

---

## Zanim zaczniesz: czego NIE testujemy, bo tego nie ma

Trzy rzeczy trzeba wiedzieć przed obiecaniem czegokolwiek klientowi.

**Powiadomienia push nie działają i nie zadziałają w tej wersji.** `IPushNotificationService` (FCM) jest w kodzie, ale **nie ma ani jednego wywołania** — to martwy kod. Dodatkowo frontend mówi VAPID/WebPush, a backend FCM, więc nawet po podpięciu te dwie strony by się nie spotkały. **Na telefonie z zamkniętą aplikacją powiadomienie nie przyjdzie.**

**Powiadomienia w aplikacji chodzą odpytywaniem co 15–30 sekund**, nie w czasie rzeczywistym. Klienta SignalR w ogóle nie ma we froncie (brak `@microsoft/signalr` w zależnościach), choć backend wystawia hub. Przy otwartej aplikacji powiadomienie pojawi się z opóźnieniem do pół minuty — i to jest poprawne zachowanie, nie usterka do zgłoszenia.

**iOS nigdy nie pokazuje automatycznego monitu instalacji.** To decyzja Apple, nie usterka WorkBase: Safari nie ma odpowiednika `beforeinstallprompt`. Jedyna droga to ręczne **Udostępnij → Do ekranu początkowego**. Obserwacja „w tej rodzinie produktów iOS notorycznie nie pokazuje promptu" opisuje więc normalne zachowanie systemu.

Co natomiast było zepsute i zostało naprawione 2026-08-26: **wszystkie ikony PWA były w SVG**, a Safari ignoruje `apple-touch-icon` w tym formacie. Po ręcznym dodaniu do ekranu początkowego iPhone pokazywał ikonę zastępczą. Teraz są PNG (192, 512 i osobna 180×180 bez kanału alfa dla Apple). **To jest do sprawdzenia w kroku 1 na iPhonie.**

---

## Krok 1 — instalacja

| | Android (Chrome) | iOS (Safari) |
|---|---|---|
| Jak | monit instalacji albo menu → „Zainstaluj aplikację" | Udostępnij → Do ekranu początkowego |
| Czego szukamy | czy monit w ogóle się pojawia | **czy ikona to granatowy kwadrat z białym „W"** |
| Po instalacji | uruchamia się bez paska adresu (`display: standalone`) | to samo |

⚠️ Na iPhonie ikona zastępcza (biała kartka albo zrzut strony) oznacza, że poprawka ikon nie zadziałała — zanotuj i zrób zdjęcie.

Uwaga przy powtórnym teście: iOS potrafi trzymać starą ikonę w pamięci. Usuń z ekranu początkowego, zamknij Safari całkowicie, dodaj ponownie.

## Krok 2 — logowanie SSO

Zaloguj się kontem pracownika przez Hub. Sprawdź:

- czy przekierowanie do Huba i z powrotem kończy się w aplikacji, a nie na białym ekranie,
- czy po powrocie jesteś zalogowany (a nie wyrzucony na ekran logowania),
- **czy zamknięcie i ponowne otwarcie aplikacji nie wymaga logowania od nowa**.

Ostatni punkt jest najważniejszy: pracownik otwiera aplikację kilka razy dziennie i logowanie za każdym razem przekreśla sens instalacji.

## Krok 3 — rejestracja czasu pracy

- Rozpocznij dzień (clock-in). Czy przycisk jest widoczny bez przewijania?
- Rozpocznij i zakończ przerwę.
- Zakończ dzień.
- Sprawdź kartę czasu — czy wpisy są na miejscu i czy godziny się zgadzają.

⚠️ Jeśli przycisku rejestracji nie ma w ogóle, to najczęściej **brak kartoteki pracownika** przy koncie, a nie usterka mobilna — sprawdź `/admin/gotowosc` na komputerze.

## Krok 4 — wniosek urlopowy

Złóż wniosek z telefonu. Sprawdź, czy da się wybrać daty (kontrolki dat bywają najsłabszym punktem na mobile) i czy wniosek trafia na listę.

## Krok 5 — powiadomienie

Z drugiego konta (na komputerze) zaakceptuj ten wniosek. Na telefonie, **przy otwartej aplikacji**, powiadomienie powinno pojawić się w ciągu ~30 sekund.

Nie sprawdzaj tego przy zamkniętej aplikacji — patrz sekcja o push wyżej.

## Krok 6 — obejrzyj resztę

Przejdź przez pulpit, listę zadań i grafik. Szukamy: treści uciekającej poza ekran, przycisków poza zasięgiem kciuka, tabel wymagających przewijania w poziomie, tekstu za małego do przeczytania.

---

## Co zapisać

Dla każdego kroku: **działa / nie działa / działa inaczej niż na komputerze**, z modelem urządzenia i wersją systemu. Zrzuty ekranu przy wszystkim, co wygląda źle.

Wynik — także negatywny — dopisać do `docs/PLAN-ROZWOJU-2026-08.md` albo do planu bieżącego cyklu. **Negatywny wynik jest tu tak samo wartościowy**: mówi, czego nie obiecywać pierwszemu klientowi.
