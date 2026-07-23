# Provisioning firm z WB Platform

## Decyzja domenowa

WB Platform (HUB) jest źródłem prawdy dla firmy i jej dostępu do produktów:

- `HUB organization (org_id)` = `WorkBase Tenant`;
- `ProductInstance.id` identyfikuje dostęp tej organizacji do WorkBase;
- lokalny `Tenant.Id` pozostaje technicznym identyfikatorem WorkBase i nie jest publikowany jako ID firmy w HUB;
- nazwa oraz slug pochodzą z HUB, ale slug WorkBase po utworzeniu pozostaje stabilny;
- plan, status i moduły pochodzą z konfiguracji instancji HUB.

Tenant przechowuje oba zewnętrzne klucze: `HubOrganizationId` i
`HubProductInstanceId`. Każdy ma unikalny indeks. Nie wolno wiązać istniejącego
tenanta z inną organizacją ani instancją.

## Nadanie dostępu

Docelowy przebieg po przyznaniu organizacji produktu WorkBase:

1. HUB tworzy lub aktywuje instancję produktu dla organizacji.
2. HUB wysyła podpisany webhook `entitlements.updated`. Payload musi zawierać
   `instanceId` (akceptowane jest też `instance_id`) albo nagłówek
   `x-wb-instance-id`.
3. WorkBase pobiera autorytatywną konfigurację przez
   `GET /api/v1/instances/{instanceId}/config` z poświadczeniami SSO produktu.
4. WorkBase weryfikuje `instanceId`, `orgId` i `productKey=workbase`.
5. Idempotentny provisioning tworzy Tenant i jego bazowy RBAC albo odnajduje
   istniejące powiązanie po `org_id`.
6. Status instancji ustawia aktywność tenanta, a lista `modules` nadpisuje
   lokalne FeatureFlags 1:1.

Pierwszy handoff SSO wykonuje te same kroki, więc użytkownik nie zostanie
wpuszczony do firmy, której webhook jeszcze nie dotarł. Przy starcie aplikacji
WorkBase rekoncyliuje wszystkie zapisane `HubProductInstanceId` oraz opcjonalną
legacy instancję z konfiguracji.

Minimalna odpowiedź Instance Config API:

```json
{
  "instanceId": "product-instance-id",
  "orgId": "hub-organization-id",
  "orgSlug": "acme",
  "orgName": "Acme Sp. z o.o.",
  "productKey": "workbase",
  "status": "active",
  "plan": "gold",
  "modules": ["org", "identity", "time", "leave"],
  "customDomain": null
}
```

`orgName` jest opcjonalne dla zgodności ze starszym HUB; wtedy WorkBase używa
`orgSlug` jako nazwy.

## Logowanie i członkostwo

Handoff JWT musi zawierać co najmniej `org_id`, `instance_id`, `product_key`,
`org_role`/`instance_role`, `email` i `sub`. Po lokalnej weryfikacji podpisu
WorkBase dodatkowo odpytuje Instance Config API i wymaga zgodności
`instance_id -> org_id`. Dopiero potem zapisuje na koncie Keycloak:

- `tenant_id` - lokalny `Tenant.Id`;
- `hub_org_id`;
- `hub_instance_id`;
- `hub_role` (`owner`, `admin` albo `member`).

Role aplikacyjne są synchronizowane przy walidacji tokenu:

| Kontekst | HUB | WorkBase | Keycloak realm role |
|---|---|---|---|
| Firma operatora | owner | Super Admin | workbase-admin |
| Firma klienta | owner | Admin | workbase-admin |
| Każda firma | admin/member | Pracownik | workbase-user |

`Super Admin` istnieje wyłącznie w firmie operatora
(`00000000-0000-0000-0000-000000000001`) i jako jedyny ma dostęp do operacji
między tenantami. W firmie klienta dokładnie właściciel organizacji z HUB jest
jedynym `Adminem`; zmiana właściciela przenosi to przypisanie przy następnym
logowaniu. Pozostali użytkownicy otrzymują rolę `Pracownik`.

