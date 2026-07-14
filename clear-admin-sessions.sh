#!/bin/bash
KC="https://auth.wb-partners.pl"
ADMIN_PASS=$(grep KC_ADMIN_PASS /opt/wb/workbase/.secrets | cut -d= -f2)
TOKEN=$(curl -sk -X POST "$KC/realms/master/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=admin-cli -d username=admin -d password="$ADMIN_PASS" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')

# Wyloguj wszystkie sesje admin@workbase.local
AID=$(curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/users?email=admin@workbase.local" | python3 -c 'import sys,json;print(json.load(sys.stdin)[0]["id"])')
curl -sk -X POST -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/users/$AID/logout" -w "logout admin: HTTP %{http_code}\n"

echo "=== sesje po wylogowaniu ==="
CID=$(curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/clients?clientId=workbase-web" | python3 -c 'import sys,json;print(json.load(sys.stdin)[0]["id"])')
curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/clients/$CID/user-sessions?max=20" | python3 -c 'import sys,json;s=json.load(sys.stdin);print("liczba sesji:",len(s));[print(x.get("username")) for x in s]'
