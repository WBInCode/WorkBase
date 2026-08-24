# WorkBase: przewodnik pracownika

> Dla osób korzystających z systemu na co dzień: rejestracja czasu pracy, urlopy, zadania i dokumenty.
> Stan na 2026-08-11.

---

## 1. Logowanie

WorkBase nie ma osobnego hasła. Logowanie odbywa się kontem WB Platform, tym samym, którego używasz do pozostałych aplikacji firmy.

1. Wejdź na `wb-platform.pl` i zaloguj się adresem e-mail.
2. Na liście aplikacji kliknij kafelek WorkBase.
3. Konto w WorkBase utworzy się automatycznie przy pierwszym wejściu.

Jeśli masz już otwartą sesję w WB Platform, przejście do WorkBase nie wymaga ponownego podawania hasła.

**Brak kafelka WorkBase** oznacza, że dostęp do aplikacji nie został jeszcze przyznany. Zgłoś to administratorowi w swojej firmie.

**Wylogowanie** z WB Platform kończy sesję również w WorkBase i pozostałych aplikacjach. To działanie zamierzone.

---

## 2. Co widzisz po zalogowaniu

Ekranem startowym jest **Mój dzień**. Jest dostępny dla każdej zalogowanej osoby, niezależnie od uprawnień.

| Element | Zawartość |
|---|---|
| Nagłówek | powitanie, licznik czasu pracy, przycisk rozpoczęcia lub zakończenia dnia |
| Pasek liczników | zadania, sprawy po terminie, oczekujące akceptacje, wnioski urlopowe |
| Kolumna główna | Twoje zadania i ostatnia aktywność |
| Kolumna boczna | wnioski urlopowe, kod QR do terminala |

W górnym pasku znajdują się: zegar, dzwonek powiadomień, przycisk rejestracji czasu pracy oraz przełącznik jasnego i ciemnego motywu.

### Dlaczego menu wygląda inaczej u różnych osób

Menu pokazuje wyłącznie ekrany, do których masz uprawnienia. Dodatkowo administrator może wyłączyć całe moduły dla firmy, a wyłączony moduł znika wszystkim, także osobom z pełnymi uprawnieniami.

Wejście na adres ekranu bez uprawnienia kończy się komunikatem z nazwą brakującego uprawnienia. Warto ją podać przy zgłoszeniu, bo od razu wskazuje, czego brakuje.

---

## 3. Czas pracy

### Rejestracja dnia

Przycisk rozpoczęcia pracy jest w górnym pasku, a na ekranie Mój dzień dodatkowo w dużym liczniku.

1. Kliknij **Rozpocznij** na początku dnia.
2. W trakcie dnia rejestruj przerwy.
3. Na koniec kliknij **Zakończ**.

Nie da się rozpocząć dnia dwa razy. Jeśli poprzedni dzień nie został zamknięty, system odmówi rozpoczęcia kolejnego.

### Przerwy

Przerwy dzielą się na **płatne** i **bezpłatne**. Płatna wlicza się do czasu pracy, bezpłatna nie.

Liczbę i długość przerw ustala firma. Po wyczerpaniu limitu system odmawia rozpoczęcia kolejnej przerwy tego samego rodzaju i podaje powód wprost.

Przerwę można rozpocząć dopiero po zarejestrowaniu wejścia.

### Karta czasu i grafik

**Karta czasu** (`/time/timesheet`) pokazuje zarejestrowane wejścia i wyjścia w układzie dziennym, tygodniowym i miesięcznym, wraz z sumą godzin i rozbiciem przerw.

**Grafik pracy** (`/time/schedule`) pokazuje zaplanowane zmiany. To plan, a nie zapis faktycznie przepracowanego czasu.

### Terminal w zakładzie

Terminal to uproszczony ekran na tablecie lub monitorze przy wejściu. Nie ma menu ani danych osobowych, tylko duże przyciski rejestracji.

Identyfikacja odbywa się kodem QR generowanym w aplikacji na ekranie Mój dzień. Kod jest ważny krótko i przestaje działać po zeskanowaniu, więc nie da się go przekazać innej osobie na później.

Terminal działa na własnym koncie terminalowym, a nie na koncie pracownika.

### Pomyłki w godzinach

Nie da się samodzielnie poprawić ani dopisać godzin. Korektę wprowadza przełożony lub dział kadr. Zgłoś sprawę możliwie szybko, podając datę i rzeczywiste godziny.

---

## 4. Urlopy i nieobecności

### Składanie wniosku

1. Otwórz **Urlopy**, potem **Wnioski** (`/leave/request`).
2. Wybierz rodzaj nieobecności.
3. Podaj datę początkową i końcową.
4. Sprawdź wyliczoną liczbę dni i wyślij wniosek.

System sprawdza saldo w momencie wysyłki. Przy niewystarczającej liczbie dni wniosku nie da się wysłać.

### Rodzaje nieobecności

Listę ustala firma, więc może się różnić. Każdy rodzaj ma trzy istotne cechy: czy jest płatny, czy wymaga akceptacji i ile dni przysługuje w roku.

Typowy zestaw:

| Rodzaj | Płatny | Wymaga akceptacji | Dni w roku |
|---|---|---|---|
| Urlop wypoczynkowy | tak | tak | 26 |
| Urlop na żądanie | tak | nie | 4 (z puli wypoczynkowego) |
| Zwolnienie lekarskie | tak | nie | bez limitu |
| Opieka nad dzieckiem | tak | tak | 2 |
| Urlop bezpłatny | nie | tak | bez limitu |

