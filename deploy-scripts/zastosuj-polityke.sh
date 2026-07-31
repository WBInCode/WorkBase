#!/usr/bin/env bash
# Kopia tabel dotknietych przez polityke stanowisk + uruchomienie + weryfikacja.
set -euo pipefail
TS=$(date +%Y%m%d-%H%M%S)
KAT=/opt/wb/backups/role-przed-polityka-$TS
FIRMA=00000000-0000-0000-0000-000000000001

sudo install -d -o "$(id -un)" -g "$(id -gn)" "$KAT"
docker exec wb-postgres pg_dump -U wbadmin -d workbase \
  -t iam_user_roles -t org_supervisor_relations --data-only > "$KAT/dane.sql"
echo "  kopia: $KAT ($(du -h "$KAT/dane.sql" | cut -f1))"

echo
echo "== stan PRZED =="
docker exec wb-postgres psql -U wbadmin -d workbase -tAc \
  "select '  rol przypisanych: '||count(*) from iam_user_roles where tenant_id='$FIRMA'"
docker exec wb-postgres psql -U wbadmin -d workbase -tAc \
  "select '  relacji przelozonych: '||count(*) from org_supervisor_relations"

echo
echo "== uruchamiam polityke =="
cd /opt/wb/workbase
docker compose run --rm --no-deps workbase-api --reapply-position-policy "$FIRMA" 2>&1 \
  | grep -Ev '^\[|Now listening|Application started|Hosting environment|Content root' | tail -6

echo
echo "== stan PO =="
docker exec wb-postgres psql -U wbadmin -d workbase -tAc \
  "select '  rol przypisanych: '||count(*) from iam_user_roles where tenant_id='$FIRMA'"
docker exec wb-postgres psql -U wbadmin -d workbase -tAc \
  "select '  relacji przelozonych: '||count(*) from org_supervisor_relations"

echo
echo "== kto ma teraz jaka role =="
docker exec wb-postgres psql -U wbadmin -d workbase -tAc \
  "select rpad(coalesce(e.first_name||' '||e.last_name, u.email),32)||' -> '||string_agg(r.name, ', ' order by r.name)
   from iam_user_roles ur
   join iam_roles r on r.id=ur.role_id
   join iam_users u on u.id=ur.user_id
   left join org_employees e on e.user_id=u.id
   where ur.tenant_id='$FIRMA'
   group by u.id, e.first_name, e.last_name, u.email order by 1" | sed 's/^/  /'

echo
echo "== kto ma teraz przelozonego =="
docker exec wb-postgres psql -U wbadmin -d workbase -tAc \
  "select rpad(coalesce(pod.first_name||' '||pod.last_name,'?'),28)||' <- '||coalesce(kier.first_name||' '||kier.last_name,'?')
   from org_supervisor_relations sr
   left join org_employees pod on pod.id=sr.subordinate_employee_id
   left join org_employees kier on kier.id=sr.supervisor_employee_id
   where sr.end_date is null order by 1" | sed 's/^/  /'
