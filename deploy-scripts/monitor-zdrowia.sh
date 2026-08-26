#!/usr/bin/env bash
# Obserwator dostepnosci produkcji. Uruchamiany cyklicznie (systemd timer albo cron).
#
# Dlaczego to istnieje, skoro compose ma restart: unless-stopped:
# ta polityka reaguje na ZAKONCZENIE PROCESU, a nie na aplikacje, ktora stoi i nie odpowiada.
# Healthcheck w compose tez sam z siebie nic nie restartuje — ustawia tylko status kontenera.
# Bez zewnetrznego obserwatora o awarii dowiedzialby sie pierwszy klient.
#
# Sprawdzamy adres PUBLICZNY, a nie kontener od srodka, bo to jest ta sama droga, ktora
# przechodzi uzytkownik: Traefik, certyfikat, aplikacja. Awaria samego Traefika jest rownie
# dotkliwa co awaria aplikacji, a od srodka byla by niewidoczna.
#
# Alarm idzie na czat przez ten sam kanal system-notices, ktorego uzywa juz WorkBase —
# zero nowej infrastruktury do utrzymania.
set -uo pipefail

KONFIG=${KONFIG:-/opt/wb/ops/monitor-zdrowia.conf}
# shellcheck source=/dev/null
[ -f "$KONFIG" ] && . "$KONFIG"

: "${ADRESY:=https://workbase.wb-partners.pl/api/hub/webhooks}"
: "${ODBIORCY:=kacper.franczyk@wb-incode.pl}"
: "${CZAT_URL:=}"
: "${PROG_ALARMU:=2}"        # ile kolejnych nieudanych sprawdzen przed alarmem
: "${RESTARTUJ:=nie}"        # "tak" wlacza restart kontenera po PROG_RESTARTU probach
: "${PROG_RESTARTU:=4}"
: "${KONTENER:=workbase-api}"          # kontener restartowany i zrodlo konfiguracji
: "${KONTENER_WYSYLKI:=workbase-web}"  # jedyny z wget; API nie ma ani wget, ani curl
: "${STAN:=/opt/wb/monitor-stan}"  # licznik kolejnych awarii; /var/lib i /opt/wb/ops naleza do roota
: "${LIMIT_SEKUND:=10}"

mkdir -p "$STAN"

# Token czatu trzymamy poza tym plikiem — czytamy go z konfiguracji dzialajacej aplikacji.
if [ -z "$CZAT_URL" ]; then
  CZAT_URL=$(docker exec "$KONTENER" printenv ChatNotices__EndpointUrl 2>/dev/null || true)
fi

powiadom() {
  local tytul="$1" tresc="$2"
  [ -z "$CZAT_URL" ] && { echo "  (brak adresu czatu — pomijam powiadomienie)"; return; }

  # JSON buduje python, a nie sed: tresc alarmu zawiera adresy i kody, wiec recznie sklejany
  # ciag potrafi sie rozsypac na pierwszym cudzyslowie.
  local paczka
  paczka=$(TYTUL="$tytul" TRESC="$tresc" ODB="$ODBIORCY" python3 -c '
import json, os
print(json.dumps({
    "recipients": [a.strip() for a in os.environ["ODB"].split(",") if a.strip()],
    "title": os.environ["TYTUL"],
    "body": os.environ["TRESC"],
    "url": "https://workbase.wb-partners.pl",
}, ensure_ascii=False))')

  # Wysylamy z kontenera WEB, bo adres czatu jest wewnetrzny (wb-chat-api), a kontener API
  # nie ma ani wget, ani curl — sprawdzone. To ta sama pulapka, ktora wychodzi tu za kazdym razem.
  printf '%s' "$paczka" | docker exec -i "$KONTENER_WYSYLKI" sh -c 'cat > /tmp/alarm.json'
  if docker exec "$KONTENER_WYSYLKI" wget -q -O- --timeout=10        --header='Content-Type: application/json'        --post-file=/tmp/alarm.json "$CZAT_URL" >/dev/null 2>&1; then
    echo "  powiadomienie wyslane"
  else
    echo "  NIE UDALO SIE wyslac powiadomienia"
  fi
}

for adres in $ADRESY; do
  klucz=$(printf '%s' "$adres" | tr -c 'a-zA-Z0-9' '_')
  plik_licznika="$STAN/$klucz.awarie"
  awarie=$(cat "$plik_licznika" 2>/dev/null || echo 0)

  # UWAGA na "|| echo 000": curl przy braku polaczenia SAM wypisuje 000 i konczy sie kodem
  # bledu, wiec dopisanie drugiego dawalo "000000" — a to nie rowna sie "000" i nie zaczyna
  # sie od 5, wiec calkowita awaria byla klasyfikowana jako sprawnosc. Alarm nigdy by nie poszedl.
  kod=$(curl -s -o /dev/null -w '%{http_code}' --max-time "$LIMIT_SEKUND" -X POST "$adres" 2>/dev/null)
  [ -z "$kod" ] && kod=000

  # Kazda odpowiedz HTTP oznacza, ze cala droga dziala. Interesuje nas cisza albo 5xx,
  # a nie konkretny kod — 401 z endpointu wymagajacego podpisu to poprawna odpowiedz.
  if [ "$kod" != "000" ] && [ "${kod:0:1}" != "5" ]; then
    if [ "$awarie" -ge "$PROG_ALARMU" ]; then
      echo "[$(date +%H:%M:%S)] $adres WROCIL (po $awarie nieudanych sprawdzeniach)"
      powiadom "WorkBase znowu odpowiada" "Adres $adres odpowiada ponownie (HTTP $kod). Przerwa objela $awarie kolejnych sprawdzen."
    fi
    echo 0 > "$plik_licznika"
    continue
  fi

  awarie=$((awarie + 1))
  echo "$awarie" > "$plik_licznika"
  echo "[$(date +%H:%M:%S)] $adres NIE ODPOWIADA (HTTP $kod), kolejna nieudana proba: $awarie"

  if [ "$awarie" -eq "$PROG_ALARMU" ]; then
    powiadom "WorkBase nie odpowiada" "Adres $adres nie odpowiada od $awarie kolejnych sprawdzen (ostatni kod: $kod). Sprawdz serwer."
  fi

  if [ "$RESTARTUJ" = "tak" ] && [ "$awarie" -eq "$PROG_RESTARTU" ]; then
    echo "  restartuje kontener $KONTENER"
    docker restart "$KONTENER" >/dev/null 2>&1 \
      && powiadom "Restart WorkBase" "Kontener $KONTENER zostal zrestartowany automatycznie po $awarie nieudanych sprawdzeniach." \
      || echo "  restart NIE POWIODL SIE"
  fi
done
