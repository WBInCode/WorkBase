# Utrzymanie produkcji

Co pilnuje działania WorkBase na produkcji, jak to sprawdzić i czego brakuje. Stan na 26 sierpnia 2026.

Serwer: `ssh wbvps`, katalog `/opt/wb/workbase/`. Wdrożenie: [`deploy-scripts/deploy-prod.sh`](../deploy-scripts/deploy-prod.sh).

---

## Kopie zapasowe

**Działają.** `wb-backup.timer` uruchamia `/opt/wb/ops/backup.sh` codziennie o 03:00 UTC. Kopia obejmuje osiem baz (`hub`, `chat`, `dziennik`, `workbase`, `inbox`, `crm`, `rytm`), konfigurację i wolumeny; `workbase.dump` waży ~7 MB. Retencja: 8 dni. Katalog: `/opt/wb/backups/daily/<data>/`.

Dodatkowo `deploy-prod.sh` robi kopię **przed każdym wdrożeniem** do `/opt/wb/backups/pre-workbase-<data>/` — z niej działa automatyczne wycofanie.

### Próbne odtworzenie — wykonane 2026-08-26

Kopia, której nikt nie odtworzył, nie jest kopią. Pierwsze odtworzenie: dump z 26 sierpnia przywrócony do jednorazowego kontenera `postgres:16-alpine`.

```
pg_restore: OK bez ostrzeżeń
kopia:     tenanty=4 pracownicy=51 wpisy_czasu=5173 urlopy=65 uprawnienia=111 tabele=84
produkcja: tenanty=5 pracownicy=51 wpisy_czasu=5175 urlopy=65 uprawnienia=111 tabele=84
```

Różnica tłumaczy się w całości: piąty tenant powstał o 08:46, czyli po kopii z 03:01, a dwa wpisy czasu doszły tego samego dnia. Odtworzona baza miała komplet 84 tabel i 28 migracji.

Powtórzenie: skrypt jest w scratchpadzie sesji; wystarczy `pg_restore` dumpu do świeżego kontenera i porównanie liczb.

### ⚠️ Czego brakuje: kopii poza serwerem

**Kopie leżą wyłącznie na tym samym VPS-ie.** Utrata serwera oznacza utratę wszystkiego — łącznie z kopiami. To jedyna pozycja z etapu odporności, której nie da się zamknąć bez decyzji właściciela, bo wymaga konta w zewnętrznym magazynie (S3, Backblaze B2 albo dowolnym zgodnym z rclone).

Potrzebne: konto magazynu, klucz dostępu i sekret. Wtedy `rclone` plus wpis w istniejącym `backup.sh` albo osobny timer — praca na godziny, nie dni.

---

## Obserwator dostępności

**Dlaczego istnieje.** `restart: unless-stopped` w compose reaguje na **zakończenie procesu**, a nie na aplikację, która stoi i nie odpowiada. `healthcheck` w compose też sam z siebie niczego nie restartuje — ustawia wyłącznie status kontenera. Bez zewnętrznego obserwatora o awarii dowiedziałby się pierwszy klient.

| | |
|---|---|
| Skrypt | [`deploy-scripts/monitor-zdrowia.sh`](../deploy-scripts/monitor-zdrowia.sh), instalowany jako `/opt/wb/ops/monitor-zdrowia.sh` |
| Konfiguracja | `/opt/wb/ops/monitor-zdrowia.conf` — adresy, odbiorcy, progi |
| Harmonogram | `wb-monitor-zdrowia.timer`, co 2 minuty |
| Stan | `/opt/wb/monitor-stan/` — licznik kolejnych awarii per adres |

Sprawdzany jest **adres publiczny**, nie kontener od środka: to ta sama droga, którą przechodzi użytkownik (Traefik, certyfikat, aplikacja). Awaria samego Traefika jest równie dotkliwa, a od środka byłaby niewidoczna.

Alarm idzie na czat przez kanał `system-notices`, którego WorkBase już używa — zero nowej infrastruktury. Domyślnie po **2 nieudanych sprawdzeniach**, czyli około 4 minut od awarii. Powiadomienie o powrocie wysyłane jest tylko wtedy, gdy wcześniej poszedł alarm.

