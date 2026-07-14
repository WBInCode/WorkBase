#!/bin/bash
set -e
KC="https://auth.wb-partners.pl"
ADMIN_PASS=$(sudo cat /opt/wb/workbase/.secrets | grep KC_ADMIN_PASS | cut -d= -f2)
TOKEN=$(curl -sk -X POST "$KC/realms/master/protocol/openid-connect/token" -d grant_type=password -d client_id=admin-cli -d username=admin -d password="$ADMIN_PASS" | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')

CID=$(curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/clients?clientId=workbase-web" | python3 -c 'import sys,json;print(json.load(sys.stdin)[0]["id"])')

# Pobierz klienta, dodaj wb-partners.pl do post.logout.redirect.uris, PUT
curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/clients/$CID" > /tmp/client.json
python3 <<PY
import json
c = json.load(open('/tmp/client.json'))
attrs = c.setdefault('attributes', {})
cur = attrs.get('post.logout.redirect.uris', '')
parts = [p for p in cur.split('##') if p]
add = 'https://wb-partners.pl/*'
if add not in parts:
    parts.append(add)
attrs['post.logout.redirect.uris'] = '##'.join(parts)
json.dump(c, open('/tmp/client2.json','w'))
print('post.logout.redirect.uris =', attrs['post.logout.redirect.uris'])
PY

curl -sk -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d @/tmp/client2.json "$KC/admin/realms/workbase/clients/$CID" -w "PUT: HTTP %{http_code}\n"
rm -f /tmp/client.json /tmp/client2.json
