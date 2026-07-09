export interface WorkflowDefinitionDto {
  id: string;
  name: string;
  description: string | null;
  definitionJson: string;
  version: number;
  isActive: boolean;
  createdAt: string;
}

export interface EscalationRuleDto {
  id: string;
  definitionId: string;
  stepName: string;
  timeoutMinutes: number;
  actionType: string;
  actionPayloadJson: string | null;
}

export interface CreateEscalationRuleRequest {
  definitionId: string;
  stepName: string;
  timeoutMinutes: number;
  actionType: string;
  actionPayloadJson?: string;
}

export interface UpdateEscalationRuleRequest {
  timeoutMinutes: number;
  actionType: string;
  actionPayloadJson?: string;
}


export interface WorkflowInstanceDto {
  id: string;
  definitionId: string;
  definitionName: string;
  entityType: string;
  entityId: string;
  currentStepName: string;
  status: string;
  initiatedBy: string;
  createdAt: string;
  completedAt: string | null;
}

export interface WorkflowStepDto {
  id: string;
  stepName: string;
  status: string;
  enteredAt: string | null;
  completedAt: string | null;
  completedBy: string | null;
  outcome: string | null;
  comment: string | null;
}

export interface WorkflowBranchDto {
  id: string;
  branchName: string;
  currentStepName: string | null;
  status: string;
  startedAt: string;
  completedAt: string | null;
}

// Definition model (JSON structure)
export interface WorkflowStepDefinition {
  name: string;
  type: 'action' | 'approval' | 'end' | 'parallel_gateway' | 'condition_gateway';
  transitions: WorkflowTransition[];
  actions?: WorkflowActionDefinition[];
  approverStrategy?: string;
  approverLevels?: number;
  parallelBranches?: ParallelBranchDefinition[];
  joinType?: 'all' | 'any';
  convergenceStep?: string;
}

export interface WorkflowTransition {
  outcome: string;
  targetStep: string;
  condition?: string;
}

export interface WorkflowActionDefinition {
  type: string;
  trigger: 'on_enter' | 'on_exit' | 'on_complete';
  payload?: Record<string, unknown>;
}

export interface ParallelBranchDefinition {
  name: string;
  steps: string[];
  condition?: string;
}

export interface WorkflowConditionDefinition {
  name: string;
  expression: string;
  description?: string;
}

export interface WorkflowDefinitionModel {
  name: string;
  version: number;
  entityType: string;
  initialStep: string;
  steps: WorkflowStepDefinition[];
  conditions?: WorkflowConditionDefinition[];
}

// Visual builder node positions
export interface NodePosition {
  x: number;
  y: number;
}

export interface WorkflowBuilderState {
  definition: WorkflowDefinitionModel;
  positions: Record<string, NodePosition>;
}

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
