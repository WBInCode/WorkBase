import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import type {
  ApprovalRequestDto,
  SubmitApprovalDecisionRequest,
} from '@/api/types/workflow';

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
