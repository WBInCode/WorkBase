#!/usr/bin/env bash
# Wdrozenie WorkBase na produkcje z automatycznym wycofaniem.
# Uzycie: bash deploy-prod.sh <skrot-commita> [--tylko-front]
#
# Zrodla dostarczamy tak:
#   git -c core.autocrlf=false -c core.eol=lf archive --format=tar.gz -o /tmp/workbase-src.tar.gz HEAD
#   scp /tmp/workbase-src.tar.gz debian@SERWER:/tmp/
set -euo pipefail

NEW_COMMIT=${1:?podaj skrot commita}
TYLKO_FRONT=0
[ "${2:-}" = "--tylko-front" ] && TYLKO_FRONT=1

BASE=/opt/wb/workbase
TS=$(date +%Y%m%d-%H%M%S)
BACKUP=/opt/wb/backups/pre-workbase-$TS
ZDROWIE=http://workbase-api:5000/health

log() { echo "[$(date +%H:%M:%S)] $*"; }

log "0/6 sprawdzam kontrole zdrowia PRZED zmiana"
if ! docker exec workbase-web wget -qO- --timeout=5 "$ZDROWIE" >/dev/null 2>&1; then
  echo "!! $ZDROWIE nie odpowiada juz teraz — przerywam, zeby nie mylic wlasnej awarii z cudza"
  exit 1
fi

log "1/6 kopia bazy, zrodel i obrazow"
sudo install -d -o "$(id -un)" -g "$(id -gn)" "$BACKUP"
docker exec wb-postgres pg_dump -U wbadmin -Fc -d workbase > "$BACKUP/workbase.dump"
cp -a "$BASE/src" "$BACKUP/src"
cp "$BASE/COMMIT" "$BACKUP/COMMIT" 2>/dev/null || true
# Tagi zgodne z konwencja juz uzywana w tym katalogu.
docker tag wb-workbase-api:local "wb-workbase-api:rollback-$TS"
docker tag wb-workbase-web:local "wb-workbase-web:rollback-$TS"
ls -lh "$BACKUP/workbase.dump" | awk '{print "    zrzut bazy: " $5}'

log "2/6 nowe zrodla"
rm -rf /tmp/workbase-new && mkdir -p /tmp/workbase-new
tar xzf /tmp/workbase-src.tar.gz -C /tmp/workbase-new
# Bramka: bez tych sladow paczka jest nie ta, ktora chcemy wdrozyc.
grep -q 'editingCellKey' /tmp/workbase-new/frontend/src/pages/time/TeamAttendancePage.tsx
grep -q "display: 'block'" /tmp/workbase-new/frontend/src/components/shared/TimeInput.tsx
rm -rf "$BASE/src" && mv /tmp/workbase-new "$BASE/src"

restore() {
  log "!! wycofywanie"
  rm -rf "$BASE/src"
  cp -a "$BACKUP/src" "$BASE/src"
  docker tag "wb-workbase-api:rollback-$TS" wb-workbase-api:local
  docker tag "wb-workbase-web:rollback-$TS" wb-workbase-web:local
  cd "$BASE" && docker compose up -d workbase-api workbase-web || true
  log "!! przywrocono poprzednia wersje (kopia: $BACKUP)"
  exit 1
}

cd "$BASE"
if [ "$TYLKO_FRONT" = 1 ]; then
  log "3/6 budowanie frontendu (zmiana nie dotyka API)"
  docker compose build workbase-web || { log "!! build nieudany"; restore; }
  log "4/6 restart frontendu"
  docker compose up -d workbase-web
else
  log "3/6 budowanie API i frontendu"
  docker compose build workbase-api workbase-web || { log "!! build nieudany"; restore; }
  log "4/6 restart"
  docker compose up -d workbase-api workbase-web
fi

log "5/6 czekam na gotowosc"
ok=0
for _ in $(seq 1 48); do
  if docker exec workbase-web wget -qO- --timeout=5 "$ZDROWIE" 2>/dev/null | grep -q 'Healthy'; then ok=1; break; fi
  sleep 5
done
if [ "$ok" != 1 ]; then
  log "!! usluga nie wstala"
  docker logs --tail 40 workbase-api || true
  docker logs --tail 20 workbase-web || true
  restore
fi

log "6/6 weryfikacja"
log "    /health -> $(docker exec workbase-web wget -qO- "$ZDROWIE" 2>/dev/null | head -c 80)"
log "    HTTPS   -> $(curl -s -o /dev/null -w '%{http_code}' --resolve workbase.wb-partners.pl:443:127.0.0.1 https://workbase.wb-partners.pl/ --max-time 15)"
# Czy poprawka faktycznie jest w zbudowanej paczce.
TRAFIENIA=$(docker exec workbase-web sh -c "grep -rl 'HH:mm' /usr/share/nginx/html/assets/*.js 2>/dev/null | wc -l")
log "    plikow paczki z polem godziny: $TRAFIENIA"

echo "$NEW_COMMIT" | sudo tee "$BASE/COMMIT" > /dev/null
log "GOTOWE. commit $NEW_COMMIT, kopia w $BACKUP"
