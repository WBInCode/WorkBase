# WorkBase: przewodnik administratora

> Dla osób konfigurujących system: role, uprawnienia, moduły, słowniki i integracja z WB Platform.
> Stan na 2026-08-11.

---

## 1. Model uprawnień

### Trzy warstwy, które trzeba rozróżniać

| Warstwa | Pytanie, na które odpowiada | Gdzie się ustawia |
|---|---|---|
| **Moduł** (flaga funkcjonalności) | czy ta część systemu w ogóle istnieje w tej firmie | Flagi funkcjonalności, źródłowo plan w WB Platform |
| **Uprawnienie** | jaką czynność wolno wykonać | rola, ekran Role |
| **Zakres danych** | na czyich rekordach | definicja roli |

Warstwy działają łącznie i każda może zablokować dostęp osobno. Wyłączony moduł ukrywa ekran wszystkim, także osobie z kompletem uprawnień.

### Uprawnienia

Uprawnienie to pojedyncza czynność zapisana jako `moduł.akcja`, na przykład `leave.approve` albo `org.import`. W systemie jest ich **108**, rozłożonych na **19 modułów**.

| Moduł | Liczba | Moduł | Liczba |
|---|---|---|---|
| identity | 8 | documents | 7 |
| leave | 8 | forms | 7 |
| tasks | 8 | org | 7 |
| time | 8 | workflow | 7 |
| cases | 7 | ai | 6 |
| contacts | 6 | dashboard | 6 |
| notification | 6 | sales | 6 |
| integration | 5 | payroll | 2 |
| reports | 2 | config | 1 |
| platform | 1 | | |

Uprawnień **nie nadaje się osobom**. Nadaje się je rolom, a osoba dostaje rolę.

### Zakres danych

Zakres decyduje, czyje rekordy widzi posiadacz uprawnienia:

| Zakres | Znaczenie |
|---|---|
| Organizacja | wszyscy pracownicy firmy |
| Jednostka | pracownicy własnej jednostki organizacyjnej |
| Własne | wyłącznie własne rekordy |

Uprawnienie mówi co wolno zrobić, zakres na czyich danych. To rozdzielenie sprawia, że ekran wynagrodzeń może być dostępny wszystkim pracownikom bez ryzyka, że ktoś zobaczy cudze rozliczenie.

---

## 2. Role standardowe

| Rola | Liczba uprawnień | Przeznaczenie |
|---|---|---|
| Super Admin | 108 | operator platformy, obejmuje zarządzanie firmami |
| Admin | 107 | pełna administracja jednej firmy, bez zarządzania firmami |
| HR | 41 | pracownicy, czas pracy, urlopy, limity |
| Kierownik | 32 | akceptacje, podgląd zespołu, zadania |
| Pracownik | 16 | własny czas pracy, własne urlopy, zadania, dokumenty |

Jedyna różnica między Super Adminem a Adminem to `platform.manage-tenants`.

Role są zdefiniowane osobno dla każdej firmy, więc można je modyfikować bez wpływu na pozostałe. Liczby dotyczą konfiguracji standardowej i po zmianach mogą się różnić.

### Skład roli Pracownik

16 uprawnień: `ai.use`, `dashboard.view`, `documents.create`, `documents.view`, `forms.submit`, `leave.create`, `leave.view`, `notification.view`, `org.view`, `payroll.view`, `tasks.comment`, `tasks.edit`, `tasks.view`, `time.create`, `time.view`, `workflow.view`.

To jest zestaw minimalny, na którym system działa poprawnie dla zwykłego pracownika. Odbieranie z niego pozycji zwykle psuje coś nieoczywistego, na przykład zdjęcie `org.view` odcina podgląd własnej karty.

---

## 3. Przełożony nie jest rolą

Akceptującego wniosek urlopowy wyznacza **struktura organizacyjna**, a nie rola. Przełożonym jest ten, kto został wskazany jako przełożony w kartach innych pracowników.

W praktyce oznacza to, że:

- osoba z rolą Pracownik widzi Akceptacje i Raport zespołu, jeśli ma podwładnych,
- nadanie roli Kierownik osobie bez podwładnych nie da jej żadnych wniosków do rozpatrzenia,
- zakończenie relacji datą odbiera dostęp automatycznie.

Wyjątek dotyczy dokładnie dwóch ekranów: `/leave/approvals` i `/time/team-report`. Nie rozciąga się na ekrany administracyjne.

