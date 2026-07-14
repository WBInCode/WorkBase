#!/bin/bash
set -e
export KC="https://auth.wb-partners.pl"
export TENANT="00000000-0000-0000-0000-000000000001"
ADMIN_PASS=$(sudo cat /opt/wb/workbase/.secrets | grep KC_ADMIN_PASS | cut -d= -f2)
export TOKEN=$(curl -sk -X POST "$KC/realms/master/protocol/openid-connect/token" -d grant_type=password -d client_id=admin-cli -d username=admin -d password="$ADMIN_PASS" | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')

echo "=== user profile: policy + czy tenant_id zadeklarowany ==="
curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/users/profile" > /tmp/prof.json
python3 -c '
import json
p=json.load(open("/tmp/prof.json"))
print("unmanagedAttributePolicy:", p.get("unmanagedAttributePolicy"))
print("zadeklarowane atrybuty:", [a["name"] for a in p.get("attributes",[])])
'

echo "=== wlaczam unmanagedAttributePolicy=ENABLED + dodaje tenant_id/employee_id do schematu ==="
python3 <<'PY'
import json, os, urllib.request
KC=os.environ['KC']; TOKEN=os.environ['TOKEN']
p=json.load(open('/tmp/prof.json'))
p['unmanagedAttributePolicy']='ENABLED'
names={a['name'] for a in p.get('attributes',[])}
for attr in ('tenant_id','employee_id'):
    if attr not in names:
        p.setdefault('attributes',[]).append({
            'name':attr,'displayName':attr,
            'permissions':{'view':['admin','user'],'edit':['admin']},
            'multivalued':False
        })
req=urllib.request.Request(f"{KC}/admin/realms/workbase/users/profile",
    data=json.dumps(p).encode(), method='PUT',
    headers={'Authorization':f'Bearer {TOKEN}','Content-Type':'application/json'})
try:
    urllib.request.urlopen(req); print('user profile updated OK')
except Exception as e:
    print('ERROR', e, e.read().decode() if hasattr(e,"read") else "")
PY

echo "=== ustawiam tenant_id na kontach (po zmianie policy) ==="
curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/users?max=100" > /tmp/users.json
python3 <<'PY'
import json, os, urllib.request
KC=os.environ['KC']; TOKEN=os.environ['TOKEN']; TENANT=os.environ['TENANT']
for u in json.load(open('/tmp/users.json')):
    if u.get('username','').startswith('kiosk'): continue
    attrs=u.get('attributes') or {}
    if not attrs.get('tenant_id'):
        attrs['tenant_id']=[TENANT]; u['attributes']=attrs
        req=urllib.request.Request(f"{KC}/admin/realms/workbase/users/{u['id']}",
            data=json.dumps(u).encode(), method='PUT',
            headers={'Authorization':f'Bearer {TOKEN}','Content-Type':'application/json'})
        try: urllib.request.urlopen(req); print(u.get('email'),'-> OK')
        except Exception as e: print(u.get('email'),'-> ERR',e)
PY

echo "=== weryfikacja kacper ==="
KID=$(curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/users?email=kacper.franczyk@wb-incode.pl" | python3 -c 'import sys,json;print(json.load(sys.stdin)[0]["id"])')
curl -sk -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/workbase/users/$KID" | python3 -c 'import sys,json;print("kacper attributes:", json.load(sys.stdin).get("attributes"))'
rm -f /tmp/prof.json /tmp/users.json
