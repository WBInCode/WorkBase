# WorkBase — Dokument Fundamentów Produktu

> Dokument roboczy dla zespołu produktowo-technicznego.
> Wersja: 0.1 | Data: 2026-03-24 | Status: Draft

---

# 1. Czym jest produkt

WorkBase to **platforma operacyjno-zarządcza dla firm** (SaaS B2B), która centralizuje codzienne zarządzanie organizacją w jednym systemie.

System **nie jest** klasycznym CRM-em sprzedażowym. Sprzedaż jest modułem opcjonalnym, nie rdzeniem.

Najbliższe kategoryzacje rynkowe:
- Operational CRM / Workforce Management Platform
- Employee Operations & Task Management System
- Team & Process Management SaaS

System łączy w jednym miejscu:
- zarządzanie pracownikami i zespołami,
- rejestrację i kontrolę czasu pracy,
- zlecanie i egzekucję zadań,
- procesy, workflow i akceptacje,
- widok kierowniczy z dashboardami i alertami,
- raportowanie operacyjne,
- audyt i historię działań,
- opcjonalnie: relacje z kontrahentami i sprzedaż.

**Kluczowa różnica wobec istniejących narzędzi:**
System nie jest ani czystym HR-em (jak Calamari), ani czystym CRM-em (jak Salesforce/Pipedrive), ani czystym task managerem (jak Asana/Jira). Jest platformą, która spina te obszary w jedną warstwę operacyjną organizacji, ze szczególnym naciskiem na widok kierowniczy i egzekucję pracy.

---

# 2. Dla jakich typów firm jest projektowany

## 2.1. Wielkość firmy

| Segment | Liczba pracowników | Wymagania wobec systemu |
|---|---|---|
| Mała firma | 5–30 | Prosta struktura, brak działów, szybki start, minimum konfiguracji |
| Średnia firma | 30–200 | Działy, zespoły, kierownicy, workflow akceptacyjne, raporty |
| Duża organizacja | 200–2000+ | Oddziały, wielopoziomowa hierarchia, zaawansowane uprawnienia, moduły działowe |

System musi działać sensownie **na każdym z tych poziomów**. Mała firma nie może być zmuszona do konfigurowania oddziałów i spółek córek. Duża firma musi mieć możliwość odwzorowania złożonej struktury.

## 2.2. Branże

System jest **branżowo-agnostyczny**. Nie jest budowany pod konkretny sektor. Rdzeń (czas pracy, zadania, procesy, kierownictwo) ma sens w każdej firmie usługowej, produkcyjnej, IT, logistycznej, administracyjnej itd.

Ewentualna specjalizacja branżowa będzie realizowana przez:
- moduły opcjonalne,
- własne pola i formularze,
- konfigurowalne workflow,
- rozszerzenia per dział.

## 2.3. Profile użytkowników w organizacji

| Profil | Główne potrzeby w systemie |
|---|---|
| Zarząd / właściciel | Widok strategiczny, dashboardy, trendy, alerty krytyczne |
| Dyrektor / kierownik działu | Zarządzanie zespołami, akceptacje, raporty, obciążenie ludzi |
| Kierownik zespołu / lider | Delegowanie zadań, kontrola obecności, follow-up, eskalacje |
| Pracownik | Rejestracja czasu pracy, wnioski, zadania, self-service |
| HR | Urlopy, nieobecności, dokumenty pracownicze, limity |
| Admin systemu | Konfiguracja struktury, ról, uprawnień, modułów |

---

# 3. Co jest rdzeniem systemu

Rdzeń to zbiór modułów, **bez których system nie ma sensu jako produkt**. Muszą istnieć od MVP.

| # | Obszar rdzeniowy | Uzasadnienie |
|---|---|---|
| R1 | Struktura organizacyjna | Fundament — bez niej nie ma ról, widoczności, zespołów |
| R2 | Role i uprawnienia | Warunek bezpieczeństwa i kontrolowanego dostępu |
| R3 | Zakres widoczności danych | Kluczowy dla multi-tenant i hierarchii |
| R4 | Czas pracy i obecności | Jeden z dwóch głównych filarów wartości produktu |
| R5 | Zadania i egzekucja pracy | Drugi główny filar — system musi prowadzić działanie |
| R6 | Procesy, workflow i akceptacje | Warstwa spajająca — urlopy, nadgodziny, zgłoszenia, zadania |
| R7 | Widok kierowniczy | Główna przewaga produktu — jedno miejsce decyzyjne |
| R8 | Audyt i historia działań | Wymóg regulacyjny i operacyjny |

### Zasada rdzeniowa

Rdzeń musi działać **bez włączonych modułów opcjonalnych**. Firma może używać systemu wyłącznie do czasu pracy + zadań + procesów + kierownictwa i system musi dawać z tego samodzielną wartość.

---

# 4. Co NIE jest rdzeniem systemu

Poniższe elementy **nie są wymagane do działania platformy** i powinny być realizowane jako moduły opcjonalne, rozszerzenia lub fazy późniejsze:

| Element | Status | Uzasadnienie |
|---|---|---|
| Sprzedaż (leady, pipeline, forecast) | Moduł opcjonalny | System nie jest CRM-em sprzedażowym |
| Relacje z kontrahentami | Moduł opcjonalny | Wartościowe, ale nie wymagane dla core use case |
| Moduły działowe (IT, HR, logistyka) | Rozszerzenia przyszłościowe | Mechanizm rozszerzalności zamiast implementacji od razu |
| Biometria | Przyszłość | Na start QR + NFC; biometria jako późniejsza opcja |
| Pełna integracja z ERP / księgowość | Poza zakresem | System nie zastępuje ERP |
| Aplikacja desktopowa | Przyszłość | Web-first; desktop jako wrapper w przyszłości |
| Zaawansowane AI (generatywne raporty, predykcja) | Przyszłość / premium | AI wspierające, nie generatywne jako rdzeń |