**Gdyby tego wyjątku nie było**, przełożeni bez roli Kierownik dostawaliby odmowę dostępu do wniosków, które system sam im przydzielił. Warto o tym pamiętać przy zaostrzaniu uprawnień.

---

## 4. Role przychodzące z WB Platform

Konta w WorkBase powstają wyłącznie przez wejście z WB Platform. Razem z kontem przychodzi rola, wyliczona z roli w organizacji:

| Rola w WB Platform | Rola w WorkBase |
|---|---|
| właściciel organizacji | Super Admin |
| administrator organizacji | Admin |
| członek | Pracownik |

Rola jest synchronizowana przy każdym świeżym logowaniu. Oznacza to, że:

- **roli nadanej przez platformę nie da się odebrać w WorkBase**, bo wróciłaby przy kolejnym logowaniu. Zmienia się ją w WB Platform,
- role nadane ręcznie w WorkBase pozostają nietknięte przez synchronizację i można je odbierać normalnie,
- system nie pozwoli odebrać roli ostatniemu Super Adminowi.

---

## 5. Ekrany administracyjne

Od wersji z 27 sierpnia 2026 sekcja „Administracja" w menu nie jest już jedną listą 22 pozycji, tylko ośmioma zwijanymi grupami według obszaru — otwarta jest ta, w której siedzi bieżący ekran. Nad grupami stoją dwa wejścia: **Przegląd ustawień** (`/admin`) — wszystkie ekrany jako kafelki z jednozdaniowym opisem, co ustawiają i co z tego wynika — oraz **Gotowość konfiguracji**. Kolejność grup odpowiada kolejności, w jakiej firma zwykle je konfiguruje.

| Grupa | Ekrany |
|---|---|
| Firma | branding, nazewnictwo, moduły |
| Struktura i kadry | typy jednostek, stanowiska, rodzaje terminów |
| Czas pracy | zasady rejestracji, limity przerw, dni wolne |
| Urlopy i wnioski | typy urlopów, polityki urlopowe, rodzaje wniosków |
| Zadania i dokumenty | statusy zadań, ustawienia zadań, ustawienia dokumentów |
| Obiegi i powiadomienia | kreator obiegów, reguły eskalacji, szablony powiadomień |
| Dostęp | role, matryca uprawnień |
| Platforma | firmy (tylko operator) |

Nowy ekran administracyjny **musi** trafić do jednej z grup w `frontend/src/nav/ustawienia.ts` — test `ustawienia.test.ts` oblewa, jeśli trasa `/admin/*` z mapy dostępu nie ma tam miejsca. Tak przez miesiące żył Kreator obiegów: miał trasę i uprawnienie, a wejścia z menu nie.

| Ekran | Co konfiguruje | Wymagane uprawnienie |
|---|---|---|
| `/admin/roles` | role i przypisania uprawnień | `identity.manage` |
| `/admin/permissions` | macierz uprawnień, podgląd i edycja | `identity.manage` |
| `/admin/feature-flags` | włączanie i wyłączanie modułów | `identity.manage-feature-flags` |
| `/admin/tenants` | firmy i plany, tylko operator | `platform.manage-tenants` |
| `/admin/leave-types` | rodzaje nieobecności | `leave.manage` |
| `/admin/leave-policies` | zasady naliczania urlopów | `leave.manage` |
| `/admin/task-statuses` | statusy zadań | `tasks.manage` |
| `/admin/positions` | stanowiska | `org.manage` |
| `/admin/unit-types` | typy jednostek organizacyjnych | `org.manage` |
| `/admin/break-policies` | limity przerw | `config.manage` |
| `/admin/time-tracking-settings` | zasady rejestracji czasu | `config.manage` |
| `/admin/branding` | logo i kolory | `config.manage` |
| `/admin/terminology` | nazewnictwo w interfejsie | `config.manage` |
| `/admin/notification-templates` | treść powiadomień | `config.manage` |
| `/admin/escalation-rules` | reakcja na brak decyzji w terminie | `config.manage` |
| `/admin/document-settings` | typy plików, limity rozmiaru | `config.manage` |
| `/admin/task-settings` | ustawienia modułu zadań | `config.manage` |
| `/workflow/builder` | obiegi akceptacji | `workflow.manage` |
| `/admin/dni-wolne` | kalendarz dni wolnych i świąt | `config.manage` |
| `/admin/typy-wnioskow` | rodzaje wniosków firmowych | `wnioski.manage` |

---

## 6. Konfiguracja obszarów

### Rodzaje nieobecności

