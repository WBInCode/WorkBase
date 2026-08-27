import {
  AlarmClockCheck,
  Bell,
  Briefcase,
  Building2,
  CalendarClock,
  CalendarOff,
  CircleDot,
  Coffee,
  FileArchive,
  FileCog,
  Flag,
  Gauge,
  GitBranch,
  Grid3X3,
  Layers,
  ListTodo,
  Palette,
  Palmtree,
  Shield,
  ToggleLeft,
  Type,
  type LucideIcon,
} from 'lucide-react';

/**
 * Ekrany administracyjne pogrupowane wedlug obszaru, ktorego dotycza.
 *
 * Do wersji z sierpnia 2026 „Administracja" byla jedna plaska lista 22 pozycji w kolejnosci
 * dodawania do kodu — „Typy urlopow" sasiadowaly z „Brandingiem", a „Polityki urlopowe" byly
 * osiem wierszy dalej. Administrator szukajacy „gdzie ustawic dni wolne" musial przeczytac cala
 * liste. Kazdy ekran jest do czegos potrzebny; problem byl w braku porzadku, nie w liczbie.
 *
 * Jedno zrodlo prawdy: z tego pliku korzysta pasek boczny (grupy zwijane) ORAZ ekran przegladu
 * ustawien (kafelki z opisami). Test `ustawienia.test.ts` pilnuje, ze kazda trasa `/admin/*`
 * z mapy dostepu ma tu swoje miejsce — nowy ekran nie moze po cichu wypasc poza grupy.
 *
 * Etykiety pozycji ida przez i18n (`nav.*`), bo firma moze je nadpisac w Nazewnictwie.
 * Tytuly grup i opisy sa tu wprost: to nasz jezyk o produkcie, nie slownik firmy.
 */
export interface PozycjaUstawien {
  path: string;
  labelKey: string;
  icon: LucideIcon;
  /** Jedno zdanie: CO tu ustawiam i co z tego wynika. */
  opis: string;
  operatorOnly?: boolean;
}

export interface GrupaUstawien {
  id: string;
  tytul: string;
  /** Jedno zdanie pod tytulem grupy na ekranie przegladu. */
  opis: string;
  icon: LucideIcon;
  pozycje: PozycjaUstawien[];
}

/**
 * Punkt wejscia, celowo poza grupami: nie jest slownikiem, tylko lista „co jeszcze nie zadziala"
 * — a wiec pierwsza rzecza, ktora administrator powinien otworzyc.
 */
export const GOTOWOSC: PozycjaUstawien = {
  path: '/admin/gotowosc',
  labelKey: 'nav.gotowosc',
  icon: Gauge,
  opis: 'Lista braków wyliczana z danych firmy: co jeszcze nie zadziała i gdzie to ustawić.',
};

