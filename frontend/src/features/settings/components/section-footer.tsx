'use client';

import { Button } from '@/components/ui/button';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '../content/form-messages';

interface SectionFooterProps {
  pending: boolean;
  message?: string;
  saved?: boolean;
  savedText?: string;
  /** True when the save bar saves this form; false draws the form's own save button. */
  inBar: boolean;
  submitLabel?: string;
}

/**
 * The last row of a settings form that the save bar saves: what happened to the last save, said
 * next to the fields it is about. Only a form with no bar to save it (outside the settings area)
 * draws its own button.
 */
export function SectionFooter({ pending, message, saved, savedText = formMessages.saved, inBar, submitLabel }: SectionFooterProps) {
  if (inBar && !message && !saved) return null;

  return (
    <div className="flex flex-wrap items-center justify-end gap-3">
      <FormStatus message={message} saved={saved} savedText={savedText} />
      {!inBar && (
        <Button type="submit" disabled={pending}>
          {pending ? formMessages.saving : (submitLabel ?? formMessages.save)}
        </Button>
      )}
    </div>
  );
}
