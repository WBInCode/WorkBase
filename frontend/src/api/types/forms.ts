export interface FormFieldDto {
  id: string;
  label: string;
  fieldType: string;
  order: number;
  isRequired: boolean;
  placeholder: string | null;
  validationRule: string | null;
  optionsJson: string | null;
  defaultValue: string | null;
}

export interface FormDefinitionDto {
  id: string;
  name: string;
  description: string | null;
  version: number;
  isActive: boolean;
  isPublic: boolean;
  workflowDefinitionName: string | null;
  createdAt: string;
  fields: FormFieldDto[];
}

export interface FormSubmissionDto {
  id: string;
  formDefinitionId: string;
  formName: string;
  submittedBy: string | null;
  valuesJson: string;
  status: string;
  workflowInstanceId: string | null;
  createdAt: string;
}

export interface CreateFormFieldRequest {
  label: string;
  fieldType: string;
  order: number;
  isRequired: boolean;
  placeholder?: string;
  validationRule?: string;
  optionsJson?: string;
  defaultValue?: string;
}

export interface CreateFormDefinitionRequest {
  name: string;
  description?: string;
  isPublic: boolean;
  workflowDefinitionName?: string;
  fields: CreateFormFieldRequest[];
}

export const FIELD_TYPES = [
  'text', 'number', 'date', 'select', 'checkbox',
  'textarea', 'file', 'email', 'phone',
] as const;

export type FieldType = (typeof FIELD_TYPES)[number];
