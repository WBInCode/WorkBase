import { useState, useMemo, useRef, type ChangeEvent } from 'react';
import { Search, Upload, Download, Trash2, FileText, FolderOpen, Filter } from 'lucide-react';
import {
  useDocuments,
  useDocumentCategories,
  useUploadDocument,
  useDeleteDocument,
  useDownloadDocument,
} from '@/api/hooks/useDocuments';
import { useIsMobile } from '@/shared';

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function DocumentListPage() {
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [showUpload, setShowUpload] = useState(false);

  const { data: documents = [], isLoading } = useDocuments(
    categoryFilter ? { categoryId: categoryFilter } : undefined,
  );
  const { data: categories = [] } = useDocumentCategories();
  const uploadMutation = useUploadDocument();
  const deleteMutation = useDeleteDocument();
  const downloadMutation = useDownloadDocument();
  const mobile = useIsMobile();

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadCategory, setUploadCategory] = useState('');
  const [uploadDesc, setUploadDesc] = useState('');

  const filtered = useMemo(() => {
    if (!search) return documents.filter((d) => !d.isDeleted);
    const q = search.toLowerCase();
    return documents
      .filter((d) => !d.isDeleted)
      .filter(
        (d) =>
          d.fileName.toLowerCase().includes(q) ||
          (d.description?.toLowerCase().includes(q) ?? false),
      );
  }, [documents, search]);

  function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (file) {
      setUploadFile(file);
      setShowUpload(true);
    }
  }

  function handleUpload() {
    if (!uploadFile) return;
    uploadMutation.mutate(
      {
        file: uploadFile,
        categoryId: uploadCategory || undefined,
        description: uploadDesc || undefined,
      },
      {
        onSuccess: () => {
          setShowUpload(false);
          setUploadFile(null);
          setUploadCategory('');
          setUploadDesc('');
          if (fileInputRef.current) fileInputRef.current.value = '';
        },
      },
    );
  }

  function handleDelete(id: string, fileName: string) {
    if (confirm(`Usunąć dokument "${fileName}"?`)) {
      deleteMutation.mutate(id);
    }
  }

  return (
    <div style={{ padding: mobile ? 16 : 24 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1 style={{ fontSize: 24, fontWeight: 700, margin: 0 }}>Dokumenty</h1>
        <label
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 8,
            padding: '8px 16px',
            background: '#2563eb',
            color: '#fff',
            borderRadius: 8,
            cursor: 'pointer',
            fontWeight: 600,
            fontSize: 14,
          }}
        >
          <Upload size={16} />
          Prześlij plik
          <input
            ref={fileInputRef}
            type="file"
            style={{ display: 'none' }}
            onChange={handleFileChange}
          />
        </label>
      </div>

      {/* Filters */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
        <div style={{ position: 'relative', flex: 1, minWidth: 200 }}>
          <Search size={16} style={{ position: 'absolute', left: 10, top: 10, color: '#94a3b8' }} />
          <input
            type="text"
            placeholder="Szukaj dokumentów..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{
              width: '100%',
              padding: '8px 8px 8px 34px',
              border: '1px solid #e2e8f0',
              borderRadius: 8,
              fontSize: 14,
              outline: 'none',
              boxSizing: 'border-box',
            }}
          />
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <Filter size={16} style={{ color: '#64748b' }} />
          <select
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
            style={{
              padding: '8px 12px',
              border: '1px solid #e2e8f0',
              borderRadius: 8,
              fontSize: 14,
              background: '#fff',
            }}
          >
            <option value="">Wszystkie kategorie</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Upload modal */}
      {showUpload && uploadFile && (
        <div
          style={{
            background: '#f8fafc',
            border: '1px solid #e2e8f0',
            borderRadius: 12,
            padding: 20,
            marginBottom: 16,
          }}
        >
          <h3 style={{ margin: '0 0 12px', fontSize: 16, fontWeight: 600 }}>
            Prześlij: {uploadFile.name} ({formatFileSize(uploadFile.size)})
          </h3>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 12 }}>
            <select
              value={uploadCategory}
              onChange={(e) => setUploadCategory(e.target.value)}
              style={{
                padding: '8px 12px',
                border: '1px solid #e2e8f0',
                borderRadius: 8,
                fontSize: 14,
                background: '#fff',
              }}
            >
              <option value="">Bez kategorii</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            <input
              type="text"
              placeholder="Opis (opcjonalnie)"
              value={uploadDesc}
              onChange={(e) => setUploadDesc(e.target.value)}
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
              onClick={handleUpload}
              disabled={uploadMutation.isPending}
              style={{
                padding: '8px 20px',
                background: '#2563eb',
                color: '#fff',
                border: 'none',
                borderRadius: 8,
                cursor: 'pointer',
                fontWeight: 600,
                fontSize: 14,
                opacity: uploadMutation.isPending ? 0.6 : 1,
              }}
            >
              {uploadMutation.isPending ? 'Przesyłanie...' : 'Prześlij'}
            </button>
            <button
              onClick={() => {
                setShowUpload(false);
                setUploadFile(null);
                if (fileInputRef.current) fileInputRef.current.value = '';
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

      {/* Document table */}
      {isLoading ? (
        <p style={{ color: '#64748b' }}>Ładowanie...</p>
      ) : filtered.length === 0 ? (
        <div
          style={{
            textAlign: 'center',
            padding: 48,
            color: '#94a3b8',
          }}
        >
          <FolderOpen size={48} style={{ marginBottom: 12, opacity: 0.5 }} />
          <p style={{ fontSize: 16, margin: 0 }}>Brak dokumentów</p>
        </div>
      ) : (
        <div style={{ overflowX: 'auto' }}>
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              fontSize: 14,
            }}
          >
            <thead>
              <tr style={{ borderBottom: '2px solid #e2e8f0', textAlign: 'left' }}>
                <th style={{ padding: '10px 12px', fontWeight: 600, color: '#475569' }}>Nazwa pliku</th>
                <th style={{ padding: '10px 12px', fontWeight: 600, color: '#475569' }}>Kategoria</th>
                <th style={{ padding: '10px 12px', fontWeight: 600, color: '#475569' }}>Opis</th>
                <th style={{ padding: '10px 12px', fontWeight: 600, color: '#475569' }}>Rozmiar</th>
                <th style={{ padding: '10px 12px', fontWeight: 600, color: '#475569' }}>Data</th>
                <th style={{ padding: '10px 12px', fontWeight: 600, color: '#475569', width: 100 }}>Akcje</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((doc) => {
                const cat = categories.find((c) => c.id === doc.categoryId);
                return (
                  <tr
                    key={doc.id}
                    style={{ borderBottom: '1px solid #f1f5f9' }}
                  >
                    <td style={{ padding: '10px 12px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        <FileText size={16} style={{ color: '#64748b', flexShrink: 0 }} />
                        <span style={{ fontWeight: 500 }}>{doc.fileName}</span>
                      </div>
                    </td>
                    <td style={{ padding: '10px 12px', color: '#64748b' }}>
                      {cat?.name ?? '—'}
                    </td>
                    <td style={{ padding: '10px 12px', color: '#64748b', maxWidth: 250, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {doc.description ?? '—'}
                    </td>
                    <td style={{ padding: '10px 12px', color: '#64748b', whiteSpace: 'nowrap' }}>
                      {formatFileSize(doc.fileSizeBytes)}
                    </td>
                    <td style={{ padding: '10px 12px', color: '#64748b', whiteSpace: 'nowrap' }}>
                      {formatDate(doc.createdAt)}
                    </td>
                    <td style={{ padding: '10px 12px' }}>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <button
                          onClick={() =>
                            downloadMutation.mutate({
                              id: doc.id,
                              fileName: doc.fileName,
                            })
                          }
                          title="Pobierz"
                          style={{
                            padding: 6,
                            background: 'none',
                            border: '1px solid #e2e8f0',
                            borderRadius: 6,
                            cursor: 'pointer',
                            display: 'flex',
                          }}
                        >
                          <Download size={14} style={{ color: '#2563eb' }} />
                        </button>
                        <button
                          onClick={() => handleDelete(doc.id, doc.fileName)}
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
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
