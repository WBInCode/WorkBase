import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Plus, RefreshCw, ChevronLeft, ChevronRight, User, X, ExternalLink, UserMinus } from 'lucide-react';
import {
  useEmployees,
  useEmployeeDetail,
  useCreateEmployee,
  useAssignEmployee,
  useDeactivateEmployee,
  useOrgUnitTree,
  usePositions,
  type EmployeesFilter,
} from '@/api/hooks/useOrganization';
import type {
  EmployeeDto,
  EmployeeDetailDto,
  EmployeeStatus,
  CreateEmployeeRequest,
  OrganizationUnitTreeNode,
} from '@/api/types/organization';
import { EmployeeForm } from '@/components/Employees/EmployeeForm';
import { ApiError } from '@/api/client';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';
import { useCurrentUser } from '@/api/hooks/useIam';

const PAGE_SIZE = 20;

const statusLabels: Record<EmployeeStatus, string> = {
  Active: 'Aktywny',
  Inactive: 'Nieaktywny',
  OnLeave: 'Na urlopie',
};

const statusColors: Record<EmployeeStatus, { bg: string; text: string }> = {
  Active: { bg: colors.success[100], text: colors.success[800] },
  Inactive: { bg: colors.gray[100], text: colors.gray[500] },
  OnLeave: { bg: '#fef9c3', text: '#854d0e' },
};

