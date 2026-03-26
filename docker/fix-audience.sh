#!/bin/bash
set -e
/opt/keycloak/bin/kcadm.sh config credentials --server http://localhost:8080 --realm master --user admin --password admin

# Get workbase-scope ID
SCOPE_ID=$(/opt/keycloak/bin/kcadm.sh get client-scopes -r workbase --fields id,name | grep -B1 '"workbase-scope"' | grep '"id"' | sed 's/.*"\([^"]*\)".*/\1/' | head -1)
echo "SCOPE_ID=$SCOPE_ID"

# Add audience mapper to include workbase-api in access token audience
/opt/keycloak/bin/kcadm.sh create "client-scopes/$SCOPE_ID/protocol-mappers/models" -r workbase \
  -s name=audience-workbase-api \
  -s protocol=openid-connect \
  -s protocolMapper=oidc-audience-mapper \
  -s 'config."included.client.audience"=workbase-api' \
  -s 'config."id.token.claim"=false' \
  -s 'config."access.token.claim"=true' \
  -s 'config."lightweight.claim"=false' \
  -s 'config."introspection.token.claim"=true'

echo "AUDIENCE_MAPPER_CREATED"
