#!/usr/bin/env bash
# Weryfikacja sciezki wgrywania plikow po wdrozeniu.
set -uo pipefail

echo "== 1. limit rozmiaru w nginx =="
docker exec workbase-web sh -c 'grep -rn client_max_body_size /etc/nginx/ || echo "  BRAK"'

echo
echo "== 2. plik 3 MB bez tokenu: 401 = przechodzi przez nginx, 413 = nadal blokowany =="
head -c 3000000 /dev/urandom > /tmp/duzy.bin
curl -s -o /dev/null -w '  /api/documents 3MB -> %{http_code}\n' \
  --resolve workbase.wb-partners.pl:443:127.0.0.1 \
  -X POST https://workbase.wb-partners.pl/api/documents -F "file=@/tmp/duzy.bin" --max-time 60
rm -f /tmp/duzy.bin

echo
echo "== 3. czy API widzi skaner =="
docker exec workbase-api printenv ClamAv__Enabled ClamAv__Host ClamAv__Port 2>/dev/null || echo "  brak zmiennych"

echo
echo "== 4. clamd odpowiada z sieci wb-net =="
docker run --rm --network wb-net busybox sh -c 'printf "zPING\0" | nc -w 5 wb-chat-clamav 3310' | tr -d '\0'
echo

echo "== 5. clamd wykrywa wzorzec testowy EICAR =="
# Ciag budowany z czesci, zeby ten skrypt sam nie wygladal na zawirusowany plik.
CZ1='X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-'
CZ2='ANTIVIRUS-TEST-FILE!$H+H*'
printf '%s%s' "$CZ1" "$CZ2" > /tmp/eicar.txt
docker run --rm --network wb-net -v /tmp/eicar.txt:/probka:ro busybox sh -c '
  ROZMIAR=$(wc -c < /probka)
  { printf "zINSTREAM\0"
    printf "$(printf "\\\\%03o\\\\%03o\\\\%03o\\\\%03o" $((ROZMIAR>>24&255)) $((ROZMIAR>>16&255)) $((ROZMIAR>>8&255)) $((ROZMIAR&255)))"
    cat /probka
    printf "\\000\\000\\000\\000"
  } | nc -w 10 wb-chat-clamav 3310' | tr -d '\0'
echo
rm -f /tmp/eicar.txt

echo
echo "== 6. blad startu API? =="
docker logs workbase-api --since 5m 2>&1 | grep -icE "FTL|Unhandled|ClamAv" || echo "  0"
