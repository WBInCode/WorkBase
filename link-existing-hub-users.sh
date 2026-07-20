#!/usr/bin/env bash
# Odtwarza powiązania federacyjne po migracji bazy Keycloak.
# Łączy wyłącznie aktywne konto Huba i dokładnie jedno konto Keycloak o tym samym e-mailu.
set -euo pipefail

KC="${KC_URL:-https://auth.wb-partners.pl}"
REALM="workbase"
SECRETS_FILE="${SECRETS_FILE:-/opt/wb/workbase/.secrets}"
ADMIN_PASS=$(grep '^KC_ADMIN_PASS=' "$SECRETS_FILE" | cut -d= -f2-)
TOKEN=$(curl -sk "$KC/realms/master/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=admin-cli -d username=admin -d "password=$ADMIN_PASS" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')
AUTH="Authorization: Bearer $TOKEN"

LINKED=0
SKIPPED=0
while IFS='|' read -r HUB_USER_ID EMAIL; do
  [ -z "$HUB_USER_ID" ] && continue
  USERS_JSON=$(curl -sk -G -H "$AUTH" --data-urlencode "email=$EMAIL" --data-urlencode "exact=true" \
    "$KC/admin/realms/$REALM/users")
  MATCH_COUNT=$(python3 -c 'import sys,json;print(len(json.load(sys.stdin)))' <<< "$USERS_JSON")
  if [ "$MATCH_COUNT" != "1" ]; then
    echo "skip $EMAIL: Keycloak matches=$MATCH_COUNT"
    SKIPPED=$((SKIPPED+1))
    continue
  fi
  KEYCLOAK_USER_ID=$(python3 -c 'import sys,json;print(json.load(sys.stdin)[0]["id"])' <<< "$USERS_JSON")
  BODY=$(python3 - "$HUB_USER_ID" "$EMAIL" <<'PY'
import json,sys
print(json.dumps({"identityProvider":"wb-hub","userId":sys.argv[1],"userName":sys.argv[2]}))
PY
)
  STATUS=$(curl -sk -o /tmp/federated-link-response.json -w '%{http_code}' -X POST \
    -H "$AUTH" -H "Content-Type: application/json" -d "$BODY" \
    "$KC/admin/realms/$REALM/users/$KEYCLOAK_USER_ID/federated-identity/wb-hub")
  if [ "$STATUS" = "204" ] || [ "$STATUS" = "409" ]; then
    echo "linked $EMAIL (HTTP $STATUS)"
    LINKED=$((LINKED+1))
  else
    echo "failed $EMAIL (HTTP $STATUS)"
    cat /tmp/federated-link-response.json
    exit 1
  fi
done < <(sudo docker exec -i wb-postgres psql -U hub -d hub -tA -F '|' <<'SQL'
select id,email from users where "disabledAt" is null order by email;
SQL
)

echo "DONE linked=$LINKED skipped=$SKIPPED"
rm -f /tmp/federated-link-response.json
