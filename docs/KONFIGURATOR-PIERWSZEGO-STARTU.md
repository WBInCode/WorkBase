# Konfigurator pierwszego startu — projekt

> Faza planu rozwoju, rozpisana 2026-08-24. Kontekst: [PLAN-ROZWOJU-2026-08.md](PLAN-ROZWOJU-2026-08.md), [ONBOARDING-AGENTA.md](ONBOARDING-AGENTA.md).
> Wszystkie liczby i braki opisane niżej sprawdzone na produkcji, nie założone.

---

## 1. Problem — i dowód, że jest realny

Szef firmy dostaje licencję na WorkBase, loguje się przez WB Platform i widzi pustą aplikację. **Nie jest to metafora — to stan faktyczny nowego najemcy.**

Dowód: 2026-08-24 o 10:47 przez Hub została założona firma **„Muszkieterowie"**. Tak wygląda po provisioningu, obok firmy, która realnie działa:

| | Muszkieterowie (nowa) | TestowaFirma | WB Partners (działa) |
|---|---|---|---|
| Jednostki organizacyjne | 1 (korzeń) | 1 | 8 |
| Typy jednostek | 3 | 3 | 3 |
| Role | 4 | 4 | 5 |
| **Stanowiska** | **0** | 0 | 5 |
| **Typy urlopów** | **0** | 0 | 4 |
| **Statusy zadań** | **0** | 0 | 4 |
| **Definicje obiegów** | **0** | 0 | 2 |
| Pracownicy | 0 | 1 | 10 |
| Relacje przełożony–podwładny | 0 | 0 | 1 |
| Polityki przerw / szablony grafiku | 0 / 0 | 0 / 0 | 0 / 0 |
| Konfiguracja najemcy | 0 | 0 | 4 |

Co z tego wynika dla szefa firmy w pierwszym dniu:

- **Nie złoży wniosku urlopowego** — lista typów urlopu jest pusta, nie ma czego wybrać.
- **Nie założy zadania** — `TaskItem` wymaga statusu, a statusów nie ma.
- **Nie ma obiegu akceptacji** — nie ma definicji obiegu, więc nie ma czego uruchomić.
- **Kierownik nie zobaczy swojego działu** — zakres `Department` wynika z przypisania na stanowisku z flagą „kierownicze", a stanowisk jest zero.
- **Kolejka akceptacji jest pusta u wszystkich** — akceptanta wyznacza relacja w `org_supervisor_relations`, a relacji jest zero.

### Dlaczego tak jest — przyczyna, nie objaw

`TenantProvisioningService.SeedAsync()` robi dokładnie dwie rzeczy: `IamSeeder.SeedTenantRbacAsync` (role, uprawnienia, zakresy, flagi) i `OrganizationSeeder.SeedTenantStructureAsync` (3 typy jednostek + korzeń). Koniec.

Seedery pozostałych słowników **istnieją, ale są globalne i jednorazowe**:

```csharp
// LeaveSeeder.cs
private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
{
    if (await dbContext.Set<LeaveType>().AnyAsync())   // ← istnieje GDZIEKOLWIEK
    {
        logger.LogInformation("Leave types already seeded, skipping.");
        return;
    }
    // ...wszystko tworzone dla DefaultTenantId
}
```

To samo w `TaskSeeder` i `WorkflowSeeder`. Są wołane raz, przy starcie aplikacji, z `DatabaseSeeder.SeedAsync` — z czasów, gdy WorkBase był jednofirmowy. Od kiedy w bazie jest choć jeden typ urlopu (czyli od pierwszego uruchomienia), **każda kolejna firma dostaje puste słowniki i nikt tego nie zauważa**, bo nic nie rzuca błędem.

**Wniosek projektowy:** konfigurator nie może być jedynym sposobem, w jaki firma dostaje działającą konfigurację. Najpierw trzeba naprawić provisioning, a konfigurator ma to, co provisioning ustawił, **potwierdzać i korygować**.

