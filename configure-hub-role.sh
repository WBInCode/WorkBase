#!/usr/bin/env bash
# Konfiguruje hub_role jako lokalny atrybut Keycloak ustawiany wyłącznie przez
# zweryfikowany handoff konkretnej instancji WorkBase. Globalny claim OIDC nie może
# określić roli, bo jedno konto HUB może należeć do wielu organizacji.
set -e
KC="https://auth.wb-partners.pl"
ADMIN_PASS=$(grep KC_ADMIN_PASS /opt/wb/workbase/.secrets | cut -d= -f2)

TOKEN=$(curl -sk -X POST "$KC/realms/master/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=admin-cli -d username=admin -d password="$ADMIN_PASS" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')
AUTH="Authorization: Bearer $TOKEN"

# ── 1. Usuń historyczne mappery IdP nadpisujące lokalny hub_role ──
MAPPER_IDS=$(curl -sk -H "$AUTH" "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
  | python3 -c 'import sys,json;print(" ".join(m["id"] for m in json.load(sys.stdin) if m.get("name")=="hubRole" or m.get("config",{}).get("user.attribute")=="hub_role"))')
for MAPPER_ID in $MAPPER_IDS; do
  curl -sk -X DELETE -H "$AUTH" \
    "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers/$MAPPER_ID" \
    -o /dev/null -w "usunięcie IdP mappera hub_role: HTTP %{http_code}\n"
done

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

# Stabilny identyfikator globalnego konta HUB używany do bieżącej weryfikacji
# InstanceAccess przy każdym JWT. Atrybut ustawia wyłącznie /sso/callback WorkBase.
EXIST_USER_ID=$(curl -sk -H "$AUTH" "$KC/admin/realms/workbase/client-scopes/$SID/protocol-mappers/models" \
  | python3 -c 'import sys,json;print(next((m["id"] for m in json.load(sys.stdin) if m["name"]=="hub_user_id"),""))')
if [ -n "$EXIST_USER_ID" ]; then
  curl -sk -X DELETE -H "$AUTH" \
    "$KC/admin/realms/workbase/client-scopes/$SID/protocol-mappers/models/$EXIST_USER_ID" >/dev/null || true
fi
cat > /tmp/huid_pm.json <<JSON
{"name":"hub_user_id","protocol":"openid-connect","protocolMapper":"oidc-usermodel-attribute-mapper",
 "config":{"user.attribute":"hub_user_id","claim.name":"hub_user_id","jsonType.label":"String",
 "id.token.claim":"true","access.token.claim":"true","userinfo.token.claim":"true"}}
JSON
curl -sk -X POST -H "$AUTH" -H "Content-Type: application/json" -d @/tmp/huid_pm.json \
  "$KC/admin/realms/workbase/client-scopes/$SID/protocol-mappers/models" \
  -w "protocol mapper hub_user_id: HTTP %{http_code}\n"

# ── 3. users/profile: zadeklaruj atrybut hub_role (unmanagedAttributePolicy juz ENABLED) ──
curl -sk -H "$AUTH" "$KC/admin/realms/workbase/users/profile" > /tmp/profile.json
python3 - <<'PY'
import json
p=json.load(open("/tmp/profile.json"))
attrs=p.setdefault("attributes",[])
if not any(a.get("name")=="hub_role" for a in attrs):
    attrs.append({"name":"hub_role","displayName":"Hub role","permissions":{"view":["admin"],"edit":["admin"]}})
if not any(a.get("name")=="hub_user_id" for a in attrs):
  attrs.append({"name":"hub_user_id","displayName":"Hub user id","permissions":{"view":["admin"],"edit":["admin"]}})
json.dump(p,open("/tmp/profile.json","w"))
PY
curl -sk -X PUT -H "$AUTH" -H "Content-Type: application/json" -d @/tmp/profile.json \
  "$KC/admin/realms/workbase/users/profile" -w "users/profile hub_role: HTTP %{http_code}\n"

echo "=== weryfikacja ==="
curl -sk -H "$AUTH" "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
  | python3 -c 'import sys,json;[print(" IdP mapper:",m["name"],"->",m["config"].get("user.attribute")) for m in json.load(sys.stdin)]'
curl -sk -H "$AUTH" "$KC/admin/realms/workbase/client-scopes/$SID/protocol-mappers/models" \
  | python3 -c 'import sys,json;[print(" scope mapper:",m["name"]) for m in json.load(sys.stdin)]'
rm -f /tmp/hr_pm.json /tmp/huid_pm.json /tmp/profile.json
echo "DONE"
