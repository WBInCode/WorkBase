#!/bin/bash
set -e
KC="https://auth.wb-partners.pl"
ADMIN_PASS=$(grep KC_ADMIN_PASS /opt/wb/workbase/.secrets | cut -d= -f2)
SECRET=$(cat /opt/wb/hub/.oidc-keycloak-secret)

TOKEN=$(curl -sk -X POST "$KC/realms/master/protocol/openid-connect/token" \
  -d grant_type=password -d client_id=admin-cli -d username=admin -d password="$ADMIN_PASS" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')

# Usuń istniejący IdP (idempotencja)
curl -sk -X DELETE -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/identity-provider/instances/wb-hub" >/dev/null 2>&1 || true

# Utwórz IdP wb-hub (Hub jako OIDC Identity Provider)
cat > /tmp/idp.json <<JSON
{
  "alias": "wb-hub",
  "displayName": "WB Platform",
  "providerId": "oidc",
  "enabled": true,
  "trustEmail": true,
  "storeToken": false,
  "linkOnly": false,
  "firstBrokerLoginFlowAlias": "first broker login",
  "config": {
    "clientId": "keycloak",
    "clientSecret": "$SECRET",
    "authorizationUrl": "https://wb-partners.pl/oidc/authorize",
    "tokenUrl": "https://wb-partners.pl/api/v1/oidc/token",
    "userInfoUrl": "https://wb-partners.pl/api/v1/oidc/userinfo",
    "jwksUrl": "https://wb-partners.pl/.well-known/jwks.json",
    "issuer": "https://wb-partners.pl",
    "defaultScope": "openid profile email",
    "useJwksUrl": "true",
    "validateSignature": "true",
    "clientAuthMethod": "client_secret_post",
    "syncMode": "FORCE"
  }
}
JSON

curl -sk -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d @/tmp/idp.json "$KC/admin/realms/workbase/identity-provider/instances" \
  -w "utworzenie IdP: HTTP %{http_code}\n"

# Mappery claimów → atrybuty konta (email/imie/nazwisko)
add_mapper() {
  local NAME="$1" CLAIM="$2" USERATTR="$3"
  cat > /tmp/m.json <<JSON
{"name":"$NAME","identityProviderAlias":"wb-hub","identityProviderMapper":"oidc-user-attribute-idp-mapper",
 "config":{"syncMode":"FORCE","claim":"$CLAIM","user.attribute":"$USERATTR"}}
JSON
  curl -sk -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
    -d @/tmp/m.json "$KC/admin/realms/workbase/identity-provider/instances/wb-hub/mappers" \
    -w "  mapper $NAME: HTTP %{http_code}\n"
}
add_mapper "email" "email" "email"
add_mapper "firstName" "given_name" "firstName"
add_mapper "lastName" "family_name" "lastName"

echo "=== weryfikacja IdP ==="
curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/identity-provider/instances/wb-hub" \
  | python3 -c 'import sys,json;d=json.load(sys.stdin);print("alias:",d.get("alias"),"| enabled:",d.get("enabled"),"| authUrl:",d["config"].get("authorizationUrl"))'
rm -f /tmp/idp.json /tmp/m.json
