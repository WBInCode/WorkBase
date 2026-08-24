# WorkBase — Roadmapa MVP i etapowanie produktu

> Dokument roboczy dla zespołu produktowo-technicznego.
> Wersja: 0.1 | Data: 2026-03-24 | Status: Draft
> Dokument bazowy: 01-product-foundation.md

---

# 1. Założenia roadmapy

## 1.1. Filozofia etapowania

System budujemy **koncentrycznie** — od jądra na zewnątrz:

```
Warstwa 0: Fundament techniczny (auth, multi-tenancy, struktura, role)
Warstwa 1: Filary wartości (czas pracy, zadania)
Warstwa 2: Warstwa procesowa (workflow, akceptacje, urlopy)
Warstwa 3: Warstwa kierownicza (dashboard, raporty, alerty)
Warstwa 4: Rozszerzenia (case management, kontrahenci, integracje)
Warstwa 5: Premium (sprzedaż, AI, moduły działowe, desktop)
```

Każda warstwa **wymaga poprzedniej**. Nie można budować dashboardu kierowniczego bez danych o czasie pracy i zadaniach. Nie można budować workflow bez struktury organizacyjnej i ról.

## 1.2. Założenia czasowe

- MVP nie jest wersją demo — to wersja, którą **pierwsza firma może realnie wdrożyć i używać na co dzień**.
- Post-MVP to rozszerzenia, które podnoszą wartość i skalują produkt na większe organizacje.
- Premium/future to elementy, które wymagają dojrzałości systemu i walidacji rynkowej.

## 1.3. Założenia zespołowe

Roadmapa zakłada równoległy rozwój backendu i frontendu. Nie zakłada konkretnego rozmiaru zespołu, ale wyznacza **logiczną kolejność**, która minimalizuje blokady między obszarami.

## 1.4. Kanały dostarczania per etap

| Etap | Web | Mobile | Desktop | Kiosk |
|---|---|---|---|---|
| MVP | ✅ Pełny | ✅ Podstawowy (PWA/Capacitor) | ❌ | ✅ Tryb fullscreen web |
| Post-MVP | ✅ Pełny | ✅ Rozbudowany | ❌ | ✅ Ulepszony |
| Premium | ✅ Pełny | ✅ Pełny (native features) | ✅ Wrapper (Tauri) | ✅ Dedykowany mode |

---

# 2. Kryteria doboru MVP

Feature wchodzi do MVP **tylko wtedy**, gdy spełnia **co najmniej 3 z 5** poniższych kryteriów:

| # | Kryterium | Opis |
|---|---|---|
| C1 | **Blokuje start** | Bez tego firma nie może zacząć używać systemu |
| C2 | **Daje natychmiastową wartość** | Firma widzi korzyść od pierwszego dnia |
| C3 | **Jest fundamentem dla innych modułów** | Inne features od tego zależą |
| C4 | **Wyróżnia na rynku** | Bez tego system jest „jeszcze jednym narzędziem" |
| C5 | **Jest wymagany regulacyjnie / operacyjnie** | Np. audyt, bezpieczeństwo danych |

Feature **nie wchodzi** do MVP, jeśli:
- Dotyczy tylko części firm (moduł opcjonalny)
- Wymaga integracji z systemami zewnętrznymi
- Jest „nice-to-have" dla UX, ale nie blokuje użytkowania
- Wymaga dojrzałości danych (np. AI, trendy, predykcja)
- Może być zastąpiony prostszym rozwiązaniem tymczasowym

---

# 3. MVP

## 3.1. Cel MVP

> Pierwsza firma (30–100 pracowników z zespołami i kierownictwem) może wdrożyć system i codziennie używać go do: rejestracji czasu pracy, zarządzania zadaniami, składania i akceptacji wniosków urlopowych, oraz podglądu stanu operacyjnego przez kierownika.

## 3.2. Moduły MVP

### MODUŁ M0: Fundament techniczny
**Priorytet: MUST-HAVE | Blokuje: wszystko**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| M0.1 | Projekt, repo, CI/CD pipeline | must-have | Bez infrastruktury nie ma kodu |
| M0.2 | Architektura modularnego monolitu (.NET) | must-have | Podział na moduły domenowe, shared kernel |
| M0.3 | Baza danych PostgreSQL + migracje EF Core | must-have | Schema, tenant isolation, seed data |
| M0.4 | Autentykacja i autoryzacja (Keycloak) | must-have | Login, rejestracja użytkownika, JWT, refresh |
| M0.5 | Multi-tenancy (shared DB + tenant_id) | must-have | Izolacja danych firm |
| M0.6 | REST API — szkielet, konwencje, wersjonowanie | must-have | Kontrakt API, Swagger/OpenAPI |
| M0.7 | Frontend — szkielet React 19 + TS + routing + layout | must-have | Shell aplikacji, nawigacja, auth flow |
| M0.8 | Mechanizm feature flags / module registry | must-have | Włączanie modułów per tenant |
| M0.9 | Logowanie (Serilog) + structured logging | must-have | Diagnostyka od dnia 1 |
| M0.10 | Konteneryzacja (Docker) + docker-compose dev | must-have | Lokalne środowisko developerskie |
| M0.11 | Mechanizm i18n (backend + frontend) | should-have | Framework tłumaczeń; PL jako pierwszy język |
| M0.12 | Storage plików (MinIO/S3) | must-have | Załączniki, dokumenty |
| M0.13 | System powiadomień — warstwa bazowa | must-have | In-app notifications, transport email |
| M0.14 | Background jobs (Hangfire) | must-have | Alerty, przypomnienia, automatyzacje asynchroniczne |
| M0.15 | SignalR — real-time notifications | should-have | Live updates powiadomień, statusów |

---

### MODUŁ M1: Struktura organizacyjna
**Priorytet: MUST-HAVE | Blokuje: M2, M3, M4, M5, M6, M7, M8**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| M1.1 | CRUD jednostek organizacyjnych (firma, dział, zespół…) | must-have | Fundament struktury |
| M1.2 | Hierarchia jednostek (closure table) — dowolna głębokość | must-have | Drzewo organizacyjne |
| M1.3 | Konfiguracja typów jednostek per tenant | must-have | Elastyczność struktur |
| M1.4 | CRUD pracowników (profil, dane podstawowe) | must-have | Bez pracowników nie ma systemu |
| M1.5 | Przypisanie pracownika do jednostki i stanowiska | must-have | Powiązanie z hierarchią |
| M1.6 | Relacja przełożony–podwładny | must-have | Approval flow, widoczność |
| M1.7 | Widok drzewa organizacyjnego (frontend) | must-have | Wizualizacja struktury |
| M1.8 | Import pracowników (CSV) | should-have | Szybki onboarding firmy |
| M1.9 | Obsługa wielu przypisań (pracownik w 2 zespołach) | nice-to-have | Edge case — odłożyć, jeśli komplikuje MVP |

