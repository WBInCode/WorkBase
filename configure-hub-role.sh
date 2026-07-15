#!/usr/bin/env bash
# Konfiguruje przepływ claimu hub_role: Hub id_token -> IdP wb-hub (user attribute) ->
# workbase-scope protocol mapper (token claim) -> WorkBase provisioning (owner -> Super Admin).
set -e
KC="https://auth.wb-partners.pl"
ADMIN_PASS=$(grep KC_ADMIN_PASS /opt/wb/workbase/.secrets | cut -d= -f2)

TOKEN=$(curl -sk -X POST "$KC/realms/master/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=admin-cli -d username=admin -d password="$ADMIN_PASS" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')
AUTH="Authorization: Bearer $TOKEN"

# ── 1. IdP wb-hub: mapper hub_role (claim) -> user attribute hub_role (FORCE) ──
# Idempotencja: usuń istniejący mapper o tej nazwie
EXIST=$(curl -sk -H "$AUTH" "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
  | python3 -c 'import sys,json;print(next((m["id"] for m in json.load(sys.stdin) if m["name"]=="hubRole"),""))')
if [ -n "$EXIST" ]; then
  curl -sk -X DELETE -H "$AUTH" "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers/$EXIST" >/dev/null || true
fi
cat > /tmp/hr_idp.json <<JSON
{"name":"hubRole","identityProviderAlias":"wb-hub","identityProviderMapper":"oidc-user-attribute-idp-mapper",
 "config":{"syncMode":"FORCE","claim":"hub_role","user.attribute":"hub_role"}}
JSON
curl -sk -X POST -H "$AUTH" -H "Content-Type: application/json" -d @/tmp/hr_idp.json \
  "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
  -w "IdP mapper hubRole: HTTP %{http_code}\n"

# ── 2. client-scope workbase-scope: protocol mapper hub_role (user attr -> token claim) ──
SID=$(curl -sk -H "$AUTH" "$KC/admin/realms/workbase/client-scopes" \
  | python3 -c 'import sys,json;print(next((s["id"] for s in json.load(sys.stdin) if s["name"]=="workbase-scope"),""))')
if [ -z "$SID" ]; then echo "BRAK client-scope workbase-scope"; exit 1; fi
EXISTM=$(curl -sk -H "$AUTH" "$KC/admin/realms/workbase/client-scopes/$SID/protocol-mappers/models" \
  | python3 -c 'import sys,json;print(next((m["id"] for m in json.load(sys.stdin) if m["name"]=="hub_role"),""))')
if [ -n "$EXISTM" ]; then
  curl -sk -X DELETE -H "$AUTH" "$KC/admin/realms/workbase/client-scopes/$SID/protocol-mappers/models/$EXISTM" >/dev/null || true
fi
cat > /tmp/hr_pm.json <<JSON
{"name":"hub_role","protocol":"openid-connect","protocolMapper":"oidc-usermodel-attribute-mapper",
 "config":{"user.attribute":"hub_role","claim.name":"hub_role","jsonType.label":"String",
 "id.token.claim":"true","access.token.claim":"true","userinfo.token.claim":"true"}}
JSON
curl -sk -X POST -H "$AUTH" -H "Content-Type: application/json" -d @/tmp/hr_pm.json \
  "$KC/admin/realms/workbase/client-scopes/$SID/protocol-mappers/models" \
  -w "protocol mapper hub_role: HTTP %{http_code}\n"

# ── 3. users/profile: zadeklaruj atrybut hub_role (unmanagedAttributePolicy juz ENABLED) ──
curl -sk -H "$AUTH" "$KC/admin/realms/workbase/users/profile" > /tmp/profile.json
python3 - <<'PY'
import json
p=json.load(open("/tmp/profile.json"))
attrs=p.setdefault("attributes",[])
if not any(a.get("name")=="hub_role" for a in attrs):
    attrs.append({"name":"hub_role","displayName":"Hub role","permissions":{"view":["admin"],"edit":["admin"]}})
json.dump(p,open("/tmp/profile.json","w"))
PY
curl -sk -X PUT -H "$AUTH" -H "Content-Type: application/json" -d @/tmp/profile.json \
  "$KC/admin/realms/workbase/users/profile" -w "users/profile hub_role: HTTP %{http_code}\n"

echo "=== weryfikacja ==="
curl -sk -H "$AUTH" "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
  | python3 -c 'import sys,json;[print(" IdP mapper:",m["name"],"->",m["config"].get("user.attribute")) for m in json.load(sys.stdin)]'
curl -sk -H "$AUTH" "$KC/admin/realms/workbase/client-scopes/$SID/protocol-mappers/models" \
  | python3 -c 'import sys,json;[print(" scope mapper:",m["name"]) for m in json.load(sys.stdin)]'
rm -f /tmp/hr_idp.json /tmp/hr_pm.json /tmp/profile.json
echo "DONE"