**Automatyczny restart jest domyślnie wyłączony** (`RESTARTUJ=nie`). Alarm dociera w kilka minut, a restart w pętli potrafi ukryć prawdziwą przyczynę. Włączyć świadomie, gdy okaże się potrzebny.

### Sprawdzenie

```bash
systemctl list-timers wb-monitor-zdrowia.timer   # kiedy następny przebieg
journalctl -u wb-monitor-zdrowia.service -n 20   # co wypisał (tylko awarie i powroty)
cat /opt/wb/monitor-stan/*.awarie                # 0 = zdrowo
```

Ręczna próba alarmu, bez ruszania produkcji — kieruje sprawdzenie na nieistniejący adres:

```bash
STAN=/tmp/probny ADRESY="https://test-alarmu.invalid/health" PROG_ALARMU=2 \
  bash /opt/wb/ops/monitor-zdrowia.sh   # uruchomić dwa razy
```

**Zweryfikowane 2026-08-26:** alarm dotarł do czatu o 09:28:30 z właściwą treścią. Przy okazji tej próby wyszedł błąd w samym skrypcie — `curl` przy braku połączenia sam wypisuje `000` i kończy się kodem błędu, więc dopisane `|| echo 000` dawało `000000`, co nie równa się `000` i nie zaczyna się od `5`. **Całkowita awaria była klasyfikowana jako sprawność i alarm nigdy by nie poszedł.** Naprawione. To jest powód, dla którego alarm trzeba przetestować, a nie tylko napisać.

---

## Poczta wychodząca

Konfiguracja żyje w `/opt/wb/workbase/.env` (sekcja `Smtp__*`, wczytywana przez `env_file` w `docker-compose.yml`). `deploy-prod.sh` tego pliku nie dotyka, więc ustawienia przeżywają wdrożenia.

**Dane są te same, co w Hubie** — jedno konto Resend na całą rodzinę, źródło: `/opt/wb/hub/.env.hub` (klucze `SMTP_*`, `MAIL_FROM`). Nadawcą jest `no-reply@wb-platform.pl`, bo to ta domena jest zweryfikowana w Resendzie; nazwa wyświetlana to `WorkBase`.

**Port 587, nie 465 — i to nie jest kosmetyka.** Hub stoi na nodemailerze z `secure: true`, czyli natychmiastowym TLS na 465. `SmtpEmailSender` łączy się przez `SecureSocketOptions.StartTlsWhenAvailable`, które na porcie 465 nie zadziała. Przepisanie ustawień jeden do jednego dałoby konfigurację wyglądającą poprawnie i niedziałającą.

Sprawdzenie po zmianie danych:

```bash
cd /opt/wb/workbase && docker compose up -d workbase-api   # NIE `restart` — nie wczyta env_file
docker exec workbase-api env | grep Smtp__Host
docker logs workbase-api --since 5m 2>&1 | grep -i -e "Email sent" -e "Nie udalo sie wyslac"
```

**Zweryfikowane 2026-08-27:** wiadomość próbna przeszła przez `smtp.resend.com:587` (STARTTLS) tymi samymi danymi co Hub.

Konsumenci poczty: dane startowe kiosku oraz kanał e-mail powiadomień. Ten drugi jest **opt-in** — dopóki nikt nie zaznaczy „mailem" w swoich ustawieniach powiadomień, nic nie wychodzi. Awaria poczty nie zabiera powiadomienia w aplikacji ani nie przerywa zadania cyklicznego.

---

## Czego nadal nie ma

- **Kopii poza serwerem** — patrz wyżej, wymaga konta w zewnętrznym magazynie.
- **Monitoringu poza „żyje / nie żyje"** — brak historii dostępności, czasów odpowiedzi, alertów per usługa. Świadomie: panel typu Uptime Kuma to kolejna rzecz do utrzymania, a przy jednym VPS-ie i kilku produktach alarm na czat pokrywa realne ryzyko.
- **Monitoringu pozostałych produktów** — skrypt jest ogólny, wystarczy dopisać adresy do `ADRESY` w konfiguracji. Dziś pilnuje wyłącznie WorkBase.