---

### MODUŁ M2: Role i uprawnienia
**Priorytet: MUST-HAVE | Blokuje: M3, M4, M5, M6, M7, M8**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| M2.1 | Definicja ról (CRUD, nazwa, opis, poziom hierarchii) | must-have | Mechanizm ról |
| M2.2 | Role predefiniowane (Super Admin, Admin) — nieusuwalne | must-have | Bezpieczeństwo |
| M2.3 | Szablony ról startowych (Kierownik, Pracownik, HR) | must-have | Szybki setup |
| M2.4 | Matryca uprawnień per rola per moduł per akcja | must-have | Kontrola dostępu |
| M2.5 | Przypisanie ról do użytkowników | must-have | Powiązanie rola ↔ user |
| M2.6 | Panel administracyjny ról i uprawnień (frontend) | must-have | Admin musi to zarządzać z UI |
| M2.7 | Walidacja uprawnień na backendzie (middleware / filter) | must-have | Security enforcement |
| M2.8 | Synchronizacja ról Keycloak ↔ WorkBase | must-have | Spójność auth |

---

### MODUŁ M3: Zakres widoczności danych
**Priorytet: MUST-HAVE | Blokuje: M4, M5, M6, M7, M8**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| M3.1 | Mechanizm data scope (global / oddział / dział / zespół / indywidualny) | must-have | Izolacja widoczności |
| M3.2 | Automatyczne scope na podstawie pozycji w strukturze | must-have | Kierownik widzi swój zespół automatycznie |
| M3.3 | Konfiguracja scope per rola per moduł | must-have | Admin definiuje kto co widzi |
| M3.4 | Filtrowanie danych po scope na backendzie (query filter) | must-have | Security — nie można obejść widoczności |
| M3.5 | UI respektujące scope (listy, widoki, wyszukiwarka) | must-have | Frontend nie pokazuje więcej niż backend pozwala |

---

