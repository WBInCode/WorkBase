import { useState } from 'react';
import { Plus, Pencil, Trash2, FolderOpen, X, Check } from 'lucide-react';
import {
  useDocumentCategories,
  useCreateDocumentCategory,
  useUpdateDocumentCategory,
  useDeleteDocumentCategory,
} from '@/api/hooks/useDocuments';
import { useIsMobile } from '@/shared';

export function DocumentCategoriesPage() {
  const { data: categories = [], isLoading } = useDocumentCategories();
  const createMutation = useCreateDocumentCategory();
  const updateMutation = useUpdateDocumentCategory();
  const deleteMutation = useDeleteDocumentCategory();
  const mobile = useIsMobile();

  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState('');
  const [newDesc, setNewDesc] = useState('');

  const [editId, setEditId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editDesc, setEditDesc] = useState('');

  function handleCreate() {
    if (!newName.trim()) return;
    createMutation.mutate(
      { name: newName.trim(), description: newDesc.trim() || undefined },
      {
        onSuccess: () => {
          setShowCreate(false);
          setNewName('');
          setNewDesc('');
        },
      },
    );
  }

  function startEdit(id: string, name: string, description: string | null) {
    setEditId(id);
    setEditName(name);
    setEditDesc(description ?? '');
  }

  function handleUpdate() {
    if (!editId || !editName.trim()) return;
    updateMutation.mutate(
      { id: editId, name: editName.trim(), description: editDesc.trim() || undefined },
      { onSuccess: () => setEditId(null) },
    );
  }

  function handleDelete(id: string, name: string) {
    if (confirm(`Usunąć kategorię "${name}"?`)) {
      deleteMutation.mutate(id);
    }
  }

  return (
    <div style={{ padding: mobile ? 16 : 24 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1 style={{ fontSize: 24, fontWeight: 700, margin: 0 }}>Kategorie dokumentów</h1>
        <button
          onClick={() => setShowCreate(true)}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 8,
            padding: '8px 16px',
            background: '#2563eb',
            color: '#fff',
            border: 'none',
            borderRadius: 8,
            cursor: 'pointer',
            fontWeight: 600,
            fontSize: 14,
          }}
        >
          <Plus size={16} />
          Nowa kategoria
        </button>
      </div>

      {/* Create form */}
      {showCreate && (
        <div
          style={{
            background: '#f8fafc',
            border: '1px solid #e2e8f0',
            borderRadius: 12,
            padding: 20,
            marginBottom: 16,
          }}
        >
          <h3 style={{ margin: '0 0 12px', fontSize: 16, fontWeight: 600 }}>Nowa kategoria</h3>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 12 }}>
            <input
              type="text"
              placeholder="Nazwa *"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              style={{
                flex: 1,
                minWidth: 200,
                padding: '8px 12px',
                border: '1px solid #e2e8f0',
                borderRadius: 8,
                fontSize: 14,
                outline: 'none',
              }}
            />
            <input
              type="text"
              placeholder="Opis (opcjonalnie)"
              value={newDesc}
              onChange={(e) => setNewDesc(e.target.value)}
              style={{
                flex: 1,
                minWidth: 200,
                padding: '8px 12px',
                border: '1px solid #e2e8f0',
                borderRadius: 8,
                fontSize: 14,
                outline: 'none',
              }}
            />
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button
              onClick={handleCreate}
              disabled={createMutation.isPending || !newName.trim()}
              style={{
                padding: '8px 20px',
                background: '#2563eb',
                color: '#fff',
                border: 'none',
                borderRadius: 8,
                cursor: 'pointer',
                fontWeight: 600,
                fontSize: 14,
                opacity: createMutation.isPending || !newName.trim() ? 0.6 : 1,
              }}
            >
              {createMutation.isPending ? 'Tworzenie...' : 'Utwórz'}
            </button>
            <button
              onClick={() => {
                setShowCreate(false);
                setNewName('');
                setNewDesc('');
              }}
              style={{
                padding: '8px 20px',
                background: '#f1f5f9',
                border: '1px solid #e2e8f0',
                borderRadius: 8,
                cursor: 'pointer',
                fontSize: 14,
              }}
            >
              Anuluj
            </button>
          </div>
        </div>
      )}

      {/* Categories list */}
      {isLoading ? (
        <p style={{ color: '#64748b' }}>Ładowanie...</p>
      ) : categories.length === 0 ? (
        <div style={{ textAlign: 'center', padding: 48, color: '#94a3b8' }}>
          <FolderOpen size={48} style={{ marginBottom: 12, opacity: 0.5 }} />
          <p style={{ fontSize: 16, margin: 0 }}>Brak kategorii</p>
        </div>
      ) : (
        <div style={{ display: 'grid', gap: 12 }}>
          {categories.map((cat) => (
            <div
              key={cat.id}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 12,
                padding: 16,
                background: '#fff',
                border: '1px solid #e2e8f0',
                borderRadius: 10,
              }}
            >
              {editId === cat.id ? (
                <>
                  <input
                    value={editName}
                    onChange={(e) => setEditName(e.target.value)}
                    style={{
                      flex: 1,
                      padding: '6px 10px',
                      border: '1px solid #cbd5e1',
                      borderRadius: 6,
                      fontSize: 14,
                      outline: 'none',
                    }}
                  />
                  <input
                    value={editDesc}
                    onChange={(e) => setEditDesc(e.target.value)}
                    placeholder="Opis"
                    style={{
                      flex: 1,
                      padding: '6px 10px',
                      border: '1px solid #cbd5e1',
                      borderRadius: 6,
                      fontSize: 14,
                      outline: 'none',
                    }}
                  />
                  <button
                    onClick={handleUpdate}
                    disabled={updateMutation.isPending}
                    title="Zapisz"
                    style={{
                      padding: 6,
                      background: '#22c55e',
                      border: 'none',
                      borderRadius: 6,
                      cursor: 'pointer',
                      display: 'flex',
                      color: '#fff',
                    }}
                  >
                    <Check size={14} />
                  </button>
                  <button
                    onClick={() => setEditId(null)}
                    title="Anuluj"
                    style={{
                      padding: 6,
                      background: '#f1f5f9',
                      border: '1px solid #e2e8f0',
                      borderRadius: 6,
                      cursor: 'pointer',
                      display: 'flex',
                    }}
                  >
                    <X size={14} />
                  </button>
                </>
              ) : (
                <>
                  <FolderOpen size={20} style={{ color: '#f59e0b', flexShrink: 0 }} />
                  <div style={{ flex: 1 }}>
                    <div style={{ fontWeight: 600, fontSize: 15 }}>{cat.name}</div>
                    {cat.description && (
                      <div style={{ color: '#64748b', fontSize: 13, marginTop: 2 }}>
                        {cat.description}
                      </div>
                    )}
                  </div>
                  <button
                    onClick={() => startEdit(cat.id, cat.name, cat.description)}
                    title="Edytuj"
                    style={{
                      padding: 6,
                      background: 'none',
                      border: '1px solid #e2e8f0',
                      borderRadius: 6,
                      cursor: 'pointer',
                      display: 'flex',
                    }}
                  >
                    <Pencil size={14} style={{ color: '#64748b' }} />
                  </button>
                  <button
                    onClick={() => handleDelete(cat.id, cat.name)}
                    title="Usuń"
                    style={{
                      padding: 6,
                      background: 'none',
                      border: '1px solid #e2e8f0',
                      borderRadius: 6,
                      cursor: 'pointer',
                      display: 'flex',
                    }}
                  >
                    <Trash2 size={14} style={{ color: '#ef4444' }} />
                  </button>
                </>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