---

## 2. Zasada projektowa: domyślne przy provisioningu, kreator potwierdza

Dwa możliwe podejścia:

| | „Kreator tworzy konfigurację" | **„Provisioning tworzy, kreator potwierdza"** |
|---|---|---|
| Firma, która porzuciła kreator w połowie | niesprawna | sprawna, z domyślnymi |
| Firma, której ktoś wdraża ręcznie (my, przy większym kliencie) | musi przejść kreator | nie musi |
| Ryzyko rozjazdu „kreator vs panel admina" | wysokie — dwie ścieżki tworzenia | niskie — jedna, kreator tylko edytuje |
| Wysiłek | mniejszy na start | trochę większy na start |

Wybieram drugie. Kreator, który jest jedyną drogą do działającej instancji, jest pojedynczym punktem awarii dla całego wdrożenia — a mamy już dowód, że ludzie zakładają firmy przez Hub i zostawiają je (TestowaFirma stoi pusta od 2026-07-23).

**Konsekwencja praktyczna:** krok 0 tej fazy to naprawa seederów, nie ekran.

---

## 3. Zakres MVP — trzy decyzje, reszta domyślna

Kryterium doboru kroku: **czy da się za użytkownika rozstrzygnąć sensownie?** Jeśli tak — domyślna wartość i miejsce w podsumowaniu. Jeśli nie — pytanie.

Nie da się rozstrzygnąć za firmę tylko trzech rzeczy:

| # | Pytanie | Dlaczego nie da się domyślnie |
|---|---|---|
| 1 | **Kto tu pracuje?** | Nikt inny tego nie wie |
| 2 | **W jakich godzinach?** | Biuro 8–16, produkcja na zmiany, sklep w soboty — norma czasu pracy zależy od tego i bez niej nadgodziny liczą się bez sensu |
| 3 | **Kto akceptuje wnioski?** | Błąd tutaj = wniosek trafia do niewłaściwej osoby albo do nikogo |

Wszystko inne dostaje wartość domyślną: typy urlopów (26 dni wypoczynkowego, na żądanie, L4, opieka nad dzieckiem — te definicje już siedzą w `LeaveSeeder` i są zgodne z Kodeksem pracy), statusy zadań, obieg akceptacji urlopu, polityka przerw, kategorie dokumentów, nazewnictwo, branding.

### Ekrany

```
[0] Witamy w WorkBase                       ← bez pytań, ustawia oczekiwania
     „Zajmie 5–10 minut. Możesz przerwać — wrócimy tu przy następnym logowaniu."

[1] Kto tu pracuje?                          ← DECYZJA
     • wgraj plik z kadr (CSV/XLSX)  ← ścieżka główna
     • albo dopisz ręcznie (imię, nazwisko, e-mail)
     • albo „na razie tylko ja"
     Podgląd przed zapisem: ile wierszy wejdzie, ile odpadnie i dlaczego.

[2] W jakich godzinach pracujecie?           ← DECYZJA
     Domyślnie: pon–pt 8:00–16:00, przerwa 30 min niepłatna.
     Dla zmianowych: „mamy zmiany" → dwie/trzy zmiany z godzinami.
     Efekt: szablon grafiku + polityka przerw + norma dobowa.

[3] Kto akceptuje wnioski?                   ← DECYZJA
     Domyślnie: wszyscy → właściciel konta.
     Alternatywa: wskaż przełożonego dla każdej osoby (lista z podpowiedziami).
     Efekt: wpisy w org_supervisor_relations.

[4] Gotowe — co ustawiliśmy za Ciebie
     Lista domyślnych z linkiem „zmień" do właściwego ekranu w Ustawieniach.
     Przycisk: „Zacznij pracę".
```

**Cztery ekrany, trzy pytania.** Każde pytanie ma odpowiedź domyślną, więc „Dalej, Dalej, Dalej, Zacznij" daje działającą firmę jednoosobową w minutę — i to jest poprawny scenariusz, nie obejście.

