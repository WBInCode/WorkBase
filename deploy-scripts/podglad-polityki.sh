#!/usr/bin/env bash
# Co dokladnie zrobi ponowne zastosowanie polityki stanowisk. NIC NIE ZMIENIA.
set -uo pipefail
q() { docker exec wb-postgres psql -U wbadmin -d workbase -tAc "$1" 2>&1; }

echo "== struktura: kto w ktorej jednostce i na jakim stanowisku =="
q "select rpad(coalesce(u.name,'?'),26)||' | '||rpad(coalesce(e.first_name||' '||e.last_name,'?'),24)||' | '||rpad(coalesce(p.name,'-'),16)||' | kierownicze='||coalesce(p.is_managerial::text,'-')
   from org_employee_assignments a
   left join org_employees e on e.id=a.employee_id
   left join org_units u on u.id=a.organization_unit_id
   left join org_positions p on p.id=a.position_id
   where a.end_date is null and a.is_primary
   order by u.name, p.is_managerial desc, e.last_name" | sed 's/^/  /'

echo
echo "== KTO DOSTANIE ROLE (stanowisko ma role domyslna, a osoba jej nie ma) =="
q "select rpad(coalesce(e.first_name||' '||e.last_name,'?'),24)||' -> dostanie role: '||r.name
   from org_employee_assignments a
   join org_employees e on e.id=a.employee_id
   join org_positions p on p.id=a.position_id
   join iam_roles r on r.id=p.default_role_id
   where a.end_date is null and a.is_primary and e.user_id is not null
     and not exists (select 1 from iam_user_roles ur where ur.user_id=e.user_id and ur.role_id=r.id)
   order by 1" | sed 's/^/  /'

echo
echo "== KTO NIE DOSTANIE ROLI (brak konta uzytkownika) =="
q "select rpad(coalesce(e.first_name||' '||e.last_name,'?'),24)||' | stanowisko: '||coalesce(p.name,'-')
   from org_employee_assignments a
   join org_employees e on e.id=a.employee_id
   left join org_positions p on p.id=a.position_id
   where a.end_date is null and a.is_primary and e.user_id is null
   order by 1" | sed 's/^/  /'

echo
echo "== KTO DOSTANIE PRZELOZONEGO (kierownik z tej samej jednostki) =="
q "select rpad(coalesce(pod.first_name||' '||pod.last_name,'?'),24)||' -> przelozony: '||coalesce(kier.first_name||' '||kier.last_name,'?')||'   (jednostka: '||coalesce(u.name,'?')||')'
   from org_employee_assignments a
   join org_employees pod on pod.id=a.employee_id
   join org_positions p on p.id=a.position_id
   left join org_units u on u.id=a.organization_unit_id
   join org_employee_assignments ka on ka.organization_unit_id=a.organization_unit_id and ka.end_date is null and ka.is_primary
   join org_positions kp on kp.id=ka.position_id and kp.is_managerial
   join org_employees kier on kier.id=ka.employee_id
   where a.end_date is null and a.is_primary and not p.is_managerial and pod.id <> kier.id
   order by 1" | sed 's/^/  /'

echo
echo "== jednostki BEZ kierownika (ich pracownicy zostana bez przelozonego) =="
q "select distinct coalesce(u.name,'?')
   from org_employee_assignments a
   left join org_units u on u.id=a.organization_unit_id
   where a.end_date is null and a.is_primary
     and not exists (select 1 from org_employee_assignments ka
                     join org_positions kp on kp.id=ka.position_id and kp.is_managerial
                     where ka.organization_unit_id=a.organization_unit_id and ka.end_date is null and ka.is_primary)
   order by 1" | sed 's/^/  /'

echo
echo "== zdublowane przypisania =="
q "select rpad(coalesce(e.first_name||' '||e.last_name,'?'),24)||' | '||count(*)||' aktywnych przypisan glownych'
   from org_employee_assignments a join org_employees e on e.id=a.employee_id
   where a.end_date is null and a.is_primary
   group by e.id, e.first_name, e.last_name having count(*) > 1" | sed 's/^/  /'
