import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  ApprovalRequestDto,
  SubmitApprovalDecisionRequest,
  WorkflowDefinitionDto,
  WorkflowInstanceDto,
  WorkflowStepDto,
  WorkflowBranchDto,
} from '@/api/types/workflow';

export function useWorkflowDefinitions() {
  return useQuery({
    queryKey: ['workflow', 'definitions'],
    queryFn: () => api.get<WorkflowDefinitionDto[]>('/api/workflow/definitions'),
  });
}

export function useWorkflowDefinition(id: string | null) {
  return useQuery({
    queryKey: ['workflow', 'definitions', id],
    queryFn: () => api.get<WorkflowDefinitionDto>(`/api/workflow/definitions/${id}`),
    enabled: !!id,
  });
}

export function useCreateWorkflowDefinition() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { name: string; definitionJson: string; description?: string }) =>
      api.post<string>('/api/workflow/definitions', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['workflow', 'definitions'] }),
  });
}

export function useUpdateWorkflowDefinition() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string; name: string; definitionJson: string; description?: string }) =>
      api.put<void>(`/api/workflow/definitions/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['workflow', 'definitions'] }),
  });
}

export function useDeleteWorkflowDefinition() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/workflow/definitions/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['workflow', 'definitions'] }),
  });
}

export function useWorkflowInstance(id: string | null) {
  return useQuery({
    queryKey: ['workflow', 'instances', id],
    queryFn: () => api.get<WorkflowInstanceDto>(`/api/workflow/instances/${id}`),
    enabled: !!id,
  });
}

export function useWorkflowSteps(instanceId: string | null) {
  return useQuery({
    queryKey: ['workflow', 'instances', instanceId, 'steps'],
    queryFn: () => api.get<WorkflowStepDto[]>(`/api/workflow/instances/${instanceId}/steps`),
    enabled: !!instanceId,
  });
}

export function useWorkflowBranches(instanceId: string | null) {
  return useQuery({
    queryKey: ['workflow', 'instances', instanceId, 'branches'],
    queryFn: () => api.get<WorkflowBranchDto[]>(`/api/workflow/instances/${instanceId}/branches`),
    enabled: !!instanceId,
  });
}

export function usePendingApprovals(approverEmployeeId: string | null) {
  return useQuery({
    queryKey: ['workflow', 'approvals', 'pending', approverEmployeeId],
    queryFn: () =>
      api.get<ApprovalRequestDto[]>(
        `/api/workflow/approvals/pending/${approverEmployeeId}`,
      ),
    enabled: !!approverEmployeeId,
  });
}

export function useApprovalRequest(id: string | null) {
  return useQuery({
    queryKey: ['workflow', 'approvals', id],
    queryFn: () => api.get<ApprovalRequestDto>(`/api/workflow/approvals/${id}`),
    enabled: !!id,
  });
}

export function useSubmitApprovalDecision() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      approvalId,
      ...body
    }: SubmitApprovalDecisionRequest & { approvalId: string }) =>
      api.post<void>(`/api/workflow/approvals/${approvalId}/decide`, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['workflow', 'approvals'] });
      qc.invalidateQueries({ queryKey: ['leave'] });
    },
  });
}