### Ekran 4 jest ważniejszy, niż wygląda

Nietechniczny użytkownik nie wie, czego nie wie. Lista „ustawiliśmy za Ciebie: 26 dni urlopu wypoczynkowego, 4 typy nieobecności, statusy zadań, obieg akceptacji urlopu, przerwa 30 min" robi dwie rzeczy naraz: buduje zaufanie i **uczy, że te rzeczy w ogóle są konfigurowalne**. Bez tego ekranu za trzy miesiące przyjdzie zgłoszenie „a da się zmienić liczbę dni urlopu?".

---

## 4. Model techniczny

### Stan konfiguracji

Bez migracji — `cfg_tenant_configs` jest już magazynem klucz–wartość per najemca:

| Klucz | Wartość |
|---|---|
| `setup.completed_at` | znacznik czasu albo brak |
| `setup.current_step` | ostatni ukończony krok |
| `setup.skipped_steps` | kroki pominięte świadomie |

Dzięki temu kreator jest **wznawialny**: szef zamyka przeglądarkę na kroku 2 i wraca w to samo miejsce.

### Egzekwowanie po stronie serwera

Sam przekierunek we froncie nie wystarcza — adres da się wkleić. Filtr endpointów: dopóki `setup.completed_at` nie istnieje, wywołania API tego najemcy poza białą listą zwracają `409` z `errorCode: SETUP_REQUIRED`. Front łapie ten kod i przenosi do kreatora.

Biała lista musi obejmować: `/api/auth/me`, `/api/setup/*`, wszystko pod `/api/hub/*` (żeby nie zablokować SSO i webhooków), `/health`, oraz odczyty potrzebne samemu kreatorowi. **To jest miejsce, w którym najłatwiej zablokować sobie własną aplikację** — dlatego biała lista wchodzi razem z testem, który przechodzi po `EndpointDataSource` i sprawdza, że nic spoza niej nie odpowiada 200 przy nieukończonej konfiguracji. Wzorzec jest już w repo: `ZakresDanychPracownikaTests`.

### API

```
GET  /api/setup/state          → { ukonczony, aktualnyKrok, kroki[] }
POST /api/setup/employees      → import/dodanie (istniejące komendy, nie nowa logika)
POST /api/setup/working-hours  → szablon grafiku + polityka przerw
POST /api/setup/approvals      → relacje przełożonych
POST /api/setup/complete       → ustawia setup.completed_at
```

Każdy krok woła **istniejące komendy** (`ImportEmployeesCommand`, `CreateEmployeeCommand`, `SetSupervisorCommand`…). Kreator jest cienką warstwą nad tym, co panel admina już potrafi — inaczej za pół roku będą dwie ścieżki tworzenia stanowisk i jedna z nich będzie miała błąd.

### Kto widzi kreator

- **Właściciel / Admin** → kreator.
- **Pracownik, który zalogował się wcześniej niż szef skończył** → ekran „Twoja firma jest w trakcie konfiguracji. Wróć za chwilę." Bez tego zobaczy pustą aplikację i uzna, że nie działa.

---

## 5. Rozbudowa — dwa dalsze etapy

Kreator z §3 domyka „żeby działało". To za mało, żeby firma z 40 osobami realnie na tym pracowała.

### Etap 2 — konfiguracja jako lista, nie jednorazowy kreator

Po ukończeniu kreator **nie znika, tylko zmienia postać**: w panelu administratora zostaje kafelek „Konfiguracja firmy — ukończono 5 z 12" z listą tego, czego jeszcze nie ruszono. Każda pozycja prowadzi do właściwego ekranu w Ustawieniach.

Dlaczego tak, a nie dłuższy kreator: nietechniczny użytkownik nie przejdzie dwunastu kroków za pierwszym razem, ale **wróci do listy**, kiedy natrafi na potrzebę. To ta sama treść, podana wtedy, kiedy ma sens.

