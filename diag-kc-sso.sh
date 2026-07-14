#!/bin/bash
# Diagnoza SSO: konta i sesje w realm workbase (Keycloak Admin REST API).
KC="http://localhost:8080"
ADMIN_PASS=$(grep KC_ADMIN_PASS /opt/wb/workbase/.secrets | cut -d= -f2)

# Token admina (realm master)
TOKEN=$(sudo docker exec workbase-keycloak sh -c "curl -s -X POST $KC/realms/master/protocol/openid-connect/token \
  -d grant_type=password -d client_id=admin-cli -d username=admin -d password='$ADMIN_PASS'" | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')

kget() { sudo docker exec workbase-keycloak sh -c "curl -s -H 'Authorization: Bearer $TOKEN' '$KC/admin/realms/workbase$1'"; }

echo "=== KONTA w realm workbase (username | email | enabled) ==="
kget "/users?max=50" | python3 -c '
import sys,json
for u in json.load(sys.stdin):
    print(u.get("username"),"|",u.get("email"),"|","enabled="+str(u.get("enabled")),"| id="+u.get("id"))
'

echo ""
echo "=== CREDENTIALS per user (czy maja haslo) ==="
for id in $(kget "/users?max=50" | python3 -c 'import sys,json;[print(u["id"]) for u in json.load(sys.stdin)]'); do
  UN=$(kget "/users/$id" | python3 -c 'import sys,json;u=json.load(sys.stdin);print(u.get("username"))')
  CREDS=$(kget "/users/$id/credentials" | python3 -c 'import sys,json;print([c.get("type") for c in json.load(sys.stdin)])')
  echo "$UN -> $CREDS"
done

echo ""
echo "=== AKTYWNE SESJE (client workbase-web) ==="
CID=$(kget "/clients?clientId=workbase-web" | python3 -c 'import sys,json;print(json.load(sys.stdin)[0]["id"])')
kget "/clients/$CID/user-sessions?max=20" | python3 -c '
import sys,json
s=json.load(sys.stdin)
print("liczba sesji:",len(s))
for x in s:
    print(x.get("username"),"| start:",x.get("start"),"| ip:",x.get("ipAddress"))
'
