import { Button } from '@/components/ui/button';
import { noShowText } from '../../content/advanced-booking';
import { appointmentDetail } from '../../content/appointment-calendar';

interface NoShowConfirmProps {
  pending: boolean;
  onConfirm: () => void;
  onBack: () => void;
}

/** One more step before a no-show, because it counts against the customer and cannot be undone. */
export function NoShowConfirm({ pending, onConfirm, onBack }: NoShowConfirmProps) {
  return (
    <div className="space-y-3">
      <p className="text-sm font-semibold text-ink-900">{noShowText.confirmTitle}</p>
      <p className="text-sm text-ink-700">{noShowText.confirmBody}</p>
      <div className="flex justify-end gap-2">
        <Button variant="ghost" size="sm" disabled={pending} onClick={onBack}>
          {noShowText.back}
        </Button>
        <Button variant="danger" size="sm" disabled={pending} onClick={onConfirm}>
          {pending ? appointmentDetail.actions.working : noShowText.submit}
        </Button>
      </div>
    </div>
  );
}