### Granica decyzyjna

Jeśli moduł jest potrzebny **tylko części firm** — jest opcjonalny.
Jeśli moduł jest potrzebny **każdej firmie korzystającej z systemu** — jest rdzeniem.

---

# 5. Funkcje uniwersalne

Funkcje uniwersalne to takie, które **działają tak samo dla każdej firmy** i nie wymagają konfiguracji organizacyjnej. Są wbudowane na stałe.

| # | Funkcja | Opis |
|---|---|---|
| U1 | Logowanie i autentykacja | SSO, login/hasło, 2FA — via Keycloak |
| U2 | Workspace użytkownika | Jeden widok startowy: „co mam dziś do zrobienia" |
| U3 | Feed aktywności | Chronologiczny strumień zdarzeń powiązanych z użytkownikiem |
| U4 | Powiadomienia | In-app, email, push (mobile) — o zdarzeniach, terminach, alertach |
| U5 | Wyszukiwanie globalne | Szukaj po pracownikach, zadaniach, sprawach, dokumentach |
| U6 | Audyt trail | Automatyczny log: kto, co, kiedy, co zmienił |
| U7 | Eksport danych | CSV / Excel / PDF z widoków i raportów |
| U8 | Karta rekordu (wzorzec) | Uniwersalny wzorzec karty 360 — dane, powiązania, historia, dokumenty |
| U9 | Komentarze i notatki | Dodawanie komentarzy do zadań, spraw, wniosków |
| U10 | Załączniki | Upload plików do dowolnego rekordu |
| U11 | Tagi | Dowolne etykiety przypisywane do rekordów |
| U12 | Filtrowanie i zapisane widoki | Filtry na listach + możliwość zapisu widoku |
| U13 | System dat i terminów | Terminy, deadline'y, daty ważności — z alertami |

---

# 6. Funkcje konfigurowalne

Funkcje konfigurowalne to takie, które **istnieją w systemie, ale ich kształt zależy od konfiguracji konkretnej organizacji**. Nie są opcjonalne — ale wymagają ustawienia.

| # | Funkcja | Co jest konfigurowalne | Przykłady |
|---|---|---|---|
| K1 | Struktura organizacyjna | Ile poziomów, jakie typy jednostek | Firma → Dział → Zespół vs. Firma → Oddział → Dział → Zespół |
| K2 | Role użytkowników | Nazwy ról, ilość, hierarchia | „Kierownik" vs. „Team Lead" vs. „Supervisor" — to samo mechanicznie |
| K3 | Uprawnienia per rola | Matryca uprawnień per rola | Kto widzi raporty, kto akceptuje wnioski, kto edytuje grafik |
| K4 | Zakres widoczności | Reguły widoczności danych per rola i pozycja w strukturze | Kierownik widzi swój zespół; dyrektor widzi cały dział |
| K5 | Typy nieobecności | Lista typów absencji | Urlop wypoczynkowy, urlop na żądanie, L4, opieka, delegacja… |
| K6 | Limity urlopowe | Reguły naliczania i pule | 26 dni/rok, proporcjonalnie, z przenoszeniem lub bez |
| K7 | Ścieżki akceptacji | Kto akceptuje co i w jakiej kolejności | Urlop → kierownik; Nadgodziny → kierownik + HR |
| K8 | Statusy zadań | Lista statusów i przejścia | Nowe → W toku → Do akceptacji → Zamknięte |
| K9 | Statusy spraw | Lista statusów spraw/case'ów | Otwarte → W trakcie → Eskalowane → Rozwiązane → Zamknięte |
| K10 | Własne pola | Dodawanie pól do kart rekordów | Pole „Numer certyfikatu" na karcie pracownika |
| K11 | Szablony formularzy | Definiowanie formularzy uruchamiających procesy | Formularz wniosku zakupowego z polami kwota, opis, uzasadnienie |
| K12 | Reguły workflow | Warunki, akcje, automatyzacje | „Jeśli kwota > 5000, wymagana akceptacja dyrektora" |
| K13 | Grafik / zmianowość | Reguły zmian, godzin pracy | Zmiana dzienna 8–16, nocna 22–6, elastyczny czas pracy |
| K14 | Reguły anomalii czasu pracy | Progi i warunki alertów | Spóźnienie > 15 min, zmiana > 12h, brak odbicia po 10h |
| K15 | Dashboardy | Konfiguracja widżetów per rola | Kierownik widzi obecność zespołu + zaległe zadania; zarząd widzi trendy |

### Kluczowa decyzja architektoniczna

> **DECYZJA: Konfiguracja vs. kod**
>
> Każdy element z powyższej listy musi być **konfigurowalny przez interfejs administracyjny** (lub przynajmniej przez API), a nie wymagać zmian w kodzie. To jest warunek sprzedawalności systemu różnym firmom.
>
> Oznacza to, że architektura musi mieć warstwę konfiguracyjną (tenant config) oddzieloną od logiki domenowej.

---

# 7. Moduły opcjonalne

Moduły opcjonalne to takie, które **firma może włączyć lub wyłączyć**. System działa pełnowartościowo bez nich.

## 7.1. Moduły planowane

