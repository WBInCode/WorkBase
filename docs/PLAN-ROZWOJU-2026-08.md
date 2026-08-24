# WorkBase — plan rozwoju (od 2026-08-24)

> Oparty na [AUDYT-2026-08-24.md](AUDYT-2026-08-24.md). Kontekst techniczny: [ONBOARDING-AGENTA.md](ONBOARDING-AGENTA.md).
> Wszystkie liczby pochodzą z produkcji i z repo, nie z dokumentacji planistycznej.

---

## 0. Punkt wyjścia

Trzy fakty, które wyznaczają cały plan:

1. **Produkt jest technicznie dojrzały.** 15 modułów, 291 endpointów, 373 zielone testy, build bez ostrzeżeń, zero TODO w 32 tys. linii C#, zero błędów w logach produkcyjnych z tygodnia. Pod względem jakości kodu to najlepiej utrzymany projekt w rodzinie.
2. **Produkt nie ma ani jednego płacącego klienta.** Jedyne realne użycie to 1–4 osoby z firmy operatora. „Nowak Industries" z 40 pracownikami i 4238 wpisami to tenant demonstracyjny wygenerowany `DemoDataSeeder`-em.
3. **Tempo zwalnia.** 152 commity w lipcu → 57 w sierpniu, prawie wyłącznie poprawki w uprawnieniach, urlopach i grafikach plus szykowanie demo. Zespół stabilizuje rdzeń i pokazuje produkt, ale nie domyka sprzedaży.

Z tego wynika cel cyklu. Nie jest nim „rozbudowa" w sensie dokładania modułów — tych jest o pięć za dużo. Jest nim **doprowadzenie do pierwszego płacącego klienta i przetrwanie jego pierwszego miesiąca**. Wszystko, co temu nie służy, czeka.

---

## 1. Jedna decyzja przed planem: co z sześcioma modułami bez tabel

AI, Cases, Contacts, Forms, Integration, Sales — kod jest, endpointy zarejestrowane, flagi włączone, tabel nie ma (K1 w audycie). Trzeba wybrać, bo wszystko dalej zależy od tej odpowiedzi.

**Rekomendacja: wyciąć wszystkie sześć z `ModuleCatalog.All`, nie generować migracji.**

Dlaczego nie „skoro kod jest, to go włączmy":

- Migracja na 17 tabel to najtańszy element tej roboty. Drogie jest to, co po niej: sześć modułów bez ani jednego ekranu we froncie, bez walidacji, bez testów, za to z pełnym kosztem utrzymania przy każdej zmianie schematu i każdym refaktorze.
- Nikt o nie nie prosi. Rdzeń — czas pracy — nie ma jeszcze płacącego użytkownika. CRM i AI to odpowiedź na pytanie, którego nikt nie zadał.
- Sprzedawanie „15 modułów", z których sześć zwraca 500 przy pierwszym kliknięciu, jest gorsze niż sprzedawanie dziewięciu działających.

Kod zostaje w repo. Powrót to jeden wpis w `ModuleCatalog` plus migracja — dzień pracy w momencie, gdy klient za to zapłaci. Jeśli któryś ma wrócić pierwszy, to **Integration** (synchronizacja urlopów z kalendarzem Google/Microsoft to typowe pierwsze pytanie przy produkcie kadrowym).

Jeśli decyzja będzie odwrotna — moduły zostają — to Faza B rośnie o co najmniej trzy tygodnie na UI i testy, a pierwszy klient przesuwa się o miesiąc. To jest realny koszt tej decyzji, wart świadomego wyboru.

---

## Faza A — uczciwy stan repo (≈1 tydzień)

Domknięcie rzeczy, które dziś kłamią o stanie systemu. Bez tego każdy kolejny krok stoi na niepewnym gruncie.

