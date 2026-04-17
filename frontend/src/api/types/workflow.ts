export interface ApprovalRequestDto {
  id: string;
  instanceId: string;
  stepId: string;
  requesterId: string;
  approverId: string;
  status: string;
  dueDate: string | null;
  order: number;
  createdAt: string;
  workflowEntityType: string | null;
  workflowEntityId: string | null;
}

export interface ApprovalDecisionDto {
  id: string;
  requestId: string;
  decidedBy: string;
  decision: string;
  comment: string | null;
  decidedAt: string;
}

export type ApprovalDecision = 'approve' | 'reject' | 'return';

export interface SubmitApprovalDecisionRequest {
  decision: ApprovalDecision;
  decidedByEmployeeId: string;
  comment?: string;
}
