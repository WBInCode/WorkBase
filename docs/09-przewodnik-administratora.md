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

---

## 6. Konfiguracja obszarów

### Rodzaje nieobecności

Typ urlopu ma cztery istotne pola: nazwę, płatność, wymóg akceptacji i liczbę dni w roku.

**Wymóg akceptacji** decyduje o całej ścieżce wniosku. Typ bez tego wymogu zatwierdza się natychmiast po wysłaniu i nigdy nie trafia do kolejki przełożonego. Tak zwykle konfiguruje się urlop na żądanie i zwolnienie lekarskie.

Zmiana liczby dni w roku dotyczy naliczeń przyszłych i **nie przelicza wstecz** sald już przyznanych.

### Limity przerw

Polityka przerw określa rodzaj (płatna lub bezpłatna), maksymalną liczbę przerw dziennie, maksymalną długość jednej przerwy i łączny czas w ciągu dnia.

To te ustawienia generują komunikat o wyczerpaniu limitu, który widzi pracownik przy próbie rozpoczęcia kolejnej przerwy.

### Statusy zadań

Jeden status musi być oznaczony jako domyślny, bo dostają go nowe zadania. Statusy końcowe zamykają sprawę i wyłączają zadanie z liczników spraw otwartych.

### Powiadomienia i eskalacje

Szablon powiadomienia zawiera treść oraz zmienne podstawiane automatycznie, na przykład imię pracownika czy zakres dat. Reguły eskalacji określają, co się dzieje, gdy decyzja nie zapada w wyznaczonym czasie.

---

## 7. Moduły i licencje

Flagi funkcjonalności decydują, które moduły są widoczne w firmie. Zestaw dostępnych modułów wynika z planu wykupionego w WB Platform i jest z nią synchronizowany.

Ręczne włączenie modułu spoza planu nie ma trwałego efektu, bo kolejna synchronizacja przywróci stan z platformy.

Wyłączenie modułu ukrywa całą jego sekcję w menu wszystkim użytkownikom, niezależnie od uprawnień. Dane nie są kasowane, wracają po ponownym włączeniu.

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

### Wnioski urlopowe nie trafiają do nikogo

Najczęściej brakuje wskazania przełożonego w karcie pracownika. Druga możliwość to typ urlopu bez wymogu akceptacji, który zatwierdza się sam.

---

## 9. Kolejność przy wdrożeniu nowej firmy

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
