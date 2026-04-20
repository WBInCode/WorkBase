export interface DocumentDto {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedById: string;
  categoryId: string | null;
  entityType: string | null;
  entityId: string | null;
  description: string | null;
  isDeleted: boolean;
  createdAt: string;
}

export interface DocumentCategoryDto {
  id: string;
  name: string;
  description: string | null;
}

export interface CreateDocumentCategoryRequest {
  name: string;
  description?: string;
}

export interface UpdateDocumentCategoryRequest {
  name: string;
  description?: string;
}
