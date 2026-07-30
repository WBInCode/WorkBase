#!/usr/bin/env bash
set -e
cd /opt/wb/workbase
source .secrets 2>/dev/null || true
KC=https://auth.wb-partners.pl
TOK=$(curl -sk "$KC/realms/master/protocol/openid-connect/token" \
  -d client_id=admin-cli -d username=admin -d "password=$KC_ADMIN_PASS" -d grant_type=password \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')

CID=$(curl -sk -H "Authorization: Bearer $TOK" "$KC/admin/realms/workbase/clients?clientId=workbase-web" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)[0]["id"])')

echo "=== default client scopes workbase-web ==="
curl -sk -H "Authorization: Bearer $TOK" "$KC/admin/realms/workbase/clients/$CID/default-client-scopes" \
  | python3 -c 'import sys,json;[print(" -",s["name"]) for s in json.load(sys.stdin)]'

echo "=== users (email~kacper) tenant_id ==="
curl -sk -H "Authorization: Bearer $TOK" "$KC/admin/realms/workbase/users?max=50" \
  | python3 -c 'import sys,json;u=json.load(sys.stdin);[print(" -",x.get("username"),"|",x.get("email"),"| tenant_id=",x.get("attributes",{}).get("tenant_id")) for x in u]'

echo "=== czy workbase-scope ma mapper tenant_id ==="
SID=$(curl -sk -H "Authorization: Bearer $TOK" "$KC/admin/realms/workbase/client-scopes" \
  | python3 -c 'import sys,json;print(next((s["id"] for s in json.load(sys.stdin) if s["name"]=="workbase-scope"),""))')
if [ -n "$SID" ]; then
  curl -sk -H "Authorization: Bearer $TOK" "$KC/admin/realms/workbase/client-scopes/$SID/protocol-mappers/models" \
    | python3 -c 'import sys,json;[print(" -",m["name"],"->",m.get("config",{}).get("claim.name"),"| userattr=",m.get("config",{}).get("user.attribute")) for m in json.load(sys.stdin)]'
else
  echo "  BRAK client-scope workbase-scope"
fi
