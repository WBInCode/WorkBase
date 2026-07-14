#!/bin/bash
KC="https://auth.wb-partners.pl"
ADMIN_PASS=$(grep KC_ADMIN_PASS /opt/wb/workbase/.secrets | cut -d= -f2)

TOKEN=$(curl -sk -X POST "$KC/realms/master/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=admin-cli -d username=admin -d password="$ADMIN_PASS" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')

kget() { curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase$1"; }

echo "=== KONTA (username | email | enabled | id) ==="
kget "/users?max=50" | python3 -c '
import sys,json
for u in json.load(sys.stdin):
    print(u.get("username"),"|",u.get("email"),"|","en="+str(u.get("enabled")),"|",u.get("id"))
'
echo ""
echo "=== CREDENTIALS (czy maja haslo) ==="
for id in $(kget "/users?max=50" | python3 -c 'import sys,json;[print(u["id"]) for u in json.load(sys.stdin)]'); do
  UN=$(kget "/users/$id" | python3 -c 'import sys,json;print(json.load(sys.stdin).get("username"))')
  CREDS=$(kget "/users/$id/credentials" | python3 -c 'import sys,json;print([c.get("type") for c in json.load(sys.stdin)])')
  echo "$UN -> $CREDS"
done
echo ""
echo "=== SESJE (workbase-web) ==="
CID=$(kget "/clients?clientId=workbase-web" | python3 -c 'import sys,json;print(json.load(sys.stdin)[0]["id"])')
kget "/clients/$CID/user-sessions?max=20" | python3 -c '
import sys,json
s=json.load(sys.stdin)
print("liczba:",len(s))
for x in s: print(x.get("username"),"| ip:",x.get("ipAddress"))
'