export function EmployeeListPage() {
  const [filter, setFilter] = useState<EmployeesFilter>({
    page: 1,
    pageSize: PAGE_SIZE,
  });
  const [searchInput, setSearchInput] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const { data, isLoading, error, refetch, isFetching } = useEmployees(filter);
  const { data: tree } = useOrgUnitTree();
  const detail = useEmployeeDetail(selectedId);
  const createMutation = useCreateEmployee();

  const flatUnits = flattenTree(tree ?? []);
  const mobile = useIsMobile();

  const handleSearch = useCallback(() => {
    setFilter((f) => ({ ...f, search: searchInput.trim() || undefined, page: 1 }));
  }, [searchInput]);

  const handleCreate = useCallback(
    (req: CreateEmployeeRequest) => {
      createMutation.mutate(req, {
        onSuccess: () => {
          setShowForm(false);
          createMutation.reset();
        },
      });
    },
    [createMutation],
  );

  const totalPages = data ? Math.ceil(data.totalCount / filter.pageSize) : 0;

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '1200px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: colors.gray[900] }}>Pracownicy</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={() => refetch()} style={iconBtnStyle} title="Odśwież" aria-label="Odśwież">
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button
            onClick={() => setShowForm(true)}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 16px',
              fontSize: '14px',
              fontWeight: 500,
              color: colors.white,
              backgroundColor: colors.primary[600],
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
            }}
          >
            <Plus size={16} />
            Nowy pracownik
          </button>
        </div>
      </div>

      {/* Filters */}
      <div style={{ display: 'flex', gap: '12px', marginBottom: '16px', flexWrap: 'wrap' }}>
        {/* Search */}
        <div style={{ position: 'relative', flex: '1 1 240px', maxWidth: '320px' }}>
          <Search
            size={16}
            style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)', color: colors.gray[400] }}
          />
          <input
            type="text"
            placeholder="Szukaj po imieniu, nazwisku, email..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
            style={{
              width: '100%',
              padding: '8px 12px 8px 34px',
              fontSize: '14px',
              border: `1px solid ${colors.gray[300]}`,
              borderRadius: '6px',
              outline: 'none',
              boxSizing: 'border-box',
            }}
          />
        </div>

        {/* Unit filter */}
        <select
          value={filter.organizationUnitId ?? ''}
          onChange={(e) =>
            setFilter((f) => ({
              ...f,
              organizationUnitId: e.target.value || undefined,
              page: 1,
            }))
          }
          style={selectStyle}
        >
          <option value="">Wszystkie jednostki</option>
          {flatUnits.map((u) => (
            <option key={u.id} value={u.id}>
              {u.prefix}{u.name}
            </option>
          ))}
        </select>

        {/* Status filter */}
        <select
          value={filter.status ?? ''}
          onChange={(e) =>
            setFilter((f) => ({
              ...f,
              status: (e.target.value as EmployeeStatus) || undefined,
              page: 1,
            }))
          }
          style={selectStyle}
        >
          <option value="">Wszystkie statusy</option>
          <option value="Active">Aktywny</option>
          <option value="Inactive">Nieaktywny</option>
          <option value="OnLeave">Na urlopie</option>
        </select>

        {/* Clear filters */}
        {(filter.search || filter.organizationUnitId || filter.status) && (
          <button
            onClick={() => {
              setSearchInput('');
              setFilter({ page: 1, pageSize: PAGE_SIZE });
            }}
            style={{
              ...iconBtnStyle,
              fontSize: '13px',
              display: 'inline-flex',
              alignItems: 'center',
              gap: '4px',
              padding: '8px 12px',
            }}
          >
            <X size={14} />
            Wyczyść
          </button>
        )}
      </div>

      {/* Content area with optional detail panel */}
      <div style={{ display: 'flex', flexDirection: mobile ? 'column' : 'row', gap: '20px' }}>
        {/* Table */}
        <div style={{ flex: 1, minWidth: 0 }}>
          {error && (
            <div style={{ padding: '16px', backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, borderRadius: '8px', color: colors.danger[600], fontSize: '14px', marginBottom: '12px' }}>
              Błąd ładowania listy pracowników.
              <button onClick={() => refetch()} style={{ marginLeft: '12px', color: colors.primary[600], background: 'none', border: 'none', cursor: 'pointer', fontSize: '14px', textDecoration: 'underline' }}>
                Ponów
              </button>
            </div>
          )}

          {isLoading ? (
            <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>
              Ładowanie...
            </div>
          ) : !data || data.items.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400] }}>
              <User size={40} style={{ marginBottom: '12px', opacity: 0.5 }} />
              <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak pracowników</div>
              <div style={{ fontSize: '13px', marginTop: '4px' }}>
                {filter.search || filter.organizationUnitId || filter.status
                  ? 'Spróbuj zmienić filtry.'
                  : 'Dodaj pierwszego pracownika klikając „Nowy pracownik".'}
              </div>
            </div>
          ) : (
            <>
              {/* Table */}
              <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '8px', overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
                  <thead>
                    <tr style={{ backgroundColor: colors.gray[50] }}>
                      <Th>Imię i nazwisko</Th>
                      <Th>Email</Th>
                      <Th>Nr pracownika</Th>
                      <Th>Data zatrudnienia</Th>
                      <Th>Status</Th>
                      <Th>Akcje</Th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.items.map((emp) => (
                      <EmployeeRow
                        key={emp.id}
                        employee={emp}
                        isSelected={emp.id === selectedId}
                        onClick={() => setSelectedId(emp.id === selectedId ? null : emp.id)}
                      />
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  marginTop: '12px',
                  fontSize: '13px',
                  color: colors.gray[500],
                }}
              >
                <span>
                  {data.totalCount} pracownik{data.totalCount === 1 ? '' : data.totalCount < 5 ? 'ów' : 'ów'} • strona{' '}
                  {filter.page} z {totalPages || 1}
                </span>
                <div style={{ display: 'flex', gap: '4px' }}>
                  <button
                    disabled={filter.page <= 1}
                    onClick={() => setFilter((f) => ({ ...f, page: f.page - 1 }))}
                    style={pageBtnStyle(filter.page <= 1)}
                    aria-label="Poprzednia strona"
                  >
                    <ChevronLeft size={16} />
                  </button>
                  <button
                    disabled={filter.page >= totalPages}
                    onClick={() => setFilter((f) => ({ ...f, page: f.page + 1 }))}
                    style={pageBtnStyle(filter.page >= totalPages)}
                    aria-label="Następna strona"
                  >
                    <ChevronRight size={16} />
                  </button>
                </div>
              </div>
            </>
          )}
        </div>

        {/* Detail panel */}
        {selectedId && (
          <EmployeeDetailPanel
            detail={detail.data ?? null}
            isLoading={detail.isLoading}
            onClose={() => setSelectedId(null)}
          />
        )}
      </div>

      {/* Create form modal */}
      {showForm && (
        <EmployeeForm
          onSubmit={handleCreate}
          onClose={() => { setShowForm(false); createMutation.reset(); }}
          isSubmitting={createMutation.isPending}
          error={
            createMutation.error instanceof ApiError
              ? createMutation.error.message
              : createMutation.error
                ? 'Wystąpił nieoczekiwany błąd.'
                : null
          }
        />
      )}
    </div>
  );
}