| # | Moduł | Opis | Etap |
|---|---|---|---|
| O1 | Urlopy i nieobecności | Wnioski, limity, kalendarz, approval flow | **MVP** (rdzeniowy, ale technicznie odseparowany) |
| O2 | Zgłoszenia i case management | Sprawy wewnętrzne, incydenty, reklamacje, SLA | Post-MVP |
| O3 | Kontrahenci i relacje | Baza kontrahentów, opiekunowie, historia współpracy | Post-MVP |
| O4 | Sprzedaż | Leady, pipeline, szanse, forecast, oferty | Premium / przyszłość |
| O5 | Integracja Google Workspace | Gmail, Calendar, Contacts, Drive | Post-MVP |
| O6 | Integracja Microsoft 365 | Outlook, Teams, Calendar | Post-MVP |
| O7 | Integracja Slack | Powiadomienia, statusy, komendy | Post-MVP |
| O8 | Moduły działowe | Rozszerzenia per dział (IT, HR, logistyka) z własnymi polami/workflow | Premium / przyszłość |
| O9 | AI — warstwa wspierająca | Podsumowania, draft odpowiedzi, klasyfikacja, sugestie | Post-MVP / premium |

## 7.2. Mechanizm włączania modułów

> **DECYZJA ARCHITEKTONICZNA: Feature flags / module registry**
>
> System musi mieć mechanizm rejestracji modułów i ich włączania/wyłączania per tenant.
>
> Opcje:
> - Feature flags w bazie per tenant
> - Module registry z dependency checkingiem
> - Konfiguracja licencyjna powiązana z planem subskrypcji
>
> **Do ustalenia przed implementacją.**

---

# 8. Główne obszary funkcjonalne

Poniżej pełna mapa obszarów z oznaczeniem charakteru (rdzeń / konfigurowalny / opcjonalny) i poziomu (MVP / post-MVP / premium).

## 8.1. Struktura organizacyjna

**Charakter:** Rdzeń, konfigurowalny
**Etap:** MVP

Opis: System musi obsługiwać elastyczną strukturę organizacyjną — od płaskiej (firma → pracownicy) do wielopoziomowej (firma → oddziały → działy → zespoły → stanowiska).

Kluczowe elementy:
- Jednostki organizacyjne (typy konfigurowalne: firma, oddział, dział, zespół, sekcja…)
- Hierarchia jednostek (drzewiasta, dowolna głębokość)
- Stanowiska / pozycje
- Relacja przełożony–podwładny
- Przypisanie pracownika do jednostki i stanowiska
- Obsługa wielu przypisań (pracownik w dwóch zespołach)

Czego NIE zakładamy na sztywno:
- Nie wymuszamy żadnego poziomu (oddział, dział, zespół — wszystkie opcjonalne)
- Nie wymuszamy konkretnych nazw (użytkownik nazywa poziomy jak chce)
- Nie wymuszamy jednego drzewa (możliwe wiele niezależnych gałęzi)

> **KONFIGURACJA:** Typy jednostek, głębokość hierarchii, nazewnictwo — konfigurowalne per tenant.

## 8.2. Role i uprawnienia

**Charakter:** Rdzeń, konfigurowalny
**Etap:** MVP

Opis: System musi mieć elastyczny model ról i uprawnień. Nie narzucamy sztywnej listy ról (prezes, dyrektor, kierownik…). Zamiast tego dostarczamy mechanizm definiowania ról i przypisywania im uprawnień.

Kluczowe elementy:
- Definicja ról (nazwa, opis, poziom w hierarchii)
- Matryca uprawnień (per moduł, per akcja: odczyt / zapis / akceptacja / admin)
- Role systemowe predefiniowane (Super Admin, Admin) — nieusuwalne
- Role organizacyjne definiowane przez admina (Kierownik, Pracownik, HR, Lider…)
- Przypisanie roli do użytkownika (możliwe wiele ról)
- Dziedziczenie uprawnień w hierarchii (opcjonalne)

> **DECYZJA:** RBAC vs. ABAC vs. hybrydowe. Rekomendacja: RBAC z elementami ABAC (atrybuty: jednostka organizacyjna, poziom hierarchii, moduł).

## 8.3. Zakres widoczności danych

**Charakter:** Rdzeń, konfigurowalny
**Etap:** MVP

Opis: Widoczność danych zależy od roli użytkownika i jego pozycji w strukturze organizacyjnej.

Poziomy widoczności:
- Globalna (cały system / cała firma)
- Per oddział
- Per dział
- Per zespół
- Indywidualna (tylko swoje dane)
- Niestandardowa (ręcznie definiowane zakresy)

Reguły:
- Kierownik widzi dane swojego zespołu
- Dyrektor widzi dane swojego działu (i zespołów poniżej)
- HR widzi dane pracownicze globalnie (konfigurowalnie)
- Pracownik widzi swoje dane + ograniczony widok zespołu

> **KONFIGURACJA:** Reguły widoczności per rola per moduł — definiowane w panelu administracyjnym.

## 8.4. Czas pracy i obecności

**Charakter:** Rdzeń
**Etap:** MVP

Opis: Jeden z dwóch głównych filarów produktu. Pełna rejestracja i kontrola czasu pracy.

Funkcje core:
- Rozpoczęcie pracy (clock-in)
- Zakończenie pracy (clock-out)
- Rozpoczęcie przerwy / zakończenie przerwy
- Wejścia / wyjścia (wielokrotne w ciągu dnia)
- Timesheet dzienny i tygodniowy
- Raport czasu pracy (dzienny, tygodniowy, miesięczny)
- Historia zdarzeń

