#!/bin/bash
set -e
KC="https://auth.wb-partners.pl"
ADMIN_PASS=$(grep KC_ADMIN_PASS /opt/wb/workbase/.secrets | cut -d= -f2)
SECRET=$(cat /opt/wb/hub/.oidc-keycloak-secret)

TOKEN=$(curl -skf --retry 60 --retry-delay 1 --retry-all-errors -X POST "$KC/realms/master/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=admin-cli -d username=admin -d password="$ADMIN_PASS" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')

# Zaufany flow brokera: nowe konto jest tworzone automatycznie, a istniejące konto
# o tym samym zweryfikowanym e-mailu jest automatycznie łączone z kontem WB Platform.
# Dzięki temu użytkownik nie widzi technicznego ekranu first-broker-login Keycloak.
FLOW_ALIAS="wb-hub-auto-link"
FLOW_EXISTS=$(curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/authentication/flows" \
  | python3 -c 'import sys,json;print("yes" if any(f.get("alias")=="wb-hub-auto-link" for f in json.load(sys.stdin)) else "no")')
if [ "$FLOW_EXISTS" = "no" ]; then
  curl -sk -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
    -d '{"alias":"wb-hub-auto-link","description":"Automatyczne tworzenie lub laczenie kont z zaufanego WB Platform IdP","providerId":"basic-flow","topLevel":true,"builtIn":false}' \
    "$KC/admin/realms/workbase/authentication/flows" -o /dev/null -w "utworzenie flow auto-link: HTTP %{http_code}\n"
fi

ensure_execution() {
  local PROVIDER="$1"
  local EXECUTION_ID
  EXECUTION_ID=$(curl -sk -H "Authorization: Bearer $TOKEN" \
    "$KC/admin/realms/workbase/authentication/flows/$FLOW_ALIAS/executions" \
    | python3 -c "import sys,json;print(next((e['id'] for e in json.load(sys.stdin) if e.get('providerId')=='$PROVIDER'),''))")
  if [ -z "$EXECUTION_ID" ]; then
    curl -sk -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d "{\"provider\":\"$PROVIDER\"}" \
      "$KC/admin/realms/workbase/authentication/flows/$FLOW_ALIAS/executions/execution" \
      -o /dev/null -w "  execution $PROVIDER: HTTP %{http_code}\n"
    EXECUTION_ID=$(curl -sk -H "Authorization: Bearer $TOKEN" \
      "$KC/admin/realms/workbase/authentication/flows/$FLOW_ALIAS/executions" \
      | python3 -c "import sys,json;print(next((e['id'] for e in json.load(sys.stdin) if e.get('providerId')=='$PROVIDER'),''))")
  fi
  curl -sk -H "Authorization: Bearer $TOKEN" \
    "$KC/admin/realms/workbase/authentication/flows/$FLOW_ALIAS/executions" > /tmp/flow-executions.json
  python3 - "$EXECUTION_ID" <<'PY'
import json,sys
execution_id=sys.argv[1]
execution=next(e for e in json.load(open('/tmp/flow-executions.json')) if e['id']==execution_id)
execution['requirement']='ALTERNATIVE'
json.dump(execution,open('/tmp/flow-execution.json','w'))
PY
  curl -sk -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
    -d @/tmp/flow-execution.json "$KC/admin/realms/workbase/authentication/flows/$FLOW_ALIAS/executions" \
    -o /dev/null -w "  requirement $PROVIDER=ALTERNATIVE: HTTP %{http_code}\n"
}
ensure_execution "idp-create-user-if-unique"
ensure_execution "idp-auto-link"

# Utwórz lub zaktualizuj IdP wb-hub (bez usuwania — zachowuje federated identities).
cat > /tmp/idp.json <<JSON
{
  "alias": "wb-hub",
  "displayName": "WB Platform",
  "providerId": "oidc",
  "enabled": true,
  "authenticateByDefault": true,
  "trustEmail": true,
  "storeToken": false,
  "linkOnly": false,
  "firstBrokerLoginFlowAlias": "$FLOW_ALIAS",
  "config": {
    "clientId": "keycloak",
    "clientSecret": "$SECRET",
    "authorizationUrl": "https://wb-partners.pl/oidc/authorize",
    "tokenUrl": "http://wb-hub-api:4100/api/v1/oidc/token",
    "userInfoUrl": "http://wb-hub-api:4100/api/v1/oidc/userinfo",
    "jwksUrl": "http://wb-hub-api:4100/.well-known/jwks.json",
    "issuer": "https://wb-partners.pl",
    "defaultScope": "openid profile email",
    "useJwksUrl": "true",
    "validateSignature": "true",
    "clientAuthMethod": "client_secret_post",
    "syncMode": "FORCE"
  }
}
JSON

IDP_STATUS=$(curl -sk -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $TOKEN" \
  "$KC/admin/realms/workbase/identity-provider/instances/wb-hub")
if [ "$IDP_STATUS" = "200" ]; then
  curl -sk -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
    -d @/tmp/idp.json "$KC/admin/realms/workbase/identity-provider/instances/wb-hub" \
    -o /dev/null -w "aktualizacja IdP: HTTP %{http_code}\n"
else
  curl -sk -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
    -d @/tmp/idp.json "$KC/admin/realms/workbase/identity-provider/instances" \
    -o /dev/null -w "utworzenie IdP: HTTP %{http_code}\n"
fi

# Mappery claimów → atrybuty konta (email/imie/nazwisko)
add_mapper() {
  local NAME="$1" CLAIM="$2" USERATTR="$3"
  local MAPPER_ID
  cat > /tmp/m.json <<JSON
{"name":"$NAME","identityProviderAlias":"wb-hub","identityProviderMapper":"oidc-user-attribute-idp-mapper",
 "config":{"syncMode":"FORCE","claim":"$CLAIM","user.attribute":"$USERATTR"}}
JSON
  MAPPER_ID=$(curl -sk -H "Authorization: Bearer $TOKEN" \
    "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
    | python3 -c "import sys,json;print(next((m['id'] for m in json.load(sys.stdin) if m.get('name')=='$NAME'),''))")
  if [ -n "$MAPPER_ID" ]; then
    echo "  mapper $NAME: exists"
  else
    curl -sk -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d @/tmp/m.json "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
      -o /dev/null -w "  mapper $NAME create: HTTP %{http_code}\n"
  fi
}
add_mapper "email" "email" "email"
add_mapper "firstName" "given_name" "firstName"
add_mapper "lastName" "family_name" "lastName"

# Każdy brokerowany użytkownik tej instancji WorkBase należy do domyślnego tenanta.
# Mapper jest częścią bazowej konfiguracji IdP, aby nie znikał po odtworzeniu realm.
cat > /tmp/m.json <<JSON
{"name":"tenant_id-hardcoded","identityProviderAlias":"wb-hub","identityProviderMapper":"hardcoded-attribute-idp-mapper",
 "config":{"syncMode":"FORCE","attribute":"tenant_id","attribute.value":"00000000-0000-0000-0000-000000000001"}}
JSON
TENANT_MAPPER_ID=$(curl -sk -H "Authorization: Bearer $TOKEN" \
  "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
  | python3 -c 'import sys,json;print(next((m["id"] for m in json.load(sys.stdin) if m.get("name")=="tenant_id-hardcoded"),""))')
if [ -n "$TENANT_MAPPER_ID" ]; then
  echo "  mapper tenant_id-hardcoded: exists"
else
  curl -sk -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
    -d @/tmp/m.json "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
    -o /dev/null -w "  mapper tenant_id-hardcoded create: HTTP %{http_code}\n"
fi

echo "=== weryfikacja IdP ==="
curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/identity-provider/instances/wb-hub" \
  | python3 -c 'import sys,json;d=json.load(sys.stdin);print("alias:",d.get("alias"),"| enabled:",d.get("enabled"),"| authUrl:",d["config"].get("authorizationUrl"))'
rm -f /tmp/idp.json /tmp/m.json /tmp/flow-executions.json /tmp/flow-execution.json
