#!/usr/bin/env bash
set -e
CS=$(sudo docker exec wb-hub-api printenv | grep -iE 'DATABASE_URL|POSTGRES|PG' | head -5)
# Haslo zamaskowane: wynik diagnostyki laduje w terminalu, logach i wklejkach.
echo "=== env DB hub ==="; echo "$CS" | sed -E 's/(assword=)[^;"]*/\1***/gI; s#(://[^:/@]+:)[^@]*@#\1***@#g'
DBC=wb-postgres
PSQL(){ sudo docker exec -i "$DBC" psql -U hub -d hub -tA -c "$1"; }

echo "=== user kacper w hub ==="
PSQL "select id, email, \"displayName\" from users where email ilike '%kacper%';"

echo "=== membership kacpera (rola per org) ==="
PSQL "select u.email, m.role, o.name, o.slug from memberships m join users u on u.id=m.\"userId\" join organizations o on o.id=m.\"orgId\" where u.email ilike '%kacper%';"

echo "=== instancje WorkBase (product key=workbase) i ich org ==="
PSQL "select o.name, o.slug, pi.name, pi.slug from product_instances pi join organizations o on o.id=pi.\"orgId\" join products p on p.id=pi.\"productId\" where p.key='workbase';"

echo "=== czy kacper jest OWNER org z instancja workbase ==="
PSQL "select u.email, m.role, o.name from memberships m join users u on u.id=m.\"userId\" join organizations o on o.id=m.\"orgId\" where u.email ilike '%kacper%' and o.id in (select \"orgId\" from product_instances pi join products p on p.id=pi.\"productId\" where p.key='workbase');"