/* ---- Sub-components ---- */

function EmployeeRow({ employee, isSelected, onClick }: { employee: EmployeeDto; isSelected: boolean; onClick: () => void }) {
  const navigate = useNavigate();
  const color = statusColors[employee.status];
  return (
    <tr
      onClick={onClick}
      onDoubleClick={() => navigate(`/org/employees/${employee.id}`)}
      style={{
        cursor: 'pointer',
        backgroundColor: isSelected ? colors.primary[50] : undefined,
        borderTop: `1px solid ${colors.gray[200]}`,
        transition: 'background-color 0.1s',
      }}
      onMouseEnter={(e) => { if (!isSelected) e.currentTarget.style.backgroundColor = colors.gray[50]; }}
      onMouseLeave={(e) => { if (!isSelected) e.currentTarget.style.backgroundColor = ''; }}
    >
      <Td style={{ fontWeight: 500, color: colors.gray[900] }}>
        {employee.firstName} {employee.lastName}
      </Td>
      <Td>{employee.email}</Td>
      <Td>{employee.employeeNumber ?? '—'}</Td>
      <Td>{formatDate(employee.hireDate)}</Td>
      <Td>
        <span
          style={{
            padding: '2px 8px',
            borderRadius: '9999px',
            fontSize: '12px',
            fontWeight: 500,
            backgroundColor: color.bg,
            color: color.text,
          }}
        >
          {statusLabels[employee.status]}
        </span>
      </Td>
      <Td>
        <button
          onClick={(e) => {
            e.stopPropagation();
            navigate(`/org/employees/${employee.id}`);
          }}
          style={{
            padding: '4px 10px',
            fontSize: '12px',
            fontWeight: 600,
            color: colors.white,
            background: colors.primary[600],
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
          }}
        >
          Otwórz kartę
        </button>
      </Td>
    </tr>
  );
}