Typ urlopu ma cztery istotne pola: nazwę, płatność, wymóg akceptacji i liczbę dni w roku.

**Wymóg akceptacji** decyduje o całej ścieżce wniosku. Typ bez tego wymogu zatwierdza się natychmiast po wysłaniu i nigdy nie trafia do kolejki przełożonego. Tak zwykle konfiguruje się urlop na żądanie i zwolnienie lekarskie.

Zmiana liczby dni w roku dotyczy naliczeń przyszłych i **nie przelicza wstecz** sald już przyznanych.

### Rodzaje wniosków

Wnioski firmowe — zaliczka, delegacja, praca zdalna, wniosek o sprzęt — stoją na tym samym silniku obiegów co wnioski urlopowe. Oznacza to, że akceptacja, przypomnienia o przekroczonym terminie, historia decyzji i zastępstwa działają dla nich bez żadnej dodatkowej konfiguracji.

Rodzaj wniosku składa się z formularza i informacji, czy wniosek wymaga akceptacji przełożonego. Pola definiuje się na ekranie: tekst, tekst wielolinijkowy, liczba, data, lista wyboru albo tak/nie. Każde ma **kod** (pod nim zapisują się dane) i **etykietę** (widoczną dla pracownika).

Kodu rodzaju nie da się zmienić po utworzeniu, bo jest zapisany przy złożonych wnioskach.

Rodzaj, którego nie chcesz już udostępniać, **odznacz jako niedostępny zamiast usuwać** — złożone wcześniej wnioski zachowają wtedy swoją nazwę.

Wniosek, który nie wymaga akceptacji, jest rejestrowany od razu w chwili złożenia. Bez tego wisiałby w stanie „Oczekuje” na zawsze, bo nie ma komu go rozstrzygnąć.

### Dni wolne

Kalendarz dni wolnych należy do firmy. System nie zna z góry żadnych dat i nie wpisuje niczego sam — także przy zakładaniu firmy.

Dzień wolny wpływa na dwie rzeczy: obniża normę czasu pracy w rozliczeniu (jeśli zaznaczono „Obniża normę”) oraz pozwala naliczyć dodatek świąteczny za pracę w tym dniu.

Przycisk **Wstaw typowe dni wolne w Polsce** dopisuje gotowy zestaw na wybrany rok, pomijając daty już wpisane — można go użyć ponownie po dodaniu własnych dni firmowych. Święta ruchome (Wielkanoc, Boże Ciało) wyliczane są dla każdego roku osobno.

Dni ustalone przez firmę, na przykład wolne za święto wypadające w sobotę, warto oznaczyć jako **firmowe** — to tylko opis, nie zmienia działania.

**Dopóki kalendarz jest pusty, dodatek świąteczny zawsze wynosi zero**, bo nie ma dni, do których mógłby się odnieść.

### Limity przerw

Polityka przerw określa rodzaj (płatna lub bezpłatna), maksymalną liczbę przerw dziennie, maksymalną długość jednej przerwy i łączny czas w ciągu dnia.

To te ustawienia generują komunikat o wyczerpaniu limitu, który widzi pracownik przy próbie rozpoczęcia kolejnej przerwy.

### Statusy zadań

Jeden status musi być oznaczony jako domyślny, bo dostają go nowe zadania. Statusy końcowe zamykają sprawę i wyłączają zadanie z liczników spraw otwartych.

### Powiadomienia i eskalacje

Szablon powiadomienia ma kod odpowiadający rodzajowi zdarzenia, własny tytuł i treść. W treści wstawia się zmienne w podwójnych klamrach, podstawiane w chwili wysyłki.

| Kod | Kiedy wychodzi | Zmienne |
|---|---|---|
| `task_assigned` | ktoś przypisał zadanie | `tytul` |
| `task_overdue` | zadanie przekroczyło termin | `tytul`, `opis` |
| `anomaly_detected` | rozbieżność grafiku i rejestracji | `pracownik`, `rodzaj`, `data` |
| `termin_zbliza` | termin kadrowy wchodzi w okno ostrzeżenia | `pracownik`, `rodzaj`, `dni`, `data` |
| `termin_minal` | termin kadrowy upłynął | `pracownik`, `rodzaj`, `dni`, `data` |
| `approval_pending` | sprawa trafiła do akceptanta | `rodzaj`, `krok`, `wnioskodawca` |
| `approval_decided` | zapadła decyzja w sprawie wnioskodawcy | `rodzaj`, `decyzja`, `akceptant` |
| `escalation` | wniosek stoi u akceptanta ponad próg | `krok`, `godziny`, `prog` |

