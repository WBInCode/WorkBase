import { useState, useMemo, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Send, UserCircle, Clock, AlertTriangle, Paperclip, Download, Upload } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { mapUserClaims } from '@/auth';
import {
  useTasks,
  useTaskStatuses,
  useTaskPriorities,
  useTaskComments,
  useTaskAttachments,
  useChangeTaskStatus,
  useAddTaskComment,
  useUploadTaskAttachment,
  useDownloadTaskAttachment,
} from '@/api/hooks/useTasks';
import { useEmployees } from '@/api/hooks/useOrganization';

export function TaskCardPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const auth = useAuth();
  const user = auth.user ? mapUserClaims(auth.user) : null;

  const { data: tasks = [] } = useTasks();
  const { data: statuses = [] } = useTaskStatuses();
  useTaskPriorities();
  const { data: comments = [] } = useTaskComments(id ?? null);
  const { data: employeesPage } = useEmployees({ page: 1, pageSize: 500 });
  const employees = employeesPage?.items ?? [];

  const task = tasks.find((t) => t.id === id);

  const changeStatusMutation = useChangeTaskStatus(id ?? '');
  const addCommentMutation = useAddTaskComment(id ?? '');
  const { data: attachments = [] } = useTaskAttachments(id ?? null);
  const uploadMutation = useUploadTaskAttachment(id ?? '');
  const downloadMutation = useDownloadTaskAttachment();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [commentText, setCommentText] = useState('');
  const [newStatusId, setNewStatusId] = useState('');

  const employeeMap = useMemo(() => {
    const map = new Map<string, string>();
    for (const e of employees) {
      map.set(e.id, `${e.firstName} ${e.lastName}`);
    }
    return map;
  }, [employees]);

  if (!task) {
    return (
      <div style={{ padding: '24px' }}>
        <button onClick={() => navigate('/tasks')} style={backBtnStyle}>
          <ArrowLeft size={16} /> Powrót
        </button>
        <div style={{ padding: '40px', textAlign: 'center', color: '#6b7280' }}>
          Zadanie nie zostało znalezione.
        </div>
      </div>
    );
  }

  const isOverdue = task.dueDate && new Date(task.dueDate) < new Date() && !task.completedAt;

  const handleChangeStatus = () => {
    if (!newStatusId || !user?.employeeId) return;
    changeStatusMutation.mutate(
      { newStatusId, changedById: user.employeeId },
      { onSuccess: () => setNewStatusId('') },
    );
  };

  const handleAddComment = () => {
    if (!commentText.trim() || !user?.employeeId) return;
    addCommentMutation.mutate(
      { authorId: user.employeeId, content: commentText.trim() },
      { onSuccess: () => setCommentText('') },
    );
  };

  return (
    <div style={{ padding: '24px', maxWidth: '900px' }}>
      {/* Back */}
      <button onClick={() => navigate(-1)} style={backBtnStyle}>
        <ArrowLeft size={16} /> Powrót
      </button>

      {/* Header */}
      <div style={{ marginTop: '16px', marginBottom: '24px' }}>
        <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 700, color: '#111827' }}>
          {task.title}
        </h1>
        {task.description && (
          <p style={{ margin: '8px 0 0', fontSize: '14px', color: '#6b7280', lineHeight: 1.6 }}>
            {task.description}
          </p>
        )}
      </div>

      {/* Info grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', marginBottom: '24px' }}>
        <InfoCard label="Status">
          <Badge label={task.statusName} color={task.statusColor ?? '#6b7280'} />
        </InfoCard>
        <InfoCard label="Priorytet">
          <Badge label={task.priorityName} color={task.priorityColor ?? '#6b7280'} />
        </InfoCard>
        <InfoCard label="Przypisane do">
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', flexWrap: 'wrap' }}>
            <UserCircle size={16} color="#9ca3af" />
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}>
              <span style={{ fontWeight: 500 }}>{employeeMap.get(task.assigneeId) ?? '—'}</span>
              <span style={{ padding: '2px 6px', fontSize: '10px', fontWeight: 600, letterSpacing: '0.3px', textTransform: 'uppercase', backgroundColor: '#fef3c7', color: '#92400e', borderRadius: '999px' }}>
                Główny wykonawca
              </span>
            </span>
            {task.additionalAssigneeIds?.map((id) => (
              <span key={id} style={{ padding: '2px 8px', fontSize: '12px', backgroundColor: '#eef2ff', color: '#4338ca', borderRadius: '999px' }}>
                + {employeeMap.get(id) ?? '—'}
              </span>
            ))}
          </div>
        </InfoCard>
        <InfoCard label="Termin">
          {task.dueDate ? (
            <span style={{ color: isOverdue ? '#dc2626' : '#374151', fontWeight: isOverdue ? 600 : 400 }}>
              {isOverdue && <AlertTriangle size={14} style={{ marginRight: '4px', verticalAlign: 'middle' }} />}
              {new Date(task.dueDate).toLocaleString('pl-PL', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
            </span>
          ) : '—'}
        </InfoCard>
        <InfoCard label="Utworzono">
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <Clock size={14} color="#9ca3af" />
            {new Date(task.createdAt).toLocaleDateString('pl-PL')}
          </div>
        </InfoCard>
        <InfoCard label="Ukończono">
          {task.completedAt ? new Date(task.completedAt).toLocaleDateString('pl-PL') : '—'}
        </InfoCard>
      </div>

      {/* Actions */}
      <div style={{ display: 'flex', gap: '16px', marginBottom: '28px', flexWrap: 'wrap' }}>
        {/* Change status */}
        <div style={actionCardStyle}>
          <div style={{ fontSize: '13px', fontWeight: 600, color: '#374151', marginBottom: '8px' }}>Zmień status</div>
          <div style={{ display: 'flex', gap: '8px' }}>
            <select value={newStatusId} onChange={(e) => setNewStatusId(e.target.value)} style={selectStyle}>
              <option value="">— wybierz —</option>
              {statuses.filter((s) => s.id !== task.statusId).map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            <button onClick={handleChangeStatus} disabled={!newStatusId || changeStatusMutation.isPending} style={actionBtnStyle}>
              {changeStatusMutation.isPending ? '...' : 'Zmień'}
            </button>
          </div>
          {changeStatusMutation.error && (
            <div style={errorStyle}>{changeStatusMutation.error.message}</div>
          )}
        </div>
      </div>

      {/* Attachments */}
      <div style={{ marginBottom: '28px' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '12px' }}>
          <h2 style={{ fontSize: '16px', fontWeight: 600, color: '#111827', margin: 0 }}>
            <Paperclip size={16} style={{ verticalAlign: 'middle', marginRight: '6px' }} />
            Załączniki ({attachments.length})
          </h2>
          <button
            onClick={() => fileInputRef.current?.click()}
            disabled={uploadMutation.isPending}
            style={{
              display: 'inline-flex', alignItems: 'center', gap: '6px',
              padding: '6px 12px', fontSize: '13px', fontWeight: 500,
              color: '#2563eb', backgroundColor: '#eff6ff', border: '1px solid #bfdbfe',
              borderRadius: '6px', cursor: 'pointer',
            }}
          >
            <Upload size={14} />
            {uploadMutation.isPending ? 'Wysyłanie...' : 'Dodaj plik'}
          </button>
          <input
            ref={fileInputRef}
            type="file"
            style={{ display: 'none' }}
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) uploadMutation.mutate(file);
              e.target.value = '';
            }}
          />
        </div>

        {attachments.length === 0 ? (
          <div style={{
            padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '14px',
            backgroundColor: '#f9fafb', borderRadius: '8px', border: '1px dashed #d1d5db',
          }}>
            Brak załączników.
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            {attachments.map((a) => (
              <div key={a.id} style={{
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                padding: '10px 14px', backgroundColor: '#f9fafb', borderRadius: '8px',
                border: '1px solid #e5e7eb',
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', minWidth: 0 }}>
                  <Paperclip size={14} color="#9ca3af" />
                  <span style={{ fontSize: '14px', color: '#374151', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {a.fileName}
                  </span>
                  <span style={{ fontSize: '12px', color: '#9ca3af', flexShrink: 0 }}>
                    ({formatFileSize(a.fileSizeBytes)})
                  </span>
                </div>
                <button
                  onClick={() => downloadMutation.mutate({ taskId: id!, attachmentId: a.id, fileName: a.fileName })}
                  style={{
                    display: 'inline-flex', alignItems: 'center', gap: '4px',
                    padding: '4px 8px', fontSize: '12px', color: '#2563eb',
                    backgroundColor: 'transparent', border: 'none', cursor: 'pointer',
                  }}
                >
                  <Download size={14} /> Pobierz
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Comments */}
      <div>
        <h2 style={{ fontSize: '16px', fontWeight: 600, color: '#111827', marginBottom: '12px' }}>
          Komentarze ({comments.length})
        </h2>

        {comments.length === 0 ? (
          <div style={{
            padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '14px',
            backgroundColor: '#f9fafb', borderRadius: '8px', border: '1px dashed #d1d5db',
            marginBottom: '12px',
          }}>
            Brak komentarzy.
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginBottom: '12px' }}>
            {comments.map((c) => (
              <div key={c.id} style={{
                padding: '12px 16px', backgroundColor: '#f9fafb', borderRadius: '8px',
                border: '1px solid #e5e7eb',
              }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                  <span style={{ fontSize: '13px', fontWeight: 600, color: '#374151' }}>
                    {employeeMap.get(c.authorId) ?? 'Nieznany'}
                  </span>
                  <span style={{ fontSize: '12px', color: '#9ca3af' }}>
                    {new Date(c.createdAt).toLocaleString('pl-PL')}
                  </span>
                </div>
                <div style={{ fontSize: '14px', color: '#374151', lineHeight: 1.5 }}>
                  {c.content}
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Add comment */}
        <div style={{ display: 'flex', gap: '8px' }}>
          <input
            value={commentText}
            onChange={(e) => setCommentText(e.target.value)}
            placeholder="Dodaj komentarz..."
            onKeyDown={(e) => { if (e.key === 'Enter') handleAddComment(); }}
            style={{
              flex: 1, padding: '8px 12px', fontSize: '14px',
              border: '1px solid #d1d5db', borderRadius: '6px',
            }}
          />
          <button
            onClick={handleAddComment}
            disabled={!commentText.trim() || addCommentMutation.isPending}
            style={{
              display: 'inline-flex', alignItems: 'center', gap: '6px',
              padding: '8px 16px', fontSize: '14px', fontWeight: 500,
              color: '#fff', backgroundColor: '#2563eb', border: 'none',
              borderRadius: '6px', cursor: 'pointer',
            }}
          >
            <Send size={14} />
          </button>
        </div>
      </div>
    </div>
  );
}

function InfoCard({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{
      padding: '12px 16px', backgroundColor: '#f9fafb', borderRadius: '8px',
      border: '1px solid #e5e7eb',
    }}>
      <div style={{ fontSize: '11px', fontWeight: 600, color: '#9ca3af', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '4px' }}>
        {label}
      </div>
      <div style={{ fontSize: '14px', color: '#374151' }}>{children}</div>
    </div>
  );
}

function Badge({ label, color }: { label: string; color: string }) {
  return (
    <span style={{
      display: 'inline-block', padding: '2px 10px', borderRadius: '4px',
      fontSize: '12px', fontWeight: 500, backgroundColor: color + '20', color,
    }}>
      {label}
    </span>
  );
}

const backBtnStyle: React.CSSProperties = {
  display: 'inline-flex', alignItems: 'center', gap: '6px',
  padding: '6px 12px', fontSize: '14px', color: '#374151',
  backgroundColor: 'transparent', border: '1px solid #d1d5db',
  borderRadius: '6px', cursor: 'pointer',
};
const selectStyle: React.CSSProperties = {
  padding: '7px 12px', fontSize: '14px', border: '1px solid #d1d5db',
  borderRadius: '6px', backgroundColor: '#fff', cursor: 'pointer', flex: 1,
};
const actionCardStyle: React.CSSProperties = {
  flex: '1 1 200px', padding: '12px 16px', backgroundColor: '#fff',
  borderRadius: '8px', border: '1px solid #e5e7eb',
};
const actionBtnStyle: React.CSSProperties = {
  padding: '7px 14px', fontSize: '14px', fontWeight: 500, color: '#fff',
  backgroundColor: '#2563eb', border: 'none', borderRadius: '6px', cursor: 'pointer',
};
const errorStyle: React.CSSProperties = {
  marginTop: '6px', fontSize: '12px', color: '#dc2626',
};

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1048576).toFixed(1)} MB`;
}