| # | Zadanie | Nakład | Efekt |
|---|---|---|---|
| A1 | Zdjąć `Ignore(PendingModelChangesWarning)` z `InfrastructureServiceCollectionExtensions.cs:137`; w CI zamienić krok „Validate EF Core migrations" na `dotnet ef migrations has-pending-model-changes` **bez** `continue-on-error` | 1 h | Rozjazd model ↔ migracje przestaje być możliwy do przeoczenia |
| A2 | Wykonać decyzję z §1: zdjąć 6 modułów z `ModuleCatalog.All`, wyłączyć ich flagi w bazie produkcyjnej, odebrać uprawnienia sześciu użytkownikom | 1 dzień | Katalog modułów zaczyna opisywać rzeczywistość; `has-pending-model-changes` przechodzi na zielono |
| A3 | `camera=(self)` w `frontend/security-headers.conf` | 15 min | Skaner QR na kiosku znów działa |
| A4 | `/workflow/builder` do nawigacji administracyjnej (uprawnienie `workflow.manage`) | 30 min | Jedyny interfejs do konfiguracji obiegów przestaje być ukryty |
| A5 | Usunąć `render.yaml`; `cd-staging.yml` skierować na realne staging albo usunąć; w `deploy-prod.sh` zastąpić listę SLADY porównaniem `git rev-parse HEAD` z pliku w archiwum | pół dnia | Jedna prawda o wdrożeniu; koniec listy rosnącej przy każdym wydaniu |
| A6 | Uzupełnić `.env.example` o `Hub__*`, `ChatNotices__*`, `TaskSearch__*`, `ClamAv__*`, `Integration__*`, `Keycloak__Admin__*`, `RateLimiting__*` z komentarzem, co się dzieje przy braku | 1 h | Nowa osoba stawia działające środowisko |
| A7 | `docs/01`–`04` do `docs/archiwum/` z nagłówkiem „stan planistyczny 2026-03" | 15 min | Znika główne źródło błędnych założeń o projekcie |

**Definicja ukończenia fazy:** `dotnet ef migrations has-pending-model-changes` zwraca „No changes", CI to sprawdza i potrafi na tym paść, a każda pozycja w `ModuleCatalog` ma tabele w bazie.

### ✅ Wykonano 2026-08-24 — wynik fazy A

Wszystkie siedem pozycji zamknięte. Weryfikacja: `dotnet build` 0 błędów / 0 ostrzeżeń · `dotnet test` 318/318 · `has-pending-model-changes` → „No changes" · front `type-check` czysty, `lint` 0 błędów, `test` 55/55, `build` przechodzi. **Nie zmieniono niczego w bazie produkcyjnej.**

| Poz. | Co weszło |
|---|---|
| A1 | Zdjęto `Ignore(PendingModelChangesWarning)` — strażnik EF znów przerywa start przy rozjeździe. Krok CI „Validate EF Core migrations" generował skrypt z istniejących migracji i miał `continue-on-error`; zastąpiony przez `has-pending-model-changes` bez `continue-on-error` |
| A2 | 6 modułów zdjętych z `ModuleCatalog.All` (9 zamiast 15) + 12 `ProjectReference` z `WorkBase.Host.csproj`. Usunięto UI formularzy (`FormBuilderPage`, `useForms`, `types/forms`, trasa, wpis w mapie dostępu). `LicensePlanSeeder` i `docs/05` zsynchronizowane |
| A2+ | Dwa miejsca pokazywałyby duchy po wycofanych modułach mimo usunięcia kodu — obie listy czytały wprost z bazy: `FeatureFlagService.GetByTenantAsync` (przełączniki modułów) i `RoleManagementService.GetAllPermissionsAsync` (macierz uprawnień, także `/matrix`). Oba filtrowane po `ModuleCatalog`; wiersze w bazie zostają nietknięte |
| A3 | `camera=(self)` w `security-headers.conf`. `geolocation=()` zostaje z komentarzem, kiedy trzeba je zdjąć |
| A4 | `/workflow/builder` w nawigacji administracyjnej (filtrowana przez `workflow.manage`), ikona + klucz `nav.workflowBuilder` |
| A5 | Usunięto `render.yaml` i `.github/workflows/cd-staging.yml`. Bramka SLADY (39 par plik:fraza, dopisywanych ręcznie przy każdym wdrożeniu) zastąpiona porównaniem SHA: nowy plik `COMMIT_SHA` z `$Format:%H$` + `export-subst` w `.gitattributes`. Skrypt skrócony z ~200 do 143 linii |
| A6 | `.env.example` uzupełniony o `Keycloak__Admin__*`, `RateLimiting__*`, `ClamAv__*`, `Hub__*`, `ChatNotices__*`, `TaskSearch__*`, `Integration__*` i brakujące klucze `Ecosystem__*`; usunięto zduplikowaną sekcję |
| A7 | `docs/01`–`04` → `docs/archiwum/` + `README.md` tłumaczący, co z nich jest nadal użyteczne. Odnośniki w `README.md`, `CONTRIBUTING.md`, `CLAUDE.md`, `ONBOARDING-AGENTA.md` poprawione |