Pozycje listy (wszystkie mają już ekran w `/admin/*`, brakuje tylko spięcia w listę): działy i struktura · stanowiska · nazewnictwo · logo i kolory · kategorie dokumentów · szablony powiadomień · reguły eskalacji · polityki urlopowe · statusy zadań · dodatkowe obiegi · integracja z czatem · kiosk / rejestracja QR.

### Etap 3 — kreator, który wie coś o firmie

Trzy rozszerzenia w kolejności wartości:

**a) Profil branżowy.** Jedno pytanie na starcie („biuro / produkcja / handel / usługi w terenie") zmienia domyślne: produkcja dostaje zmiany i kiosk, teren dostaje geofence i aplikację mobilną, biuro dostaje 8–16 i dokumenty. To jest ta sama mechanika co dziś, tylko z inną tabelą domyślnych — tanie, a bardzo podnosi trafność.

**b) Kontrola stanu zamiast ciszy.** Ekran „Co jeszcze nie zadziała" liczony z danych, nie z checklisty:

- „3 pracowników nie ma przełożonego — ich wnioski urlopowe nie będą miały akceptanta"
- „12 osób nie ma stawki godzinowej — nie policzymy im wynagrodzenia"
- „Nikt nie ma przypisanego stanowiska kierowniczego — żaden kierownik nie zobaczy danych działu"
- „5 osób nie przyjęło zaproszenia do WorkBase"

Każdy z tych warunków to jedno zapytanie do bazy i każdy odpowiada realnej pułapce, którą znamy z produkcji. **To jest najwartościowszy element całej rozbudowy** — zamienia ciszę w konkretne zdanie.

**c) Kreator ponownego użycia.** Ten sam mechanizm przy dodawaniu nowego działu albo oddziału: „skonfiguruj dział" z tymi samymi trzema pytaniami w mniejszej skali.

---

## 6. Czego świadomie nie robimy

- **Nie przenosimy do kreatora wszystkich 17 ekranów administracyjnych.** Byłaby to ta sama złożoność w innym opakowaniu.
- **Nie blokujemy pracy do czasu „100% konfiguracji".** Blokada dotyczy wyłącznie stanu przed ukończeniem MVP-owych czterech ekranów.
- **Nie budujemy edytora obiegów w kreatorze.** Domyślny obieg akceptacji urlopu wystarcza na start; kto potrzebuje więcej, ma kreator obiegów w Ustawieniach (dopisany do nawigacji w Fazie A).
- **Nie robimy własnego importera XLSX na backendzie.** Import CSV działa i po poprawkach z B4 radzi sobie z plikami z polskich programów kadrowych.

---

## 7. Kolejność wdrożenia

