#!/usr/bin/env bash
# Czy poprawka jest w zbudowanej paczce produkcyjnej?
# Szukamy tekstow widocznych dla uzytkownika — te przetrwaja minifikacje.
set -uo pipefail

echo "== teksty z nowej obslugi bledow =="
for tekst in "Uzupe" "musi by" "Nie uda" "Zapisuj" "HH:mm"; do
  n=$(docker exec workbase-web sh -c "grep -rl '$tekst' /usr/share/nginx/html/assets/ 2>/dev/null | wc -l")
  if [ "$n" -gt 0 ]; then printf '  OK   %-12s w %s plikach\n' "$tekst" "$n"; else printf '  BRAK %-12s\n' "$tekst"; fi
done

echo
echo "== czy zniknelo natywne pole czasu na tej stronie =="
# type="time" moze wystepowac gdzie indziej, wiec tylko liczymy dla orientacji.
docker exec workbase-web sh -c "grep -ro 'type:\"time\"' /usr/share/nginx/html/assets/ 2>/dev/null | wc -l" \
  | sed 's/^/  wystapien type="time" w calej paczce: /'

echo
echo "== znacznik i kontenery =="
printf '  COMMIT: %s\n' "$(cat /opt/wb/workbase/COMMIT)"
docker ps --filter name=workbase --format '  {{.Names}} {{.Status}}'

echo
echo "== zdrowie =="
docker exec workbase-web wget -qO- http://workbase-api:5000/health 2>/dev/null | head -c 100
echo
printf '  HTTPS -> %s\n' "$(curl -s -o /dev/null -w '%{http_code}' --resolve workbase.wb-partners.pl:443:127.0.0.1 https://workbase.wb-partners.pl/ --max-time 12)"

echo
echo "== bledy w logach od restartu =="
docker logs --since 5m workbase-web 2>&1 | grep -ci 'error' | sed 's/^/  web: /' || true
docker logs --since 5m workbase-api 2>&1 | grep -ci '\[ERR\]\|Exception' | sed 's/^/  api: /' || true