Rodzaje, które nie wymagają akceptacji, zatwierdzają się od razu po wysłaniu i nie trafiają do kolejki przełożonego.

### Saldo dni

Saldo prowadzone jest osobno dla każdego rodzaju nieobecności i każdego roku.

- Wysłanie wniosku **rezerwuje** dni. Znikają z puli dostępnej, ale nie są jeszcze wykorzystane.
- Akceptacja zamienia rezerwację na **wykorzystanie**.
- Odrzucenie lub cofnięcie **zwalnia** rezerwację i dni wracają do puli.

### Statusy wniosku

| Status | Znaczenie |
|---|---|
| Roboczy | wniosek niewysłany, możesz go swobodnie edytować |
| Oczekuje | czeka na decyzję przełożonego, dni są zarezerwowane |
| Zaakceptowany | decyzja pozytywna, nieobecność trafia do kalendarza |
| Odrzucony | sprawa zamknięta, dni wracają do puli |
| Cofnięty | przełożony poprosił o poprawki, wniosek wraca do stanu roboczego |

### Kto akceptuje

Wniosek trafia do osoby wskazanej jako Twój przełożony w strukturze organizacyjnej. Jeśli w karcie pracownika nie ustawiono przełożonego, wniosek nie ma adresata. Zgłoś to działowi kadr.

### Kalendarz nieobecności

Kalendarz (`/leave/calendar`) pokazuje **zatwierdzone** nieobecności w wybranym okresie, z kolorami odpowiadającymi rodzajom urlopu. Wnioski oczekujące nie są tam widoczne.

---

## 5. Zadania

**Moje zadania** (`/tasks/my`) pokazuje wyłącznie zadania przypisane do Ciebie, z licznikami spraw otwartych i ukończonych. **Wszystkie** (`/tasks`) pokazuje szerszy zakres, zależny od uprawnień.

Zestaw statusów ustala firma. Typowo: Nowe, W analizie, W toku, Do akceptacji, Wstrzymane, Zamknięte, Odrzucone. Jeden status jest domyślny dla nowych zadań, a statusy końcowe zamykają sprawę.

Komentarze znajdują się pod opisem zadania i zapisują się od razu po wysłaniu.

Termin podaje się jako datę i godzinę w Twojej strefie czasowej. Zadania po terminie są zliczane osobno na ekranie Mój dzień.

Tworzenie zadań i przypisywanie ich innym osobom wymaga osobnych uprawnień i nie każdy je ma.

---

## 6. Dokumenty

Na ekranie **Pliki** (`/documents`) wybierasz plik z dysku i opcjonalnie przypisujesz go do kategorii.

Dopuszczalne typy plików i maksymalny rozmiar ustala administrator. Odrzucony plik zawsze ma podany powód odrzucenia.

Kategorie porządkują pliki i nie są obowiązkowe.

Usunięcie oznacza plik jako skasowany i znika on z listy. O odzyskanie trzeba poprosić administratora.

---

## 7. Wynagrodzenia

Ekran **Wynagrodzenia** (`/payroll`) pokazuje rozliczenie za wybrany okres, wyliczone na podstawie zarejestrowanego czasu pracy.

Zwykły pracownik widzi wyłącznie własne dane. Zakres widoczności wynika z roli i nie da się go obejść wpisaniem adresu ekranu.

Stawkę godzinową innej osoby widzą tylko osoby z uprawnieniem do rozliczeń zespołu — standardowo kierownik i dział kadr, każde w swoim zakresie. Własną stawkę widzisz zawsze.

---

## 8. Typowe problemy

**Nie widzę przycisku rozpoczęcia pracy.**
Przycisk pojawia się tylko wtedy, gdy konto jest powiązane z kartą pracownika. Jeśli konto powstało inaczej niż przez wejście z WB Platform, powiązania może brakować. Zgłoś to działowi kadr, podając swój adres e-mail.

**Widzę komunikat o braku dostępu do widoku.**
Komunikat podaje nazwę brakującego uprawnienia. Przekaż ją administratorowi razem z opisem, co próbowałeś zrobić.

**Zostałem wylogowany, choć nic nie robiłem.**
Wylogowanie z WB Platform kończy sesję we wszystkich aplikacjach firmy. Sesja kończy się także po dłuższym czasie bezczynności.

**Aplikacja zachowuje się dziwnie po aktualizacji.**
Przeglądarka mogła zapamiętać starą wersję. Odśwież stronę skrótem `Ctrl` + `Shift` + `R`.

---

## 9. Gdzie zgłaszać sprawy

| Rodzaj sprawy | Do kogo |
|---|---|
| dane pracownika, przełożony, limity urlopowe, korekty godzin | dział kadr |
| role, uprawnienia, włączone moduły | administrator systemu w firmie |
| dostęp do aplikacji, kafelek w WB Platform | administrator organizacji w WB Platform |

Przy zgłoszeniu podaj datę, godzinę, nazwę ekranu i treść komunikatu. To zwykle wystarcza, żeby ustalić przyczynę bez dopytywania.

---

## Powiązane dokumenty

- [Przewodnik kierownika i przełożonego](08-przewodnik-kierownika.md)
- [Przewodnik administratora](09-przewodnik-administratora.md)