**Dowód, że wycofanie modułów zadziałało, a nie tylko wygląda:** 42 endpointy w tych sześciu modułach wymagają uprawnień typu `sales.view`, a `IamSeeder.AllPermissionCodes` wywodzi się z `ModuleCatalog` i już ich nie tworzy. `PermissionCatalogTests` chodzi po `EndpointDataSource` i sprawdza, że każde wymagane uprawnienie istnieje w słowniku — gdyby te endpointy były nadal zarejestrowane, test by padł. Przeszedł.

### 🔴 Znaleziono przy okazji: grafiki indywidualne kasowane przez zadanie cykliczne

Po zdjęciu sześciu modułów `has-pending-model-changes` **nadal** zgłaszał rozjazd. Została jedna rzecz: domyślna wartość `time_schedules.source` — w bazie 0 (`OrgUnit`), w modelu 1 (`Individual`). To nie była kosmetyka, bo konfiguracja używa `HasSentinel(ScheduleSource.Individual)`: EF pomija tę kolumnę w INSERT dokładnie wtedy, gdy grafik jest **indywidualny**, i baza wstawiała `OrgUnit`.

Skutki w działającym systemie:
- `OrgUnitScheduleRollingGenerationJob` (w każdy poniedziałek 02:00 UTC) kasuje wszystkie wpisy o `Source == OrgUnit` i odtwarza je z szablonu jednostki. Grafiki indywidualne, które miał **omijać**, wyglądały dla niego jak własne.
- `ClearSchedulesHandler` bez `IncludeOrgUnitGenerated` kasuje tylko `Source != OrgUnit`, czyli „wyczyść grafik" nie kasowało niczego.

Potwierdzenie w danych produkcyjnych: 1889 wierszy z `source = 0`, **zero** z `source = 1`, przy 6 skonfigurowanych grafikach jednostek. Wszystkie 1889 mają `org_unit_schedule_id IS NULL`, a generator zawsze ustawia `source` i to powiązanie razem — więc żaden z nich nie pochodzi z grafiku jednostki.

Naprawa: migracja `20260824101104_FixScheduleSourceDefault` zmienia domyślną wartość na 1 i koryguje istniejące wiersze (`UPDATE ... SET source = 1 WHERE source = 0 AND org_unit_schedule_id IS NULL`). **Nie została jeszcze zastosowana na produkcji** — wejdzie przy najbliższym wdrożeniu.

---

## Faza B — gotowość na pierwszego klienta (≈4 tygodnie)

Pięć rzeczy, które dzielą „dobre demo" od „firma pracuje na tym od poniedziałku".

### B1. Eksport ewidencji do kadr i płac — 🔴 największa luka funkcjonalna

Firma, która wdraża system rejestracji czasu pracy, robi to po to, żeby raz w miesiącu wysłać księgowej kartę pracy. Dziś się nie da: jedyny działający eksport to XLSX na `TeamAttendancePage` (widok jednego zespołu), a `/api/dashboard/reports/export` to dziewięciolinijkowy CSV z kafelkami dashboardu — atrapa. Dopóki kadrowa musi cokolwiek przepisywać ręcznie, WorkBase nie zastąpi Excela, tylko go uzupełni.

Zakres: miesięczna karta pracy per pracownik (godziny, nadgodziny, przerwy, nieobecności, urlopy) plus zestawienie zbiorcze dla całej firmy lub jednostki, XLSX i CSV, z filtrem po okresie i jednostce organizacyjnej. Format uzgodniony z realną kadrową, nie wymyślony.

Nakład: ~1 tydzień. **To jest pozycja, przy której zaczynam.**

#### ✅ Wykonano 2026-08-24 — B1 zamknięte, znacznie mniejszym kosztem niż szacowano

Weryfikacja: `dotnet test` 327/327, front `type-check` czysty, `lint` 0 błędów, `test` **60/60** (było 55), `build` przechodzi. Nic w bazie produkcyjnej.

**Szacunek tygodnia był zawyżony, bo połowa roboty już istniała** — i to jest ustalenie warte zapamiętania przy kolejnych pozycjach planu:

- **Rozbicie dzienne już było.** Eksport XLSX z `TeamAttendancePage` to macierz pracownik × dzień z sumą netto. Karta obecności do teczki jest, i nikt jej nie musiał pisać.
- **Zestawienie rozliczeniowe też już było policzone.** `PayrollPage` liczy per pracownik normę z grafiku, czas pracy netto z kart, godziny zwykłe, nadgodziny (×mnożnik z ustawień), dni urlopu i nieobecności oraz kwoty. Brakowało **wyłącznie sposobu wyjęcia tego z ekranu**.

