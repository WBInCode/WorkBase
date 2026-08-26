import {
  BookOpen,
  CalendarDays,
  ClipboardCheck,
  FileArchive,
  LifeBuoy,
  ListTodo,
  Palmtree,
  Settings2,
  Users,
  Wallet,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

/**
 * Tresc ekranu Pomoc. Trzymana osobno od widoku, zeby dalo sie ja poprawiac bez
 * dotykania ukladu, a takze zeby ten sam material dalo sie wykorzystac gdzie indziej.
 *
 * Kazdy wpis moze deklarowac `wymaga` (kody uprawnien) albo `tylkoPrzelozony`.
 * Widok pokazuje tylko to, co dana osoba faktycznie moze zrobic, zeby pracownik
 * nie czytal instrukcji do ekranow, ktorych nie zobaczy.
 */

export interface WpisPomocy {
  id: string;
  pytanie: string;
  odpowiedz: string[];
  kroki?: string[];
  uwaga?: string;
  sciezka?: string;
  etykietaSciezki?: string;
  /** Wpis widoczny, gdy uzytkownik ma KTORYKOLWIEK z tych kodow. Pusto = widoczny zawsze. */
  wymaga?: string[];
  /** Wpis widoczny wylacznie dla osoby majacej podwladnych w strukturze. */
  tylkoPrzelozony?: boolean;
}

export interface SekcjaPomocy {
  id: string;
  tytul: string;
  opis: string;
  ikona: LucideIcon;
  wpisy: WpisPomocy[];
}

export const SEKCJE_POMOCY: readonly SekcjaPomocy[] = [
  {
    id: 'start',
    tytul: 'Pierwsze kroki',
    opis: 'Logowanie, orientacja w interfejsie i to, co zobaczysz zaraz po wejsciu.',
    ikona: BookOpen,
    wpisy: [
      {
        id: 'jak-sie-zalogowac',
        pytanie: 'Jak zalogować się do WorkBase?',
        odpowiedz: [
          'WorkBase nie ma osobnego hasła. Logujesz się kontem WB Platform, czyli tym samym, którego używasz do pozostałych aplikacji firmy.',
          'Jeśli masz już otwartą sesję w WB Platform, przejście do WorkBase nie wymaga ponownego podawania hasła.',
        ],
        kroki: [
          'Wejdź na wb-platform.pl i zaloguj się swoim adresem e-mail.',
          'Na liście aplikacji kliknij kafelek WorkBase.',
          'Zostaniesz przeniesiony do systemu, a konto utworzy się automatycznie przy pierwszym wejściu.',
        ],
        uwaga:
          'Konto w WorkBase powstaje wyłącznie przez wejście z WB Platform. Jeśli ktoś nie widzi kafelka, znaczy to, że nie ma jeszcze przyznanego dostępu do aplikacji.',
      },
      {
        id: 'ekran-startowy',
        pytanie: 'Co widzę na ekranie „Mój dzień”?',
        odpowiedz: [
          'To ekran startowy dostępny dla każdej zalogowanej osoby, niezależnie od uprawnień.',
          'U góry znajdziesz powitanie i licznik czasu pracy wraz z przyciskiem rozpoczęcia lub zakończenia dnia. Pod spodem są skróty do zadań, spraw po terminie, oczekujących akceptacji i wniosków urlopowych.',
          'Niżej po lewej stronie są Twoje zadania i ostatnia aktywność, a po prawej wnioski urlopowe oraz kod QR do terminala.',
        ],
        sciezka: '/workspace',
        etykietaSciezki: 'Otwórz Mój dzień',
      },
      {
        id: 'co-wymaga-uwagi',
        pytanie: 'Co to jest panel „Co wymaga uwagi”?',
        odpowiedz: [
          'Lista rzeczy do zrobienia, wyliczana na bieżąco z danych systemu — a nie kolejny zestaw liczb.',
          'Najpierw pokazuje sprawy pilne, czyli takie, przez które ktoś czeka albo coś stanęło: wnioski bez decyzji, osoby bez zarejestrowanego wejścia mimo grafiku, pracowników bez przełożonego.',
          'Niżej trafiają rzeczy do uzupełnienia, które niczego nie blokują — na przykład brakujące stawki godzinowe czy nierozpatrzone anomalie czasu pracy.',
        ],
        uwaga:
          'Widzisz wyłącznie osoby ze swojego zakresu danych. Wnioski czekające na Twoją decyzję pokazują się niezależnie od uprawnień, bo akceptanta wyznacza struktura, a nie rola.',
        sciezka: '/dashboard',
        etykietaSciezki: 'Otwórz Dashboard',
        wymaga: ['dashboard.view'],
      },
      {
        id: 'kreator-pierwszego-startu',
        pytanie: 'Czym jest kreator pierwszego startu?',
        odpowiedz: [
          'Nowa firma po nadaniu licencji przechodzi krótki kreator: cztery pytania o to, kto tu pracuje, w jakich godzinach, kto akceptuje wnioski i ile jest dni urlopu.',
          'Każde pytanie ma odpowiedź domyślną, więc przeklikanie kreatora do końca daje działającą firmę jednoosobową w minutę. To poprawny sposób przejścia, nie obejście.',
          'Kreator można przerwać — po ponownym zalogowaniu wraca w to samo miejsce, w którym został zamknięty.',
        ],
        uwaga:
          'Firma działa, zanim ktokolwiek dotknie kreatora: typy urlopów, statusy zadań i obiegi akceptacji są zakładane automatycznie przy tworzeniu firmy. Kreator służy do ich potwierdzenia i uzupełnienia, a nie do uruchomienia systemu.',
      },
      {
        id: 'dlaczego-nie-widze',
        pytanie: 'Dlaczego nie widzę niektórych pozycji w menu?',
        odpowiedz: [
          'Menu pokazuje wyłącznie te ekrany, do których masz uprawnienia. To nie jest usterka, tylko celowe działanie.',
          'Dodatkowo administrator może wyłączyć całe moduły dla firmy. Wyłączony moduł znika z menu wszystkim, także osobom z pełnymi uprawnieniami.',
          'Jeśli wejdziesz na adres ekranu bez uprawnienia, zobaczysz komunikat z nazwą brakującego uprawnienia. Warto podać ją administratorowi przy zgłoszeniu.',
        ],
      },
      {
        id: 'motyw-i-jezyk',
        pytanie: 'Czy mogę zmienić wygląd aplikacji?',
        odpowiedz: [
          'W górnym pasku znajduje się przełącznik jasnego i ciemnego motywu. Wybór zapamiętuje się w przeglądarce.',
          'Domyślnie system podąża za ustawieniem systemu operacyjnego.',
          'Interfejs jest w języku polskim i nie ma obecnie przełącznika języka.',
        ],
      },
      {
        id: 'powiadomienia',
        pytanie: 'Gdzie znajdę powiadomienia?',
        odpowiedz: [
          'Dzwonek w górnym pasku pokazuje liczbę nieprzeczytanych powiadomień. Po kliknięciu rozwija się lista z możliwością oznaczenia wszystkich jako przeczytane.',
          'Powiadomienia dostajesz między innymi o decyzji w sprawie wniosku urlopowego oraz o wnioskach czekających na Twoją akceptację.',
        ],
      },
    ],
  },

  {
    id: 'czas-pracy',
    tytul: 'Czas pracy',
    opis: 'Rejestracja wejścia i wyjścia, przerwy, karta czasu i terminal.',
    ikona: CalendarDays,
    wpisy: [
      {
        id: 'rozpoczecie-dnia',
        pytanie: 'Jak zarejestrować rozpoczęcie i zakończenie pracy?',
        odpowiedz: [
          'Przycisk rozpoczęcia pracy znajduje się w górnym pasku, a na ekranie „Mój dzień” dodatkowo w dużym liczniku.',
          'Po rozpoczęciu licznik zaczyna odmierzać czas, a przycisk zmienia się w zakończenie pracy.',
        ],
        kroki: [
          'Kliknij Rozpocznij na początku dnia.',
          'W trakcie dnia możesz rejestrować przerwy.',
          'Na koniec kliknij Zakończ.',
        ],
        uwaga:
          'Nie da się rozpocząć dnia dwa razy. Jeśli zapomnisz zakończyć poprzedni dzień, system odmówi rozpoczęcia kolejnego i trzeba poprosić przełożonego lub dział kadr o korektę.',
        wymaga: ['time.create'],
      },
      {
        id: 'przerwy',
        pytanie: 'Jak działają przerwy?',
        odpowiedz: [
          'Przerwy dzielą się na płatne i bezpłatne. Płatna wlicza się do czasu pracy, bezpłatna nie.',
          'Liczbę i długość przerw ustala firma w politykach przerw. Po wyczerpaniu limitu system odmówi rozpoczęcia kolejnej przerwy tego samego rodzaju i napisze wprost, że limit został osiągnięty.',
        ],
        uwaga: 'Przerwę można rozpocząć dopiero po zarejestrowaniu wejścia.',
        wymaga: ['time.create'],
      },
      {
        id: 'karta-czasu',
        pytanie: 'Gdzie sprawdzę swoje godziny?',
        odpowiedz: [
          'Karta czasu pokazuje zarejestrowane wejścia i wyjścia w układzie dziennym, tygodniowym i miesięcznym, wraz z sumą godzin i rozbiciem przerw.',
        ],
        sciezka: '/time/timesheet',
        etykietaSciezki: 'Otwórz Kartę czasu',
        wymaga: ['time.view'],
      },
      {
        id: 'grafik',
        pytanie: 'Gdzie zobaczę swój grafik?',
        odpowiedz: [
          'Grafik pracy pokazuje zaplanowane zmiany. Jest to plan, a nie zapis faktycznie przepracowanego czasu, który znajdziesz w karcie czasu.',
        ],
        sciezka: '/time/schedule',
        etykietaSciezki: 'Otwórz Grafik pracy',
        wymaga: ['time.view'],
      },
      {
        id: 'terminal',
        pytanie: 'Jak działa terminal w zakładzie?',
        odpowiedz: [
          'Terminal to osobny, uproszczony ekran przeznaczony na tablet lub monitor przy wejściu. Nie ma menu ani danych osobowych, tylko duże przyciski rejestracji.',
          'Pracownik identyfikuje się kodem QR wygenerowanym w aplikacji na ekranie „Mój dzień”.',
          'Kod QR jest ważny krótko i po zeskanowaniu przestaje działać, więc nie da się go przekazać innej osobie na później.',
        ],
        uwaga:
          'Terminal loguje się własnym kontem terminalowym, a nie kontem pracownika. Konfiguruje je administrator.',
        wymaga: ['time.create'],
      },
      {
        id: 'blad-godzin',
        pytanie: 'Zapomniałem zarejestrować wyjście. Co teraz?',
        odpowiedz: [
          'Nie da się cofnąć ani dopisać sobie godzin samodzielnie. Korektę wprowadza przełożony w raporcie zespołu albo dział kadr.',
          'Zgłoś to możliwie szybko, podając datę i rzeczywiste godziny.',
        ],
      },
    ],
  },

  {
    id: 'urlopy',
    tytul: 'Urlopy i nieobecności',
    opis: 'Składanie wniosków, saldo dni, statusy i kalendarz zespołu.',
    ikona: Palmtree,
    wpisy: [
      {
        id: 'zloz-wniosek',
        pytanie: 'Jak złożyć wniosek urlopowy?',
        odpowiedz: [
          'Wniosek składasz na ekranie Wnioski. Wybierasz rodzaj nieobecności, zakres dat i opcjonalnie dopisujesz uzasadnienie.',
          'System od razu sprawdza, czy masz wystarczającą liczbę dni. Jeśli nie, wniosku nie da się wysłać.',
        ],
        kroki: [
          'Otwórz Urlopy, potem Wnioski.',
          'Wybierz rodzaj nieobecności.',
          'Podaj datę początkową i końcową.',
          'Sprawdź wyliczoną liczbę dni i wyślij wniosek.',
        ],
        sciezka: '/leave/request',
        etykietaSciezki: 'Otwórz Wnioski',
        wymaga: ['leave.create', 'leave.view'],
      },
      {
        id: 'rodzaje-urlopow',
        pytanie: 'Jakie są rodzaje nieobecności?',
        odpowiedz: [
          'Listę ustala firma, więc może się różnić. Typowy zestaw to urlop wypoczynkowy, urlop na żądanie, zwolnienie lekarskie, opieka nad dzieckiem i urlop bezpłatny.',
          'Każdy rodzaj ma trzy istotne cechy: czy jest płatny, czy wymaga akceptacji przełożonego oraz ile dni przysługuje w roku.',
          'Rodzaje, które nie wymagają akceptacji, zatwierdzają się od razu po wysłaniu. Dotyczy to zwykle urlopu na żądanie i zwolnienia lekarskiego.',
        ],
        wymaga: ['leave.view'],
      },
      {
        id: 'saldo',
        pytanie: 'Jak liczone jest saldo dni?',
        odpowiedz: [
          'Saldo prowadzone jest osobno dla każdego rodzaju nieobecności i każdego roku.',
          'W chwili wysłania wniosku dni są rezerwowane, więc znikają z puli dostępnej, choć nie są jeszcze wykorzystane. Dopiero akceptacja zamienia rezerwację na wykorzystanie.',
          'Jeśli wniosek zostanie odrzucony albo cofnięty do poprawy, rezerwacja znika i dni wracają do puli.',
        ],
        wymaga: ['leave.view'],
      },
      {
        id: 'statusy-wnioskow',
        pytanie: 'Co oznaczają statusy wniosku?',
        odpowiedz: [
          'Roboczy oznacza wniosek jeszcze niewysłany, który możesz swobodnie edytować.',
          'Oczekuje oznacza, że wniosek czeka na decyzję przełożonego, a dni są już zarezerwowane.',
          'Zaakceptowany to decyzja pozytywna. Dni przechodzą na wykorzystane, a nieobecność pojawia się w kalendarzu zespołu.',
          'Odrzucony kończy sprawę, dni wracają do puli.',
          'Cofnięty oznacza, że przełożony poprosił o poprawki. Wniosek wraca do stanu roboczego i możesz go wysłać ponownie.',
        ],
        wymaga: ['leave.view'],
      },
      {
        id: 'kto-akceptuje',
        pytanie: 'Kto akceptuje mój wniosek?',
        odpowiedz: [
          'Wniosek trafia do osoby wskazanej jako Twój przełożony w strukturze organizacyjnej.',
          'Jeśli w karcie pracownika nie ustawiono przełożonego, wniosek nie ma do kogo trafić. Zgłoś to działowi kadr.',
        ],
        wymaga: ['leave.view'],
      },
      {
        id: 'wnioski-firmowe',
        pytanie: 'Jak złożyć wniosek inny niż urlopowy?',
        odpowiedz: [
          'Ekran Wnioski pozwala złożyć wniosek na formularzu przygotowanym przez firmę — na przykład o zaliczkę, delegację, pracę zdalną albo sprzęt.',
          'Wniosek trafia do Twojego przełożonego tą samą drogą co wniosek urlopowy, razem z przypomnieniami i zastępstwami.',
          'Dopóki nikt nie podjął decyzji, możesz wniosek wycofać przyciskiem „Wycofaj”.',
        ],
        uwaga:
          'Jeśli lista rodzajów jest pusta, firma nie zdefiniowała jeszcze żadnego wniosku — zrobi to administrator.',
        sciezka: '/wnioski',
        etykietaSciezki: 'Otwórz Wnioski',
        wymaga: ['wnioski.view'],
      },
      {
        id: 'kalendarz-urlopow',
        pytanie: 'Gdzie zobaczę, kto jest nieobecny?',
        odpowiedz: [
          'Kalendarz pokazuje zatwierdzone nieobecności w wybranym okresie, z kolorami odpowiadającymi rodzajom urlopu.',
          'Wnioski oczekujące na decyzję nie są tam widoczne.',
        ],
        sciezka: '/leave/calendar',
        etykietaSciezki: 'Otwórz Kalendarz',
        wymaga: ['leave.view'],
      },
    ],
  },

  {
    id: 'zadania',
    tytul: 'Zadania',
    opis: 'Prowadzenie zadań, statusy, komentarze i terminy.',
    ikona: ListTodo,
    wpisy: [
      {
        id: 'moje-zadania',
        pytanie: 'Gdzie znajdę zadania przypisane do mnie?',
        odpowiedz: [
          'Ekran Moje zadania pokazuje wyłącznie zadania przypisane do Ciebie, z licznikami spraw otwartych i ukończonych.',
          'Ekran Wszystkie pokazuje szerszy zakres, zależny od Twoich uprawnień.',
        ],
        sciezka: '/tasks/my',
        etykietaSciezki: 'Otwórz Moje zadania',
        wymaga: ['tasks.view'],
      },
      {
        id: 'statusy-zadan',
        pytanie: 'Jakie są statusy zadań?',
        odpowiedz: [
          'Zestaw statusów ustala firma. Typowo są to: Nowe, W analizie, W toku, Do akceptacji, Wstrzymane, Zamknięte i Odrzucone.',
          'Jeden ze statusów jest oznaczony jako domyślny dla nowych zadań, a statusy końcowe zamykają sprawę.',
        ],
        wymaga: ['tasks.view'],
      },
      {
        id: 'komentarze',
        pytanie: 'Jak dopisać komentarz do zadania?',
        odpowiedz: [
          'Otwórz zadanie z listy. Komentarze znajdują się pod opisem i zapisują od razu po wysłaniu.',
        ],
        wymaga: ['tasks.comment'],
      },
      {
        id: 'termin-zadania',
        pytanie: 'Jak działa termin zadania?',
        odpowiedz: [
          'Termin podaje się jako datę i godzinę w Twojej strefie czasowej.',
          'Zadania po terminie są zliczane osobno na ekranie „Mój dzień”.',
        ],
        wymaga: ['tasks.view'],
      },
      {
        id: 'tworzenie-zadan',
        pytanie: 'Jak utworzyć zadanie i przypisać je komuś?',
        odpowiedz: [
          'Nowe zadanie tworzy się przyciskiem na liście zadań. Podajesz tytuł, opis, priorytet i termin.',
          'Przypisanie zadania innej osobie wymaga osobnego uprawnienia i nie każdy je ma.',
        ],
        sciezka: '/tasks',
        etykietaSciezki: 'Otwórz listę zadań',
        wymaga: ['tasks.create'],
      },
    ],
  },

  {
    id: 'dokumenty',
    tytul: 'Dokumenty',
    opis: 'Wysyłanie plików, pobieranie i porządkowanie.',
    ikona: FileArchive,
    wpisy: [
      {
        id: 'dodaj-dokument',
        pytanie: 'Jak dodać dokument?',
        odpowiedz: [
          'Na ekranie Pliki wybierasz plik z dysku i opcjonalnie przypisujesz go do kategorii.',
          'Dopuszczalne typy plików i maksymalny rozmiar ustala administrator. Jeśli plik zostanie odrzucony, komunikat poda powód.',
        ],
        sciezka: '/documents',
        etykietaSciezki: 'Otwórz Pliki',
        wymaga: ['documents.create'],
      },
      {
        id: 'kategorie-dokumentow',
        pytanie: 'Do czego służą kategorie dokumentów?',
        odpowiedz: [
          'Kategorie porządkują pliki i ułatwiają wyszukiwanie. Nie są obowiązkowe.',
          'Listę kategorii tworzy osoba z uprawnieniem do zarządzania dokumentami.',
        ],
        sciezka: '/documents/categories',
        etykietaSciezki: 'Otwórz Kategorie',
        wymaga: ['documents.manage'],
      },
      {
        id: 'usuwanie-dokumentu',
        pytanie: 'Czy usunięty dokument da się odzyskać?',
        odpowiedz: [
          'Usunięcie oznacza plik jako skasowany i znika on z listy. O odzyskanie trzeba poprosić administratora.',
        ],
        wymaga: ['documents.view'],
      },
    ],
  },

  {
    id: 'wynagrodzenia',
    tytul: 'Wynagrodzenia',
    opis: 'Podgląd rozliczenia własnego i zespołu.',
    ikona: Wallet,
    wpisy: [
      {
        id: 'moje-rozliczenie',
        pytanie: 'Co widzę na ekranie Wynagrodzenia?',
        odpowiedz: [
          'Rozliczenie za wybrany okres, wyliczone na podstawie zarejestrowanego czasu pracy.',
          'Zwykły pracownik widzi wyłącznie własne dane. Zakres widoczności wynika z roli i nie da się go obejść adresem ekranu.',
        ],
        sciezka: '/payroll',
        etykietaSciezki: 'Otwórz Wynagrodzenia',
        wymaga: ['payroll.view'],
      },
      {
        id: 'rozliczenie-zespolu',
        pytanie: 'Jak zobaczyć rozliczenia zespołu?',
        odpowiedz: [
          'Potrzebne jest uprawnienie do podglądu rozliczeń zespołu. Mają je standardowo kierownicy i dział kadr.',
          'Kierownik widzi swoją jednostkę, dział kadr i administrator — całą firmę. Zakres wynika z tych samych ustawień, co widoczność danych pracownika.',
        ],
        wymaga: ['payroll.view-team'],
      },
      {
        id: 'dodatki-nocne-swiateczne',
        pytanie: 'Skąd biorą się dodatki nocny i świąteczny?',
        odpowiedz: [
          'Godziny nocne liczone są z rzeczywistych wejść i wyjść, a nie z sumy na karcie — zmiana przechodząca przez północ dzieli się między dwie doby.',
          'Godziny świąteczne to praca w dniu oznaczonym w kalendarzu dni wolnych. Bez wpisanych dni wolnych dodatek świąteczny zawsze wyniesie zero.',
          'Oba dodatki liczą się jako nadwyżka ponad stawkę, więc godzina nocna będąca jednocześnie nadgodziną nie jest płatna dwa razy — dostaje wynagrodzenie za nadgodzinę plus sam dodatek.',
        ],
        uwaga:
          'Mnożnik ustawiony na 1 oznacza brak dodatku. Porę nocną i mnożniki ustawia się w ustawieniach wynagrodzeń.',
        wymaga: ['payroll.view'],
      },
      {
        id: 'eksport-rozliczenia',
        pytanie: 'Jak przekazać rozliczenie do księgowości?',
        odpowiedz: [
          'Przycisk „Eksport XLSX” nad tabelą pobiera zestawienie za wybrany okres: normę z grafiku, czas pracy, godziny zwykłe i nadgodziny, dni urlopu i nieobecności oraz kwoty, wraz z wierszem podsumowania.',
          'Godziny i kwoty są w pliku liczbami, więc można na nich liczyć bezpośrednio w arkuszu.',
          'Plik zawiera dokładnie te osoby, które widzisz na ekranie — eksport nie omija zakresu danych.',
        ],
        uwaga:
          'Pracownik bez ustawionej stawki ma kolumny kwotowe puste, a nie zerowe. Puste pole oznacza „stawki nikt nie ustawił”, zero oznaczałoby wyliczone zero.',
        sciezka: '/payroll',
        etykietaSciezki: 'Otwórz Wynagrodzenia',
        wymaga: ['payroll.view'],
      },
      {
        id: 'stawka-kto-widzi',
        pytanie: 'Kto widzi stawkę godzinową pracownika?',
        odpowiedz: [
          'Wyłącznie osoby z uprawnieniem do rozliczeń zespołu i tylko w swoim zakresie danych. Każdy widzi własną stawkę.',
          'Na karcie pracownika i na liście pracowników stawka innej osoby jest ukryta, jeśli nie masz do niej prawa.',
        ],
      },
    ],
  },

  {
    id: 'przelozony',
    tytul: 'Dla przełożonego',
    opis: 'Akceptacje wniosków i nadzór nad czasem pracy zespołu.',
    ikona: ClipboardCheck,
    wpisy: [
      {
        id: 'kim-jest-przelozony',
        pytanie: 'Skąd system wie, że jestem przełożonym?',
        odpowiedz: [
          'Nie decyduje o tym rola, tylko struktura organizacyjna. Przełożonym jesteś wtedy, gdy w kartach innych pracowników wskazano Cię jako ich przełożonego.',
          'Dzięki temu osoba z rolą Pracownik również widzi Akceptacje i Raport zespołu, jeśli faktycznie ma podwładnych.',
          'Jeśli relacja została zakończona datą, uprawnienie znika razem z nią.',
        ],
        tylkoPrzelozony: true,
      },
      {
        id: 'akceptacja-wniosku',
        pytanie: 'Jak rozpatrzyć wniosek urlopowy?',
        odpowiedz: [
          'Ekran Akceptacje pokazuje wnioski czekające na Twoją decyzję wraz z licznikiem.',
          'Masz trzy możliwości: zaakceptować, odrzucić albo cofnąć wniosek do poprawy. Do każdej decyzji możesz dopisać komentarz, który zobaczy wnioskodawca.',
        ],
        kroki: [
          'Otwórz Urlopy, potem Akceptacje.',
          'Kliknij Rozpatrz przy wybranym wniosku.',
          'Dopisz komentarz, jeśli decyzja wymaga wyjaśnienia.',
          'Wybierz Akceptuj, Odrzuć albo Cofnij.',
        ],
        uwaga:
          'Akceptacja od razu zmienia saldo pracownika i wpisuje nieobecność do kalendarza. Odrzucenie i cofnięcie zwalniają zarezerwowane dni.',
        sciezka: '/leave/approvals',
        etykietaSciezki: 'Otwórz Akceptacje',
        tylkoPrzelozony: true,
      },
      {
        id: 'raport-zespolu',
        pytanie: 'Co pokazuje Raport zespołu?',
        odpowiedz: [
          'Czas pracy Twoich podwładnych w wybranym tygodniu lub miesiącu, z podziałem na jednostki organizacyjne.',
          'Raport można wyeksportować do pliku Excel.',
        ],
        sciezka: '/time/team-report',
        etykietaSciezki: 'Otwórz Raport zespołu',
        tylkoPrzelozony: true,
      },
      {
        id: 'korekta-czasu',
        pytanie: 'Czy mogę poprawić godziny pracownikowi?',
        odpowiedz: [
          'Poprawianie wpisów czasu wymaga osobnego uprawnienia do zarządzania czasem pracy, które standardowo ma dział kadr i administrator.',
          'Sam podgląd zespołu nie wystarcza do wprowadzania korekt.',
        ],
        wymaga: ['time.edit', 'time.manage'],
      },
      {
        id: 'brak-wnioskow',
        pytanie: 'Nie widzę wniosku, który pracownik na pewno wysłał.',
        odpowiedz: [
          'Najczęstsza przyczyna to brak ustawionego przełożonego w karcie tego pracownika. Wniosek nie ma wtedy adresata.',
          'Druga możliwość to rodzaj nieobecności, który nie wymaga akceptacji. Taki wniosek zatwierdza się sam i nie trafia do kolejki.',
        ],
        tylkoPrzelozony: true,
      },
    ],
  },

  {
    id: 'organizacja',
    tytul: 'Pracownicy i struktura',
    opis: 'Karty pracowników, jednostki organizacyjne i import danych.',
    ikona: Users,
    wpisy: [
      {
        id: 'struktura',
        pytanie: 'Do czego służy Struktura?',
        odpowiedz: [
          'Pokazuje hierarchię jednostek organizacyjnych firmy i przypisanych do nich pracowników.',
          'Struktura jest podstawą działania akceptacji, raportów zespołu i zakresów widoczności danych. Błędy w strukturze przekładają się bezpośrednio na to, kto co widzi.',
        ],
        sciezka: '/org/tree',
        etykietaSciezki: 'Otwórz Strukturę',
        wymaga: ['org.view'],
      },
      {
        id: 'karta-pracownika',
        pytanie: 'Co zawiera karta pracownika?',
        odpowiedz: [
          'Dane podstawowe, przypisanie do jednostki i stanowiska, wskazanie przełożonego, a także czas pracy, urlopy i zadania tej osoby.',
          'Przypisanie i przełożonego ustawia się bezpośrednio w karcie.',
        ],
        wymaga: ['org.view'],
      },
      {
        id: 'dodanie-pracownika',
        pytanie: 'Jak dodać pracownika?',
        odpowiedz: [
          'Pojedynczą osobę dodaje się przyciskiem na liście pracowników.',
          'Zaraz po utworzeniu warto ustawić jednostkę, stanowisko i przełożonego. Bez przełożonego wnioski urlopowe tej osoby nie będą miały adresata.',
        ],
        wymaga: ['org.create'],
      },
      {
        id: 'terminy-badania-bhp',
        pytanie: 'Jak pilnować badań lekarskich, BHP i uprawnień?',
        odpowiedz: [
          'Ekran „Terminy” pokazuje, co wygasa w najbliższych 30, 60 albo 90 dniach — badania okresowe, szkolenia BHP, uprawnienia z datą ważności i końce umów.',
          'Terminy wprowadza się na karcie pracownika, w sekcji „Terminy”. Rodzaje terminów i to, ile dni wcześniej ma pojawić się ostrzeżenie, ustala firma w Ustawieniach.',
          'Odnowienie zakłada nowy wpis, a poprzedni trafia do historii — dzięki temu widać przebieg badań, a nie tylko ostatnią datę.',
          'Dzień po wejściu terminu w okno ostrzeżenia oraz w dniu jego upływu system wysyła powiadomienie pracownikowi i jego przełożonemu. Każde z nich idzie raz, nie codziennie.',
        ],
        uwaga:
          'System niczego nie blokuje. Osoba z nieaktualnym badaniem normalnie zarejestruje czas pracy i złoży wniosek — pokazujemy stan, a decyzja o dopuszczeniu do pracy należy do pracodawcy.',
        sciezka: '/terminy',
        etykietaSciezki: 'Otwórz terminy',
        wymaga: ['org.view'],
      },
      {
        id: 'zaproszenia-przy-dodawaniu',
        pytanie: 'Kiedy pracownik dostaje zaproszenie do logowania?',
        odpowiedz: [
          'Dodanie pracownika w Ustawieniach — pojedynczo albo importem — od razu kolejkuje zaproszenie do platformy WB, na adres podany w kartotece.',
          'Wyjątkiem jest kreator pierwszego startu: tam zaproszenia są domyślnie wyłączone, a wysyłkę włącza się osobnym przełącznikiem. Chodzi o to, żeby import kilkudziesięciu osób nie rozesłał kilkudziesięciu zaproszeń, zanim ktokolwiek sprawdzi poprawność listy.',
        ],
        uwaga:
          'Zaproszenie zakłada konto na platformie, a nie tylko w WorkBase — dlatego warto najpierw obejrzeć zaimportowaną listę, a dopiero potem zapraszać.',
      },
      {
        id: 'import-csv',
        pytanie: 'Jak zaimportować listę pracowników z pliku?',
        odpowiedz: [
          'Import prowadzi przez kolejne kroki: wybór pliku, dopasowanie kolumn i podgląd przed zapisem.',
          'System sam próbuje dopasować kolumny po nagłówkach. Jeśli któregoś wymaganego pola nie rozpozna, napisze wprost, czego brakuje.',
          'Plik z programu kadrowego można wgrać bez konwersji: rozpoznawane jest zarówno kodowanie UTF-8, jak i Windows-1250 używane przez Symfonię, Optimę i Excela, a separatorem może być przecinek albo średnik.',
          'Datę zatrudnienia można podać jako 15.03.2015, 15-03-2015, 15/03/2015 albo 2015-03-15. Zapis z ukośnikiem czytany jest po polsku — dzień jako pierwszy.',
        ],
        uwaga:
          'Wiersze z nieprawidłową datą albo bez adresu e-mail są pomijane i wypisane na podglądzie przed zapisem. Import nie zapisze zmyślonej daty w miejsce błędnej.',
        sciezka: '/org/employees/import',
        etykietaSciezki: 'Otwórz Import CSV',
        wymaga: ['org.import'],
      },
    ],
  },

  {
    id: 'administracja',
    tytul: 'Administracja',
    opis: 'Role, uprawnienia, moduły i konfiguracja słowników.',
    ikona: Settings2,
    wpisy: [
      {
        id: 'gotowosc-konfiguracji',
        pytanie: 'Skąd wiem, czego jeszcze brakuje w konfiguracji?',
        odpowiedz: [
          'Ekran „Gotowość konfiguracji” wylicza na bieżąco z danych firmy, co jeszcze nie zadziała i gdzie to ustawić.',
          'Pozycje dzielą się na dwie grupy: takie, które blokują funkcję całkowicie (np. brak przełożonych — wnioski nie mają komu trafić do akceptacji), oraz takie, przez które funkcja działa w okrojonej formie (np. brak stawek godzinowych — godziny policzą się, ale kwoty będą puste).',
          'Kreator pierwszego startu zadaje tylko trzy pytania i celowo nie pyta o resztę, więc zaraz po nim ta lista zwykle nie jest pusta. To normalne.',
        ],
        uwaga:
          'Nic z tej listy nie jest wymagane. Jeśli któraś funkcja nie jest Wam potrzebna, można zostawić ją nieustawioną — system niczego nie wymusza, tylko informuje o skutkach.',
        sciezka: '/admin/gotowosc',
        etykietaSciezki: 'Otwórz gotowość konfiguracji',
        wymaga: ['org.edit'],
      },
      {
        id: 'model-uprawnien',
        pytanie: 'Jak zbudowany jest system uprawnień?',
        odpowiedz: [
          'Uprawnienie to pojedyncza czynność zapisana jako moduł i akcja, na przykład leave.approve albo org.import. Pełną listę widać na ekranie Macierz uprawnień.',
          'Uprawnień nie nadaje się osobom, tylko rolom. Osoba dostaje rolę, a wraz z nią komplet uprawnień.',
          'Niezależnie od uprawnień działa zakres danych, który decyduje, czyje rekordy widzisz: całej firmy, swojej jednostki albo wyłącznie własne.',
        ],
        wymaga: ['identity.manage', 'identity.view'],
      },
      {
        id: 'role-wbudowane',
        pytanie: 'Jakie role są dostępne standardowo?',
        odpowiedz: [
          'Super Admin ma komplet 108 uprawnień i jest przeznaczony dla operatora platformy.',
          'Admin ma 107 uprawnień, czyli wszystko oprócz zarządzania firmami na platformie.',
          'Dział kadr ma około 41 uprawnień skupionych wokół pracowników, czasu pracy i urlopów.',
          'Kierownik ma około 32 uprawnienia, w tym akceptację wniosków i podgląd zespołu.',
          'Pracownik ma 16 uprawnień, czyli własny czas pracy, własne urlopy, zadania i dokumenty.',
        ],
        uwaga:
          'Liczby dotyczą standardowej konfiguracji. Role można modyfikować, więc w konkretnej firmie mogą się różnić.',
        sciezka: '/admin/roles',
        etykietaSciezki: 'Otwórz Role',
        wymaga: ['identity.manage'],
      },
      {
        id: 'role-z-platformy',
        pytanie: 'Dlaczego nie mogę odebrać komuś roli?',
        odpowiedz: [
          'Role wynikające z WB Platform są zarządzane po stronie platformy. Właściciel organizacji dostaje Super Admina, administrator organizacji Admina, a pozostali Pracownika.',
          'Takiej roli nie da się odebrać w WorkBase, bo wróciłaby przy kolejnym logowaniu. Zmienia się ją w WB Platform.',
          'Role nadane ręcznie w WorkBase można odebrać normalnie.',
        ],
        wymaga: ['identity.manage'],
      },
      {
        id: 'zmiana-uprawnien-nie-dziala',
        pytanie: 'Zmieniłem uprawnienia, a użytkownik nadal dostaje odmowę.',
        odpowiedz: [
          'Uprawnienia są zapamiętywane w pamięci podręcznej serwera na kilka minut. Zaraz po zmianie użytkownik może jeszcze przez chwilę dostawać odmowę.',
          'Jeśli po kilku minutach problem nie znika, zgłoś to administratorowi systemu, ponieważ może wymagać restartu usługi.',
        ],
        wymaga: ['identity.manage'],
      },
      {
        id: 'moduly',
        pytanie: 'Jak włączyć lub wyłączyć moduł?',
        odpowiedz: [
          'Służą do tego Flagi funkcjonalności. Wyłączenie modułu ukrywa go wszystkim w firmie, niezależnie od posiadanych uprawnień.',
          'Zestaw dostępnych modułów wynika z planu wykupionego w WB Platform i jest z nią synchronizowany.',
        ],
        sciezka: '/admin/feature-flags',
        etykietaSciezki: 'Otwórz Flagi funkcjonalności',
        wymaga: ['identity.manage-feature-flags'],
      },
      {
        id: 'konfiguracja-urlopow',
        pytanie: 'Gdzie ustawia się rodzaje urlopów i limity?',
        odpowiedz: [
          'Typy urlopów określają nazwę, kolor, płatność, wymóg akceptacji i liczbę dni w roku.',
          'Polityki urlopowe opisują zasady naliczania i rozliczania dni.',
          'Zmiana liczby dni w roku dotyczy naliczeń przyszłych i nie przelicza wstecz sald już przyznanych.',
        ],
        sciezka: '/admin/leave-types',
        etykietaSciezki: 'Otwórz Typy urlopów',
        wymaga: ['leave.manage'],
      },
      {
        id: 'konfiguracja-przerw',
        pytanie: 'Gdzie ustawia się limity przerw?',
        odpowiedz: [
          'W politykach przerw określa się rodzaj przerwy, maksymalną liczbę przerw dziennie, maksymalną długość jednej przerwy i łączny czas w ciągu dnia.',
          'To te ustawienia decydują o komunikacie o wyczerpaniu limitu, który widzi pracownik.',
        ],
        sciezka: '/admin/break-policies',
        etykietaSciezki: 'Otwórz Polityki przerw',
        wymaga: ['config.manage'],
      },
      {
        id: 'slowniki',
        pytanie: 'Jakie jeszcze słowniki mogę skonfigurować?',
        odpowiedz: [
          'Statusy zadań określają dostępne etapy pracy, w tym status domyślny i statusy końcowe.',
          'Stanowiska i typy jednostek porządkują strukturę organizacyjną.',
          'Nazewnictwo pozwala dopasować etykiety w interfejsie do słownictwa używanego w firmie, a Branding logo i kolory.',
        ],
        wymaga: ['config.manage', 'org.manage', 'tasks.manage'],
      },
      {
        id: 'dni-wolne',
        pytanie: 'Jak ustawić dni wolne i święta?',
        odpowiedz: [
          'Kalendarz dni wolnych należy do firmy — system nie zna z góry żadnych dat i sam niczego nie wpisuje.',
          'Dzień wolny robi dwie rzeczy: obniża normę czasu pracy w rozliczeniu i pozwala naliczyć dodatek świąteczny za pracę w tym dniu.',
          'Przycisk „Wstaw typowe dni wolne w Polsce” dopisuje gotowy zestaw na wybrany rok. Wpisy, które już masz, zostają nietknięte, więc można go użyć ponownie po dodaniu własnych dni.',
        ],
        kroki: [
          'Wybierz rok.',
          'Wstaw gotowy zestaw albo dodaj dni pojedynczo.',
          'Dni ustalone przez firmę — na przykład wolne za święto wypadające w sobotę — oznacz jako firmowe.',
        ],
        uwaga:
          'Odznacz „Obniża normę”, jeśli chcesz tylko oznaczyć dzień w kalendarzu, nie zmieniając normy czasu pracy.',
        sciezka: '/admin/dni-wolne',
        etykietaSciezki: 'Otwórz Dni wolne',
        wymaga: ['config.manage'],
      },
      {
        id: 'typy-wnioskow',
        pytanie: 'Jak dodać nowy rodzaj wniosku?',
        odpowiedz: [
          'Rodzaj wniosku to formularz plus informacja, czy wniosek wymaga akceptacji przełożonego.',
          'Pola dodajesz sam: tekst, liczba, data, lista wyboru albo tak/nie. Każde pole ma kod (używany w danych) i etykietę (widoczną dla pracownika).',
          'Obieg akceptacji jest wspólny dla wszystkich rodzajów wniosków, więc dodanie nowego nie wymaga konfigurowania czegokolwiek poza tym ekranem.',
        ],
        uwaga:
          'Kodu nie da się zmienić po utworzeniu — jest zapisany przy złożonych wnioskach. Rodzaj, którego nie chcesz już udostępniać, odznacz jako niedostępny zamiast go usuwać: złożone wnioski zachowają nazwę.',
        sciezka: '/admin/typy-wnioskow',
        etykietaSciezki: 'Otwórz Rodzaje wniosków',
        wymaga: ['wnioski.manage'],
      },
      {
        id: 'kreator-obiegow',
        pytanie: 'Gdzie konfiguruje się obiegi akceptacji?',
        odpowiedz: [
          'Kreator obiegów znajdziesz w Ustawieniach. Opisuje, przez jakie kroki przechodzi wniosek i kto go zatwierdza.',
          'Nowa firma dostaje gotowy obieg akceptacji wniosku urlopowego i akceptacji zadania — można ich używać bez żadnej konfiguracji.',
        ],
        uwaga:
          'Akceptanta wniosku wyznacza przełożony wskazany w strukturze, a nie rola. Pracownik bez przełożonego nie ma komu złożyć wniosku do akceptacji.',
        sciezka: '/workflow/builder',
        etykietaSciezki: 'Otwórz Kreator obiegów',
        wymaga: ['workflow.manage'],
      },
      {
        id: 'powiadomienia-szablony',
        pytanie: 'Czy mogę zmienić treść powiadomień?',
        odpowiedz: [
          'Tak, w szablonach powiadomień. Szablon zawiera treść oraz zmienne podstawiane automatycznie, na przykład imię pracownika czy zakres dat.',
          'Reguły eskalacji określają, co się dzieje, gdy decyzja nie zapada w wyznaczonym czasie.',
        ],
        sciezka: '/admin/notification-templates',
        etykietaSciezki: 'Otwórz Szablony powiadomień',
        wymaga: ['config.manage'],
      },
    ],
  },

  {
    id: 'problemy',
    tytul: 'Typowe problemy',
    opis: 'Najczęstsze sytuacje i to, co zrobić w pierwszej kolejności.',
    ikona: LifeBuoy,
    wpisy: [
      {
        id: 'brak-przycisku-rcp',
        pytanie: 'Nie widzę przycisku rozpoczęcia pracy.',
        odpowiedz: [
          'Przycisk pojawia się tylko wtedy, gdy Twoje konto jest powiązane z kartą pracownika.',
          'Jeśli konto powstało inaczej niż przez wejście z WB Platform, powiązania może brakować. Zgłoś to działowi kadr, podając swój adres e-mail.',
        ],
      },
      {
        id: 'brak-dostepu',
        pytanie: 'Widzę komunikat o braku dostępu do widoku.',
        odpowiedz: [
          'Komunikat podaje nazwę brakującego uprawnienia. Przekaż ją administratorowi razem z informacją, co próbowałeś zrobić.',
          'Sam adres ekranu nie wystarczy do wejścia, więc kopiowanie odsyłacza od kolegi nic nie da.',
        ],
      },
      {
        id: 'wylogowanie',
        pytanie: 'Zostałem wylogowany, choć nic nie robiłem.',
        odpowiedz: [
          'Wylogowanie z WB Platform kończy sesję również w WorkBase i pozostałych aplikacjach firmy. To zamierzone działanie.',
          'Sesja kończy się także po dłuższym czasie bezczynności.',
        ],
      },
      {
        id: 'stara-wersja',
        pytanie: 'Aplikacja zachowuje się dziwnie po aktualizacji.',
        odpowiedz: [
          'Przeglądarka mogła zapamiętać starą wersję. Odśwież stronę z pominięciem pamięci podręcznej, skrótem Ctrl i Shift i R.',
        ],
      },
      {
        id: 'kogo-pytac',
        pytanie: 'Do kogo zgłosić problem?',
        odpowiedz: [
          'Sprawy kadrowe, czyli dane pracownika, przełożony, limity urlopowe i korekty godzin, prowadzi dział kadr.',
          'Sprawy dostępowe, czyli role, uprawnienia i moduły, prowadzi administrator systemu w Twojej firmie.',
          'Przy zgłoszeniu podaj datę, godzinę, nazwę ekranu i treść komunikatu. To zwykle wystarcza, żeby ustalić przyczynę bez dopytywania.',
        ],
      },
    ],
  },
] as const;
