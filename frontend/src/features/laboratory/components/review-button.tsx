'use client';

import { Button } from '@/components/ui/button';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { reviewLabResultsAction } from '../actions/lab-actions';
import { labPage } from '../content/lab-page';

/**
 * "อ่านผลแล้ว": takes the request off the list of results nobody has read. Opening the list is
 * not reading, so this is a deliberate press. Results added later put it back on the list.
 */
export function ReviewButton({ labRequestId }: { labRequestId: string }) {
  const text = labPage.review;
  const { message, pending, submit } = useFormSubmit(labPage.messages);

  return (
    <div className="flex flex-wrap items-center gap-3">
      {message && (
        <p role="alert" className="text-sm text-red-700">
          {message}
        </p>
      )}
      <Button size="sm" disabled={pending} onClick={() => submit(() => reviewLabResultsAction({ labRequestId }))}>
        {pending ? text.working : text.submit}
      </Button>
    </div>
  );
}
