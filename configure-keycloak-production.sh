#!/usr/bin/env bash
# Idempotentna konfiguracja produkcyjna realm po imporcie/migracji bazy Keycloak.
# Ustawia branding, polski locale, bezpieczne adresy klienta SPA i czyści cache kluczy IdP.
set -euo pipefail

KC="${KC_URL:-https://auth.wb-partners.pl}"
REALM="workbase"
SECRETS_FILE="${SECRETS_FILE:-/opt/wb/workbase/.secrets}"
ADMIN_PASS=$(grep '^KC_ADMIN_PASS=' "$SECRETS_FILE" | cut -d= -f2-)

TOKEN=$(curl -skf --retry 60 --retry-delay 1 --retry-all-errors "$KC/realms/master/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=admin-cli -d username=admin -d "password=$ADMIN_PASS" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')
AUTH="Authorization: Bearer $TOKEN"

curl -sk -H "$AUTH" "$KC/admin/realms/$REALM" > /tmp/workbase-realm-current.json
python3 - <<'PY'
import json
path = "/tmp/workbase-realm-current.json"
realm = json.load(open(path, encoding="utf-8"))
realm.update({
    "displayName": "WorkBase",
    "displayNameHtml": "<strong>WorkBase</strong>",
    "internationalizationEnabled": True,
    "supportedLocales": ["pl"],
    "defaultLocale": "pl",
    "loginTheme": "workbase",
    "accountTheme": "workbase",
})
json.dump(realm, open(path, "w", encoding="utf-8"), ensure_ascii=False)
PY
curl -sk -o /dev/null -w "realm branding/locale: HTTP %{http_code}\n" \
  -X PUT -H "$AUTH" -H "Content-Type: application/json" \
  -d @/tmp/workbase-realm-current.json "$KC/admin/realms/$REALM"

CLIENT_ID=$(curl -sk -H "$AUTH" "$KC/admin/realms/$REALM/clients?clientId=workbase-web" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)[0]["id"])')
curl -sk -H "$AUTH" "$KC/admin/realms/$REALM/clients/$CLIENT_ID" > /tmp/workbase-web-client.json
python3 - <<'PY'
import json
path = "/tmp/workbase-web-client.json"
client = json.load(open(path, encoding="utf-8"))
client.update({
    "rootUrl": "https://workbase.wb-partners.pl",
    "baseUrl": "https://workbase.wb-partners.pl/",
    "adminUrl": "https://workbase.wb-partners.pl/",
    "redirectUris": ["https://workbase.wb-partners.pl/*"],
    "webOrigins": ["https://workbase.wb-partners.pl"],
})
attributes = client.setdefault("attributes", {})
attributes["pkce.code.challenge.method"] = "S256"
attributes["post.logout.redirect.uris"] = "https://workbase.wb-partners.pl/*##https://wb-partners.pl/*"
json.dump(client, open(path, "w", encoding="utf-8"), ensure_ascii=False)
PY
curl -sk -o /dev/null -w "workbase-web URLs: HTTP %{http_code}\n" \
  -X PUT -H "$AUTH" -H "Content-Type: application/json" \
  -d @/tmp/workbase-web-client.json "$KC/admin/realms/$REALM/clients/$CLIENT_ID"

curl -skf -H "$AUTH" "$KC/admin/realms/$REALM/users/profile" > /tmp/workbase-user-profile.json
python3 - <<'PY'
import json

path = "/tmp/workbase-user-profile.json"
profile = json.load(open(path, encoding="utf-8"))
profile["unmanagedAttributePolicy"] = "ADMIN_EDIT"
attributes = profile.setdefault("attributes", [])

last_name = next(attribute for attribute in attributes if attribute.get("name") == "lastName")
last_name.pop("required", None)

technical_attributes = {
    "tenant_id": "Tenant ID",
    "employee_id": "Employee ID",
    "hub_role": "HUB role",
    "hub_user_id": "HUB user ID",
    "hub_org_id": "HUB organization ID",
    "hub_instance_id": "HUB instance ID",
}
for name, display_name in technical_attributes.items():
    attribute = next((item for item in attributes if item.get("name") == name), None)
    if attribute is None:
        attribute = {"name": name}
        attributes.append(attribute)
    attribute.update({
        "displayName": display_name,
        "permissions": {"view": ["admin"], "edit": ["admin"]},
        "multivalued": False,
    })

json.dump(profile, open(path, "w", encoding="utf-8"), ensure_ascii=False)
PY
curl -skf -o /dev/null -w "user profile privacy: HTTP %{http_code}\n" \
  -X PUT -H "$AUTH" -H "Content-Type: application/json" \
  -d @/tmp/workbase-user-profile.json "$KC/admin/realms/$REALM/users/profile"

curl -sk -o /dev/null -w "clear keys cache: HTTP %{http_code}\n" \
  -X POST -H "$AUTH" "$KC/admin/realms/$REALM/clear-keys-cache"
curl -sk -o /dev/null -w "clear realm cache: HTTP %{http_code}\n" \
  -X POST -H "$AUTH" "$KC/admin/realms/$REALM/clear-realm-cache"

echo "=== verification ==="
curl -sk -H "$AUTH" "$KC/admin/realms/$REALM" | python3 -c '
import sys,json
r=json.load(sys.stdin)
print("locale:",r.get("defaultLocale"),"| loginTheme:",r.get("loginTheme"),"| accountTheme:",r.get("accountTheme"))
'
curl -sk -H "$AUTH" "$KC/admin/realms/$REALM/clients/$CLIENT_ID" | python3 -c '
import sys,json
c=json.load(sys.stdin)
print("rootUrl:",c.get("rootUrl"),"| baseUrl:",c.get("baseUrl"),"| redirects:",c.get("redirectUris"))
'
curl -skf -H "$AUTH" "$KC/admin/realms/$REALM/users/profile" | python3 -c '
import sys,json
p=json.load(sys.stdin)
attrs={a.get("name"):a for a in p.get("attributes",[])}
technical=("tenant_id","employee_id","hub_role","hub_user_id","hub_org_id","hub_instance_id")
print("lastName required:",bool(attrs.get("lastName",{}).get("required")),"| user-visible technical:",[name for name in technical if "user" in attrs.get(name,{}).get("permissions",{}).get("view",[])])
'
rm -f /tmp/workbase-realm-current.json /tmp/workbase-web-client.json /tmp/workbase-user-profile.json