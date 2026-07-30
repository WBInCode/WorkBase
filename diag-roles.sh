#!/usr/bin/env bash
set -e
cd /opt/wb/workbase
DBC=wb-postgres
# Wykryj usera/db z connection stringa workbase-api (ConnectionStrings__Default lub podobne)
CS=$(sudo docker exec workbase-api printenv | grep -iE 'ConnectionStrings|POSTGRES|_DB|DATABASE' | head -20)
echo "=== env DB workbase-api ==="
# Haslo zamaskowane: wynik diagnostyki laduje w terminalu, logach i wklejkach.
echo "$CS" | sed -E 's/(assword=)[^;"]*/\1***/gI; s#(://[^:/@]+:)[^@]*@#\1***@#g'
DBUSER=$(echo "$CS" | grep -oiE 'Username=[^;"]+' | head -1 | cut -d= -f2)
DBNAME=$(echo "$CS" | grep -oiE 'Database=[^;"]+' | head -1 | cut -d= -f2)
DBUSER=${DBUSER:-workbase}
DBNAME=${DBNAME:-workbase}
echo "DB container: $DBC user=$DBUSER db=$DBNAME"
PSQL(){ sudo docker exec -i "$DBC" psql -U "$DBUSER" -d "$DBNAME" -tA -c "$1"; }

echo "=== tabele iam_* ==="
PSQL "select tablename from pg_tables where tablename like 'iam_%' order by 1;"
echo "=== kolumny iam_roles ==="
PSQL "select column_name from information_schema.columns where table_name='iam_roles' order by ordinal_position;"
echo "=== kolumny iam_user_roles ==="
PSQL "select column_name from information_schema.columns where table_name='iam_user_roles' order by ordinal_position;"
echo "=== kolumny iam_users ==="
PSQL "select column_name from information_schema.columns where table_name='iam_users' order by ordinal_position;"

echo "=== iam_users (email ~ kacper) ==="
PSQL "select id, email, keycloak_id, tenant_id from iam_users where email ilike '%kacper%';"

echo "=== przypisane role kacpera (iam_user_roles -> iam_roles) ==="
PSQL "select u.email, r.name, r.type, r.tenant_id from iam_user_roles ur join iam_users u on u.id=ur.user_id join iam_roles r on r.id=ur.role_id where u.email ilike '%kacper%';"

echo "=== wszystkie role w tenant 000...001 (nazwa/type) ==="
PSQL "select name, type, level from iam_roles where tenant_id='00000000-0000-0000-0000-000000000001' order by level;"

echo "=== liczba permisji przez role kacpera ==="
PSQL "select count(distinct rp.permission_id) from iam_user_roles ur join iam_role_permissions rp on rp.role_id=ur.role_id join iam_users u on u.id=ur.user_id where u.email ilike '%kacper%';"