Zamiast budować endpoint raportowy na backendzie dołożono eksport do istniejącego widoku. Dane są już poprawnie zawężone po B2 — eksport nie omija uprawnień, bo wychodzi z tych samych wierszy, które widać na ekranie.

| Co weszło |
|---|
| Eksport XLSX zestawienia w `PayrollPage`: 12 kolumn + wiersz „RAZEM", zamrożony nagłówek, liczby jako **liczby** z maską komórki (godziny `0.00`, kwoty `# ##0.00`) — arkusz, w którym da się liczyć, a nie napisy typu `7.333333333333333` |
| `shared/arkusz.ts` — wspólne dobieranie ExcelJS-a na żądanie, styl nagłówka i mechanika pobierania. Powstało dlatego, że po dołożeniu drugiego eksportu ten kod byłby przepisany dwa razy; `TeamAttendancePage` przepięty na to samo, więc `exceljs` (940 kB) nadal ładuje się dopiero przy kliknięciu |
| `rozliczenieDoArkusza.ts` — czyste odwzorowanie wiersza i sumy, wydzielone tak, żeby dało się sprawdzić **same liczby** bez uruchamiania ExcelJS-a. 5 testów: zgodność liczby kolumn z nagłówkiem, liczby a nie napisy, brak stawki daje puste kwoty zamiast zer (w liście płac to różnica między „do wypłaty 0 zł" a „stawki nikt nie ustawił"), suma liczy godziny wszystkich a kwoty tylko osób ze stawką, pusta lista daje zera zamiast `NaN` |

**Naprawione po drodze — ekran płac pokazywał kierownikowi jeden wiersz.** `isHr`/`isManager` czytały rolę z roszczenia `roles` w tokenie Keycloaka, a rola `workbase-manager` nigdy nie jest tam zakładana (`KeycloakAdminService.CreateRealmRolesAsync`) — w kodzie stał nawet komentarz oznaczający to jako dług. Kierownik wpadał więc do gałęzi „tylko własny wiersz" i tyle też by wyeksportował. Teraz zakres bierze się z tego samego źródła, które sprawdza backend: uprawnienia `payroll.view-team`. Szerzej niż pozwala zakres i tak nie zobaczy — stawki spoza zakresu backend zeruje po B2, więc zmiana nie może niczego odsłonić.

**Nadal otwarte w tej pozycji:** format nie był konsultowany z kadrową (brak dostępu), więc kolumny są standardowe, ale **do potwierdzenia przy pierwszym realnym użyciu**. Jeśli księgowa oczekuje konkretnego układu pod import do swojego systemu, to zmiana w jednym pliku (`rozliczenieDoArkusza.ts`) i jego teście.

### B2. Testy zakresu danych (data scope) — 🔴 największe ryzyko

Commit `sec(dostep)` naprawił realny wyciek: *„szesc endpointow oddawalo dane dowolnego pracownika po podmianie id w adresie"*. Testów zakresu danych **nadal nie ma ani jednego** — dług zgłoszony już w audycie lipcowym jako P10 i wciąż nietknięty.

System trzyma stawki godzinowe (`org_employees`), dane kadrowe i ewidencję czasu. Jeden taki błąd u klienta to incydent RODO i koniec rozmów sprzedażowych — a wiemy, że ta warstwa już raz pękła w sześciu miejscach naraz.

Zakres: test integracyjny, który dla każdego endpointu przyjmującego `employeeId`, `unitId` lub `tenantId` w adresie próbuje sięgnąć po cudzy zasób jako pracownik, jako kierownik innej jednostki i jako użytkownik innej firmy — i oczekuje 403/404, nigdy 200. Napisany tak, żeby nowy endpoint bez sprawdzenia zakresu **oblewał test automatycznie** (wyliczenie tras z `EndpointDataSource`, nie ręczna lista — inaczej za trzy miesiące będzie tak samo nieaktualny jak lista SLADY).

Nakład: ~1 tydzień.

#### ✅ Wykonano 2026-08-24 — B2 zamknięte

Kolejność względem planu odwrócona: B2 przed B1, bo eksport płacowy z B1 to endpoint, na którym wyciek cudzych stawek boli najbardziej, a bramka z B2 obejmuje go automatycznie.