| # | Krok | Nakład | Dlaczego w tej kolejności |
|---|---|---|---|
| 1 | ✅ **Wykonane 2026-08-24** — seedery `Leave`/`Task`/`Workflow` na per-najemcę, wołane z `TenantProvisioningService` | ~2 h | Bez tego kreator jest jedyną drogą do działającej firmy |
| 2 | ✅ **Wykonane 2026-08-24** — stan konfiguracji + `GET /api/setup/state` + `POST /api/setup/complete` + blokada `SETUP_REQUIRED` z białą listą i testami | ~2 h | Szkielet, na którym wiszą ekrany |
| 3 | ✅ **Wykonane 2026-08-26** — ekran 1 (ludzie), na istniejącym parserze CSV | | Najcięższy i najważniejszy krok |
| 4 | ✅ **Wykonane 2026-08-26** — ekrany 2 i 3 (godziny, akceptacje) | | |
| 5 | ✅ **Wykonane 2026-08-26** — ekran 4 (podsumowanie „ustawiliśmy za Ciebie") | | Tani, a robi najwięcej dla zaufania |
| 6 | Ekran „firma w trakcie konfiguracji" dla pracowników | 0,5 dnia | |
| 7 | Etap 2 — lista konfiguracji w panelu admina | 2 dni | Po MVP |
| 8 | Etap 3b — kontrola stanu „co jeszcze nie zadziała" | 2 dni | Największa wartość z rozbudowy |

Razem MVP (kroki 2–6): **około 6–7 dni roboczych.**

### ✅ Krok 1 wykonany 2026-08-24

Zajął ~2 godziny zamiast szacowanych 1–2 dni, bo `WorkflowSeeder` **już miał** wariant per-najemcę (`SeedTenantAsync`, idempotentny, z komentarzem opisującym dokładnie ten problem) — tylko nikt go nie wołał z provisioningu. Dopisane zostały analogiczne warianty w `LeaveSeeder` i `TaskSeeder`, a wszystkie trzy spięte w `TenantProvisioningService.SeedTenantBaselineAsync`.

**Migracja uzupełniająca okazała się niepotrzebna.** `SeedTenantBaselineAsync` wykonuje się także dla firm **już istniejących**: `Program.cs` przy starcie woła `HubEntitlementsSyncService.SyncAllAsync()`, ta przechodzi po wszystkich firmach z `HubProductInstanceId` i dla każdej wywołuje `EnsureHubTenantAsync` → baseline. Skoro seedery są dopisujące, backfill przychodzi sam przy najbliższym wdrożeniu. Sprawdzone na produkcji: **wszystkie cztery firmy mają `hub_product_instance_id`**, więc obejmie każdą.

Zastrzeżenie: `SyncInstanceAsync` najpierw pyta Hub o konfigurację instancji i przerywa, gdy nie dostanie odpowiedzi. Firma, której instancji Hub już nie zna, nie zostanie uzupełniona — wyjdzie to w logach jako pominięta synchronizacja.

6 testów w `SlownikiNowejFirmyTests` pilnuje trzech własności naraz: komplet dla nowej firmy (4 typy urlopu, 4 statusy zadań z dokładnie jednym domyślnym i co najmniej jednym końcowym, 4 priorytety, 2 obiegi), idempotentność przy trzykrotnym wywołaniu (provisioning powtarza się przy **każdej** synchronizacji, więc seeder tworzący duplikaty puchłby przy każdym starcie) oraz brak wpływu na dane innej firmy.

**Stan po wdrożeniu:** nowa firma będzie mogła złożyć wniosek urlopowy i założyć zadanie od pierwszego logowania — bez kreatora. Kreator z §3 pozostaje potrzebny do trzech rzeczy, których nie da się zasiać: ludzi, godzin pracy i tego, kto akceptuje.

### ✅ Krok 2 wykonany 2026-08-24

Szkielet blokady, bez migracji — stan siedzi w `cfg_tenant_configs` przez istniejący `ITenantConfigService`.

**Zabezpieczenie z definicji, nie z ostrożności.** Znacznik `setup.required` zapisywany jest **wyłącznie na ścieżce tworzenia firmy** (`EnsureHubTenantAsync` w gałęzi nowej firmy oraz `CreateTenantAsync`). Ponowna synchronizacja z Hubem przechodzi gałęzią firmy istniejącej i znacznika nie dotyka, więc firmy założone przed powstaniem kreatora **nie mogą zostać zablokowane** — dotyczy to WB Partners, które jest w codziennym użyciu. Rozważana alternatywa („zgadnij po danych, czy firma jest skonfigurowana") odpadła: pierwsza pomyłka heurystyki zamyka dostęp działającej firmie.

Blokada to middleware **po uwierzytelnieniu** (firmę czyta z roszczenia w tokenie), zwracający `409` z `errorCode: SETUP_REQUIRED`. Żądania bez firmy w tokenie przechodzą bez zmian — to ruch anonimowy albo trasy techniczne.

Stan jest cache'owany w pamięci na minutę: czytamy go przy **każdym** żądaniu, więc bez tego blokada dokładałaby zapytanie do bazy do całego ruchu. Ukończenie kreatora czyści wpis od razu, żeby użytkownik nie czekał na wygaśnięcie.

**Biała lista pilnowana testami, nie uwagą przy przeglądzie** — 19 testów w `KonfiguracjaStartowaTests`:

- 11 tras krytycznych sprawdzanych z osobna, każda z powodem w treści testu (`/api/setup/*` — bo inaczej kreatora nie da się ukończyć; `/api/auth/me` — bo interfejs nie odczyta uprawnień; `/api/hub/*` i `/sso/*` — bo to odcina logowanie; `/api/billing/webhook`; `/health`; `/hubs`; `/`).
- Bramka odwrotna: gdyby ktoś rozszerzył listę o zbyt ogólny prefiks (np. `/api`), blokada przestałaby chronić cokolwiek, a testy pojedynczych tras by tego nie zauważyły. Dlatego osobny test liczy trasy z `EndpointDataSource` i wymaga, żeby przepuszczana była mniejszość.
- Trzy testy zachowania: firma bez znacznika nigdy nie dostaje 409; firma z nieukończoną konfiguracją dostaje 409 z kodem; kreator działa mimo blokady i po `POST /api/setup/complete` reszta aplikacji odpowiada normalnie.

### ✅ Kroki 3–5 wykonane 2026-08-26 — MVP kreatora działa

Backend: `POST /api/setup/employees`, `/working-hours`, `/approvals` plus `GET /api/setup/employees`. Ten ostatni **musi** żyć pod `/api/setup`, bo ekran akceptantów potrzebuje listy pracowników, a `/api/org/employees` jest za blokadą. Lista jest okrojona (identyfikator, imię, nazwisko, e-mail) — pełne `EmployeeDto` niesie stawkę godzinową, a kreator nie ma powodu jej pokazywać.

**Ryzyko z §8 rozbrojone.** `ImportEmployeesCommand` i `CreateEmployeeCommand` dostały flagę `ZapraszajDoHuba`, domyślnie `true`, żeby nie zmienić zachowania panelu administratora. Kreator przekazuje `false` i pyta osobno przełącznikiem „wyślij zaproszenia od razu”. Trzy testy jednostkowe pilnują obu stron tej decyzji — że kreator nie zaprasza i że panel nadal zaprasza.

**Wznawialność:** `setup.current_step` i `setup.skipped_steps` w `cfg_tenant_configs`, `GET /state` zwraca `aktualnyKrok`, `pominieteKroki` i listę kroków. Kreator wraca na krok następny po ostatnim zapisanym.

Front: `frontend/src/pages/setup/KreatorStartuPage.tsx`, zamontowany w `App.tsx` **poza `AppRoutes`**, obok `/kiosk` — z dokładnie tego powodu, który przewidywał ten dokument. Klient API (`frontend/src/api/client.ts`) łapie `409` z `errorCode: SETUP_REQUIRED` i przenosi na `/kreator`; trzy testy pilnują, żeby nie przenosił, gdy już tam jesteśmy (pętla przeładowań), i żeby nie reagował na zwykły konflikt 409.

Ekran „ludzie” korzysta z istniejących `odczytajCsv`/`parseCsv`/`parsujDateZatrudnienia`, więc obsługa Windows-1250 i polskich formatów daty przychodzi za darmo. Kolumny rozpoznaje po nagłówkach; gdy ich nie znajdzie, mówi wprost, żeby dodać osoby ręcznie albo użyć pełnego importu po zakończeniu kreatora — zamiast udawać, że rozumie plik.

**Krok 6 rowniez wykonany.** Bramka wpuszcza do kreatora kazdego uzytkownika zablokowanej firmy, nie tylko wlasciciela — pracownik, ktory zaloguje sie pierwszy, ladowal w cudzym kreatorze i mogl w nim zaimportowac ludzi. Zamkniete z dwoch stron:

- **Serwer:** kroki zapisujace dostaly te same uprawnienia, co ich odpowiedniki w panelu (`org.create`, `time.manage`, `org.edit`). Role sa zasiewane przy tworzeniu firmy, jeszcze przed pierwszym logowaniem, wiec wlasciciel przychodzacy z Huba jako Admin ma je od poczatku; szeregowy pracownik nie ma zadnego z nich. Cztery testy.
- **Interfejs:** kto nie ma `org.create` ani `org.edit`, widzi ekran „Firma jest w trakcie konfiguracji" zamiast formularza, ktorego i tak nie moglby wyslac.

`POST /api/setup/complete` zostaje **swiadomie otwarte**. Zdjecie blokady niczego nie niszczy — firma ma komplet domyslnych z provisioningu — a zamkniecie zamienialoby kazda pomylke w przypisaniu roli wlascicielowi w trwale zablokowana firme bez wyjscia. Pilnuje tego osobny test, zeby nikt nie „poprawil" tego przez przypadek.

### Czwarte pytanie dodane 2026-08-26 — wymiar urlopu

Projekt zakładał trzy pytania i to była dobra domyślna decyzja, ale jedna rzecz wypadła z niej niesłusznie: **nowa firma dostaje 26 dni urlopu wypoczynkowego z seedera i nikt się o tym nie dowiaduje**. To jest ustawienie firmy, nie nasze, więc właściciel powinien je przynajmniej zobaczyć.

Krok `urlop` (`GET`/`POST /api/setup/leave`) pokazuje aktualną wartość i pozwala ją zmienić. Ekran podaje dla orientacji, że Kodeks pracy przewiduje 20 albo 26 dni zależnie od stażu — **jako informację**. System nie sprawdza tej liczby i nie narzuca żadnej wartości; test pilnuje, że 35 dni przechodzi tak samo jak 26. Odrzucane są wyłącznie wartości, które nie są liczbą dni w roku.

Firma, która skasowała typ urlopu wypoczynkowego, dostaje krok oznaczony jako pominięty — nie odtwarzamy go za nią.

### Stan po kroku 2 (zapis historyczny)

**Czego jeszcze nie ma:** samych kroków kreatora (`POST /api/setup/employees`, `/working-hours`, `/approvals`) i strony we froncie. Front musi łapać `SETUP_REQUIRED` i przenosić do kreatora, a sam kreator renderować się **poza `MainLayout`** — tak jak `KioskPage` — bo powłoka aplikacji odpytuje trasy, które blokada odcina.

---

## 8. Ryzyka

| Ryzyko | Reakcja |
|---|---|
| Filtr `SETUP_REQUIRED` zablokuje SSO albo webhooki z Huba i firma nie zaloguje się w ogóle | Biała lista wchodzi razem z testem po `EndpointDataSource`; `/api/hub/*` i `/api/auth/me` zawsze przepuszczane |
| Szef przejdzie kreator „Dalej, Dalej" i zostanie z domyślnymi, które mu nie pasują | Ekran 4 nazywa każdą domyślną wprost i daje link „zmień"; etap 3b później wyłapuje skutki |
| Import 40 osób rozsyła 40 zaproszeń z Huba, zanim szef jest gotowy | Krok „ludzie" musi mieć świadomy wybór: „zaproś teraz" albo „dodaj bez zapraszania" — dziś `ImportEmployeesCommand` kolejkuje zaproszenie **zawsze**, to wymaga zmiany |
| Kreator i panel admina rozjadą się z czasem | Kreator woła wyłącznie istniejące komendy, nie ma własnej logiki zapisu |
| Migracja domyślnych dla firm już istniejących nadpisze czyjąś konfigurację | Seedery zostają idempotentne i dopisujące — wzorzec `IamSeeder.BackfillMissingPermissionsAsync` jest już w repo i sprawdzony na produkcji |
