#!/bin/bash
echo "=== czy build ma logout-redirect (nowy auth-config) ==="
sudo docker exec workbase-web sh -c "grep -c logout-redirect /usr/share/nginx/html/assets/*.js" 2>&1
echo "=== jaki JS referuje index.html ==="
curl -sk https://workbase.wb-partners.pl/index.html | grep -o 'index-[A-Za-z0-9_-]*\.js' | head -1
echo "=== cache-control index.html ==="
curl -sk -D - https://workbase.wb-partners.pl/index.html -o /dev/null | grep -i cache-control
echo "=== test logout-redirect endpoint ==="
curl -sk -o /dev/null -w 'HTTP %{http_code} -> %{redirect_url}\n' "https://wb-partners.pl/api/v1/auth/logout-redirect?return=https://workbase.wb-partners.pl/logged-out"
echo "=== Keycloak end_session z post_logout (bez id_token_hint) ==="
curl -sk -o /dev/null -w 'HTTP %{http_code} -> %{redirect_url}\n' "https://auth.wb-partners.pl/realms/workbase/protocol/openid-connect/logout?post_logout_redirect_uri=https%3A%2F%2Fwb-partners.pl%2Fapi%2Fv1%2Fauth%2Flogout-redirect&client_id=workbase-web"