Weryfikacja: `dotnet build` 0 ostrzeżeń, `dotnet test` **327/327** (było 318; integracyjne 88 → 97). Nic nie zmieniano w bazie produkcyjnej.

**Sprostowanie do audytu:** teza „testów zakresu danych nie ma ani jednego" była błędna. Istniał `DostepDoDanychInnegoPracownikaTests` — z ręcznie wpisaną listą sześciu ścieżek, czyli pokrywał dokładnie to, co już raz pękło. Brakowało nie testu, tylko **bramki kompletności** — tego samego antywzorca co lista SLADY w `deploy-prod.sh`.

**Co powstało.** `ZakresDanychPracownikaTests` zastępuje poprzedni plik. Trasy wylicza z `EndpointDataSource`; każda trasa GET z parametrem w adresie musi trafić do jednej z dwóch list — „parametr to pracownik" (asercja 403 dla cudzego identyfikatora + 200 dla własnego) albo „nie dotyczy pracownika" **z obowiązkowym powodem**. Nowy endpoint bez wpisu oblewa test. Świadome ograniczenie: tylko GET — zapisy bez poprawnego ciała zwracają 400, z czego nie da się wyczytać, czy strażnik tam stoi.

**Co bramka wykryła przy pierwszym uruchomieniu** — 34 nieskalsyfikowane trasy, w tym:

| Znalezisko | Waga |
|---|---|
| **Stawki godzinowe całej firmy widoczne dla każdego pracownika.** `EmployeeDto` niesie `HourlyRate`, a wszystkie trzy odczyty (`/api/org/employees`, `/{id}`, `/by-number/{nr}`) wymagały tylko `org.view` — uprawnienia roli „Pracownik". Na produkcji: stawki **42 z 51 osób** dostępne dla **12 kont** szeregowych. Karta w UI była zasłonięta `isAdmin`, ale API oddawało dane każdemu, a ekran płac filtrował wyłącznie po stronie klienta | 🔴 |
| `/api/dashboard/configs/{userId}` — identyfikator wprost z adresu, tylko `dashboard.view` (ma je każdy). Podmiana adresu dawała układ pulpitu i zapisane filtry innej osoby | 🟠 |
| Trzy trasy ze strażnikiem (`/api/leave/balances/{id}`, `/api/leave/requests/{id}`, `/api/workflow/approvals/pending/{id}`) nie były w starej liście — nikt nie pilnował, żeby go nie zgubiły | 🟡 |
| `/api/documents/audit/user/{id}` sprawdzone i uznane za niegroźne: cały moduł dokumentów jest firmowy dla `documents.view`, więc filtr po autorze nie odsłania nic ponad zwykłą listę | ✅ |

**Naprawy.** Stawka zerowana w trzech odczytach pracownika, gdy pytający nie ma `payroll.view-team` — przez istniejące strażniki `CanAccessEmployeeAsync` i wsadowy `FilterAccessibleEmployeesAsync`, bez pisania nowych. Karta i lista zostają dostępne (to katalog firmowy), znika z nich tylko stawka. `/api/dashboard/configs/{userId}` dopuszcza wyłącznie własne konto.

**Pułapka rozbrojona po drodze:** zakres liczony jest wg modułu `org`, nie `payroll`. `payroll` nie jest modułem z `ModuleCatalog` (uprawnienia dopisano osobno), więc nie ma dla niego **ani jednego** wiersza w `iam_data_scopes`, a brak wierszy oznacza domyślny poziom `Team` — administrator zobaczyłby puste stawki większości firmy i ekran płac pokazałby zera. Moduł `org` ma zakresy nadane wszystkim rolom (Organization dla Admin/HR, Department dla Kierownika, Own dla Pracownika) i dokładnie tak ma działać widoczność stawek.

**Przy okazji: wspólna fabryka testowa nie miała działającej bazy.** `WorkBaseWebFactory` usuwała `DbContextOptions`, ale zostawiała usługi dostawcy Npgsql — każde zapytanie do bazy kończyło się wyjątkiem o dwóch dostawcach. Do tego nazwa bazy w pamięci powstawała **wewnątrz lambdy budującej opcje**, więc każde żądanie HTTP dostawało własną, pustą bazę. Dlatego obok wyrosły `WebhookTestFactory` i `TaskSearchTestFactory` — każdy test potrzebujący danych dorabiał sobie fabrykę. Naprawione we wspólnej (`UseInternalServiceProvider` + nazwa liczona raz), więc kolejne testy nie muszą już powielać fabryk.

