#!/bin/bash
echo "=== TEST: co zwraca JWKS/discovery Huba z kontenera workbase-api ==="
sudo docker exec workbase-api sh -c "wget -qO- http://wb-hub-api:4100/.well-known/openid-configuration 2>&1 | head -c 300" 2>&1 || \
sudo docker exec workbase-api sh -c "curl -s http://wb-hub-api:4100/.well-known/openid-configuration 2>&1 | head -c 300" 2>&1
echo ""
echo "=== TEST: jwks.json ==="
sudo docker exec workbase-api sh -c "wget -qO- http://wb-hub-api:4100/.well-known/jwks.json 2>&1 | head -c 300" 2>&1
echo ""
echo "=== SWIEZE LOGI /sso/callback (ostatnia godzina) ==="
sudo docker logs workbase-api --since 60m 2>&1 | grep -iE 'sso.callback|HubSsoCallback|handoff|redeem|CreateUser|Keycloak|IDX|DOCTYPE|Odrzucony|provision' | tail -25
