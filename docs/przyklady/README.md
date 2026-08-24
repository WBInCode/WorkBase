# Przykładowe pliki do testów

## `import-pracownikow-przyklad.csv`

Odwzorowuje to, co realnie przychodzi od klienta: eksport z polskiego programu kadrowego (Symfonia, Optima, albo „Zapisz jako CSV" z Excela).

**Plik jest zapisany w Windows-1250, nie w UTF-8** — i to jest celowe. Otwarty w edytorze ustawionym na UTF-8 wygląda na uszkodzony; tak właśnie wygląda plik, który klient przysyła mailem. Nie konwertuj go „przy okazji", bo przestanie testować to, do czego służy.

Zawiera: separator średnikowy, końce linii Windows (CRLF), polskie znaki, pola w cudzysłowie z przecinkiem w środku, cztery zapisy daty i trzy wiersze, które **mają zostać odrzucone**.

| Wiersz | Co sprawdza | Oczekiwany wynik |
|---|---|---|
| 001 | pole w cudzysłowie z przecinkiem (`"Kierownik, dział handlowy"`) | import |
| 002, 003 | polskie znaki z Windows-1250 (`Żółw`, `Ćwikliński`) | import, nazwiska bez krzaków |
| 003 | jednocyfrowy dzień i miesiąc (`5.3.2020`) | import, 5 marca |
| 004 | data ISO (`2021-01-04`) | import |
| 005 | data z ukośnikiem (`12/07/2022`) | import, **12 lipca** — czytamy po polsku, nie po amerykańsku |
| 007 | data nieistniejąca (`31.02.2023`) | odrzucony, bez cichego przewinięcia na 3 marca |
| 008 | brak adresu e-mail | odrzucony |
| 009 | data jako tekst (`brak`) | odrzucony |

Razem: **7 wierszy do zaimportowania, 3 do odrzucenia.**

Adresy są w domenie `example.invalid` (zarezerwowana w RFC 2606, poczta nigdy nie zostanie dostarczona). To nie jest kosmetyka: import **kolejkuje zaproszenie do WB Platform dla każdego zaimportowanego pracownika**, więc plik z prawdziwymi adresami rozsyła prawdziwe zaproszenia.

Automatyczne odpowiedniki tych przypadków siedzą w `frontend/src/utils/csvParser.test.ts` — plik służy do sprawdzenia całej ścieżki ręcznie, w przeglądarce.