Metody rejestracji (MVP):
- QR code (generowany w systemie, skan z telefonu)
- Przycisk w aplikacji web/mobile (manual clock-in/out)
- NFC (przyłożenie telefonu do tagu)

Metody rejestracji (przyszłość):
- Biometria
- Geofencing
- Integracja z fizycznymi czytnikami

Tryb KIOSK:
- Wspólny punkt odbicia (tablet/terminal przy wejściu)
- Identyfikacja: kod PIN / QR pracownika / NFC
- Web app w trybie pełnoekranowym

Wykrywanie anomalii:
- Brak zakończenia pracy
- Podwójne rozpoczęcie
- Spóźnienie (względem grafiku)
- Wcześniejsze wyjście
- Zbyt długa zmiana (> próg konfigurowalny)
- Zbyt krótka zmiana
- Praca w dzień wolny
- Brak odbicia mimo obecności w grafiku

Powiązania:
- Z grafikiem / zmianą pracownika
- Z lokalizacją (opcjonalnie — geolokalizacja przy clock-in)
- Z urlopami i nieobecnościami

> **KONFIGURACJA:** Progi anomalii, metody rejestracji, tryb KIOSK, reguły zmianowe — per tenant.

## 8.5. Urlopy, nieobecności i wnioski

**Charakter:** Rdzeń (wydzielony jako moduł, ale włączony domyślnie)
**Etap:** MVP

Funkcje:
- Typy nieobecności (konfigurowalne: urlop wypoczynkowy, na żądanie, L4, opieka, delegacja, szkolenie…)
- Limity urlopowe per typ per pracownik
- Naliczanie puli (roczne, proporcjonalne, z przenoszeniem)
- Saldo dni (wykorzystane / pozostałe / planowane)
- Historia wykorzystania

Workflow wniosku:
- Złożenie wniosku (wybór typu, dat, opcjonalny komentarz)
- Przypisanie do akceptanta (automatyczne wg reguł lub ręczne)
- Akceptacja / odrzucenie
- Komentarz przełożonego
- Historia decyzji
- Załączniki (np. zwolnienie lekarskie)

Widoki:
- Kalendarz nieobecności (per zespół / dział / firma)
- Dostępność zespołu (kto jest, kto nie ma)
- Konflikty urlopowe (zbyt wielu na urlopie jednocześnie)
- Alerty o brakach kadrowych

## 8.6. Zadania i egzekucja pracy

**Charakter:** Rdzeń
**Etap:** MVP

Opis: Drugi główny filar. System musi prowadzić działanie, nie tylko ewidencjonować.

Funkcje core:
- Tworzenie zadań
- Przypisywanie do osoby lub zespołu
- Terminy (deadline, planowane rozpoczęcie)
- Priorytety (konfigurowalne)
- Statusy (konfigurowalne, z przejściami)
- Komentarze
- Załączniki
- Historia zmian

Zarządzanie wykonaniem:
- Delegowanie (przekazanie innemu pracownikowi)
- Przekazywanie (zmiana właściciela)
- Akceptacja wykonania (przełożony potwierdza zakończenie)
- Odrzucenie (zwrot do poprawy)
- Eskalacja (w górę hierarchii)
- Przypomnienia (automatyczne, przy zbliżającym się terminie)
- Alerty o opóźnieniu (termin minął, brak akcji)
- Zależności między zadaniami (blokujące / miękkie)