### B3. Przejście onboardingu firmy od zera

Wszystkie trzy tenanty w produkcji powstały inaczej niż przez normalną ścieżkę: operator jest zaszyty w kodzie (`00000000-…-0001`), demo z seedera, trzeci pusty. **Nikt nigdy nie przeszedł drogi Hub → webhook `entitlements.updated` → provisioning → pierwsze logowanie właściciela → zaproszenie pracowników → pierwszy clock-in.** Pierwszy klient odkryje w niej każdy błąd osobiście.

Zakres: przejść tę ścieżkę na jednorazowym, czwartym tenancie, spisać każde miejsce, gdzie trzeba było dotknąć bazy albo Keycloaka ręcznie, naprawić je, powtórzyć — aż przejdzie bez interwencji. Potem tenant usunąć. Efektem ubocznym jest procedura wdrożenia klienta, której dziś nie ma.

Nakład: ~3 dni, plus naprawy tego, co wyjdzie.

### ✅ B4 wykonane 2026-08-24 — import startowy

Plik testowy wygenerowany zamiast pozyskiwany: [`docs/przyklady/import-pracownikow-przyklad.csv`](przyklady/import-pracownikow-przyklad.csv) — **zapisany w Windows-1250**, ze średnikami, CRLF, polskimi znakami, czterema zapisami daty i trzema wierszami, które mają zostać odrzucone (7 do importu, 3 do odrzucenia). Opis: [`docs/przyklady/README.md`](przyklady/README.md).

Znalezione i naprawione dwie usterki — obie dokładnie te przewidziane w planie:

| Usterka | Skutek przed naprawą |
|---|---|
| `FileReader.readAsText` bez kodowania, czyli zawsze UTF-8 | Eksport z Symfonii/Optimy/Excela jest w Windows-1250 → polskie znaki trafiały do bazy jako krzaki. Teraz `odczytajCsv` próbuje UTF-8 w trybie ścisłym i przy błędzie czyta jako Windows-1250 |
| `new Date(tekst)` / `Date.parse` | `15.03.2015` odrzucane w całości, a `05/03/2015` czytane po amerykańsku jako **3 maja** zamiast 5 marca — bez żadnego błędu, zła data zatrudnienia szła do bazy. Teraz `parsujDateZatrudnienia` obsługuje ISO i polskie zapisy z kropką, myślnikiem i ukośnikiem, buduje datę w południe UTC (północ potrafi cofnąć dzień przez strefę) i odrzuca `31.02` zamiast przewijać na 3 marca |

14 testów w `frontend/src/utils/csvParser.test.ts`, w tym dekodowanie prawdziwych bajtów Windows-1250 i przypadek dwuznacznej daty. Front: 74 testy (było 60).

`.gitattributes` dostał `docs/przyklady/*.csv -text`, żeby git nie „naprawił" kodowania i końców linii pliku, który służy właśnie do ich sprawdzania.

**Nie uruchomiono importu na produkcji.** `ImportEmployeesCommand` kolejkuje zaproszenie do Huba **dla każdego** importowanego pracownika, a `Hub__EmployeeAccessSyncEnabled=true` — import nawet na `TestowaFirma` wygenerowałby zaproszenia w Hubie, czyli zapis w danych innego produktu. Ścieżka parsowania jest pokryta testami; przejście end-to-end w przeglądarce zostaje do świadomej decyzji.

### B4 (pierwotny opis). Import startowy z realnego pliku HR

`CsvImportPage` istnieje i ma mapowanie kolumn, ale sprawdzony był na danych, które sami przygotowaliśmy. Klient przyjdzie z eksportem z Symfonii, Optimy albo z arkusza prowadzonego od dziesięciu lat. Przetestować na co najmniej dwóch prawdziwych plikach: polskie znaki, kodowanie Windows-1250, puste stanowiska, pracownicy bez maila, daty w trzech formatach.

Nakład: ~2 dni.

### B5. Ścieżka mobilna pracownika

Pracownik produkcyjny lub terenowy nie zaloguje się do systemu z desktopa — a to on generuje wpisy czasu pracy. PWA jest skonfigurowane, ostatnie commity to poprawki mobilne (boczny szuflada, znaczniki iOS), ale scenariusz nie był przejechany w całości na urządzeniu.

