import { useCallback, useState } from 'react';
import { Building2, ToggleLeft, ToggleRight, RefreshCw, Package, Plus } from 'lucide-react';
import {
  usePlatformTenants,
  usePlatformTenantFeatureFlags,
  useTogglePlatformTenantFeatureFlag,
  useApplyLicensePlanToPlatformTenant,
  useLicensePlans,
  useCreateTenant,
} from '@/api/hooks/useIam';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

// Keep in sync with src/WorkBase.Shared/Modules/ModuleCatalog.cs (Key -> DisplayName).
const moduleLabels: Record<string, string> = {
  org: 'Organizacja',
  identity: 'Zarządzanie dostępem',
  time: 'Czas pracy',
  leave: 'Urlopy',
  tasks: 'Zadania',
  workflow: 'Procesy',
  dashboard: 'Dashboard',
  notification: 'Powiadomienia',
  documents: 'Dokumenty',
  integration: 'Integracje',
  forms: 'Formularze',
  cases: 'Sprawy',
  contacts: 'Kontakty',
  sales: 'Sprzedaż',
  ai: 'AI',
};

/**
 * Platform-operator panel: pick a company (tenant) from the list, then manage its module
 * licensing — apply a plan (Bronze/Silver/Gold) or toggle individual modules a la carte.
 * See docs/05-module-licensing-architecture.md step 5.
 *
 * Only works for users authenticated in our own operator tenant with the
 * platform.manage-tenants permission — the backend enforces this; everyone else gets a 403
 * from the underlying endpoints even though this page is reachable in the nav for any admin.
 */