Follow-up:
- Pole „następny krok"
- Przypomnienie o kolejnej akcji
- Alert przy braku reakcji (konfigurowalny próg czasowy)
- Automatyczne zadania po zmianie statusu (np. zamknięcie → „zweryfikuj za 7 dni")

> **KONFIGURACJA:** Statusy, priorytety, pola dodatkowe, reguły eskalacji, progi alertów — per tenant.

## 8.7. Procesy, workflow i akceptacje

**Charakter:** Rdzeń (engine), konfigurowalny (definicje procesów)
**Etap:** MVP (silnik podstawowy), post-MVP (zaawansowany builder)

Opis: Warstwa spajająca cały system. Workflow engine obsługuje urlopy, zadania, zgłoszenia, wnioski — wszystko przechodzi przez ten sam mechanizm.

Elementy silnika:
- Etapy procesu (statusy z metadanymi)
- Przejścia między etapami (dozwolone ścieżki)
- Reguły warunkowe (jeśli kwota > X, wymagaj dodatkowej akceptacji)
- Automatyczne przypisania (np. „przypisz do kierownika wnioskodawcy")
- Automatyczne powiadomienia (email, in-app, push)
- Automatyczne zadania (utwórz task po przejściu do etapu X)
- Automatyczne eskalacje (jeśli brak reakcji > Y godzin → eskaluj)
- Timery (SLA, terminy reakcji)

Approval flow:
- Akceptacja przez przełożonego
- Akceptacja wielopoziomowa (kierownik → dyrektor → HR)
- Odrzucenie
- Cofnięcie do poprawy
- Delegowanie akceptacji (na zastępcę)
- Historia decyzji z timestampami

Zastosowania workflow engine:
- Urlopy i nieobecności
- Nadgodziny
- Delegacje
- Zgłoszenia wewnętrzne
- Wnioski zakupowe
- Wnioski o dostęp
- Akceptacja wykonania zadań
- Procesy onboardingowe
- Procesy między działami

> **DECYZJA ARCHITEKTONICZNA:** Workflow engine musi być **generyczny** — nie osobny kod per typ procesu. Jeden engine, wiele definicji procesów. Definicje procesów przechowywane jako konfiguracja (JSON/YAML w bazie), nie jako kod.

## 8.8. Widok kierowniczy i raportowanie

**Charakter:** Rdzeń
**Etap:** MVP (dashboardy podstawowe), post-MVP (zaawansowane raportowanie)

Opis: Jedna z głównych przewag produktu. Kierownik / dyrektor / zarząd widzi w jednym miejscu całość stanu operacyjnego.

Dashboard kierownika (MVP):
- Kto jest dziś obecny / nieobecny
- Kto się spóźnił
- Ile zadań otwartych / zaległych per zespół
- Które zadania/sprawy są opóźnione
- Które akceptacje czekają na decyzję
- Alerty i wyjątki (anomalie czasu pracy, eskalacje)

Raportowanie (post-MVP):
- Raporty dzienne, tygodniowe, miesięczne
- Trendy absencji (per zespół, per dział)
- Trendy efektywności (realizacja zadań w terminie)
- Trendy opóźnień
- Obciążenie zespołów (workload distribution)
- Analiza czasu pracy (nadgodziny, niedogodziny, średni czas pracy)

> **KONFIGURACJA:** Widżety dashboardowe per rola, zakresy raportów, progi alertów — per tenant.

## 8.9. Zgłoszenia, sprawy i case management

**Charakter:** Moduł opcjonalny (ale silnie powiązany z rdzeniem)
**Etap:** Post-MVP

Opis: Obsługa spraw wewnętrznych i zewnętrznych. Inspiracja z Salesforce Case Management.

Typy spraw:
- Zgłoszenia wewnętrzne (od pracowników)
- Incydenty operacyjne
- Problemy techniczne
- Reklamacje
- Sprawy pracownicze
- Sprawy kontrahenckie

Elementy sprawy:
- Status (konfigurowalny)
- Priorytet
- Właściciel (osoba odpowiedzialna)
- Termin reakcji (SLA)
- Termin rozwiązania (SLA)
- Komentarze
- Historia działań
- Eskalacje

> **KONFIGURACJA:** Typy spraw, statusy, SLA, reguły eskalacji, formularze zgłoszeniowe — per tenant.

## 8.10. Karty rekordów 360

**Charakter:** Rdzeń (wzorzec), konfigurowalny (zawartość kart)
**Etap:** MVP (karty podstawowe: pracownik, zespół, zadanie), post-MVP (rozszerzone: kontrahent, sprawa, proces)

Typy kart:
- Karta pracownika
- Karta zespołu
- Karta kierownika
- Karta kontrahenta (moduł opcjonalny)
- Karta partnera (moduł opcjonalny)
- Karta sprawy
- Karta zadania
- Karta procesu

Zawartość karty (wzorzec uniwersalny):
- Dane podstawowe (pola core + pola własne)
- Powiązania (z innymi rekordami)
- Historia działań (timeline)
- Komunikacja (komentarze, notatki)
- Dokumenty / załączniki
- Zadania powiązane
- Zgłoszenia powiązane
- Odpowiedzialny / właściciel
- Aktywność (ostatnie zdarzenia)
- Tagi
- Pola własne (definiowane per tenant)

> **DECYZJA ARCHITEKTONICZNA:** Karta 360 musi być oparta na **generycznym mechanizmie** (entity card renderer), a nie osobnym kodzie per typ karty. Frontend renderuje kartę na podstawie definicji (jakie sekcje, jakie pola, jakie relacje) — to pozwala na konfiguralność i rozszerzalność.

## 8.11. Workspace użytkownika i komunikacja

**Charakter:** Rdzeń
**Etap:** MVP (workspace podstawowy), post-MVP (integracje zewnętrzne)

Workspace — jedno centrum pracy użytkownika:
- Widok „co mam dziś do zrobienia"
- Moje zadania (otwarte, zbliżające się terminy)
- Moje akceptacje do podjęcia
- Moje wnioski (status)
- Ostatnia aktywność
- Mój czas pracy (dzisiejszy status)
- Powiadomienia

Integracje (post-MVP):
- Gmail / Google Workspace
- Google Calendar (synchronizacja kalendarza nieobecności, zadań)
- Google Contacts
- Google Drive (załączniki)
- Outlook / Microsoft 365
- Microsoft Teams
- Slack

> Cel integracji: **nie zmuszać ludzi do pracy wyłącznie w systemie**, tylko spiąć go z ich realnym środowiskiem pracy.

## 8.12. Formularze i uruchamianie procesów

**Charakter:** Konfigurowalny
**Etap:** MVP (formularze podstawowe: wniosek urlopowy, zgłoszenie), post-MVP (builder formularzy)

Przeznaczenie:
Formularze to punkt wejścia do procesów. Pracownik wypełnia formularz → system tworzy sprawę / wniosek / zgłoszenie → uruchamia workflow.

Przykłady formularzy:
- Wniosek urlopowy
- Zgłoszenie incydentu
- Wniosek zakupowy
- Wniosek o dostęp
- Formularz onboardingowy
- Zgłoszenie wewnętrzne
- Formularz dla kontrahenta (opcjonalny)

Co formularz może zrobić:
- Utworzyć rekord (sprawę, wniosek, zadanie)
- Przypisać właściciela (wg reguł)
- Uruchomić workflow
- Wysłać powiadomienie

> **DECYZJA ARCHITEKTONICZNA:** Formularze powinny być definiowalne (form builder) — co najmniej w wersji post-MVP. W MVP mogą być predefiniowane szablony z konfigurowalnymi polami.

## 8.13. Dokumenty i audyt

**Charakter:** Rdzeń
**Etap:** MVP (audyt trail + załączniki), post-MVP (zarządzanie dokumentami, terminy ważności)

Dokumenty:
- Dokumenty pracownicze (umowy, certyfikaty, zaświadczenia)
- Załączniki do spraw
- Załączniki do zadań
- Dokumenty kontrahentów (opcjonalny moduł)
- Terminy ważności dokumentów (z alertami)

Audyt trail:
- Pełny ślad działań
- Kto wykonał akcję
- Co zrobił (jaki typ zdarzenia)
- Kiedy (timestamp)
- Co zmienił (diff: stara wartość → nowa wartość)
- Kto zatwierdził (jeśli dotyczy akceptacji)
- Historia decyzji (akceptacje, odrzucenia, cofnięcia)
- Historia zmian statusów
- Niemodyfikowalny (append-only log)

> **DECYZJA:** Audyt trail musi być **niemodyfikowalny**. Żaden użytkownik, w tym admin, nie może usunąć ani edytować wpisów audytu. To wymóg compliance.

## 8.14. AI jako warstwa wspierająca

**Charakter:** Moduł opcjonalny / premium
**Etap:** Post-MVP / Premium

AI **nie jest rdzeniem produktu**. Jest warstwą, która zwiększa wartość istniejących modułów.

Zastosowania AI:
- Podsumowanie karty pracownika (historia, aktywność, trendy)
- Podsumowanie sprawy (streszczenie przebiegu, kluczowe decyzje)
- Draft odpowiedzi (na komentarz, na zgłoszenie)
- Streszczenie historii działań
- Sugestia następnego kroku (na podstawie wzorców)
- Klasyfikacja zgłoszeń (automatyczne przypisanie typu/priorytetu)
- Wyszukiwanie semantyczne w danych i dokumentach
- Wsparcie kierownika w decyzjach (anomalie, trendy, rekomendacje)

> **Zasada:** AI ma pomagać w analizie, komunikacji i workflow — nie ma być ozdobą.

## 8.15. Moduły opcjonalne — szczegóły

### A. Kontrahenci i relacje

**Etap:** Post-MVP

- Baza kontrahentów (firma, dane kontaktowe, branża)
- Opiekun kontrahenta (przypisanie pracownika)
- Historia współpracy (timeline)
- Dokumenty kontrahenta
- Notatki
- Zadania powiązane z kontrahentem
- Status relacji (aktywny, zawieszony, archiwum)

### B. Sprzedaż

**Etap:** Premium / przyszłość

- Leady (źródło, status, scoring)
- Pipeline (etapy, wartość, prawdopodobieństwo)
- Szanse sprzedaży
- Follow-up handlowy
- Forecast
- Oferty

> **NIE budujemy tego jako rdzenia.** To jest moduł, który może nigdy nie powstać, jeśli rynek potwierdzi, że wartość jest w części operacyjnej.

### C. Moduły działowe

**Etap:** Premium / przyszłość

Mechanizm rozszerzalności per dział:
- IT: certyfikaty, uprawnienia techniczne, sprzęt, licencje
- HR: oceny okresowe, ścieżki kariery
- Logistyka: flota, zasoby
- Inne: własne zakładki, własne pola, własne raporty, własne workflow

> **DECYZJA:** Na tym etapie projektujemy **mechanizm rozszerzalności** (custom fields, custom views, custom workflows), a nie konkretne moduły per dział. Moduły działowe powstaną, gdy mechanizm rozszerzalności będzie gotowy.

---

# 9. Główne przewagi produktu

| # | Przewaga | Opis |
|---|---|---|
| 1 | Widok kierowniczy | Jedno miejsce, gdzie kierownik / dyrektor widzi pełny stan operacyjny organizacji |
| 2 | Spójność | Czas pracy + zadania + procesy + akceptacje w jednym systemie, nie w 5 osobnych narzędziach |
| 3 | Workflow engine | Uniwersalny silnik procesów — urlopy, zadania, zgłoszenia, wnioski — jeden mechanizm |
| 4 | Elastyczność struktury | Działa dla firmy 10-osobowej i dla organizacji z oddziałami i spółkami |
| 5 | Konfigurowalność | Role, widoczność, statusy, pola, procesy — definiowane przez admina, nie przez developera |
| 6 | Egzekucja pracy | System prowadzi działanie (follow-up, alerty, eskalacje), nie tylko przechowuje dane |
| 7 | Wykrywanie anomalii | Automatyczne wykrywanie problemów z czasem pracy, zadaniami, procesami |
| 8 | Jeden workspace | Pracownik ma jeden widok startowy — nie nawiguje po chaosie modułów |
| 9 | Modularność | Firma włącza tylko to, czego potrzebuje — nie płaci za sprzedaż, jeśli jej nie używa |
| 10 | Integracje z codziennymi narzędziami | Gmail, Calendar, Teams, Slack — system żyje tam, gdzie ludzie pracują |

---

# 10. Najważniejsze zasady projektowe

## 10.1. Zasady produktowe

1. **Nie budujemy molocha.** Nie robimy wszystkiego naraz. Najpierw rdzeń operacyjny.
2. **Rdzeń = czas pracy + zespoły + zadania + procesy + kierownictwo.** To musi działać doskonale.
3. **Sprzedaż jest opcjonalna.** Nie wpływa na architekturę rdzenia.
4. **System prowadzi działanie.** Alerty, follow-up, eskalacje, przypomnienia — nie tylko ewidencja.
5. **Jeden workspace.** Pracownik widzi „co mam dziś zrobić", nie 15 zakładek.
6. **Prosty i lekki UX.** Mimo złożoności pod spodem — interfejs ma być czysty.
7. **Integracje z codziennymi narzędziami.** System musi żyć w ekosystemie użytkownika.

## 10.2. Zasady architektoniczne

1. **Modularny monolit.** Nie mikroserwisy. Podział na moduły domenowe wewnątrz jednego deploymentu.
2. **DDD light / Clean Architecture light.** Wydzielone domeny, ale bez over-engineeringu.
3. **REST API as the core interface.** Frontend, mobile, integracje — wszystko przez API.
4. **Jedna baza na start.** PostgreSQL. Schema-per-module lub shared schema z wyraźną separacją.
5. **Multi-tenancy.** Każda firma to osobny tenant. Izolacja danych na poziomie bazy (tenant_id).
6. **Konfiguracja zamiast kodu.** Role, statusy, workflow, pola — w bazie jako konfiguracja, nie w kodzie.
7. **Feature flags / module registry.** Moduły włączane per tenant.
8. **Audyt append-only.** Logi działań niemodyfikowalne.
9. **Web-first, API-first.** Ale architektura musi pozwalać na desktop wrapper i mobile.
10. **Keycloak jako IAM.** Nie budujemy auth od zera.

## 10.3. Zasady wielokanałowości (web / mobile / desktop)

1. **Web jest głównym kanałem.** Pełna funkcjonalność w przeglądarce.
2. **Mobile jest kanałem operacyjnym.** Kluczowe akcje z telefonu: QR, clock-in/out, wnioski, akceptacje.
3. **Desktop jest kanałem przyszłościowym.** Web app opakowana w Electron/Tauri — nie osobna aplikacja.
4. **API stanowi jedyne źródło prawdy.** Każdy klient (web, mobile, desktop) używa tego samego REST API.
5. **Frontend React — współdzielenie logiki.** Hooki, logika biznesowa, state management — współdzielone między web i ewentualnym desktop wrapperem.
6. **Mobile: React Native lub PWA.** Decyzja do podjęcia. PWA na MVP, React Native jeśli potrzebne NFC/biometria.
7. **Kiosk mode = web fullscreen.** Nie osobna aplikacja — tryb pełnoekranowy web app na tablecie.

Mapowanie funkcji na kanały:

| Funkcja | Web | Mobile | Desktop | Kiosk |
|---|---|---|---|---|
| Pełna administracja | ✅ | ❌ | ✅ | ❌ |
| Dashboard kierowniczy | ✅ | ✅ (uproszczony) | ✅ | ❌ |
| Clock-in / clock-out | ✅ | ✅ (główny kanał) | ✅ | ✅ (główny kanał) |
| Skanowanie QR | ❌ | ✅ (główny kanał) | ❌ | ✅ |
| NFC | ❌ | ✅ | ❌ | ❌ |
| Składanie wniosków | ✅ | ✅ | ✅ | ❌ |
| Akceptacje | ✅ | ✅ | ✅ | ❌ |
| Zarządzanie zadaniami | ✅ | ✅ (uproszczony) | ✅ | ❌ |
| Raporty i analityka | ✅ | ❌ | ✅ | ❌ |
| Powiadomienia push | ❌ | ✅ | ✅ (opcjonalnie) | ❌ |
| Self-service pracownika | ✅ | ✅ | ✅ | ❌ |
| Formularz zgłoszeniowy | ✅ | ✅ | ✅ | ❌ |
| Przeglądanie dokumentów | ✅ | ✅ (podgląd) | ✅ | ❌ |

---

# 11. Ograniczenia i granice produktu

## 11.1. Czego system NIE robi

| Element | Dlaczego poza zakresem |
|---|---|
| Księgowość / fakturowanie | System nie jest ERP. Ewentualnie integracja z systemem FK przez API. |
| Naliczanie wynagrodzeń / payroll | Zbyt skomplikowane regulacyjnie. Integracja z dedykowanymi systemami. |
| Pełne zarządzanie HR (rekrutacja, oceny, talent management) | System nie jest HRIS. Może mieć elementy HR-owe, ale nie zastępuje Workday/BambooHR. |
| Komunikator / chat | System nie jest Slackiem/Teamsem. Integruje się z nimi. |
| Zarządzanie projektami w sensie Jira/Asana | Moduł zadań jest uproszczony. Nie budujemy sprintów, backlogu, velocity. |
| Business Intelligence / hurtownia danych | Raporty operacyjne tak. BI / OLAP — nie. Ewentualnie eksport do zewnętrznych narzędzi. |
| E-commerce / obsługa klienta końcowego (B2C) | System jest B2B, wewnętrzny. |

## 11.2. Ograniczenia techniczne MVP

- Jedna baza danych (PostgreSQL), brak shardingu
- Jeden region deploymentu
- Brak offline mode (mobile wymaga połączenia)
- Brak real-time collaboration (np. edycja zadania przez dwóch użytkowników jednocześnie)
- Brak integracji z systemami zewnętrznymi (poza Keycloak)
- Brak API publicznego (API wewnętrzne dla frontendów)
- Brak white-label / customowego brandingu per tenant

## 11.3. Ograniczenia skalowalności na start

- Zakładany max na MVP: ~50 tenantów, ~5000 użytkowników łącznie
- Brak auto-scalingu (dopiero post-MVP z Kubernetes)
- Brak CDN na start (statyki z tego samego serwera)

---

# 12. Decyzje, które trzeba podjąć przed wejściem w backlog

Poniższe decyzje muszą być podjęte **zanim zespół zacznie implementację**. Każda z nich wpływa na architekturę, model danych lub UX.

## 12.1. Decyzje architektoniczne

| # | Decyzja | Opcje | Wpływ | Rekomenacja wstępna |
|---|---|---|---|---|
| D1 | Multi-tenancy: izolacja danych | (a) Shared DB + tenant_id, (b) Schema per tenant, (c) DB per tenant | Architektura bazy, wydajność, koszty, migracje | (a) Shared DB + tenant_id na MVP — najprostsze. Migracja do (b) jeśli potrzeba izolacji. |
| D2 | RBAC vs ABAC | (a) Czysty RBAC, (b) RBAC + atrybuty kontekstowe, (c) Pełny ABAC | Złożoność, elastyczność, performance | (b) RBAC + atrybuty — rola + pozycja w strukturze + moduł |
| D3 | Workflow engine: gotowy vs. custom | (a) Własny engine, (b) Elsa Workflows (.NET), (c) Inny silnik open-source | Czas rozwoju, elastyczność, maintenance | (a) Własny engine (uproszczony) na MVP — pełna kontrola. Ewaluacja Elsa na post-MVP. |
| D4 | Mobile: PWA vs. React Native | (a) PWA, (b) React Native, (c) Flutter, (d) Ionic/Capacitor | Dostęp do NFC, push, UX natywny, koszty | (d) Ionic/Capacitor lub (a) PWA na MVP — współdzielenie kodu React. React Native jeśli NFC kluczowe. |
| D5 | Przechowywanie plików | (a) Filesystem (local/NFS), (b) S3/MinIO, (c) Azure Blob | Skalowalność, koszty, backup | (b) MinIO na self-hosted, S3 na cloud — od MVP. |
| D6 | Powiadomienia push (mobile) | (a) Firebase Cloud Messaging, (b) OneSignal, (c) Własne via WebSocket | Zależność od dostawcy, koszty | (a) FCM — standard rynkowy, darmowy. |
| D7 | Real-time updates (frontend) | (a) Polling, (b) SSE, (c) WebSocket via SignalR | Złożoność, UX, serwer resources | (c) SignalR — naturalne w .NET, obsługuje fallback na polling. |
| D8 | Strategia migracji bazy | (a) EF Core Migrations, (b) DbUp, (c) Flyway | Kontrola, tooling, multi-tenant | (a) EF Core Migrations na MVP — spójne z ORM. |
| D9 | Tenant onboarding | (a) Self-service (rejestracja online), (b) Manual (admin tworzy tenant) | UX, bezpieczeństwo, czas do produkcji | (b) Manual na MVP — szybciej. Self-service w post-MVP. |
| D10 | Desktop wrapper technology | (a) Electron, (b) Tauri, (c) Brak na MVP | Bundle size, wydajność, dostęp do OS | (c) Brak na MVP. Tauri w przyszłości (mniejszy footprint niż Electron). |

## 12.2. Decyzje produktowe

| # | Decyzja | Opis | Rekomendacja wstępna |
|---|---|---|---|
| P1 | Język systemu na MVP | Tylko PL? PL + EN? Mechanizm i18n od razu? | Mechanizm i18n od razu, PL jako pierwszy język, EN jako drugi w post-MVP. |
| P2 | Onboarding użytkownika | Wizard? Predefiniowane szablony? Pusty system? | Predefiniowane szablony ról + struktury + typów nieobecności. Wizard w post-MVP. |
| P3 | Branding | Jedna marka (WorkBase)? White-label? | Jedna marka na MVP. White-label premium. |
| P4 | Pricing model | Per user? Per moduł? Tiered? | Do ustalenia przed Go-to-Market. Nie wpływa na architekturę MVP. |
| P5 | Czy urlopy to oddzielny moduł czy rdzeń? | Czy firma może wyłączyć moduł urlopów? | Rdzeń (domyślnie włączony), ale technicznie wydzielony moduł — możliwy do wyłączenia. |
| P6 | Czy mówimy „CRM" w nazwie produktu? | System nie jest klasycznym CRM-em. Nazwa może mylić. | Nie. Rekomendacja: „platforma operacyjno-zarządcza" lub własna nazwa (np. WorkBase). |
| P7 | Self-service vs. managed onboarding | Czy firmy same konfigurują system, czy robimy to za nie? | Managed na start (learning), self-service jako cel. |

## 12.3. Decyzje dotyczące modelu danych

| # | Decyzja | Opis | Rekomendacja wstępna |
|---|---|---|---|
| M1 | Custom fields storage | (a) EAV (Entity-Attribute-Value), (b) JSONB column, (c) Dedykowane tabele per tenant | Elastyczność, query performance, indeksowanie | (b) JSONB column w PostgreSQL — dobry balans. EAV dla zaawansowanych scenariuszy. |
| M2 | Workflow definitions storage | (a) JSON w bazie, (b) Dedykowane tabele relacyjne, (c) YAML w filesystemie | Edytowalność, wersjonowanie, performance | (a) JSON w bazie — edytowalne przez UI, wersjonowalne per rekord. |
| M3 | Audit log storage | (a) Tabela w main DB, (b) Osobna baza/schema, (c) Event store | Izolacja, performance, retencja | (a) Tabela w main DB na MVP. Osobna baza jeśli wolumen > X. |
| M4 | Hierarchia organizacyjna | (a) Adjacency list, (b) Nested set, (c) Materialized path, (d) Closure table | Łatwość zapytań hierarchicznych, performance update | (d) Closure table — dobre dla zapytań „kto jest powyżej/poniżej" i update'ów. |

---

> **Następny krok:** Po podjęciu decyzji z sekcji 12, przejść do budowy backlogu (epiki → features → taski) na podstawie tego dokumentu.
