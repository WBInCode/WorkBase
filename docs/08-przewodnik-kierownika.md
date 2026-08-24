# WorkBase: przewodnik kierownika i przełożonego

> Dla osób nadzorujących zespół: akceptacje wniosków, raporty czasu pracy, zadania zespołu.
> Stan na 2026-08-11.

---

## 1. Rzecz najważniejsza: przełożony to relacja, nie rola

To jest najczęstsze źródło nieporozumień w całym systemie, więc warto zacząć od wyjaśnienia.

WorkBase rozdziela dwie zupełnie różne rzeczy:

| Pojęcie | Skąd wynika | Co daje |
|---|---|---|
| **Rola** (Pracownik, Kierownik, HR, Admin) | przypisanie w systemie uprawnień | zestaw uprawnień, czyli jakie ekrany i operacje są dostępne |
| **Bycie przełożonym** | wskazanie w karcie pracownika, kto jest czyim przełożonym | prawo do rozpatrywania wniosków swoich podwładnych |

**Osoba z rolą Pracownik również widzi Akceptacje i Raport zespołu**, jeśli faktycznie ma podwładnych w strukturze. Nie musi mieć roli Kierownik.

Działa to tak, ponieważ akceptującego wniosek wyznacza struktura organizacyjna, a nie rola. Gdyby dostęp do kolejki akceptacji zależał wyłącznie od uprawnienia, przełożeni bez roli Kierownik nie mogliby rozpatrzyć wniosków, które system im przydzielił.

Relacja przełożony i podwładny może mieć datę zakończenia. Po jej upływie dostęp znika automatycznie.

### Konsekwencja praktyczna

Jeśli ktoś ma rozpatrywać wnioski, nie wystarczy nadać mu roli Kierownik. Trzeba wskazać go jako przełożonego w kartach konkretnych pracowników. I odwrotnie: nadanie roli Kierownik osobie bez podwładnych nie sprawi, że pojawią się u niej jakiekolwiek wnioski.

---

## 2. Rozpatrywanie wniosków urlopowych

Ekran **Akceptacje** (`/leave/approvals`) pokazuje wnioski czekające na Twoją decyzję wraz z licznikiem.

1. Otwórz Urlopy, potem Akceptacje.
2. Kliknij **Rozpatrz** przy wybranym wniosku.
3. Dopisz komentarz, jeśli decyzja wymaga wyjaśnienia. Komentarz zobaczy wnioskodawca.
4. Wybierz **Akceptuj**, **Odrzuć** albo **Cofnij**.

### Co dzieje się po decyzji

| Decyzja | Skutek |
|---|---|
| Akceptuj | wniosek zmienia status na Zaakceptowany, zarezerwowane dni przechodzą na wykorzystane, nieobecność trafia do kalendarza zespołu, pracownik dostaje powiadomienie |
| Odrzuć | wniosek zamknięty, zarezerwowane dni wracają do puli pracownika |
| Cofnij | wniosek wraca do stanu roboczego, dni wracają do puli, pracownik może poprawić i wysłać ponownie |

Decyzja działa natychmiast. Nie ma osobnego kroku zatwierdzania ani okna na wycofanie.

### Kiedy wniosek nie trafia do kolejki

Dwie najczęstsze przyczyny:

1. **W karcie pracownika nie ustawiono przełożonego.** Wniosek nie ma wtedy adresata. Wymaga poprawy w karcie pracownika przez dział kadr.
2. **Rodzaj nieobecności nie wymaga akceptacji.** Urlop na żądanie i zwolnienie lekarskie zwykle zatwierdzają się same. To ustawienie typu urlopu, nie usterka.

---

## 3. Raport zespołu

Ekran **Raport zespołu** (`/time/team-report`) pokazuje czas pracy podwładnych w wybranym tygodniu lub miesiącu, z podziałem na jednostki organizacyjne.

Raport można wyeksportować do pliku Excel.

### Korekty godzin

Podgląd zespołu **nie wystarcza** do poprawiania wpisów. Edycja czasu pracy wymaga osobnego uprawnienia (`time.edit` lub `time.manage`), które w standardowej konfiguracji ma dział kadr i administrator.

Jeśli w Twojej firmie kierownicy mają poprawiać godziny, administrator musi im to uprawnienie nadać świadomie.

---

## 4. Uprawnienia roli Kierownik

W standardowej konfiguracji rola Kierownik ma **32 uprawnienia**. Najważniejsze z nich:

| Obszar | Co obejmuje |
|---|---|
| Urlopy | podgląd, składanie własnych, **akceptacja**, podgląd kalendarza zespołu, eksport |
| Czas pracy | podgląd własny i **zespołu**, rejestracja, edycja, akceptacja kart, eksport |
| Zadania | podgląd, tworzenie, edycja, usuwanie, **przypisywanie innym**, komentowanie, eksport |
| Wynagrodzenia | podgląd własny i **zespołu** |
| Organizacja | podgląd struktury i pracowników, eksport |
| Dokumenty | podgląd, dodawanie, eksport |
| Pozostałe | dashboard, powiadomienia, raporty, akceptacja kroków procesów |

Czego rola Kierownik **nie** obejmuje:

- zarządzania strukturą organizacyjną i stanowiskami,
- dodawania i usuwania pracowników,
- zarządzania typami urlopów i limitami,
- zarządzania czasem pracy na poziomie firmy (`time.manage`),
- jakiejkolwiek konfiguracji administracyjnej.

Liczby dotyczą konfiguracji standardowej. Role można modyfikować, więc w konkretnej firmie mogą się różnić.

---

## 5. Zakres widoczności danych

Niezależnie od uprawnień działa **zakres danych**, który decyduje, czyje rekordy widzisz:

| Zakres | Znaczenie |
|---|---|
| Organizacja | wszyscy pracownicy firmy |
| Jednostka | pracownicy własnej jednostki organizacyjnej |
| Własne | wyłącznie własne rekordy |

Uprawnienie mówi **co wolno zrobić**, a zakres **na czyich danych**. Kierownik z uprawnieniem do podglądu czasu pracy zespołu i zakresem ograniczonym do jednostki zobaczy tylko swoją jednostkę, nie całą firmę.

To rozdzielenie oznacza, że nie da się zobaczyć cudzych danych przez wpisanie adresu ekranu. Serwer i tak przefiltruje wynik.

---

## 6. Zadania zespołu

Kierownik może tworzyć zadania, przypisywać je konkretnym osobom, zmieniać statusy i usuwać.

Zestaw statusów ustala firma. Typowo: Nowe, W analizie, W toku, Do akceptacji, Wstrzymane, Zamknięte, Odrzucone. Statusy końcowe zamykają sprawę i wyłączają zadanie z liczników spraw otwartych.

Zmiana samych statusów, czyli słownika dostępnych etapów, wymaga uprawnienia administracyjnego (`tasks.manage`).

---

## 7. Wynagrodzenia zespołu

Ekran Wynagrodzenia pokazuje rozliczenia na podstawie zarejestrowanego czasu pracy. Podgląd zespołu wymaga uprawnienia `payroll.view-team`, które w standardowej konfiguracji ma Kierownik i dział kadr.

Zakres jest ten sam, co przy pozostałych danych pracowników: kierownik widzi swoją jednostkę, dział kadr i administrator — całą firmę.

### Przekazanie rozliczenia do księgowości

Przycisk **Eksport XLSX** nad tabelą pobiera zestawienie za wybrany okres: normę z grafiku, czas pracy, godziny zwykłe i nadgodziny, dni urlopu i nieobecności, kwoty oraz wiersz podsumowania. Godziny i kwoty są liczbami, więc arkusz od razu na nich liczy.

Plik zawiera dokładnie te osoby, które widzisz na ekranie — eksport nie omija zakresu danych.

Pracownik bez ustawionej stawki ma kolumny kwotowe **puste**, a nie zerowe. To rozróżnienie jest celowe: puste pole znaczy „stawki nikt nie ustawił", zero znaczyłoby „wyliczono zero".

Rozbicie dzień po dniu jest osobno, w raporcie zespołu — tam eksport daje siatkę pracownik × dzień.

---

## 8. Lista kontrolna przy nowym pracowniku

Żeby nowa osoba działała poprawnie od pierwszego dnia, w jej karcie muszą być ustawione:

1. **Jednostka organizacyjna**, bo od niej zależy zakres danych i miejsce w raportach.
2. **Stanowisko**.
3. **Przełożony**, bo bez niego wnioski urlopowe nie mają adresata.
4. **Dostęp do WorkBase w WB Platform**, bo bez kafelka osoba w ogóle się nie zaloguje.

Karta pracownika ostrzega, gdy przełożony nie jest ustawiony. Warto reagować na to ostrzeżenie od razu, a nie przy pierwszym nieudanym wniosku.

---

## Powiązane dokumenty

- [Przewodnik pracownika](07-przewodnik-pracownika.md)
- [Przewodnik administratora](09-przewodnik-administratora.md)