function EmployeeDetailPanel({
  detail,
  isLoading,
  onClose,
}: {
  detail: EmployeeDetailDto | null;
  isLoading: boolean;
  onClose: () => void;
}) {
  const navigate = useNavigate();
  const [showAssignForm, setShowAssignForm] = useState(false);
  const [showDeactivateConfirm, setShowDeactivateConfirm] = useState(false);
  const deactivate = useDeactivateEmployee();
  const mobile = useIsMobile();
  // isAdmin is sourced from the app's own Role/Permission data, not the Keycloak "roles" claim
  // — see docs/AUDIT-KNOWLEDGE-MAP.md (role system consistency).
  const { data: currentUser } = useCurrentUser();
  const isAdmin = !!currentUser?.isAdmin;
  return (
    <aside
      style={{
        width: mobile ? '100%' : '340px',
        flexShrink: 0,
        border: `1px solid ${colors.gray[200]}`,
        borderRadius: '8px',
        backgroundColor: colors.white,
        overflow: 'auto',
      }}
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '14px 16px',
          borderBottom: `1px solid ${colors.gray[200]}`,
        }}
      >
        <span style={{ fontSize: '14px', fontWeight: 600, color: colors.gray[900] }}>Szczegóły pracownika</span>
        <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500], padding: '2px', display: 'inline-flex' }} aria-label="Zamknij">
          <X size={16} />
        </button>
      </div>

      {isLoading ? (
        <div style={{ padding: '32px 16px', textAlign: 'center', color: colors.gray[400], fontSize: '13px' }}>
          Ładowanie...
        </div>
      ) : !detail ? (
        <div style={{ padding: '32px 16px', textAlign: 'center', color: colors.gray[400], fontSize: '13px' }}>
          Nie znaleziono danych.
        </div>
      ) : (
        <div style={{ padding: '16px', fontSize: '13px' }}>
          {/* Basic info */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '16px' }}>
            <div
              style={{
                width: '48px',
                height: '48px',
                borderRadius: '50%',
                backgroundColor: '#e0e7ff',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#4338ca',
                fontWeight: 600,
                fontSize: '16px',
                flexShrink: 0,
              }}
            >
              {detail.firstName.charAt(0)}{detail.lastName.charAt(0)}
            </div>
            <div>
              <div style={{ fontWeight: 600, fontSize: '15px', color: colors.gray[900] }}>
                {detail.firstName} {detail.lastName}
              </div>
              <div style={{ color: colors.gray[500], marginTop: '2px' }}>{detail.email}</div>
            </div>
          </div>

          <DetailField label="Status">
            <span
              style={{
                padding: '2px 8px',
                borderRadius: '9999px',
                fontSize: '12px',
                fontWeight: 500,
                backgroundColor: statusColors[detail.status].bg,
                color: statusColors[detail.status].text,
              }}
            >
              {statusLabels[detail.status]}
            </span>
          </DetailField>
          {detail.employeeNumber && <DetailField label="Nr pracownika">{detail.employeeNumber}</DetailField>}
          <DetailField label="Data zatrudnienia">{formatDate(detail.hireDate)}</DetailField>
          {detail.terminationDate && (
            <DetailField label="Data zakończenia">{formatDate(detail.terminationDate)}</DetailField>
          )}

          {/* Supervisor */}
          {detail.supervisor ? (
            <DetailField label="Przełożony">
              {detail.supervisor.firstName} {detail.supervisor.lastName}
            </DetailField>
          ) : (
            <DetailField label="Przełożony">
              <span style={{ color: colors.warning[700], fontSize: '13px' }} title="Bez przełożonego pracownik nie będzie mógł składać wniosków wymagających akceptacji.">
                Brak
              </span>
            </DetailField>
          )}

          {/* Card 360 link */}
          <button
            onClick={() => navigate(`/org/employees/${detail.id}`)}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 14px',
              fontSize: '13px',
              fontWeight: 500,
              color: colors.white,
              backgroundColor: colors.primary[600],
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
              width: '100%',
              justifyContent: 'center',
              marginBottom: '16px',
            }}
          >
            <ExternalLink size={14} />
            Karta pracownika 360°
          </button>

          {/* Assignments */}
          {detail.assignments.length > 0 && (
            <div style={{ marginTop: '16px' }}>
              <div style={{ fontWeight: 600, color: colors.gray[700], marginBottom: '8px' }}>Przypisania</div>
              {detail.assignments.map((a) => (
                <div
                  key={a.id}
                  style={{
                    padding: '8px 10px',
                    borderRadius: '6px',
                    backgroundColor: colors.gray[50],
                    marginBottom: '6px',
                    border: `1px solid ${colors.gray[100]}`,
                  }}
                >
                  <div style={{ fontWeight: 500, color: colors.gray[900] }}>{a.organizationUnitName}</div>
                  <div style={{ color: colors.gray[500], marginTop: '2px' }}>
                    {a.positionName}
                    {a.isPrimary && (
                      <span
                        style={{
                          marginLeft: '6px',
                          padding: '1px 6px',
                          borderRadius: '4px',
                          fontSize: '11px',
                          backgroundColor: colors.primary[100],
                          color: colors.primary[700],
                        }}
                      >
                        główne
                      </span>
                    )}
                  </div>
                  <div style={{ color: colors.gray[400], marginTop: '2px', fontSize: '12px' }}>
                    od {formatDate(a.startDate)}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Assign to unit button / form */}
          {!showAssignForm ? (
            <button
              onClick={() => setShowAssignForm(true)}
              style={{
                display: 'flex', alignItems: 'center', gap: '6px',
                padding: '8px 14px', fontSize: '13px', fontWeight: 500,
                color: colors.primary[500], backgroundColor: colors.primary[50],
                border: `1px solid ${colors.primary[200]}`, borderRadius: '6px',
                cursor: 'pointer', marginTop: '16px', width: '100%', justifyContent: 'center',
              }}
            >
              <Plus size={14} />
              Przypisz do jednostki
            </button>
          ) : (
            <AssignEmployeeForm
              employeeId={detail.id}
              onClose={() => setShowAssignForm(false)}
            />
          )}

          {/* Deactivate button — admin only, active employees only */}
          {isAdmin && detail.status === 'Active' && (
            <div style={{ marginTop: '16px', paddingTop: '12px', borderTop: `1px solid ${colors.danger[100]}` }}>
              {!showDeactivateConfirm ? (
                <button
                  onClick={() => setShowDeactivateConfirm(true)}
                  style={{
                    display: 'flex', alignItems: 'center', gap: '6px',
                    padding: '8px 14px', fontSize: '13px', fontWeight: 600,
                    color: colors.danger[600], backgroundColor: colors.danger[50],
                    border: `1px solid ${colors.danger[200]}`, borderRadius: '6px',
                    cursor: 'pointer', width: '100%', justifyContent: 'center',
                  }}
                >
                  <UserMinus size={14} />
                  Dezaktywuj pracownika
                </button>
              ) : (
                <div style={{ padding: '12px', backgroundColor: colors.danger[50], borderRadius: '8px', border: `1px solid ${colors.danger[200]}` }}>
                  <p style={{ margin: '0 0 10px', fontSize: '13px', color: colors.danger[800], fontWeight: 500 }}>
                    Dezaktywować <strong>{detail.firstName} {detail.lastName}</strong>?
                  </p>
                  <div style={{ display: 'flex', gap: '6px' }}>
                    <button
                      onClick={async () => {
                        await deactivate.mutateAsync(detail.id);
                        setShowDeactivateConfirm(false);
                        onClose();
                      }}
                      disabled={deactivate.isPending}
                      style={{
                        flex: 1, padding: '7px', fontSize: '12px', fontWeight: 600,
                        color: colors.white, backgroundColor: colors.danger[600],
                        border: 'none', borderRadius: '6px', cursor: 'pointer',
                        opacity: deactivate.isPending ? 0.6 : 1,
                      }}
                    >
                      {deactivate.isPending ? 'Dezaktywowanie...' : 'Tak, dezaktywuj'}
                    </button>
                    <button
                      onClick={() => setShowDeactivateConfirm(false)}
                      style={{
                        padding: '7px 14px', fontSize: '12px', fontWeight: 500,
                        color: colors.gray[700], backgroundColor: colors.white,
                        border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', cursor: 'pointer',
                      }}
                    >
                      Anuluj
                    </button>
                  </div>
                  {deactivate.error && (
                    <p style={{ margin: '6px 0 0', fontSize: '12px', color: colors.danger[600] }}>
                      {(deactivate.error as Error)?.message || 'Wystąpił błąd.'}
                    </p>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </aside>
  );
}

function flattenUnits(nodes: OrganizationUnitTreeNode[], prefix = ''): { id: string; label: string }[] {
  const result: { id: string; label: string }[] = [];
  for (const node of nodes) {
    result.push({ id: node.id, label: prefix + node.name });
    result.push(...flattenUnits(node.children, prefix + node.name + ' › '));
  }
  return result;
}

function AssignEmployeeForm({ employeeId, onClose }: { employeeId: string; onClose: () => void }) {
  const { data: tree = [] } = useOrgUnitTree();
  const { data: positions = [] } = usePositions();
  const assignMutation = useAssignEmployee();

  const [unitId, setUnitId] = useState('');
  const [positionId, setPositionId] = useState('');
  const [isPrimary, setIsPrimary] = useState(true);

  const flatUnits = flattenUnits(tree);

  const handleSubmit = () => {
    if (!unitId || !positionId) return;
    assignMutation.mutate({
      employeeId,
      organizationUnitId: unitId,
      positionId,
      isPrimary,
      startDate: new Date().toISOString(),
    }, { onSuccess: () => onClose() });
  };

  const fieldStyle: React.CSSProperties = {
    width: '100%', padding: '7px 10px', fontSize: '13px',
    border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', boxSizing: 'border-box',
  };

  return (
    <div style={{ marginTop: '16px', padding: '12px', backgroundColor: colors.gray[50], borderRadius: '8px', border: `1px solid ${colors.gray[200]}` }}>
      <div style={{ fontWeight: 600, color: colors.gray[700], marginBottom: '10px', fontSize: '13px' }}>Przypisz do jednostki</div>

      <div style={{ marginBottom: '8px' }}>
        <label style={{ display: 'block', fontSize: '12px', color: colors.gray[500], marginBottom: '3px' }}>Jednostka</label>
        <select value={unitId} onChange={(e) => setUnitId(e.target.value)} style={fieldStyle}>
          <option value="">Wybierz...</option>
          {flatUnits.map((u) => (
            <option key={u.id} value={u.id}>{u.label}</option>
          ))}
        </select>
      </div>

      <div style={{ marginBottom: '8px' }}>
        <label style={{ display: 'block', fontSize: '12px', color: colors.gray[500], marginBottom: '3px' }}>Stanowisko</label>
        <select value={positionId} onChange={(e) => setPositionId(e.target.value)} style={fieldStyle}>
          <option value="">Wybierz...</option>
          {positions.filter((p) => p.isActive).map((p) => (
            <option key={p.id} value={p.id}>{p.name}</option>
          ))}
        </select>
      </div>

      <div style={{ marginBottom: '10px' }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px', color: colors.gray[700], cursor: 'pointer' }}>
          <input type="checkbox" checked={isPrimary} onChange={(e) => setIsPrimary(e.target.checked)} />
          Przypisanie główne
        </label>
      </div>

      {assignMutation.error && (
        <div style={{ color: colors.danger[600], fontSize: '12px', marginBottom: '8px' }}>
          {(assignMutation.error as Error)?.message || 'Wystąpił błąd'}
        </div>
      )}

      <div style={{ display: 'flex', gap: '6px' }}>
        <button
          onClick={handleSubmit}
          disabled={!unitId || !positionId || assignMutation.isPending}
          style={{
            flex: 1, padding: '7px', fontSize: '13px', fontWeight: 600,
            color: colors.white, backgroundColor: (!unitId || !positionId) ? colors.primary[300] : colors.primary[500],
            border: 'none', borderRadius: '6px', cursor: 'pointer',
          }}
        >
          {assignMutation.isPending ? 'Zapisywanie...' : 'Zapisz'}
        </button>
        <button
          onClick={onClose}
          style={{
            padding: '7px 14px', fontSize: '13px', fontWeight: 500,
            color: colors.gray[700], backgroundColor: colors.white,
            border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', cursor: 'pointer',
          }}
        >
          Anuluj
        </button>
      </div>
    </div>
  );
}

function DetailField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: '10px' }}>
      <div style={{ color: colors.gray[400], fontSize: '12px', marginBottom: '2px' }}>{label}</div>
      <div style={{ color: colors.gray[900] }}>{children}</div>
    </div>
  );
}

function Th({ children }: { children: React.ReactNode }) {
  return (
    <th
      style={{
        padding: '10px 14px',
        textAlign: 'left',
        fontWeight: 500,
        color: colors.gray[500],
        fontSize: '13px',
        whiteSpace: 'nowrap',
      }}
    >
      {children}
    </th>
  );
}

function Td({ children, style }: { children: React.ReactNode; style?: React.CSSProperties }) {
  return (
    <td style={{ padding: '10px 14px', color: colors.gray[700], whiteSpace: 'nowrap', ...style }}>
      {children}
    </td>
  );
}

/* ---- Helpers ---- */

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

interface FlatUnit {
  id: string;
  name: string;
  prefix: string;
}

function flattenTree(nodes: OrganizationUnitTreeNode[], depth = 0): FlatUnit[] {
  const result: FlatUnit[] = [];
  for (const n of nodes) {
    result.push({ id: n.id, name: n.name, prefix: '\u00A0'.repeat(depth * 3) });
    result.push(...flattenTree(n.children, depth + 1));
  }
  return result;
}

/* ---- Style helpers ---- */

const iconBtnStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  width: '36px',
  height: '36px',
  backgroundColor: colors.white,
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px',
  cursor: 'pointer',
  color: colors.gray[700],
};

const selectStyle: React.CSSProperties = {
  padding: '8px 12px',
  fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`,
  borderRadius: '6px',
  outline: 'none',
  backgroundColor: colors.white,
  color: colors.gray[700],
  minWidth: '160px',
};

function pageBtnStyle(disabled: boolean): React.CSSProperties {
  return {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '32px',
    height: '32px',
    backgroundColor: colors.white,
    border: `1px solid ${colors.gray[300]}`,
    borderRadius: '6px',
    cursor: disabled ? 'not-allowed' : 'pointer',
    color: disabled ? colors.gray[300] : colors.gray[700],
    opacity: disabled ? 0.5 : 1,
  };
}
