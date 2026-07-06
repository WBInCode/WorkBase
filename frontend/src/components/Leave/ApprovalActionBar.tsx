import { useState } from 'react';
import { CheckCircle, XCircle, RotateCcw } from 'lucide-react';
import type { ApprovalDecision } from '@/api/types/workflow';
import { colors } from '@/theme/tokens';

interface ApprovalActionBarProps {
  onDecide: (decision: ApprovalDecision, comment: string) => void;
  isPending: boolean;
}

const ACTIONS: {
  decision: ApprovalDecision;
  label: string;
  icon: typeof CheckCircle;
  bg: string;
  bgHover: string;
}[] = [
  { decision: 'approve', label: 'Akceptuj', icon: CheckCircle, bg: colors.success[600], bgHover: colors.success[700] },
  { decision: 'reject', label: 'Odrzuć', icon: XCircle, bg: colors.danger[600], bgHover: colors.danger[700] },
  { decision: 'return', label: 'Cofnij', icon: RotateCcw, bg: colors.warning[600], bgHover: colors.warning[700] },
];

export function ApprovalActionBar({ onDecide, isPending }: ApprovalActionBarProps) {
  const [comment, setComment] = useState('');
  const [activeDecision, setActiveDecision] = useState<ApprovalDecision | null>(null);

  const handleDecide = (decision: ApprovalDecision) => {
    setActiveDecision(decision);
    onDecide(decision, comment);
  };

  return (
    <div
      style={{
        display: 'flex',
        flexWrap: 'wrap',
        alignItems: 'flex-end',
        gap: '12px',
        padding: '16px 0',
      }}
    >
      <div style={{ flex: 1, minWidth: '200px' }}>
        <label
          style={{
            display: 'block',
            fontSize: '13px',
            fontWeight: 500,
            color: colors.gray[700],
            marginBottom: '4px',
          }}
        >
          Komentarz (opcjonalnie)
        </label>
        <textarea
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          placeholder="Dodaj komentarz do decyzji..."
          rows={2}
          style={{
            width: '100%',
            padding: '8px 12px',
            fontSize: '14px',
            border: `1px solid ${colors.gray[300]}`,
            borderRadius: '6px',
            resize: 'vertical',
            fontFamily: 'inherit',
          }}
          disabled={isPending}
        />
      </div>

      <div style={{ display: 'flex', gap: '8px', flexShrink: 0, paddingBottom: '2px' }}>
        {ACTIONS.map(({ decision, label, icon: Icon, bg }) => (
          <button
            key={decision}
            onClick={() => handleDecide(decision)}
            disabled={isPending}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '8px 16px',
              fontSize: '14px',
              fontWeight: 500,
              color: colors.white,
              backgroundColor: bg,
              border: 'none',
              borderRadius: '6px',
              cursor: isPending ? 'not-allowed' : 'pointer',
              opacity: isPending && activeDecision !== decision ? 0.5 : 1,
            }}
          >
            <Icon size={16} />
            {isPending && activeDecision === decision ? 'Wysyłanie...' : label}
          </button>
        ))}
      </div>
    </div>
  );
}
