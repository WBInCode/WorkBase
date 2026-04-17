import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Plus, RefreshCw, ChevronLeft, ChevronRight, User, X, ExternalLink } from 'lucide-react';
import {
  useEmployees,
  useEmployeeDetail,
  useCreateEmployee,
  useOrgUnitTree,
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

const PAGE_SIZE = 20;

const statusLabels: Record<EmployeeStatus, string> = {
  Active: 'Aktywny',
  Inactive: 'Nieaktywny',
  OnLeave: 'Na urlopie',
};

const statusColors: Record<EmployeeStatus, { bg: string; text: string }> = {
  Active: { bg: '#dcfce7', text: '#166534' },
  Inactive: { bg: '#f3f4f6', text: '#6b7280' },
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
    <div style={{ padding: '24px 32px', maxWidth: '1200px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: '#111827' }}>Pracownicy</h1>
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
              color: '#fff',
              backgroundColor: '#2563eb',
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
            style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)', color: '#9ca3af' }}
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
              border: '1px solid #d1d5db',
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
      <div style={{ display: 'flex', gap: '20px' }}>
        {/* Table */}
        <div style={{ flex: 1, minWidth: 0 }}>
          {error && (
            <div style={{ padding: '16px', backgroundColor: '#fef2f2', border: '1px solid #fecaca', borderRadius: '8px', color: '#dc2626', fontSize: '14px', marginBottom: '12px' }}>
              Błąd ładowania listy pracowników.
              <button onClick={() => refetch()} style={{ marginLeft: '12px', color: '#2563eb', background: 'none', border: 'none', cursor: 'pointer', fontSize: '14px', textDecoration: 'underline' }}>
                Ponów
              </button>
            </div>
          )}

          {isLoading ? (
            <div style={{ textAlign: 'center', padding: '48px 0', color: '#6b7280', fontSize: '14px' }}>
              Ładowanie...
            </div>
          ) : !data || data.items.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '48px 0', color: '#9ca3af' }}>
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
              <div style={{ border: '1px solid #e5e7eb', borderRadius: '8px', overflow: 'hidden' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
                  <thead>
                    <tr style={{ backgroundColor: '#f9fafb' }}>
                      <Th>Imię i nazwisko</Th>
                      <Th>Email</Th>
                      <Th>Nr pracownika</Th>
                      <Th>Data zatrudnienia</Th>
                      <Th>Status</Th>
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
                  color: '#6b7280',
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
        backgroundColor: isSelected ? '#eff6ff' : undefined,
        borderTop: '1px solid #e5e7eb',
        transition: 'background-color 0.1s',
      }}
      onMouseEnter={(e) => { if (!isSelected) e.currentTarget.style.backgroundColor = '#f9fafb'; }}
      onMouseLeave={(e) => { if (!isSelected) e.currentTarget.style.backgroundColor = ''; }}
    >
      <Td style={{ fontWeight: 500, color: '#111827' }}>
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
  return (
    <aside
      style={{
        width: '340px',
        flexShrink: 0,
        border: '1px solid #e5e7eb',
        borderRadius: '8px',
        backgroundColor: '#fff',
        overflow: 'auto',
      }}
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '14px 16px',
          borderBottom: '1px solid #e5e7eb',
        }}
      >
        <span style={{ fontSize: '14px', fontWeight: 600, color: '#111827' }}>Szczegóły pracownika</span>
        <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#6b7280', padding: '2px', display: 'inline-flex' }} aria-label="Zamknij">
          <X size={16} />
        </button>
      </div>

      {isLoading ? (
        <div style={{ padding: '32px 16px', textAlign: 'center', color: '#9ca3af', fontSize: '13px' }}>
          Ładowanie...
        </div>
      ) : !detail ? (
        <div style={{ padding: '32px 16px', textAlign: 'center', color: '#9ca3af', fontSize: '13px' }}>
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
              <div style={{ fontWeight: 600, fontSize: '15px', color: '#111827' }}>
                {detail.firstName} {detail.lastName}
              </div>
              <div style={{ color: '#6b7280', marginTop: '2px' }}>{detail.email}</div>
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
          {detail.supervisor && (
            <DetailField label="Przełożony">
              {detail.supervisor.firstName} {detail.supervisor.lastName}
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
              color: '#fff',
              backgroundColor: '#2563eb',
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
              <div style={{ fontWeight: 600, color: '#374151', marginBottom: '8px' }}>Przypisania</div>
              {detail.assignments.map((a) => (
                <div
                  key={a.id}
                  style={{
                    padding: '8px 10px',
                    borderRadius: '6px',
                    backgroundColor: '#f9fafb',
                    marginBottom: '6px',
                    border: '1px solid #f3f4f6',
                  }}
                >
                  <div style={{ fontWeight: 500, color: '#111827' }}>{a.organizationUnitName}</div>
                  <div style={{ color: '#6b7280', marginTop: '2px' }}>
                    {a.positionName}
                    {a.isPrimary && (
                      <span
                        style={{
                          marginLeft: '6px',
                          padding: '1px 6px',
                          borderRadius: '4px',
                          fontSize: '11px',
                          backgroundColor: '#dbeafe',
                          color: '#1d4ed8',
                        }}
                      >
                        główne
                      </span>
                    )}
                  </div>
                  <div style={{ color: '#9ca3af', marginTop: '2px', fontSize: '12px' }}>
                    od {formatDate(a.startDate)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </aside>
  );
}

function DetailField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: '10px' }}>
      <div style={{ color: '#9ca3af', fontSize: '12px', marginBottom: '2px' }}>{label}</div>
      <div style={{ color: '#111827' }}>{children}</div>
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
        color: '#6b7280',
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
    <td style={{ padding: '10px 14px', color: '#374151', whiteSpace: 'nowrap', ...style }}>
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
  backgroundColor: '#fff',
  border: '1px solid #d1d5db',
  borderRadius: '6px',
  cursor: 'pointer',
  color: '#374151',
};

const selectStyle: React.CSSProperties = {
  padding: '8px 12px',
  fontSize: '14px',
  border: '1px solid #d1d5db',
  borderRadius: '6px',
  outline: 'none',
  backgroundColor: '#fff',
  color: '#374151',
  minWidth: '160px',
};

function pageBtnStyle(disabled: boolean): React.CSSProperties {
  return {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '32px',
    height: '32px',
    backgroundColor: '#fff',
    border: '1px solid #d1d5db',
    borderRadius: '6px',
    cursor: disabled ? 'not-allowed' : 'pointer',
    color: disabled ? '#d1d5db' : '#374151',
    opacity: disabled ? 0.5 : 1,
  };
}