export const GRUPY_USTAWIEN: readonly GrupaUstawien[] = [
  {
    id: 'firma',
    tytul: 'Firma',
    opis: 'Jak system wygląda i nazywa rzeczy u Was.',
    icon: Building2,
    pozycje: [
      {
        path: '/admin/branding',
        labelKey: 'nav.branding',
        icon: Palette,
        opis: 'Logo, kolory i nazwa aplikacji widoczne dla wszystkich w firmie.',
      },
      {
        path: '/admin/terminology',
        labelKey: 'nav.terminology',
        icon: Type,
        opis: 'Własne nazwy w interfejsie, np. „Oddział" zamiast „Jednostka".',
      },
      {
        path: '/admin/feature-flags',
        labelKey: 'nav.featureFlags',
        icon: ToggleLeft,
        opis: 'Które moduły są włączone. Wyłączony znika z menu wszystkim.',
      },
    ],
  },
  {
    id: 'struktura',
    tytul: 'Struktura i kadry',
    opis: 'Słowniki, z których zbudowana jest karta pracownika.',
    icon: Layers,
    pozycje: [
      {
        path: '/admin/unit-types',
        labelKey: 'nav.unitTypes',
        icon: Layers,
        opis: 'Poziomy struktury: dział, zespół, oddział.',
      },
      {
        path: '/admin/positions',
        labelKey: 'nav.positions',
        icon: Briefcase,
        opis: 'Stanowiska. Kierownicze decydują, kto widzi dane działu.',
      },
      {
        path: '/admin/typy-terminow',
        labelKey: 'nav.typyTerminow',
        icon: Flag,
        opis: 'Badania, BHP, uprawnienia, końce umów — i ile dni wcześniej ostrzegać.',
      },
    ],
  },
  {
    id: 'czas-pracy',
    tytul: 'Czas pracy',
    opis: 'Zasady rejestracji, przerw i dni wolnych.',
    icon: CalendarClock,
    pozycje: [
      {
        path: '/admin/time-tracking-settings',
        labelKey: 'nav.timeTrackingSettings',
        icon: CalendarClock,
        opis: 'Zaokrąglanie, tolerancja spóźnień, wykrywanie anomalii.',
      },
      {
        path: '/admin/break-policies',
        labelKey: 'nav.breakPolicies',
        icon: Coffee,
        opis: 'Ile przerw dziennie, jak długich i czy płatnych.',
      },
      {
        path: '/admin/dni-wolne',
        labelKey: 'nav.dniWolne',
        icon: CalendarOff,
        opis: 'Święta i dni firmowe, w które nie liczymy nieobecności.',
      },
    ],
  },
  {
    id: 'urlopy-wnioski',
    tytul: 'Urlopy i wnioski',
    opis: 'Co można złożyć i według jakich zasad.',
    icon: Palmtree,
    pozycje: [
      {
        path: '/admin/leave-types',
        labelKey: 'nav.leaveTypes',
        icon: Palmtree,
        opis: 'Rodzaje nieobecności: wypoczynkowy, na żądanie, L4, bezpłatny.',
      },
      {
        path: '/admin/leave-policies',
        labelKey: 'nav.leavePolicies',
        icon: Palmtree,
        opis: 'Wymiar roczny, przenoszenie niewykorzystanego, zasady naliczania.',
      },
      {
        path: '/admin/typy-wnioskow',
        labelKey: 'nav.typyWnioskow',
        icon: FileCog,
        opis: 'Własne formularze wniosków firmowych z polami do wypełnienia.',
      },
    ],
  },
  {
    id: 'zadania-dokumenty',
    tytul: 'Zadania i dokumenty',
    opis: 'Statusy, limity i to, co wolno wgrać.',
    icon: ListTodo,
    pozycje: [
      {
        path: '/admin/task-statuses',
        labelKey: 'nav.taskStatuses',
        icon: CircleDot,
        opis: 'Statusy zadań. Jeden domyślny dla nowych, końcowe zamykają sprawę.',
      },
      {
        path: '/admin/task-settings',
        labelKey: 'nav.taskSettings',
        icon: ListTodo,
        opis: 'Domyślne terminy, priorytety i widoczność zadań.',
      },
      {
        path: '/admin/document-settings',
        labelKey: 'nav.documentSettings',
        icon: FileArchive,
        opis: 'Dozwolone typy plików i limity rozmiaru.',
      },
    ],
  },
  {
    id: 'obiegi-powiadomienia',
    tytul: 'Obiegi i powiadomienia',
    opis: 'Kto zatwierdza, co się dzieje przy zwłoce i jak brzmią komunikaty.',
    icon: GitBranch,
    pozycje: [
      {
        path: '/workflow/builder',
        labelKey: 'nav.workflowBuilder',
        icon: GitBranch,
        opis: 'Przez jakie kroki przechodzi wniosek i kto go zatwierdza.',
      },
      {
        path: '/admin/escalation-rules',
        labelKey: 'nav.escalationRules',
        icon: AlarmClockCheck,
        opis: 'Po ilu minutach bez decyzji akceptant dostaje przypomnienie.',
      },
      {
        path: '/admin/notification-templates',
        labelKey: 'nav.notificationTemplates',
        icon: Bell,
        opis: 'Treść powiadomień. To, kto je dostaje, każdy ustawia sobie sam.',
      },
    ],
  },
  {
    id: 'dostep',
    tytul: 'Dostęp',
    opis: 'Kto co może.',
    icon: Shield,
    pozycje: [
      {
        path: '/admin/roles',
        labelKey: 'nav.roles',
        icon: Shield,
        opis: 'Role i ich przypisanie do kont pracowników.',
      },
      {
        path: '/admin/permissions',
        labelKey: 'nav.permissions',
        icon: Grid3X3,
        opis: 'Macierz: które uprawnienie wchodzi w skład której roli.',
      },
    ],
  },
  {
    id: 'platforma',
    tytul: 'Platforma',
    opis: 'Widoczne wyłącznie dla operatora WB Platform.',
    icon: Building2,
    pozycje: [
      {
        path: '/admin/tenants',
        labelKey: 'nav.tenantsOperator',
        icon: Building2,
        opis: 'Wszystkie firmy na tej instancji, ich plany i moduły.',
        operatorOnly: true,
      },
    ],
  },
];

/** Wszystkie pozycje administracyjne w jednej liscie — do tytulu strony i bramek. */
export const WSZYSTKIE_POZYCJE_USTAWIEN: readonly PozycjaUstawien[] = [
  GOTOWOSC,
  ...GRUPY_USTAWIEN.flatMap((g) => g.pozycje),
];