Zakres: na realnym Androidzie i realnym iPhonie — instalacja aplikacji, logowanie SSO, clock-in, złożenie wniosku urlopowego, odebranie powiadomienia. ⚠️ W tej rodzinie produktów iOS notorycznie nie pokazuje promptu instalacji — sprawdzić, czy WorkBase ma ten sam problem, zanim obieca się klientowi aplikację na telefon. **Zwężenie okna przeglądarki niczego nie dowodzi.**

Nakład: ~2 dni, plus naprawy.

---

---

## Faza E — konfigurator pierwszego startu

Rozpisana osobno: **[KONFIGURATOR-PIERWSZEGO-STARTU.md](KONFIGURATOR-PIERWSZEGO-STARTU.md)**.

Skrót: nowa firma po nadaniu licencji dostaje z provisioningu wyłącznie role i korzeń struktury — **zero typów urlopów, statusów zadań i obiegów**, więc szef nie złoży wniosku ani nie założy zadania. Przyczyną nie jest brak kreatora, tylko to, że `LeaveSeeder`/`TaskSeeder`/`WorkflowSeeder` są globalne, mają zaszyty identyfikator firmy operatora i pomijają się, gdy cokolwiek już istnieje. Dlatego kolejność jest odwrotna niż intuicyjna: **najpierw domyślne przy provisioningu, dopiero potem kreator, który je potwierdza i koryguje** — inaczej kreator staje się jedynym sposobem na działającą firmę i pojedynczym punktem awarii wdrożenia.

**Krok 1 tej fazy wykonany 2026-08-24** (~2 h): seedery per-najemcę spięte z provisioningiem, 6 testów, backfill istniejących firm przychodzi sam przy najbliższym wdrożeniu (synchronizacja z Hubem na starcie i tak powtarza baseline dla każdej firmy). Po wdrożeniu nowa firma będzie działać bez kreatora — kreator zostaje potrzebny do trzech rzeczy, których nie da się zasiać.

MVP to cztery ekrany i **trzy pytania** (kto tu pracuje, w jakich godzinach, kto akceptuje) — wszystko inne dostaje wartość domyślną plus ekran „co ustawiliśmy za Ciebie". Nakład MVP ≈ 8–9 dni, przy czym krok 1 (naprawa seederów, 1–2 dni) warto zrobić od razu i niezależnie: to poprawka błędu, nie nowa funkcja, i odblokowuje dwie firmy stojące dziś pusto na produkcji.

---

## Faza C — higiena, równolegle i w tle (≈3 dni rozproszone)

Drobiazgi, które taniej zrobić teraz niż przy trzech deweloperach więcej.

- **C1.** Zdiagnozować synchronizację z Rytmem: dlaczego pętla obejmuje 3 z 51 pracowników i dlaczego żaden nie ma konta w Rytmie. Integracja jest włączona na produkcji i prawdopodobnie nigdy nie zadziałała end-to-end. Poziom logu obniżyć **dopiero po diagnozie** — samo wyciszenie ukryje martwą funkcję.
- **C2.** Przyciąć ~13 lokalnych gałęzi z zerem commitów przed `main`; sprawdzić `git log main..<branch>` zanim się usunie te z `+1`.
- **C3.** 19 ostrzeżeń ESLint, w tym 3 zbędne dyrektywy `eslint-disable`.
- **C4.** Potwierdzić, czy `app.MapOpenApi()` bez warunku środowiskowego (publiczna specyfikacja API w produkcji) to decyzja czy przeoczenie.

---

## Faza D — dopiero po pierwszym kliencie

Kolejność wewnątrz tej fazy ustali pierwszy klient, nie ten dokument.

- **Rozliczenia.** `BrandingBillingEndpoints` + Stripe w WorkBase to relikt sprzed powstania Huba — a Hub jest właścicielem katalogu, entitlementów i billingu w tej rodzinie. **Rekomendacja: usunąć, nie kończyć.** WorkBase ma konsumować entitlementy z Huba (już to robi), a nie prowadzić własną subskrypcję. Przed usunięciem potwierdzić z właścicielem Huba, że billing faktycznie jest po jego stronie.
- **Style → klasy CSS**, zaczynając od `SchedulePage.tsx` (1588 linii), `TeamAttendancePage.tsx` (1075) i `EmployeeListPage.tsx` (899), tylko przy okazji dotykania tych plików. Miernik ukończenia: zdjęcie `'unsafe-inline'` ze `style-src` w CSP. Dopóki tam jest, white-label i tryb ciemny działają tylko miejscami — a white-label to argument sprzedażowy dla większych klientów.
- **NFC, biometria, geofence, kiosk** — cztery podsystemy z pełnym backendem i zerową adopcją (0/0/0/1 rekord w produkcji), geofence dodatkowo bez UI i zablokowany przez `geolocation=()` w `Permissions-Policy`. Decyzja per funkcja: dokończyć u konkretnego klienta, który o to prosi, albo wygasić.
- **Moduły premium** — wracają pojedynczo, gdy ktoś za nie płaci. Integration pierwszy.
- **Tauri** — 86 linii Rusta stojące bez zmian od lipca. Albo plan na desktop, albo `rm -rf src-tauri`.

