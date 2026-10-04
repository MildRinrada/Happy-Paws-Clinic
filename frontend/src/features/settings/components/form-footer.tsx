import { Button } from '@/components/ui/button';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '../content/form-messages';

interface FormFooterProps {
  pending: boolean;
  message?: string;
  saved?: boolean;
  /** Shows a cancel button when given. */
  onCancel?: () => void;
  submitLabel?: string;
  pendingLabel?: string;
}

/** A settings form's last row: what happened, cancel, and save. */
export function FormFooter({ pending, message, saved, onCancel, submitLabel, pendingLabel }: FormFooterProps) {
  return (
    <div className="flex flex-wrap items-center justify-end gap-3">
      <FormStatus message={message} saved={saved} savedText={formMessages.saved} />
      {onCancel && (
        <Button variant="ghost" disabled={pending} onClick={onCancel}>
          {formMessages.cancel}
        </Button>
      )}
      <Button type="submit" disabled={pending}>
        {pending ? (pendingLabel ?? formMessages.saving) : (submitLabel ?? formMessages.save)}
      </Button>
    </div>
  );
}