export function PlatformTenantsPage() {
  const mobile = useIsMobile();
  const { data: tenants, isLoading, error, refetch, isFetching } = usePlatformTenants();
  const { data: plans } = useLicensePlans();
  const [selectedTenantId, setSelectedTenantId] = useState<string | null>(null);
  const [newName, setNewName] = useState('');
  const [newSlug, setNewSlug] = useState('');
  const createTenantMutation = useCreateTenant();

  const { data: flags, isLoading: flagsLoading } = usePlatformTenantFeatureFlags(selectedTenantId);
  const toggleMutation = useTogglePlatformTenantFeatureFlag(selectedTenantId);
  const applyPlanMutation = useApplyLicensePlanToPlatformTenant(selectedTenantId);

  const handleToggle = useCallback(
    (module: string) => toggleMutation.mutate(module),
    [toggleMutation],
  );

  const slugify = (value: string) =>
    value.toLowerCase().trim()
      .replace(/[^a-z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-');

  const handleCreateTenant = useCallback(() => {
    if (!newName.trim()) return;
    const slug = newSlug.trim() || slugify(newName);
    createTenantMutation.mutate(
      { name: newName.trim(), slug },
      {
        onSuccess: () => {
          setNewName('');
          setNewSlug('');
        },
      },
    );
  }, [newName, newSlug, createTenantMutation]);

  const selectedTenant = tenants?.find((t) => t.id === selectedTenantId) ?? null;

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '1000px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: colors.gray[900] }}>Firmy (panel operatora)</h1>
        <button
          onClick={() => refetch()}
          style={{
            display: 'inline-flex', alignItems: 'center', gap: '6px',
            padding: '7px 12px', fontSize: '13px', fontWeight: 500,
            color: colors.gray[700], backgroundColor: colors.white,
            border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', cursor: 'pointer',
          }}
          title="Odśwież"
        >
          <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
        </button>
      </div>

      {error && (
        <div style={{
          padding: '12px 16px', marginBottom: '16px', borderRadius: '8px',
          backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px',
        }}>
          Błąd ładowania firm — brak uprawnień operatora platformy lub błąd sieci.
        </div>
      )}

      <div style={{ display: 'flex', gap: '20px', flexDirection: mobile ? 'column' : 'row' }}>
        {/* Tenant list */}
        <div style={{ flex: '0 0 320px' }}>
          {/* Create new tenant */}
          <div style={{
            marginBottom: '14px', padding: '12px', borderRadius: '8px',
            border: `1px solid ${colors.gray[200]}`, backgroundColor: colors.white,
          }}>
            <div style={{ fontSize: '12px', fontWeight: 600, color: colors.gray[700], marginBottom: '8px' }}>
              Nowa firma
            </div>
            <input
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="Nazwa firmy"
              style={{
                width: '100%', padding: '7px 10px', fontSize: '13px', marginBottom: '6px',
                border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', boxSizing: 'border-box',
              }}
            />
            <input
              value={newSlug}
              onChange={(e) => setNewSlug(e.target.value)}
              placeholder={newName ? slugify(newName) : 'slug (opcjonalnie)'}
              style={{
                width: '100%', padding: '7px 10px', fontSize: '13px', marginBottom: '8px',
                border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', boxSizing: 'border-box',
              }}
            />
            {createTenantMutation.isError && (
              <div style={{ fontSize: '12px', color: colors.danger[700], marginBottom: '6px' }}>
                Nie udało się utworzyć firmy (slug może już istnieć).
              </div>
            )}
            <button
              onClick={handleCreateTenant}
              disabled={!newName.trim() || createTenantMutation.isPending}
              style={{
                display: 'inline-flex', alignItems: 'center', gap: '6px', width: '100%',
                justifyContent: 'center',
                padding: '8px 12px', fontSize: '13px', fontWeight: 500, borderRadius: '6px',
                border: 'none', backgroundColor: colors.primary[600], color: colors.white,
                cursor: !newName.trim() || createTenantMutation.isPending ? 'not-allowed' : 'pointer',
                opacity: !newName.trim() || createTenantMutation.isPending ? 0.6 : 1,
              }}
            >
              <Plus size={14} />
              {createTenantMutation.isPending ? 'Tworzenie...' : 'Utwórz firmę'}
            </button>
          </div>

          {isLoading ? (
            <div style={{ color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
              {tenants?.map((tenant) => (
                <button
                  key={tenant.id}
                  onClick={() => setSelectedTenantId(tenant.id)}
                  style={{
                    display: 'flex', alignItems: 'center', gap: '10px', textAlign: 'left',
                    padding: '12px 14px', borderRadius: '8px', cursor: 'pointer',
                    border: `1px solid ${tenant.id === selectedTenantId ? colors.primary[400] : colors.gray[200]}`,
                    backgroundColor: tenant.id === selectedTenantId ? colors.primary[50] : colors.white,
                  }}
                >
                  <Building2 size={18} color={colors.gray[500]} />
                  <div>
                    <div style={{ fontSize: '14px', fontWeight: 600, color: colors.gray[900] }}>{tenant.name}</div>
                    <div style={{ fontSize: '12px', color: colors.gray[500] }}>{tenant.slug} · {tenant.status}</div>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Selected tenant's flags/plans */}
        <div style={{ flex: 1 }}>
          {!selectedTenant ? (
            <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400] }}>
              Wybierz firmę z listy, aby zarządzać jej modułami.
            </div>
          ) : (
            <>
              {/* Plans */}
              {plans && plans.length > 0 && (
                <div style={{ marginBottom: '20px' }}>
                  <div style={{ fontSize: '13px', fontWeight: 600, color: colors.gray[700], marginBottom: '8px' }}>
                    Zastosuj pakiet
                  </div>
                  <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                    {plans.map((plan) => (
                      <button
                        key={plan.id}
                        onClick={() => applyPlanMutation.mutate(plan.id)}
                        disabled={applyPlanMutation.isPending}
                        style={{
                          display: 'inline-flex', alignItems: 'center', gap: '6px',
                          padding: '8px 14px', fontSize: '13px', fontWeight: 500, borderRadius: '6px',
                          border: `1px solid ${colors.primary[300]}`, backgroundColor: colors.primary[50],
                          color: colors.primary[700], cursor: applyPlanMutation.isPending ? 'wait' : 'pointer',
                        }}
                      >
                        <Package size={14} />
                        {plan.name}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {/* Feature flags */}
              {flagsLoading ? (
                <div style={{ color: colors.gray[500], fontSize: '14px' }}>Ładowanie flag...</div>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {flags?.map((flag) => (
                    <div
                      key={flag.module}
                      style={{
                        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                        padding: '14px 18px', borderRadius: '8px',
                        border: `1px solid ${colors.gray[200]}`, backgroundColor: colors.white,
                      }}
                    >
                      <div style={{ fontSize: '14px', fontWeight: 600, color: colors.gray[900] }}>
                        {moduleLabels[flag.module] ?? flag.module}
                      </div>
                      <button
                        onClick={() => handleToggle(flag.module)}
                        disabled={toggleMutation.isPending}
                        style={{
                          display: 'inline-flex', alignItems: 'center', gap: '6px',
                          padding: '6px 14px', fontSize: '13px', fontWeight: 500, borderRadius: '6px',
                          border: 'none', cursor: toggleMutation.isPending ? 'wait' : 'pointer',
                          backgroundColor: flag.isEnabled ? colors.success[100] : colors.gray[100],
                          color: flag.isEnabled ? colors.success[800] : colors.gray[500],
                        }}
                      >
                        {flag.isEnabled ? <ToggleRight size={18} /> : <ToggleLeft size={18} />}
                        {flag.isEnabled ? 'Włączony' : 'Wyłączony'}
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
