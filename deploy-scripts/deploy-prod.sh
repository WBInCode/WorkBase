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
# Bramka: paczka musi zawierac slady tego, co wlasnie wdrazamy. Liste aktualizujemy przy
# kazdym wdrozeniu — chodzi o wylapanie sytuacji, w ktorej podlozylo sie starsze zrodlo.
SLADY=(
  "src/WorkBase.Infrastructure/Auth/EmployeeScopeResolver.cs:Select(scope => scope.ScopeLevel)"
  "src/WorkBase.Infrastructure/Auth/EmployeeScopeResolver.cs:DataScopeLevelValue.Department"
  "frontend/src/api/hooks/useTimeTracking.ts:fetchSchedulesPerEmployee"
  "src/WorkBase.Infrastructure/Seeding/IamSeeder.cs:BackfillMissingPermissionsAsync"
  "src/WorkBase.Infrastructure/Seeding/IamSeeder.cs:KierownikScopeFor"
  "src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Application/Commands/ClearSchedulesHandler.cs:ClearSchedulesHandler"
  "frontend/index.html:favicon.svg"
  "src/WorkBase.Infrastructure/Auth/AuthorizationCacheInvalidator.cs:IAuthorizationCacheInvalidator"
  "src/WorkBase.Host/Program.cs:UseForwardedHeaders"
  "src/WorkBase.Host/Endpoints/WorkspaceEndpoints.cs:CanAccessEmployeeAsync"
  "src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Api/Endpoints/TimeEntryEndpoints.cs:CanAccessEmployeeAsync"
  "src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Api/Endpoints/TimeEntryEndpoints.cs:EnsureCanRecordFor"
  "src/WorkBase.Infrastructure/HubPlatform/HubNotificationForwarder.cs:HubNotificationJob"
  "src/Modules/Tasks/WorkBase.Modules.Tasks.Application/EventHandlers/TaskAssignedNotificationHandler.cs:TaskAssignedNotificationHandler"
  "src/Modules/Tasks/WorkBase.Modules.Tasks.Application/Services/TaskStatusMachine.cs:HasAnyAsync"
  "frontend/src/pages/time/TeamAttendancePage.tsx:currentWorkState"
  "frontend/src/pages/time/TeamAttendancePage.tsx:endMin !== null && endMin <= startMin"
  "src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Api/Endpoints/ScheduleEndpoints.cs:CanAccessEmployeeAsync"
  "src/Modules/TimeTracking/WorkBase.Modules.TimeTracking.Api/Endpoints/TimeCorrectionEndpoints.cs:CanAccessEmployeeAsync"
  "src/Modules/Organization/WorkBase.Modules.Organization.Application/Commands/Positions/ReapplyPositionPolicyCommand.cs:ReapplyPositionPolicyHandler"
)
for wpis in "${SLADY[@]}"; do
  plik=${wpis%%:*}
  wzor=${wpis#*:}
  if ! grep -qF "$wzor" "/tmp/workbase-new/$plik" 2>/dev/null; then
    echo "!! paczka nie zawiera oczekiwanej zmiany: $plik -> $wzor"
    exit 1
  fi
done
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
for i in $(seq 1 48); do
  if docker exec workbase-web wget -qO- --timeout=5 "$ZDROWIE" 2>/dev/null | grep -q 'Healthy'; then ok=1; break; fi
  # Kontener w petli restartow juz nie wstanie, a czekanie pelnych czterech minut tylko
  # przedluza czas, w ktorym uzytkownicy dostaja 502. Wycofujemy sie od razu po wykryciu.
  if [ "$i" -ge 3 ] && [ "$(docker inspect workbase-api --format '{{.State.Restarting}}' 2>/dev/null)" = "true" ]; then
    log "!! API jest w petli restartow — nie czekam dalej"
    docker logs --tail 40 workbase-api 2>&1 | grep -E 'FTL|Exception|Unhandled' | head -8
    restore
  fi
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
# Traefik potrzebuje chwili, zeby zauwazyc odtworzony kontener. Bez ponowienia ten pomiar
# pokazywal 404 przy wdrozeniu samego frontu, mimo ze strona za chwile dzialala.
kod_https=""
for _ in $(seq 1 10); do
  kod_https=$(curl -s -o /dev/null -w '%{http_code}' --resolve workbase.wb-partners.pl:443:127.0.0.1 https://workbase.wb-partners.pl/ --max-time 15)
  [ "$kod_https" = "200" ] && break
  sleep 3
done
log "    HTTPS   -> $kod_https"
# Czy uzupelnianie slownika uprawnien wykonalo sie przy starcie.
log "    uprawnien w slowniku: $(docker exec wb-postgres psql -U wbadmin -d workbase -tAc 'select count(*) from iam_permissions' 2>/dev/null | tr -d ' ')"
# Blad tlumaczenia na SQL w module urlopow nie moze juz wystepowac.
log "    bledow 22P02 od startu: $(docker logs workbase-api --since 5m 2>&1 | grep -c '22P02')"

echo "$NEW_COMMIT" | sudo tee "$BASE/COMMIT" > /dev/null
log "GOTOWE. commit $NEW_COMMIT, kopia w $BACKUP"