W bieżącym modelu jedno konto Keycloak może należeć tylko do jednej organizacji.
Próba użycia istniejącego konta z innym `tenant_id` lub `hub_org_id` jest
odrzucana. Obsługa jednej osoby w wielu firmach wymaga osobnych, świadomie
zaprojektowanych sesji/realmów organizacji i nie jest częścią tego wdrożenia.

## Odebranie dostępu

Każdy status instancji inny niż `active` zawiesza Tenant i wyłącza wszystkie jego
moduły. Walidacja JWT sprawdza stan tenanta przy każdym uwierzytelnionym żądaniu,
więc token firmy zawieszonej lub anulowanej jest odrzucany. Ponowne ustawienie
`active` przez HUB reaktywuje tenant przy następnym webhooku/reconciliation.

HUB powinien ponawiać webhooki do odpowiedzi 2xx. WorkBase odpowiada fail-soft na
chwilową niedostępność HUB i zachowuje ostatni poprawnie zsynchronizowany stan.

## Migracja obecnej instancji

`Hub:TenantId` jest wyłącznie przejściowym bootstrapem. Przy pierwszym syncu
`Hub:InstanceId` wskazany istniejący tenant zostaje powiązany z `org_id`, dzięki
czemu wdrożenie nie tworzy duplikatu obecnej firmy. Nowe organizacje nie wymagają
żadnego `TenantId` w konfiguracji.

Skrypt `configure-hub-idp.sh` usuwa historyczny mapper
`tenant_id-hardcoded`. Po wdrożeniu należy uruchomić ten skrypt na realmie
produkcyjnym; pozostawienie mappera przypisywałoby każdą firmę do seedowego
tenanta.

Migracja usuwa rolę `Super Admin` z tenantów klientów, zachowując najwyżej jedno
przypisanie `Admin`, i dodaje constraint bazy rezerwujący `Super Admin` dla firmy
operatora.

## Zapraszanie pracowników z WorkBase

Dodanie pracownika w WorkBase i zlecenie dostępu do HUB są zapisywane w jednej
transakcji. Dla tenanta powiązanego z HUB powstaje rekord
`hub_employee_access_requests`; dla instalacji bez HUB pozostaje dotychczasowy
provisioning bezpośrednio w Keycloak. Job Hangfire co minutę wysyła oczekujące
zaproszenia z wykładniczym retry i stabilnym kluczem idempotencji.

HUB musi udostępnić endpoint:

```http
POST /api/v1/organizations/{orgId}/invitations
x-sso-client-id: workbase
x-sso-secret: <product-secret>
Idempotency-Key: workbase:invite:{tenantId}:{employeeId}
Content-Type: application/json
```

```json
{
  "email": "jan@firma.pl",
  "firstName": "Jan",
  "lastName": "Kowalski",
  "productKey": "workbase",
  "productInstanceId": "product-instance-id",
  "role": "member",
  "externalReference": "workbase-employee-id"
}
```

Wymagane zachowanie HUB:

1. Zweryfikować klienta produktu i potwierdzić, że `productInstanceId` należy do
   organizacji `orgId` oraz ma aktywny dostęp do WorkBase.
2. Ten sam `Idempotency-Key` zawsze zwracać jako tę samą operację, bez drugiego
   e-maila i bez drugiego członkostwa.
3. Jeśli e-mail nie ma konta HUB, utworzyć zaproszenie i wysłać wiadomość.
4. Jeśli konto istnieje, dodać lub odnaleźć członkostwo w organizacji i przyznać
   dostęp do instancji WorkBase.
5. Nie zmieniać właściciela ani administratora firmy; pracownik zawsze otrzymuje
   rolę `member`.

Odpowiedź `200 OK` albo `201 Created`:

```json
{
  "invitationId": "invitation-id-or-null",
  "membershipId": "membership-id-or-null",
  "hubUserId": "hub-user-id-or-null",
  "status": "pending"
}
```

`status` może mieć wartość `pending` albo `active`. Błędy `4xx/5xx` nie mogą
oznaczać częściowego sukcesu bez możliwości bezpiecznego ponowienia.

Po pierwszym poprawnym handoffie SSO WorkBase łączy konto z pracownikiem po
`tenant_id + email`, zapisuje `hub_user_id` i oznacza żądanie jako `Active`.

Każda walidacja lokalnego JWT dla tenanta HUB dodatkowo sprawdza przez
service-to-service endpoint `POST /api/v1/instances/{instanceId}/user-access/check`,
czy użytkownik nadal ma aktywne konto, członkostwo, `InstanceAccess` i licencję.
Pozytywna decyzja oraz aktualna rola są cache'owane maksymalnie przez 30 sekund.
Dzięki temu wejście bezpośrednio przez starą sesję Keycloak nie omija HUB, revoke
blokuje także istniejące tokeny, a transfer OWNER-a aktualizuje lokalny RBAC bez
czekania na kolejny handoff.

### Kolejność wdrożenia

Zmiana handoffu i mapperów Keycloak wymaga jednego skoordynowanego okna:

1. Włączyć maintenance mode w HUB, aby na czas zmiany nie wystawiać nowych
   handoffów.
2. Wdrożyć migracje i WorkBase z `Hub:EmployeeAccessSyncEnabled=false` oraz
   `Hub:UserAccessCheckEnabled=false`.
3. Wdrożyć migrację `ProductAccessBinding` i nową wersję `hub-api`.
4. Uruchomić `configure-hub-idp.sh` oraz `configure-hub-role.sh`; usuwają one
   historyczne mappery `tenant_id-hardcoded` i globalnego `hub_role`.
5. Uruchomić `deploy-scripts/preflight-workbase-rollout.sh` przy nadal włączonym
   maintenance mode i trzech flagach WorkBase ustawionych na `false`.
6. Ustawić `Hub__Enabled=true`, `Hub__EmployeeAccessSyncEnabled=true` oraz
   `Hub__UserAccessCheckEnabled=true` w WorkBase. Wszystkie wcześniej
   zakolejkowane żądania zostaną wtedy dostarczone automatycznie, a każdy JWT
   zacznie podlegać bieżącej weryfikacji InstanceAccess.
7. Zrestartować WorkBase, wyłączyć maintenance mode i natychmiast uruchomić w HUB
   `pnpm --filter @wb/hub-api smoke:workbase-access` na środowisku z PostgreSQL.
8. Sprawdzić pełne SSO właściciela oraz jednego pracownika. W razie błędu ponownie
   włączyć maintenance mode i wyłączyć trzy flagi WorkBase.

Nie należy rozdzielać kroków 2–4 długą przerwą: stary mapper Keycloak nadpisuje
tenant wyliczony z handoffu, a stary HUB nie przekazuje WorkBase kontekstu
`org_id + instance_id`.

### Odebranie dostępu pracownikowi

Dezaktywacja pracownika zapisuje operację `Revoke` w tej samej transakcji co
zmiana jego statusu. Job wywołuje:

```http
DELETE /api/v1/organizations/{orgId}/product-instances/{productInstanceId}/members/by-external-reference/{employeeId}?email={employeeEmail}
x-sso-client-id: workbase
x-sso-secret: <product-secret>
Idempotency-Key: workbase:revoke:{tenantId}:{employeeId}
```

HUB ma anulować oczekujące zaproszenie albo odebrać użytkownikowi wyłącznie
dostęp do wskazanej instancji WorkBase. Nie wolno usuwać globalnego konta HUB ani
członkostwa użytkownika w organizacji, ponieważ może korzystać z innych produktów.
`204 No Content` oraz `404 Not Found` są sukcesem idempotentnym.