### MODUŁ M4: Czas pracy i obecności
**Priorytet: MUST-HAVE (filar #1) | Zależności: M0, M1, M2, M3**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| M4.1 | Clock-in / clock-out (przycisk web) | must-have | Podstawowa rejestracja |
| M4.2 | Przerwy (start / stop) | must-have | Część czasu pracy |
| M4.3 | Clock-in przez QR code (generowanie + skanowanie) | must-have | Preferowana metoda rejestracji |
| M4.4 | Tryb KIOSK (web fullscreen, identyfikacja PIN/QR) | should-have | Punkt odbicia przy wejściu |
| M4.5 | Timesheet dzienny (widok pracownika) | must-have | Przegląd dnia |
| M4.6 | Timesheet tygodniowy / miesięczny (widok pracownika) | must-have | Przegląd okresu |
| M4.7 | Raport czasu pracy per pracownik (widok kierownika) | must-have | Kontrola managerska |
| M4.8 | Raport zbiorczy czasu pracy per zespół/dział | must-have | Widok operacyjny |
| M4.9 | Grafik / zmiana (przypisanie pracownika do godzin pracy) | must-have | Bez grafiku nie ma anomalii |
| M4.10 | Wykrywanie anomalii — podstawowe (brak clock-out, spóźnienie, podwójne wejście) | must-have | Alerta kierownika |
| M4.11 | Ręczna korekta czasu pracy przez kierownika | must-have | Korekty błędów |
| M4.12 | Historia zdarzeń czasu pracy | must-have | Audyt |
| M4.13 | NFC clock-in (mobile) | should-have | Rozszerzenie rejestracji — wymaga mobile z Web NFC lub Capacitor |
| M4.14 | Geolokalizacja przy clock-in (opcjonalna metadana) | nice-to-have | Dodatkowa kontrola — nie krytyczne na MVP |
| M4.15 | Zaawansowane anomalie (zbyt długa/krótka zmiana, praca w dzień wolny, brak odbicia mimo obecności) | should-have | Pełniejsza kontrola |

---

### MODUŁ M5: Urlopy, nieobecności i wnioski
**Priorytet: MUST-HAVE | Zależności: M0, M1, M2, M3, M7 (approval flow)**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| M5.1 | Typy nieobecności (konfigurowalna lista per tenant) | must-have | Elastyczność |
| M5.2 | Limity urlopowe per typ per pracownik | must-have | Kontrola puli |
| M5.3 | Naliczanie puli (roczne, proporcjonalnie do zatrudnienia) | must-have | Automatyzacja limitu |
| M5.4 | Saldo: wykorzystane / pozostałe / planowane | must-have | Widok pracownika i kierownika |
| M5.5 | Formularz wniosku urlopowego | must-have | Punkt wejścia |
| M5.6 | Workflow akceptacji wniosku (single-level: kierownik) | must-have | Approval flow |
| M5.7 | Odrzucenie + komentarz | must-have | Pełna ścieżka |
| M5.8 | Historia wniosków i decyzji | must-have | Audyt |
| M5.9 | Kalendarz nieobecności (per zespół / dział) | must-have | Widok dostępności |
| M5.10 | Alerty o konfliktach urlopowych | should-have | Ostrzeżenie przy zbyt wielu nieobecnych |
| M5.11 | Załącznik do wniosku (np. L4) | should-have | Dokumentowanie |
| M5.12 | Przenoszenie dni urlopowych na kolejny rok (konfigurowalnie) | nice-to-have | Bardziej zaawansowany scenariusz |
| M5.13 | Wielopoziomowa akceptacja (kierownik → HR) | nice-to-have | Wchodzi z dojrzeniem workflow engine |

---

### MODUŁ M6: Zadania i egzekucja pracy
**Priorytet: MUST-HAVE (filar #2) | Zależności: M0, M1, M2, M3**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| M6.1 | Tworzenie zadania (tytuł, opis, deadline, priorytet) | must-have | Podstawowy CRUD |
| M6.2 | Przypisanie do osoby | must-have | Bez tego nie ma egzekucji |
| M6.3 | Statusy zadań (konfigurowalne per tenant) | must-have | Flow pracy |
| M6.4 | Zmiana statusu + walidacja przejść | must-have | Kontrola procesu |
| M6.5 | Komentarze do zadania | must-have | Komunikacja w kontekście |
| M6.6 | Załączniki do zadania | must-have | Artefakty pracy |
| M6.7 | Lista zadań (filtrowanie, sortowanie, wyszukiwanie) | must-have | Przegląd pracy |
| M6.8 | Widok „Moje zadania" (workspace) | must-have | Co mam do zrobienia |
| M6.9 | Priorytetyzacja (konfigurowalna lista priorytetów) | must-have | Ustalanie ważności |
| M6.10 | Delegowanie zadania | should-have | Przekazywanie pracy |
| M6.11 | Akceptacja wykonania (przełożony) | should-have | Kontrola jakości |
| M6.12 | Przypomnienia o zbliżającym się terminie | should-have | Proaktywność |
| M6.13 | Alerty o opóźnieniu (termin minął) | should-have | Eskalacja problemów |
| M6.14 | Historia zmian zadania | must-have | Audyt |
| M6.15 | Pole „następny krok" | nice-to-have | Follow-up — wartościowe, ale nie krytyczne na MVP |
| M6.16 | Zależności między zadaniami | nice-to-have | Komplikuje — odłożyć |
| M6.17 | Automatyczne zadania po zmianie statusu | nice-to-have | Wymaga dojrzałego workflow engine |

---

### MODUŁ M7: Procesy, workflow i akceptacje — silnik podstawowy
**Priorytet: MUST-HAVE | Zależności: M0, M1, M2 | Blokuje: M5 (approval), M6 (task flow)**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| M7.1 | Silnik statusów + przejść (state machine per typ obiektu) | must-have | Fundament workflow |
| M7.2 | Definicja workflow jako konfiguracja (JSON w bazie) | must-have | Konfigurowalność |
| M7.3 | Approval flow — single-level (jeden akceptant) | must-have | Minimum dla urlopów i zadań |
| M7.4 | Automatyczne przypisanie akceptanta (wg relacji w strukturze) | must-have | „Przypisz do kierownika wnioskodawcy" |
| M7.5 | Powiadomienie o oczekującej akceptacji | must-have | Bez tego approval nie działa |
| M7.6 | Akcja: zatwierdź / odrzuć / cofnij do poprawy | must-have | Pełna ścieżka decyzyjna |
| M7.7 | Historia decyzji z timestampami | must-have | Audyt |
| M7.8 | Predefiniowane workflow (urlop, zadanie) | must-have | Out-of-the-box |
| M7.9 | Approval wielopoziomowy (łańcuch akceptantów) | should-have | Ważne dla większych firm |
| M7.10 | Automatyczne eskalacje (brak reakcji > X godzin) | should-have | Zapobieganie blokadom |
| M7.11 | Delegowanie akceptacji (na zastępcę) | nice-to-have | Edge case — nie blokuje MVP |
| M7.12 | Reguły warunkowe w workflow (if kwota > X, then…) | nice-to-have | Zaawansowane — post-MVP |

---

### MODUŁ M8: Widok kierowniczy — dashboard MVP
**Priorytet: MUST-HAVE (przewaga produktu) | Zależności: M1, M3, M4, M5, M6**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| M8.1 | Dashboard — kto jest obecny / nieobecny (dziś) | must-have | Podstawowa informacja operacyjna |
| M8.2 | Dashboard — kto się spóźnił | must-have | Anomalie |
| M8.3 | Dashboard — zadania otwarte / zaległe (per zespół) | must-have | Stan pracy |
| M8.4 | Dashboard — akceptacje oczekujące na moją decyzję | must-have | Akcje do podjęcia |
| M8.5 | Dashboard — alerty i wyjątki (anomalie czasu pracy) | must-have | Sygnały o problemach |
| M8.6 | Scope dashboardu per rola (kierownik → zespół, dyrektor → dział) | must-have | Widoczność hierarchiczna |
| M8.7 | Raport czasu pracy per zespół (zestawienie godzinowe) | should-have | Raportowanie podstawowe |
| M8.8 | Raport nieobecności per zespół (kalendarz) | should-have | Planowanie zasobów |
| M8.9 | Eksport raportów CSV/Excel | should-have | Wymóg operacyjny |

---

### MODUŁ M-CARDS: Karty rekordów 360 — wariant MVP
**Priorytet: MUST-HAVE | Zależności: M1, M4, M5, M6**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| MC.1 | Karta pracownika (dane, zespół, czas pracy, urlopy, zadania, historia) | must-have | Centralny widok osoby |
| MC.2 | Karta zespołu (skład, obecność, zadania, urlopy) | must-have | Widok kierownika |
| MC.3 | Karta zadania (dane, komentarze, historia, załączniki) | must-have | Widok szczegółów zadania |
| MC.4 | Mechanizm generyczny karty (entity card renderer) | should-have | Fundament rozszerzalności |
| MC.5 | Pola własne (custom fields) na kartach | should-have | Konfigurowalność |

---

### MODUŁ M-WS: Workspace użytkownika — wariant MVP
**Priorytet: MUST-HAVE | Zależności: M4, M5, M6, M7**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| MW.1 | Widok startowy „co mam dziś do zrobienia" | must-have | Jedno miejsce pracy |
| MW.2 | Moje zadania (lista z terminami i priorytetami) | must-have | Egzekucja pracy |
| MW.3 | Moje akceptacje (oczekujące na decyzję) | must-have | Workflow |
| MW.4 | Mój czas pracy (dzisiejszy status: clock-in, godziny) | must-have | Self-service |
| MW.5 | Moje wnioski (urlopy — status) | must-have | Self-service |
| MW.6 | Ostatnia aktywność (feed) | should-have | Kontekst |
| MW.7 | Powiadomienia in-app | must-have | Informowanie o zdarzeniach |

---

### MODUŁ M-AUD: Audyt i historia
**Priorytet: MUST-HAVE | Zależności: M0**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| MA.1 | Audyt trail — automatyczny log akcji (append-only) | must-have | Compliance, bezpieczeństwo |
| MA.2 | Logowanie: kto, co, kiedy, co zmienił (diff) | must-have | Pełen kontekst |
| MA.3 | Timeline na kartach rekordów (historia pracownika, zadania) | must-have | Widok historyczny |
| MA.4 | Przeglądarka audytu dla admina | should-have | Narzędzie administracyjne |

---

### MODUŁ M-MOB: Mobile — wariant MVP
**Priorytet: SHOULD-HAVE | Zależności: M0 (API), M4, M5, M6**

| # | Feature | Priorytet | Uzasadnienie |
|---|---|---|---|
| MM.1 | PWA / Capacitor shell (instalacja na telefonie) | must-have | Kanał mobilny |
| MM.2 | Clock-in / clock-out z telefonu | must-have | Główny use case mobilny |
| MM.3 | Skanowanie QR z aparatu | must-have | Rejestracja czasu pracy |
| MM.4 | Składanie wniosku urlopowego | should-have | Self-service mobile |
| MM.5 | Akceptacja wniosku / zadania (manager quick actions) | should-have | Szybkie decyzje |
| MM.6 | Podgląd „mój dzień" (uproszczony workspace) | should-have | Szybka orientacja |
| MM.7 | Push notifications | should-have | Informowanie w czasie rzeczywistym |
| MM.8 | NFC clock-in | nice-to-have | Wymaga Capacitor plugin lub Web NFC |

---

## 3.3. Czego świadomie NIE bierzemy do MVP

| Element | Dlaczego odłożony | Kiedy wróci |
|---|---|---|
| Case management / zgłoszenia | Wymaga dojrzałego workflow engine; firmy radzą sobie mailem/Teamsem | Post-MVP |
| Kontrahenci i relacje | Moduł opcjonalny; nie każda firma potrzebuje | Post-MVP |
| Sprzedaż (leady, pipeline) | Nie jest rdzeniem — świadoma decyzja produktowa | Premium |
| Integracja Google Workspace | Wartościowe, ale nie blokuje startu | Post-MVP |
| Integracja Microsoft 365 | j.w. | Post-MVP |
| Integracja Slack | j.w. | Post-MVP |
| AI (podsumowania, klasyfikacja) | Wymaga danych historycznych; nie daje wartości na pustym systemie | Post-MVP / premium |
| Form builder (konfigurator formularzy) | Predefiniowane formularze wystarczą na MVP | Post-MVP |
| Zaawansowane raporty (trendy, wykresy, predykcja) | Dashboard MVP wystarcza; raportowanie wymaga danych | Post-MVP |
| Moduły działowe | Wymaga mechanizmu rozszerzalności | Premium |
| Desktop wrapper | Web wystarcza; Tauri w przyszłości | Premium |
| Biometria | QR + NFC + PIN wystarczają | Premium |
| White-label / custom branding | Jedna marka na start | Premium |
| Publiczne API dla integracji trzecich | Wewnętrzne API wystarcza; publiczne wymaga dokumentacji, rate limiting, versioning | Post-MVP |
| Wiele ról per użytkownik | Edge case — jedna rola per user na MVP, wiele ról post-MVP | Post-MVP |
| Workflow warunkowy (if/else/branching) | MVP ma prosty łańcuch; zaawansowany branching post-MVP | Post-MVP |
| Offline mode (mobile) | Wymaga sync engine — zbyt duży koszt | Premium |

---

## 3.4. Podsumowanie scope MVP

**Moduły w MVP:**

```
M0  Fundament techniczny
M1  Struktura organizacyjna
M2  Role i uprawnienia
M3  Zakres widoczności danych
M4  Czas pracy i obecności
M5  Urlopy, nieobecności i wnioski
M6  Zadania i egzekucja pracy
M7  Workflow i akceptacje (silnik podstawowy)
M8  Widok kierowniczy (dashboard)
MC  Karty rekordów 360 (wariant MVP)
MW  Workspace użytkownika (wariant MVP)
MA  Audyt i historia
MM  Mobile (wariant MVP)
```

**Liczba features MVP (orientacyjna):**
- must-have: ~75
- should-have: ~25
- nice-to-have: ~15 (poza scope — odłożone)

---

# 4. Post-MVP

## 4.1. Cel post-MVP

> Rozszerzenie platformy o funkcje, które zwiększają retencję, podnoszą wartość dla większych organizacji, otwierają nowe segmenty klientów i wprowadzają integracje z codziennymi narzędziami.

## 4.2. Moduły i features post-MVP

### P1: Case management i zgłoszenia
**Priorytet: SHOULD-HAVE | Zależności: M7 (workflow engine)**

| # | Feature | Priorytet |
|---|---|---|
| P1.1 | Konfigurowalne typy spraw (zgłoszenie, incydent, reklamacja, sprawa pracownicza) | must-have |
| P1.2 | Formularz zgłoszenia (konfigurowalny) | must-have |
| P1.3 | Statusy spraw (konfigurowalne per typ) | must-have |
| P1.4 | Przypisanie właściciela sprawy | must-have |
| P1.5 | Komentarze, załączniki, historia | must-have |
| P1.6 | SLA — terminy reakcji i rozwiązania | should-have |
| P1.7 | Automatyczne eskalacje przy przekroczeniu SLA | should-have |
| P1.8 | Karta sprawy 360 | must-have |
| P1.9 | Dashboard spraw (per dział / per typ) | should-have |
| P1.10 | Workflow na sprawach (statusy + automatyzacje) | must-have |

### P2: Kontrahenci i relacje
**Priorytet: SHOULD-HAVE | Zależności: M1, karty 360**

| # | Feature | Priorytet |
|---|---|---|
| P2.1 | Baza kontrahentów (CRUD: firma, dane kontaktowe, branża, NIP) | must-have |
| P2.2 | Opiekun kontrahenta (przypisanie pracownika) | must-have |
| P2.3 | Historia współpracy (timeline) | should-have |
| P2.4 | Dokumenty kontrahenta | should-have |
| P2.5 | Notatki i komentarze | must-have |
| P2.6 | Zadania powiązane z kontrahentem | should-have |
| P2.7 | Status relacji (aktywny, zawieszony, archiwum) | must-have |
| P2.8 | Karta kontrahenta 360 | must-have |

### P3: Integracje — Google Workspace
**Priorytet: SHOULD-HAVE | Zależności: M0 (API), M5 (kalendarz), M6 (zadania)**

| # | Feature | Priorytet |
|---|---|---|
| P3.1 | OAuth2 z Google | must-have |
| P3.2 | Google Calendar — sync nieobecności (do kalendarza) | must-have |
| P3.3 | Google Calendar — sync zadań/terminów | should-have |
| P3.4 | Gmail — linkowanie maili do spraw/kontrahentów (sidebar/plugin) | should-have |
| P3.5 | Google Contacts — import/sync kontaktów | nice-to-have |
| P3.6 | Google Drive — podpinanie plików jako załączników | nice-to-have |

### P4: Integracje — Microsoft 365
**Priorytet: SHOULD-HAVE | Zależności: M0, M5, M6**

| # | Feature | Priorytet |
|---|---|---|
| P4.1 | OAuth2 z Microsoft | must-have |
| P4.2 | Outlook Calendar — sync nieobecności | must-have |
| P4.3 | Outlook Calendar — sync zadań/terminów | should-have |
| P4.4 | Teams — powiadomienia z systemu (bot/webhook) | should-have |
| P4.5 | Outlook — linkowanie maili do spraw/kontrahentów | nice-to-have |

### P5: Integracja Slack
**Priorytet: NICE-TO-HAVE | Zależności: M0, powiadomienia**

| # | Feature | Priorytet |
|---|---|---|
| P5.1 | Webhook — powiadomienia z systemu do kanału Slack | should-have |
| P5.2 | Slash commands (/workbase status, /workbase clockin) | nice-to-have |
| P5.3 | Statusy Slack na podstawie obecności w systemie | nice-to-have |

### P6: Zaawansowany workflow engine
**Priorytet: SHOULD-HAVE | Zależności: M7 (MVP engine)**

| # | Feature | Priorytet |
|---|---|---|
| P6.1 | Reguły warunkowe (if/else na polach formularza) | must-have |
| P6.2 | Wielopoziomowa akceptacja z konfiguracją łańcucha | must-have |
| P6.3 | Automatyczne tworzenie zadań po przejściu etapu | should-have |
| P6.4 | Timery i SLA na etapach workflow | should-have |
| P6.5 | Delegowanie akceptacji na zastępcę | should-have |
| P6.6 | Wizualny builder workflow (frontend) | should-have |
| P6.7 | Szablony workflow (urlopy, nadgodziny, zakupy, onboarding) | must-have |

### P7: Zaawansowane raportowanie
**Priorytet: SHOULD-HAVE | Zależności: M4, M5, M6, M8**

| # | Feature | Priorytet |
|---|---|---|
| P7.1 | Raporty trendów absencji (per zespół/dział, per miesiąc) | must-have |
| P7.2 | Raporty trendów efektywności (% zadań w terminie) | should-have |
| P7.3 | Analiza czasu pracy (nadgodziny, niedogodziny, średnia) | must-have |
| P7.4 | Obciążenie zespołów (workload distribution) | should-have |
| P7.5 | Konfigurowalne dashboardy (drag & drop widżetów) | should-have |
| P7.6 | Eksport raportów PDF | should-have |
| P7.7 | Scheduled reports (email cykliczny z raportem) | nice-to-have |

### P8: Form builder
**Priorytet: SHOULD-HAVE | Zależności: M7, P6**

| # | Feature | Priorytet |
|---|---|---|
| P8.1 | Definiowanie formularza (pola: tekst, data, select, załącznik) | must-have |
| P8.2 | Powiązanie formularza z workflow (formularz → sprawa → workflow) | must-have |
| P8.3 | Reguły walidacji formularza | should-have |
| P8.4 | Formularze publiczne (np. dla kontrahenta, bez logowania) | nice-to-have |

### P9: Rozszerzone karty 360 i custom fields
**Priorytet: SHOULD-HAVE | Zależności: MC (MVP cards)**

| # | Feature | Priorytet |
|---|---|---|
| P9.1 | Karta sprawy 360 | must-have |
| P9.2 | Karta kontrahenta 360 | must-have |
| P9.3 | Karta procesu 360 | should-have |
| P9.4 | Custom fields na wszystkich typach kart (JSONB) | must-have |
| P9.5 | Sekcje konfigurowalnie na karcie (admin decyduje co widzi rola) | should-have |
| P9.6 | Tagi globalne + per typ rekordu | should-have |
| P9.7 | Zapisane widoki list (filtry + kolumny + sortowanie) | must-have |

### P10: Rozszerzony mobile
**Priorytet: SHOULD-HAVE | Zależności: MM (MVP mobile)**

| # | Feature | Priorytet |
|---|---|---|
| P10.1 | NFC clock-in (natywna obsługa) | must-have |
| P10.2 | Push notifications (FCM) | must-have |
| P10.3 | Dashboard kierownika (mobile) | should-have |
| P10.4 | Przeglądanie spraw i zgłoszeń | should-have |
| P10.5 | Offline queue (zapisz akcję, wyślij po połączeniu) — basic | nice-to-have |

### P11: Publiczne API
**Priorytet: NICE-TO-HAVE | Zależności: M0**

| # | Feature | Priorytet |
|---|---|---|
| P11.1 | API publiczne (subset endpointów: pracownicy, czas pracy, urlopy) | should-have |
| P11.2 | API key management per tenant | must-have |
| P11.3 | Rate limiting | must-have |
| P11.4 | Dokumentacja API (Swagger public) | must-have |
| P11.5 | Webhooks (eventy z systemu → zewnętrzny endpoint) | should-have |

---

## 4.3. Zależności post-MVP wobec MVP

```
P1 (Case management)  → M7 (workflow engine), M1 (struktura), MC (karty)
P2 (Kontrahenci)       → M1 (struktura), MC (karty)
P3 (Google)            → M0 (API), M5 (urlopy), M6 (zadania)
P4 (Microsoft)         → M0 (API), M5 (urlopy), M6 (zadania)
P5 (Slack)             → M0 (powiadomienia)
P6 (Adv. workflow)     → M7 (MVP engine)
P7 (Raporty)           → M4, M5, M6, M8
P8 (Form builder)      → M7, P6
P9 (Adv. karty)        → MC (MVP cards)
P10 (Adv. mobile)      → MM (MVP mobile)
P11 (Public API)       → M0 (API)
```

---

# 5. Premium / future

## 5.1. Cel

> Funkcje, które wymagają dojrzałości systemu, większej bazy klientów, lub adresują tylko zaawansowane organizacje. Budowane po walidacji rynkowej post-MVP.

## 5.2. Moduły i features premium

### F1: Moduł sprzedaży
**Priorytet: NICE-TO-HAVE | Zależności: P2 (kontrahenci), P6 (adv. workflow)**

| # | Feature | Priorytet |
|---|---|---|
| F1.1 | Leady (źródło, status, scoring) | must-have (w module) |
| F1.2 | Pipeline sprzedażowy (etapy, wartość, prawdopodobieństwo) | must-have (w module) |
| F1.3 | Szanse sprzedaży | must-have (w module) |
| F1.4 | Forecast | should-have |
| F1.5 | Oferty (generowanie, śledzenie) | should-have |
| F1.6 | Follow-up handlowy (mechanizm przypomnienia) | must-have (w module) |
| F1.7 | Raporty sprzedażowe | should-have |

### F2: AI — warstwa wspierająca
**Priorytet: NICE-TO-HAVE | Zależności: dane historyczne z M4, M5, M6, P1**

| # | Feature | Priorytet |
|---|---|---|
| F2.1 | Podsumowanie karty pracownika (AI summary) | should-have |
| F2.2 | Podsumowanie sprawy (AI streszczenie przebiegu) | should-have |
| F2.3 | Draft odpowiedzi na komentarz / zgłoszenie | nice-to-have |
| F2.4 | Klasyfikacja zgłoszeń (auto-typ, auto-priorytet) | should-have |
| F2.5 | Sugestia następnego kroku | nice-to-have |
| F2.6 | Wyszukiwanie semantyczne (w dokumentach i rekordach) | should-have |
| F2.7 | Wsparcie kierownika — anomalie + rekomendacje (AI insights) | should-have |

### F3: Moduły działowe
**Priorytet: NICE-TO-HAVE | Zależności: P8 (form builder), P9 (custom fields), P6 (adv. workflow)**

| # | Feature | Priorytet |
|---|---|---|
| F3.1 | Mechanizm tworzenia modułów działowych (admin) | must-have (w module) |
| F3.2 | IT: certyfikaty, sprzęt, uprawnienia techniczne, licencje | should-have |
| F3.3 | HR: oceny okresowe, ścieżki kariery | should-have |
| F3.4 | Własne zakładki per dział | should-have |
| F3.5 | Własne raporty per dział | nice-to-have |

### F4: Desktop wrapper
**Priorytet: NICE-TO-HAVE | Zależności: stabilny web app**

| # | Feature | Priorytet |
|---|---|---|
| F4.1 | Tauri wrapper (Windows + macOS) | should-have |
| F4.2 | System tray + status (clock-in/out z tray) | should-have |
| F4.3 | Auto-start z systemem | nice-to-have |
| F4.4 | Dedykowany kiosk mode (Tauri kiosk) | nice-to-have |

### F5: Biometria i zaawansowane metody rejestracji
**Priorytet: NICE-TO-HAVE | Zależności: M4 (czas pracy)**

| # | Feature | Priorytet |
|---|---|---|
| F5.1 | Face recognition (integracja z kamerą) | nice-to-have |
| F5.2 | Fingerprint (integracja z czytnikiem USB/mobilny) | nice-to-have |
| F5.3 | Geofencing (auto clock-in po wejściu w strefę) | should-have |
| F5.4 | Integracja z fizycznymi czytnikami (RFID, terminale) | nice-to-have |

### F6: White-label i zaawansowany multi-tenant
**Priorytet: NICE-TO-HAVE | Zależności: dojrzałość platformy**

| # | Feature | Priorytet |
|---|---|---|
| F6.1 | Custom branding per tenant (logo, kolory, domena) | should-have |
| F6.2 | Subdomena per tenant (firma.workbase.app) | should-have |
| F6.3 | Self-service tenant onboarding (rejestracja online) | should-have |
| F6.4 | Plany subskrypcyjne (billing integration) | must-have (przy skalowaniu) |
| F6.5 | Schema per tenant (izolacja bazy) | nice-to-have |

### F7: Zaawansowany mobile
**Priorytet: NICE-TO-HAVE | Zależności: P10**

| # | Feature | Priorytet |
|---|---|---|
| F7.1 | Pełny offline mode z sync engine | nice-to-have |
| F7.2 | Widget na ekran główny (status dnia) | nice-to-have |
| F7.3 | Biometria na telefonie (fingerprint/face do logowania) | should-have |
| F7.4 | Wearable — zegar/opaska (clock-in) | nice-to-have |

---

# 6. Absolutne minimum produktu

> Gdybyśmy musieli wyciąć MVP do absolutnego minimum — co jest **nieprzekazywalnym minimum**, bez którego system nie ma żadnego sensu?

## 6.1. Minimum Viable Product — Hard Core

```
1. Użytkownik może się zalogować                    (M0: auth)
2. Admin może zdefiniować strukturę firmy            (M1: struktura)
3. Admin może tworzyć i przypisywać role             (M2: role)
4. Pracownik może zarejestrować czas pracy           (M4: clock-in/out)
5. Pracownik może zobaczyć swój timesheet            (M4: timesheet)
6. Pracownik może złożyć wniosek urlopowy            (M5: wniosek)
7. Kierownik może zaakceptować lub odrzucić wniosek  (M7: approval)
8. Pracownik może zobaczyć przypisane zadania         (M6: zadania)
9. Kierownik może przypisać zadanie pracownikowi      (M6: delegowanie)
10. Kierownik widzi kto jest obecny i co jest zaległe  (M8: dashboard)
11. System loguje kto co zmienił                       (MA: audyt)
```

**Jeśli któregokolwiek z powyższych 11 punktów brakuje — system nie ma wartości jako produkt.**

## 6.2. Minimalny user journey — pracownik

```
Rano:
→ Otwiera aplikację (web lub mobile)
→ Klika „Rozpocznij pracę" (lub skanuje QR)
→ Widzi swój workspace: „Moje zadanie na dziś: X, Y"
→ Pracuje, aktualizuje statusy zadań
→ Klika „Przerwa" → „Koniec przerwy"
→ Na koniec dnia: „Zakończ pracę"

Gdy planuje urlop:
→ Składa wniosek urlopowy
→ Widzi status: „Oczekujący"
→ Dostaje powiadomienie: „Zatwierdzony"
```

## 6.3. Minimalny user journey — kierownik

```
Rano:
→ Otwiera dashboard
→ Widzi: 12/15 osób obecnych, 1 spóźniony, 2 na urlopie
→ Widzi: 3 oczekujące akceptacje
→ Akceptuje 2 urlopy, odrzuca 1
→ Widzi: 4 zaległe zadania w zespole
→ Przypisuje 1 nowe zadanie pracownikowi
→ Sprawdza kartę pracownika X — widzę czas pracy + zadania + urlopy
```

---

# 7. Rekomendowana kolejność realizacji

## 7.1. Fazy MVP

### Faza 0: Fundament (tyg. 1–4)
**Cel: Infrastruktura, architektura, auth, szkielet**

```
M0.1  Repo + CI/CD
M0.2  Architektura modularnego monolitu
M0.3  Baza PostgreSQL + migracje
M0.4  Auth (Keycloak) + JWT + login/logout
M0.5  Multi-tenancy (tenant_id)
M0.6  REST API szkielet + Swagger
M0.7  Frontend shell (React + layout + routing + auth)
M0.8  Feature flags
M0.9  Serilog
M0.10 Docker + docker-compose
M0.12 MinIO storage
M0.14 Hangfire
```

**Deliverable:** Developer może zalogować się do pustej aplikacji, ma działający backend z API, frontend z routingiem, dane izolowane per tenant.

---

### Faza 1: Struktura i role (tyg. 5–8)
**Cel: Firma, pracownicy, role, uprawnienia, widoczność**

```
M1.1–M1.7  Struktura organizacyjna (CRUD + hierarchia + widok drzewa)
M2.1–M2.8  Role i uprawnienia (definicja, matryca, panel admin)
M3.1–M3.5  Widoczność danych (scope engine + filtrowanie)
```

**Deliverable:** Admin może stworzyć firmę z działami i zespołami, dodać pracowników, przypisać role. Kierownik widzi tylko swój zespół. Pracownik widzi swoje dane.

---

### Faza 2: Czas pracy (tyg. 9–12)
**Cel: Clock-in/out, przerwy, timesheet, QR, grafik, anomalie**

```
M4.1–M4.3   Clock-in/out (web + QR)
M4.5–M4.6   Timesheet (dzień, tydzień)
M4.9         Grafik / zmiana
M4.10        Anomalie podstawowe
M4.7–M4.8   Raporty czasu pracy (per pracownik, per zespół)
M4.11        Korekta przez kierownika
M4.12        Historia zdarzeń
```

**Deliverable:** Pracownik rejestruje czas pracy przyciskiem lub QR. Kierownik widzi raporty i anomalie.

---

### Faza 3: Workflow + Urlopy (tyg. 13–16)
**Cel: Silnik akceptacji, wnioski urlopowe, kalendarz nieobecności**

```
M7.1–M7.8   Workflow engine (statusy, przejścia, approval single-level, powiadomienia)
M5.1–M5.9   Urlopy (typy, limity, saldo, wniosek, akceptacja, kalendarz)
M0.13        Powiadomienia (in-app + email)
```

**Deliverable:** Pracownik składa wniosek urlopowy, kierownik akceptuje/odrzuca z powiadomieniem. Kalendarz nieobecności działa.

---

### Faza 4: Zadania (tyg. 17–20)
**Cel: CRUD zadań, statusy, przypisania, komentarze, alerty**

```
M6.1–M6.9   Zadania (CRUD, statusy, priorytety, filtrowanie, lista)
M6.10–M6.14 Delegowanie, akceptacja, przypomnienia, alerty, historia
```

**Deliverable:** Kierownik tworzy i deleguje zadania. Pracownik aktualizuje statusy. System alertuje o opóźnieniach.

---

### Faza 5: Widok kierowniczy + Workspace + Karty (tyg. 21–24)
**Cel: Dashboard, workspace, karty 360, feed aktywności**

```
M8.1–M8.6   Dashboard kierownika (obecność, spóźnienia, zadania, akceptacje, alerty)
MW.1–MW.7   Workspace pracownika (mój dzień, zadania, akceptacje, czas pracy, wnioski)
MC.1–MC.3   Karty 360 (pracownik, zespół, zadanie)
MA.1–MA.3   Audyt trail + timeline
M8.7–M8.9   Raporty podstawowe + eksport
```

**Deliverable:** Kierownik ma pełny widok operacyjny. Pracownik ma jeden workspace. Karty 360 dają kontekst.

---

### Faza 6: Mobile + Kiosk + Polish (tyg. 25–28)
**Cel: Aplikacja mobilna, tryb kiosk, finalny szlif**

```
MM.1–MM.7   Mobile (PWA/Capacitor, clock-in, QR, wnioski, akceptacje, push)
M4.4        Kiosk mode (web fullscreen)
M0.11       i18n (PL)
M0.15       SignalR (real-time)
MC.4–MC.5   Generyczny card renderer + custom fields
M1.8        Import CSV
```

**Deliverable:** MVP kompletny. Aplikacja web + mobile + kiosk. System gotowy do pilotażowego wdrożenia.

---

## 7.2. Diagram zależności faz

```
Faza 0 (Fundament)
  │
  ├── Faza 1 (Struktura, Role, Widoczność)
  │     │
  │     ├── Faza 2 (Czas pracy)
  │     │     │
  │     │     └── Faza 3 (Workflow + Urlopy) ←── zależy też od Fazy 1
  │     │           │
  │     │           └── Faza 5 (Dashboard + Workspace + Karty) ←── zależy od Faz 2, 3, 4
  │     │
  │     └── Faza 4 (Zadania) ←── zależy od Fazy 1
  │           │
  │           └── Faza 5 (Dashboard + Workspace + Karty)
  │
  └── Faza 6 (Mobile + Kiosk + Polish) ←── zależy od Faz 2, 3, 4, 5
```

**Uwaga:** Fazy 2 i 4 mogą być rozwijane **równolegle** przez dwa strumienie (backend + frontend), ponieważ nie mają wzajemnych zależności. Faza 3 (workflow) powinna startować razem z Fazą 2, bo approval engine jest potrzebny do urlopów.

## 7.3. Kolejność post-MVP

Rekomendowana kolejność post-MVP (po MVP):

```
Runda 1 (natychmiast po MVP):
  P6  Zaawansowany workflow engine  → odblokuje P1, P8
  P9  Custom fields + karty 360    → odblokuje P2, P1
  P7  Zaawansowane raportowanie    → podnosi wartość dashboardu

Runda 2:
  P1  Case management              → nowy moduł wartości
  P2  Kontrahenci                  → nowy segment klientów
  P10 Rozszerzony mobile           → NFC, push, offline basic

Runda 3:
  P3  Google Workspace             → integracja #1
  P4  Microsoft 365                → integracja #2
  P8  Form builder                 → samoobsługa procesów

Runda 4:
  P5  Slack                        → integracja #3
  P11 Public API                   → ekosystem
```

---

# 8. Największe ryzyka przy złym doborze zakresu

## 8.1. Ryzyka scope'u MVP

| # | Ryzyko | Opis | Skutek | Mitygacja |
|---|---|---|---|---|
| R1 | **Za duży scope MVP** | Próba zbudowania case management, integracji i AI w MVP | 12+ miesięcy do pierwszego wdrożenia, wypalenie zespołu | Trzymanie się Hard Core (sekcja 6.1). Cut ruthlessly. |
| R2 | **Za mały scope MVP** | Np. sam czas pracy bez zadań i dashboardu | System nie wyróżnia się od Calamari — brak wartości | Oba filary (czas pracy + zadania) + widok kierowniczy = minimum wyróżnienia |
| R3 | **Brak workflow engine od MVP** | Kodowanie approval flow ad-hoc per moduł | Techniczna katastrofa — każdy nowy typ akceptacji wymaga nowego kodu | Silnik workflow (choćby prosty) od Fazy 3 |
| R4 | **Brak multi-tenancy od MVP** | Dodanie izolacji per tenant po fakcie | Refaktor całej bazy danych i warstwy dostępu | tenant_id od dnia 1 |
| R5 | **Brak konfigurowalności** | Zahardkodowane statusy, role wg jednej firmy | System działa tylko dla firmy pilotażowej | Konfiguracja ról, statusów, typów absencji od MVP |
| R6 | **Pominięcie mobile** | Web-only na MVP | Pracownicy fizyczni nie mają komputerów → nie mogą rejestrować czasu → brak wartości | PWA/Capacitor w Fazie 6 |

## 8.2. Ryzyka architektoniczne

| # | Ryzyko | Opis | Skutek | Mitygacja |
|---|---|---|---|---|
| R7 | **Big ball of mud** | Brak wyraźnych modułów → spaghetti | Każda zmiana łamie coś innego | Modular monolith od Fazy 0, wyraźne granice modułów |
| R8 | **Custom fields afterthought** | Dodanie custom fields po zbudowaniu modelu sztywnego | Refaktor modelu danych, migracja danych istniejących klientów | Zaprojektować JSONB column od razu (nawet jeśli UI do tego będzie w post-MVP) |
| R9 | **Auth/IAM w kodzie** | Budowanie auth i zarządzania sesjami samodzielnie | Luki bezpieczeństwa, brak SSO, brak 2FA | Keycloak od dnia 1 |
| R10 | **Sztywna hierarchia** | Model danych zakładający konkretne poziomy (oddział > dział > zespół) | Nie działa dla firmy z inną strukturą | Closure table z konfigurowalnymi typami jednostek |
| R11 | **API nie-mobilne** | API projektowane pod web, nieprzystosowane do mobile (za dużo danych, brak pagination) | Mobile działa wolno, wysokie zużycie danych | API-first design od Fazy 0, pagination, sparse fieldsets |

## 8.3. Ryzyka produktowe

| # | Ryzyko | Opis | Skutek | Mitygacja |
|---|---|---|---|---|
| R12 | **Brak wyróżnika** | System robi „trochę wszystkiego, nic dobrze" | Przegrana z wyspecjalizowanymi narzędziami | Dashboard kierowniczy = główna przewaga. Musi być doskonały. |
| R13 | **Za wczesna sprzedaż modułu sales** | Budowanie pipeline'u przed dojrzałością rdzenia | Rozproszenie uwagi, niedokończony rdzeń | Sprzedaż = premium. Nie wcześniej niż Runda 4+ post-MVP. |
| R14 | **Brak pilotażowej firmy** | Budowanie w vakuum, bez feedbacku | System nie pasuje do realnych potrzeb | Znaleźć firmę pilotażową przed końcem Fazy 2 |

---

# 9. Rekomendacje scope control

## 9.1. Zasady gatekeepingu scope'u

| Zasada | Opis |
|---|---|
| **Jeden filar na raz** | Nie startuj drugiego filaru, zanim pierwszy nie jest stabilny |
| **Must-have first** | Should-have dopiero wtedy, gdy WSZYSTKIE must-have w module są gotowe |
| **No gold plating** | Jeśli feature MVP działa poprawnie ale nie jest „piękny" — ship it |
| **Demo co 2 tygodnie** | Każda faza kończy się demonstracją z użyciem realnych danych |
| **Cut not delay** | Jeśli feature opóźnia fazę — wycinamy z fazy, nie opóźniamy fazy |
| **Config > code** | Za każdym razem, gdy developer chce zahardkodować wartość — pytaj: „czy to nie powinno być konfigurowalne?" |
| **Mobile later, but design for it now** | API projektujemy mobile-friendly od razu, nawet jeśli mobile UI jest w Fazie 6 |

## 9.2. Definition of Done — per faza

Faza jest zakończona, gdy:
1. Wszystkie must-have features działają end-to-end (backend → frontend → user journey)
2. Dane izolowane per tenant
3. Uprawnienia egzekwowane na backendzie
4. Audyt trail zapisuje zdarzenia
5. Testy API dla happy path + edge cases
6. Można wykonać user journey opisany w deliverable fazy

## 9.3. Czerwone flagi scope creep

Jeśli w trakcie fazy pojawia się jedno z poniższych — STOP, scope review:

- „Dodajmy jeszcze tylko jedno pole…" → własne pola? to post-MVP feature (custom fields)
- „A co jeśli firma ma 3 poziomy akceptacji?" → to P6 (zaawansowany workflow), nie MVP
- „Potrzebujemy integracji z Outlookiem od razu" → to P4, nie MVP
- „Zróbmy jeszcze raport trendów" → to P7, nie MVP
- „Niech pracownik sam wybierze kolorystykę" → to F6 (white-label), zdecydowanie nie MVP
- „Dodajmy AI do podsumowania" → to F2, nie MVP — brak danych do podsumowania

## 9.4. Checkpoint review schedule

| Po fazie | Pytania checkpoint |
|---|---|
| Faza 0 | Czy developer experience jest dobry? Czy pipeline działa? Czy login jest stabilny? |
| Faza 1 | Czy model struktury i ról jest wystarczająco elastyczny? Test na 3 różnych strukturach firm. |
| Faza 2 | Czy czas pracy działa end-to-end? Czy firma pilotażowa potwierdziła, że to ma sens? |
| Faza 3 | Czy workflow engine jest wystarczająco generyczny? Czy da się dodać nowy typ procesu bez kodu? |
| Faza 4 | Czy zadania dają wartość? Czy follow-up działa? Feedback z pilota. |
| Faza 5 | Czy dashboard „wow effect"? Czy kierownik naprawdę chce go otwierać rano? |
| Faza 6 | Czy mobile działa stabilnie? Czy QR scan jest szybki? Czy UX kiosku jest jednoznaczny? |

---

> **Następny krok:** Po zatwierdzeniu roadmapy — przejść do szczegółowego backlogu tasków implementacyjnych per faza (docs/03-...).
