import { useState, useCallback } from 'react';
import { Bell, Plus, RefreshCw, Edit2, Trash2, X } from 'lucide-react';
import {
  useNotificationTemplates,
  useCreateNotificationTemplate,
  useUpdateNotificationTemplate,
  useDeleteNotificationTemplate,
} from '@/api/hooks/useNotifications';
import type {
  NotificationTemplateDto,
  CreateNotificationTemplateRequest,
  UpdateNotificationTemplateRequest,
} from '@/api/types/notification';
import { useIsMobile } from '@/shared';
import { colors } from '@/theme/tokens';

export function NotificationTemplatesConfigPage() {
  const { data: templates, isLoading, error, refetch, isFetching } = useNotificationTemplates();
  const createMutation = useCreateNotificationTemplate();
  const updateMutation = useUpdateNotificationTemplate();
  const deleteMutation = useDeleteNotificationTemplate();
  const mobile = useIsMobile();

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<NotificationTemplateDto | null>(null);

  const handleCreate = useCallback(
    (req: CreateNotificationTemplateRequest) => {
      createMutation.mutate(req, {
        onSuccess: () => { setShowForm(false); createMutation.reset(); },
      });
    },
    [createMutation],
  );

  const handleUpdate = useCallback(
    (id: string, req: UpdateNotificationTemplateRequest) => {
      updateMutation.mutate({ id, ...req }, {
        onSuccess: () => { setEditing(null); setShowForm(false); updateMutation.reset(); },
      });
    },
    [updateMutation],
  );

  const handleDelete = useCallback(
    (id: string) => {
      if (!confirm('Czy na pewno chcesz usunąć ten szablon powiadomienia?')) return;
      deleteMutation.mutate(id);
    },
    [deleteMutation],
  );

  return (
    <div style={{ padding: mobile ? '16px' : '24px 32px', maxWidth: '1100px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600, color: colors.gray[900] }}>Szablony powiadomień</h1>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={() => refetch()} style={iconBtnStyle} title="Odśwież">
            <RefreshCw size={16} style={isFetching ? { animation: 'spin 1s linear infinite' } : undefined} />
          </button>
          <button onClick={() => { setEditing(null); setShowForm(true); }} style={primaryBtnStyle}>
            <Plus size={16} /> Nowy szablon
          </button>
        </div>
      </div>

      <p style={{ fontSize: '13px', color: colors.gray[500], marginTop: '-12px', marginBottom: '20px' }}>
        Treść powiadomień (e-mail/in-app) wysyłanych przez system — dostosuj wg potrzeb Twojej firmy.
      </p>

      {error && (
        <div style={errorBoxStyle}>
          Błąd ładowania szablonów.
          <button onClick={() => refetch()} style={retryLinkStyle}>Ponów</button>
        </div>
      )}

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[500], fontSize: '14px' }}>Ładowanie...</div>
      ) : !templates || templates.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '48px 0', color: colors.gray[400] }}>
          <Bell size={40} style={{ marginBottom: '12px', opacity: 0.5 }} />
          <div style={{ fontSize: '15px', fontWeight: 500 }}>Brak szablonów</div>
          <div style={{ fontSize: '13px', marginTop: '4px' }}>Dodaj pierwszy szablon klikając „Nowy szablon".</div>
        </div>
      ) : (
        <div style={{ border: `1px solid ${colors.gray[200]}`, borderRadius: '8px', overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
            <thead>
              <tr style={{ backgroundColor: colors.gray[50] }}>
                <Th>Kod</Th>
                <Th>Nazwa</Th>
                <Th>Kategoria</Th>
                <Th>Tytuł</Th>
                <Th style={{ width: '80px' }}></Th>
              </tr>
            </thead>
            <tbody>
              {templates.map((tpl) => (
                <tr key={tpl.id} style={{ borderTop: `1px solid ${colors.gray[200]}` }}>
                  <Td><code style={{ fontSize: '12px', background: colors.gray[100], padding: '2px 6px', borderRadius: '4px' }}>{tpl.code}</code></Td>
                  <Td style={{ fontWeight: 500 }}>{tpl.name}</Td>
                  <Td>{tpl.category}</Td>
                  <Td style={{ maxWidth: '280px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{tpl.titleTemplate}</Td>
                  <Td>
                    <div style={{ display: 'flex', gap: '4px' }}>
                      <button onClick={() => { setEditing(tpl); setShowForm(true); }} style={smallIconBtn} title="Edytuj">
                        <Edit2 size={14} />
                      </button>
                      <button onClick={() => handleDelete(tpl.id)} style={{ ...smallIconBtn, color: colors.danger[600] }} title="Usuń">
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </Td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <TemplateFormModal
          template={editing}
          isPending={editing ? updateMutation.isPending : createMutation.isPending}
          error={editing ? updateMutation.error : createMutation.error}
          onSubmit={(data) => {
            if (editing) handleUpdate(editing.id, data);
            else handleCreate(data as CreateNotificationTemplateRequest);
          }}
          onClose={() => { setShowForm(false); setEditing(null); createMutation.reset(); updateMutation.reset(); }}
        />
      )}
    </div>
  );
}

function TemplateFormModal({ template, isPending, error, onSubmit, onClose }: {
  template: NotificationTemplateDto | null;
  isPending: boolean;
  error: Error | null;
  onSubmit: (data: CreateNotificationTemplateRequest) => void;
  onClose: () => void;
}) {
  const [code, setCode] = useState(template?.code ?? '');
  const [name, setName] = useState(template?.name ?? '');
  const [category, setCategory] = useState(template?.category ?? '');
  const [titleTemplate, setTitleTemplate] = useState(template?.titleTemplate ?? '');
  const [bodyTemplate, setBodyTemplate] = useState(template?.bodyTemplate ?? '');
  const mobile = useIsMobile();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ code, name, category, titleTemplate, bodyTemplate });
  };

  return (
    <div style={overlayStyle}>
      <div style={modalStyle}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '18px', fontWeight: 600, color: colors.gray[900] }}>
            {template ? 'Edytuj szablon' : 'Nowy szablon powiadomienia'}
          </h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500] }}><X size={20} /></button>
        </div>

        {error && <div style={{ ...errorBoxStyle, marginBottom: '12px' }}>Błąd: {error.message}</div>}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: mobile ? '1fr' : '1fr 1fr', gap: '12px' }}>
            <label style={labelStyle}>
              Kod
              <input value={code} onChange={(e) => setCode(e.target.value)} required disabled={!!template} style={inputStyle} placeholder="np. LEAVE_APPROVED" />
            </label>
            <label style={labelStyle}>
              Kategoria
              <input value={category} onChange={(e) => setCategory(e.target.value)} required style={inputStyle} placeholder="np. leave" />
            </label>
          </div>
          <label style={labelStyle}>
            Nazwa
            <input value={name} onChange={(e) => setName(e.target.value)} required style={inputStyle} placeholder="np. Urlop zaakceptowany" />
          </label>
          <label style={labelStyle}>
            Szablon tytułu
            <input value={titleTemplate} onChange={(e) => setTitleTemplate(e.target.value)} required style={inputStyle} placeholder="np. Twój wniosek urlopowy został zaakceptowany" />
          </label>
          <label style={labelStyle}>
            Szablon treści
            <textarea value={bodyTemplate} onChange={(e) => setBodyTemplate(e.target.value)} required rows={4} style={{ ...inputStyle, resize: 'vertical', fontFamily: 'inherit' }} />
          </label>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
            <button type="button" onClick={onClose} style={cancelBtnStyle}>Anuluj</button>
            <button type="submit" disabled={isPending} style={primaryBtnStyle}>
              {isPending ? 'Zapisywanie...' : template ? 'Zapisz' : 'Utwórz'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Th({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <th style={{ padding: '10px 14px', textAlign: 'left', fontSize: '12px', fontWeight: 600, color: colors.gray[500], whiteSpace: 'nowrap', ...style }}>{children}</th>;
}
function Td({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return <td style={{ padding: '10px 14px', color: colors.gray[900], ...style }}>{children}</td>;
}

const iconBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '34px', height: '34px', border: `1px solid ${colors.gray[300]}`, borderRadius: '6px',
  background: colors.white, color: colors.gray[700], cursor: 'pointer',
};
const primaryBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', gap: '6px',
  padding: '7px 16px', fontSize: '13px', fontWeight: 600,
  color: colors.white, backgroundColor: colors.primary[600], border: 'none', borderRadius: '6px', cursor: 'pointer',
};
const cancelBtnStyle: React.CSSProperties = {
  padding: '7px 16px', fontSize: '13px', fontWeight: 500,
  color: colors.gray[700], backgroundColor: colors.white, border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', cursor: 'pointer',
};
const smallIconBtn: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
  width: '28px', height: '28px', background: 'none', border: 'none', cursor: 'pointer', color: colors.gray[500], borderRadius: '4px',
};
const errorBoxStyle: React.CSSProperties = {
  padding: '12px 16px', marginBottom: '16px', borderRadius: '8px',
  backgroundColor: colors.danger[50], border: `1px solid ${colors.danger[200]}`, color: colors.danger[800], fontSize: '13px',
};
const retryLinkStyle: React.CSSProperties = {
  marginLeft: '8px', color: colors.primary[600], background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline', fontSize: '13px',
};
const overlayStyle: React.CSSProperties = {
  position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100,
};
const modalStyle: React.CSSProperties = {
  backgroundColor: colors.white, borderRadius: '12px', padding: '24px', width: '100%', maxWidth: '560px', maxHeight: '90vh', overflow: 'auto',
  boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
};
const labelStyle: React.CSSProperties = {
  display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '13px', fontWeight: 500, color: colors.gray[700],
};
const inputStyle: React.CSSProperties = {
  width: '100%', padding: '8px 12px', fontSize: '14px',
  border: `1px solid ${colors.gray[300]}`, borderRadius: '6px', boxSizing: 'border-box',
};