Nowa firma dostaje **komplet ośmiu szablonów** odwzorowujących domyślne teksty — po to, żeby administrator w ogóle zobaczył, jakie kody system rozpoznaje, i miał co przepisać pod siebie.

Trzy zachowania warte zapamiętania, bo są celowe:

- **Zmienna, której szablon nie zna, zostaje w tekście widoczna.** Literówka w nazwie ma rzucać się w oczy, zamiast zostawiać w zdaniu dziurę, której nie da się z niczym powiązać.
- **Szablon wyłączony albo nieutworzony to powrót do treści domyślnej.** Błąd w konfiguracji nigdy nie wycisza powiadomienia.
- **Treść ustala firma, ale odbiór jest decyzją pracownika.** Każdy ma własny ekran ustawień powiadomień (`/powiadomienia`) i może wyciszyć wybrany rodzaj. Administrator tego nie widzi ani nie zmienia.

### Powiadomienia na pocztę

Kanał pocztowy jest **opt-in**: pracownik sam zaznacza „mailem" przy wybranym rodzaju. Domyślnie nie wychodzi nic. Odwrotna domyślka oznaczałaby wysyłanie na skrzynki, o które nikt nie pytał.

Do działania potrzebny jest serwer pocztowy w konfiguracji instancji (`Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__FromEmail`, `Smtp__FromName`). Adres odbiorcy bierze się z kartoteki pracownika — konto bez kartoteki albo bez adresu nie dostanie wiadomości.

Awaria poczty **nie zabiera powiadomienia w aplikacji**: wpis jest już zapisany, a nieudana wysyłka zostawia ostrzeżenie w logu i nie przerywa zadania cyklicznego, więc pozostali odbiorcy z tej samej partii dostają swoje.

Dwa ostatnie kody są nowe. Wcześniej **nikt nie dostawał żadnej informacji o akceptacjach**: akceptant musiał sam zaglądać do kolejki, a wnioskodawca sam sprawdzać, czy coś się zmieniło. Powiadomienie nie idzie do osoby, która sama złożyła i sama zatwierdza — w firmie jednoosobowej byłoby to wyłącznie hałasem.

Reguła eskalacji wiąże **konkretny krok obiegu** z progiem w minutach. Po jego przekroczeniu akceptant dostaje przypomnienie. Sprawdzenie idzie co 15 minut, ale przypomnienie o danym wniosku wychodzi **raz** — wniosek stojący tydzień nie zasypie nikogo powtórkami. Reguła obejmuje wyłącznie krok, przy którym ją ustawiono; żeby przypilnować całego obiegu, trzeba dodać regułę do każdego kroku osobno.

System nie narzuca żadnego progu. Bez ustawionej reguły nie przypomina o niczym.

---

## 7. Moduły i licencje

Flagi funkcjonalności decydują, które moduły są widoczne w firmie. Zestaw dostępnych modułów wynika z planu wykupionego w WB Platform i jest z nią synchronizowany.

Ręczne włączenie modułu spoza planu nie ma trwałego efektu, bo kolejna synchronizacja przywróci stan z platformy.

Wyłączenie modułu ukrywa całą jego sekcję w menu wszystkim użytkownikom, niezależnie od uprawnień. Dane nie są kasowane, wracają po ponownym włączeniu.

### Moduły dostępne w tej wersji

Aplikacja obsługuje dziewięć modułów: Organizacja, Zarządzanie dostępem, Czas pracy, Urlopy, Zadania, Procesy, Dashboard, Powiadomienia i Dokumenty.

Moduły Formularze, Integracje, Sprawy, Kontakty, Sprzedaż i AI zostały **wycofane z tej wersji** i nie pojawiają się na liście flag funkcjonalności. Wrócą pojedynczo, gdy będą gotowe do wdrożenia u klienta.

---

## 8. Diagnostyka

### Użytkownik dostaje odmowę mimo nadanego uprawnienia

Uprawnienia są zapamiętywane w pamięci podręcznej serwera na kilka minut. Zaraz po zmianie użytkownik może jeszcze dostawać odmowę.

Jeśli po kilku minutach problem nie znika, sprawę trzeba zgłosić na poziom utrzymania systemu, bo może wymagać restartu usługi.

### Użytkownik nie widzi sekcji, choć ma rolę

Kolejność sprawdzania:

1. Czy moduł jest włączony we Flagach funkcjonalności.
2. Czy rola faktycznie zawiera uprawnienie, sprawdź w macierzy uprawnień.
3. Czy użytkownik ma tę rolę przypisaną, sprawdź licznik użytkowników przy roli.
4. Czy użytkownik zalogował się ponownie po zmianie.

### Pracownik nie ma przycisku rejestracji czasu

Przycisk pojawia się tylko wtedy, gdy konto jest powiązane z kartą pracownika. Powiązanie powstaje przy wejściu z WB Platform. Konta utworzone inną drogą mogą go nie mieć.

### Import pracowników z pliku kadrowego

Ekran `/org/employees/import` przyjmuje pliki w kodowaniu UTF-8 oraz Windows-1250 (to drugie stosują Symfonia, Optima i Excel przy „Zapisz jako CSV") — nie trzeba ich wcześniej konwertować. Separatorem może być przecinek albo średnik.

Data zatrudnienia może być zapisana jako `15.03.2015`, `15-03-2015`, `15/03/2015` albo `2015-03-15`. Zapis z ukośnikiem czytany jest po polsku, dzień jako pierwszy.

Wiersze z nieprawidłową datą lub bez adresu e-mail są pomijane i wypisane na podglądzie przed zapisem. Nieistniejąca data (np. `31.02`) jest odrzucana, a nie przewijana na kolejny miesiąc.

Plik do sprawdzenia całej ścieżki: [`docs/przyklady/import-pracownikow-przyklad.csv`](przyklady/import-pracownikow-przyklad.csv) — zapisany w Windows-1250, z trzema wierszami do odrzucenia.

**Import wysyła zaproszenia.** Każdy zaimportowany pracownik dostaje zaproszenie do WorkBase przez WB Platform. Nie importuj list testowych z prawdziwymi adresami.

### Nowa firma zaraz po nadaniu licencji

Firma zakładana przez WB Platform dostaje komplet do pracy od razu: role, strukturę, rodzaje nieobecności, statusy i priorytety zadań oraz obiegi akceptacji wniosku urlopowego, zadania i wniosku ogólnego.

Do uzupełnienia pozostają trzy rzeczy, których system nie zgadnie: lista pracowników, godziny pracy (szablon grafiku) oraz wskazanie przełożonych. Bez tego ostatniego wnioski urlopowe nie mają akceptanta — patrz niżej.

**Właściciel nowej firmy przechodzi te trzy rzeczy w kreatorze pierwszego startu**, który uruchamia się sam przy pierwszym zalogowaniu i nie wymaga niczego od administratora platformy.

### Wnioski urlopowe nie trafiają do nikogo

Najczęściej brakuje wskazania przełożonego w karcie pracownika. Druga możliwość to typ urlopu bez wymogu akceptacji, który zatwierdza się sam.

### Anomalie czasu pracy — wykrywanie i rozpatrywanie

Zadanie cykliczne o 01:00 porównuje grafik z rzeczywistą rejestracją i zapisuje rozbieżności: brak wejścia, brak wyjścia, spóźnienie, praca w dniu wolnym, podwójne wejście. Które z nich są wykrywane, ustala się w **Ustawieniach czasu pracy**.

Ekran **Anomalie** (`/time/anomalie`, `time.view`) pozwala je rozpatrzyć. Dwie decyzje, bo to dwie różne rzeczy:

| Decyzja | Znaczenie |
|---|---|
| **Przejrzane** | sprawa obejrzana i zamknięta |
| **To nie problem** | nie było czego prostować — urlop, dzień wolny, nieaktualny grafik |

Rozróżnienie pozwala później policzyć, ile wykrytych anomalii było realnych. Samo rozpatrzenie wymaga `time.manage`; bez niego widać listę bez przycisków.

Poprawianie ewidencji odbywa się na **karcie czasu**, do której prowadzi odsyłacz przy każdej pozycji — świadomie nie na liście anomalii, żeby nie powstała druga ścieżka edycji wpisów.

⚠️ **Lista jest zawężana do zakresu danych pytającego.** Do 26 sierpnia 2026 nie była: zapytanie filtrowało wyłącznie po firmie, a endpoint wymaga `time.view`, które ma każdy pracownik — czyli dowolna osoba mogła pobrać anomalie całej załogi. Poprawione, przypięte testem.

---

### Terminy: badania, BHP, uprawnienia, końce umów

Ekran **Terminy** (`/terminy`, uprawnienie `org.view`) pokazuje, co wygasa w najbliższych 30, 60 albo 90 dniach. Wpisy wprowadza się na karcie pracownika; rodzaje terminów i wyprzedzenie ostrzeżenia ustala firma w **Ustawieniach → Rodzaje terminów** (`org.edit`).

Nowa firma dostaje zestaw startowy — badania lekarskie (30 dni), szkolenie BHP (30), uprawnienia i certyfikaty (60), koniec umowy (60) — **edytowalny jak każdy inny słownik**. Wyprzedzenie jest osobne dla każdego rodzaju, bo badanie umawia się z miesięcznym wyprzedzeniem, a wypowiedzenie umowy wymaga dwóch.

| Rzecz | Zachowanie |
|---|---|
| Odnowienie | zakłada **nowy** wpis, poprzedni trafia do historii — zostaje przebieg badań, nie tylko ostatnia data |
| Powiadomienie | do pracownika i jego przełożonego, **raz przy wejściu w okno ostrzeżenia i raz przy upływie** — nie codziennie |
| Widoczność cudzych terminów | wymaga `org.view-team` (Admin, HR, Kierownik) **i** mieszczenia się w zakresie danych; własne widzi każdy |

**System niczego nie blokuje.** Osoba z nieaktualnym badaniem normalnie zarejestruje czas pracy i złoży wniosek. Pokazujemy stan; dopuszczenie do pracy jest odpowiedzialnością pracodawcy, nie systemu — i tak ma zostać.

### Odejście pracownika a dostęp do systemu

„Dezaktywuj pracownika" na karcie (`org.edit`) robi teraz trzy rzeczy naraz:

| Krok | Co się dzieje |
|---|---|
| Status | pracownik znika z list aktywnych i z raportów |
| Konto | **wyłączone w Keycloaku** — logowanie przestaje działać |
| Sesje | otwarte sesje zostają zamknięte |

Ostatnie dwa punkty są nowe. Wcześniej zwolnienie zmieniało wyłącznie status, a konto działało dalej — kadry widziały „Nieaktywny" i miały pełne prawo sądzić, że dostęp zniknął. **Jeśli ktoś odszedł z firmy przed tą wersją, jego konto może być nadal czynne**: wejdź na kartę i dezaktywuj ponownie, to wystarczy.

Samo wyłączenie konta nie kończy sprawy natychmiast, dlatego zamykamy też sesje: token dostępu wydany przed wyłączeniem byłby ważny aż do wygaśnięcia.

**Konta nie kasujemy.** Skasowane zabrałoby ślad, kto co zrobił, i uniemożliwiło powrót. Przywrócenie pracownika (przycisk na karcie osoby nieaktywnej) oddaje dostęp i — w firmach obsługiwanych przez WB Platform — ponawia zaproszenie.

Awaria Keycloaka nie wycofuje zwolnienia: zmiana w kadrach zostaje zapisana, a w logu pojawia się błąd z numerem pracownika. Wtedy konto trzeba wyłączyć ręcznie w konsoli.

### Mienie powierzone

Rejestr rzeczy firmy wydanych pracownikowi — laptop, telefon, klucze, karta dostępu, odzież, narzędzia. Odpowiada na pytanie, które zadaje sobie każda firma i praktycznie każda odpowiada arkuszem: „co ten człowiek ma od nas i co ma oddać, gdy odejdzie”.

| Gdzie | Co |
|---|---|
| Karta pracownika → **Mienie powierzone** (`org.edit`) | wydanie, zwrot, pełna historia pod przełącznikiem |
| Karta pracownika, przycisk **Potwierdzam odbiór** | widoczny wyłącznie dla samego pracownika |
| **Do zwrotu** (`/mienie/do-zwrotu`, `org.view-team`) | niezwrócone rzeczy u osób nieaktywnych lub z datą odejścia, w zakresie danych pytającego |
| Potwierdzenie dezaktywacji | ostrzeżenie, ile rzeczy osoba ma jeszcze oddać |

Trzy decyzje projektowe, celowe:

- **Rodzaj to tekst, nie słownik.** Firmy wydają bardzo różne rzeczy; lista z góry byłaby albo za krótka, albo za długa. Formularz podpowiada typowe wartości, ale nie zmusza do nich.
- **Zwrot nie kasuje wpisu.** Historia „kto miał ten laptop przede mną” jest tak samo potrzebna jak stan bieżący, a przy sporze o uszkodzenie to jedyny dowód, kiedy sprzęt zmienił ręce.
- **Potwierdzenie składa wyłącznie pracownik**, ze swojego konta. Kadry mogą wpisać wydanie, ale nie mogą potwierdzić za niego — wtedy potwierdzenie nic by nie znaczyło. Brak potwierdzenia niczego nie blokuje; to informacja, nie bramka.

„Do zwrotu” celowo nie pokazuje wszystkich wydanych rzeczy: laptop u kogoś, kto pracuje, nie jest do zwrotu.

### Potwierdzenie zapoznania się z dokumentem

Regulamin, instrukcja BHP, polityka bezpieczeństwa — dokumenty, przy których firma chce mieć ślad, że każdy je przeczytał. Buduje się na module Dokumenty (storage, kategorie, uprawnienia, skanowanie antywirusowe są już tam).

| Gdzie | Co |
|---|---|
| Lista dokumentów, kolumna **Potwierdzenie** (`documents.create`) | przełącznik „wymagane” przy pliku |
| Baner **Do potwierdzenia** nad listą dokumentów | każdy adresat widzi swoje zaległe i klika „Zapoznałem się” |
| Przycisk **kto** przy dokumencie (`documents.manage`) | kto potwierdził, kto nie i od ilu dni od publikacji |

Kogo dokument dotyczy: **firmowy** (bez powiązania) — każdego aktywnego pracownika; **dołączony do karty pracownika** — tylko tej osoby. Załączników zadań nie da się oznaczyć, bo nie mają adresata.

Potwierdza wyłącznie sam pracownik, ze swojego konta; kadry nie mogą potwierdzić za kogoś. Nie ma cofnięcia: nowa wersja regulaminu to nowy dokument i nowe potwierdzenia. System niczego nie wymusza ani nie blokuje — to rejestr do pokazania przy kontroli, egzekwowanie należy do firmy.

### Listy kontrolne przyjęcia i odejścia

Szablon, który przy dodaniu albo dezaktywacji pracownika sam zakłada zadania: co zrobić, ile dni po zdarzeniu, kto. **Ustawienia → Listy kontrolne** (`org.manage`).

| Wykonawca | Kto dostaje zadanie |
|---|---|
| sam pracownik | osoba, której dotyczy zdarzenie |
| jego przełożony | aktualny przełożony ze struktury; bez przełożonego pozycja jest pomijana, reszta listy powstaje |
| wskazana osoba | np. kadrowa, informatyk |

Powstają zwykłe zadania — widać je w „Zadania”, mają domyślny status i priorytet „Normalny”, termin = data zdarzenia + dni z pozycji. W opisie jest nazwa listy i osoba, której dotyczą.

Nowa firma dostaje **dwa przykłady wyłączone** (przyjęcie i odejście). Lista wyłączona nic nie robi; włączenie to jedno kliknięcie. Celowo nie włączamy ich za firmę — nikt nie ma dostać zadań, o które nie prosił. Przywrócenie pracownika nie uruchamia listy przyjęcia.

Lista odejścia spina się z mieniem powierzonym: typowa pozycja „Odbierz mienie firmy” trafia do przełożonego tego samego dnia, a co dokładnie ma wrócić, widać na ekranie **Do zwrotu**.

---

## 9. Kreator pierwszego startu

Nowa firma jest do czasu ukończenia kreatora **zablokowana**: API odpowiada `409` z kodem `SETUP_REQUIRED` na wszystkim poza samym kreatorem, logowaniem i webhookami, a interfejs przenosi użytkownika do `/kreator`. Blokada dotyczy **wyłącznie firm zakładanych od tej wersji** — firmy istniejące wcześniej nigdy nie dostają znacznika i kreator ich nie zatrzyma.

Pięć ekranów, cztery pytania:

| Ekran | Pytanie | Domyślnie | Co powstaje |
|---|---|---|---|
| 1 | Kto tu pracuje? | „na razie tylko ja" | pracownicy (plik CSV, ręcznie albo nikt) |
| 2 | W jakich godzinach? | pn–pt 8:00–16:00, przerwa 30 min niepłatna | szablon grafiku + polityka przerw |
| 3 | Kto akceptuje wnioski? | krok do pominięcia | relacje przełożonych |
| 4 | Ile dni urlopu wypoczynkowego? | 26 dni | wymiar urlopu wypoczynkowego |
| 5 | Podsumowanie | — | lista „co ustawiliśmy za Ciebie" |

Krok 4 istnieje po to, żeby właściciel w ogóle **dowiedział się**, że taka wartość jest ustawiona: seeder wpisuje nowej firmie 26 dni i dotąd nikt się o tym nie dowiadywał. Ekran podaje dla orientacji, że Kodeks pracy przewiduje 20 albo 26 dni zależnie od stażu, ale **system tej liczby nie sprawdza i żadnej nie narzuca** — 35 dni przejdzie tak samo jak 26.

Każde pytanie ma odpowiedź domyślną, więc „Dalej, Dalej, Dalej, Dalej, Zacznij" daje działającą firmę jednoosobową. Kreator jest **wznawialny**: zamknięcie przeglądarki na kroku 2 wraca po zalogowaniu na krok 2, nie na początek.

**Zaproszenia w kreatorze są domyślnie wyłączone.** Dodanie pracownika w Ustawieniach kolejkuje zaproszenie do platformy WB od razu; w kreatorze wysyłkę włącza się osobnym przełącznikiem, żeby import kilkudziesięciu osób nie rozesłał kilkudziesięciu zaproszeń przed sprawdzeniem listy.

Kreator jest cienką warstwą nad tymi samymi komendami, których używają ekrany administracyjne — nie tworzy danych własną ścieżką. Co za tym idzie: wszystko, co ustawi, da się później zmienić normalnie w Ustawieniach.

Kreator zakłada też **kartotekę pracownika właścicielowi**. Bez niej właściciel ma konto użytkownika, ale nie ma kartoteki — a SSO wiąże je po adresie e-mail, więc token nie dostaje `employee_id` i nie da się zarejestrować czasu pracy ani złożyć wniosku. Dlatego kreator kończy się ponownym zalogowaniem: token wydany przed konfiguracją tego identyfikatora jeszcze nie niósł.

---

## 10. Gotowość konfiguracji — „co jeszcze nie zadziała"

Ekran **Gotowość konfiguracji** (`/admin/gotowosc`, uprawnienie `org.edit`) wylicza na bieżąco z danych firmy, które funkcje jeszcze nie zadziałają. Kreator zadaje trzy pytania i celowo nie pyta o resztę, więc zaraz po nim ta lista zwykle nie jest pusta i **to jest normalne**.

Pozycje dzielą się na dwie grupy:

| Grupa | Znaczenie | Przykłady |
|---|---|---|
| **Blokuje** | funkcja nie zadziała wcale | brak pracowników, brak przełożonych (wnioski nie mają komu trafić), brak stanowiska kierowniczego (zakres danych „Dział" nie ma z czego powstać), brak kont do logowania, brak szablonu grafiku |
| **Warto** | funkcja działa w okrojonej formie | brak polityki przerw, brak stawek godzinowych (godziny policzą się, kwoty będą puste), pusty kalendarz dni wolnych (dodatek świąteczny się nie naliczy), brak rodzajów wniosków |

Każda pozycja mówi, **co nie zadziała**, a nie czego brakuje — „brak stanowisk kierowniczych" nic nie znaczy dla nietechnicznego właściciela, „nikt nie zobaczy danych swojego działu" znaczy. Przy każdej jest skrót do ekranu, na którym da się to ustawić.

**Nic z tej listy nie jest wymagane.** Firma ma prawo świadomie zostawić dowolną pozycję nieustawioną; ekran informuje o skutkach, niczego nie wymusza.

---

## 11. Kolejność przy wdrożeniu nowej firmy

1. Sprawdź, które moduły obejmuje plan, we Flagach funkcjonalności.
2. Zdefiniuj **typy jednostek** i **stanowiska**, bo bez nich nie da się zbudować struktury.
3. Zbuduj **strukturę organizacyjną**.
4. Wprowadź **pracowników**, pojedynczo albo importem CSV.
5. Uzupełnij w kartach **jednostkę, stanowisko i przełożonego**. Ten krok najczęściej się pomija, a bez niego nie działają wnioski urlopowe ani raporty zespołu.
6. Skonfiguruj **rodzaje nieobecności** i limity.
7. Skonfiguruj **polityki przerw** i ustawienia czasu pracy.
8. Skonfiguruj **statusy zadań**.
9. Sprawdź **role**, w razie potrzeby dostosuj.
10. Ustaw **branding** i nazewnictwo.

Punkt 5 jest najczęstszą przyczyną zgłoszeń w pierwszych tygodniach po wdrożeniu.

---

## Powiązane dokumenty

- [Przewodnik pracownika](07-przewodnik-pracownika.md)
- [Przewodnik kierownika i przełożonego](08-przewodnik-kierownika.md)
- [Architektura modułów i licencjonowania](05-module-licensing-architecture.md)
- [Provisioning firm z WB Platform](06-hub-company-provisioning.md)