---

## Czego świadomie nie robimy w tym cyklu

Żeby plan miał sens, musi mówić też, co odpada:

- Nie dokładamy modułów. Jest ich o sześć za dużo względem tego, co ktokolwiek używa.
- Nie migrujemy frontu na Tailwind. Hybryda `style={{ var(--wb-*) }}` jest brzydka, ale działa i nie blokuje sprzedaży.
- Nie przepisujemy `SchedulePage` ani `TaskListPage` „bo duże". Dotykamy ich tylko przy okazji realnej zmiany.
- Nie budujemy własnego billingu (patrz Faza D).
- Nie ruszamy Tauri.
- Nie skalujemy infrastruktury. Jeden VPS obsłuży pierwszych kilkunastu klientów; problemem jest brak klientów, nie wydajność.

---

## Mierniki ukończenia cyklu

Cykl jest skończony, gdy wszystkie pięć jest prawdą:

1. `dotnet ef migrations has-pending-model-changes` zwraca „No changes", a CI potrafi na tym paść.
2. Kadrowa eksportuje miesięczną ewidencję do XLSX bez pomocy programisty.
3. Test integracyjny dowodzi, że kierownik jednostki A nie odczyta danych pracownika z jednostki B na **żadnym** endpointcie przyjmującym identyfikator w adresie — i oblewa automatycznie, gdy ktoś doda nowy endpoint bez sprawdzenia zakresu.
4. Nowa firma przechodzi od webhooka z Huba do pierwszego clock-ina bez dotykania bazy i Keycloaka ręcznie.
5. Pierwszy klient płaci.

---

## Ryzyka planu

| Ryzyko | Dlaczego realne | Reakcja |
|---|---|---|
| Tempo nie wystarczy | 152 → 57 commitów miesięcznie, trend spadkowy. Faza A+B to ~5 tygodni przy obecnym tempie, nie przy lipcowym | Faza A jest niepodzielna (tydzień). Z Fazy B da się odciąć B4 i B5, jeśli pierwszy klient jest biurowy i mały |
| Warstwa uprawnień pęknie znowu | Już raz pękła w sześciu endpointach naraz, testów nadal zero, a każda kolejna funkcja dokłada endpointy z `employeeId` w adresie | B2 przed pozyskaniem klienta, nie po |
| Decyzja o modułach zostanie odłożona | Najłatwiejsza rzecz do „przedyskutowania później", a blokuje A2 i całą Fazę B | Rozstrzygnąć przed rozpoczęciem Fazy A; brak decyzji traktować jako wybór opcji „wyciąć" |
| Pierwszy klient wywróci założenia | Nikt nigdy nie przeszedł onboardingu od zera | Dlatego B3 jest w planie przed klientem, a nie w trakcie |
| Demo obieca więcej, niż produkt umie | Demo pokazuje 15 modułów, działa 9; kiosk w materiałach, kamera zablokowana | A2 i A3 przed następną prezentacją |

---

## Jak korzystać z tego planu w nowej sesji

Kolejność czytania dla agenta wchodzącego w projekt: `ONBOARDING-AGENTA.md` (co to jest i czego nie zakładać) → `AUDYT-2026-08-24.md` (dowody i szczegóły findingów) → ten plik (co robimy i w jakiej kolejności). Każda pozycja fazy A i B odsyła do konkretnego findingu w audycie — jeśli coś w planie wygląda na arbitralne, dowód jest tam.

Po zamknięciu fazy dopisać wynik w tym pliku (data, co weszło, co się zmieniło w założeniach). Ten dokument ma się zestarzeć razem z projektem, a nie obok niego — inaczej za dwa miesiące dołączy do `docs/01`–`04`.
