'use client';

import { useContext } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '../../content/form-messages';
import { saveBarText } from '../../content/save-bar';
import { SaveBarEntriesContext, SaveBarResetContext } from './save-bar-context';
import { useJustSaved } from './use-just-saved';

/**
 * The bar along the bottom of a settings page while a form on it has unsaved changes: which part,
 * cancel (back to what was last saved) and save. It sits at the foot of the content column and
 * sticks to the bottom of the screen, so it never covers the staff menu, and keeps clear of a
 * phone's home bar.
 *
 * With several forms changed at once it names them all, cancel puts them all back and save submits
 * each one, each with its own request. Each form still says what happened beside its own fields
 * (`role="status"` or `role="alert"`), which is what a screen reader hears; the bar's own "saved"
 * line is for the eye only, so the news is not read twice.
 */
export function SaveBar() {
  const entries = useContext(SaveBarEntriesContext).filter((entry) => entry.inBar);
  const reset = useContext(SaveBarResetContext);
  const unsaved = entries.filter((entry) => entry.dirty);
  const pending = entries.some((entry) => entry.pending);
  const failed = unsaved.filter((entry) => entry.failed && !entry.pending);
  const justSaved = useJustSaved(pending, unsaved.length === 0);

  if (unsaved.length === 0 && !justSaved) {
    return null;
  }

  function saveAll() {
    for (const entry of unsaved) {
      const form = document.getElementById(entry.formId);
      if (form instanceof HTMLFormElement) form.requestSubmit();
    }
  }

  return (
    <div className="no-print sticky bottom-0 z-20 pb-[max(0.75rem,env(safe-area-inset-bottom))] pt-3">
      {unsaved.length === 0 ? (
        <p aria-hidden="true" className="ml-auto w-fit rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-2.5 text-sm font-medium text-emerald-800 shadow-lg">
          {saveBarText.saved}
        </p>
      ) : (
        <section
          aria-label={saveBarText.region}
          className="flex flex-wrap items-center gap-x-4 gap-y-2 rounded-xl border border-ink-200 bg-white/95 px-4 py-3 shadow-lg backdrop-blur"
        >
          <div className="min-w-0 flex-1 text-sm">
            <p className="font-medium text-ink-900">{saveBarText.unsaved}</p>
            <p className="text-ink-600">{saveBarText.sections(unsaved.map((entry) => entry.section))}</p>
            {failed.length > 0 && <p className="text-red-700">{saveBarText.failed(failed.map((entry) => entry.section))}</p>}
          </div>
          <div className="flex shrink-0 items-center gap-2">
            <Button variant="ghost" disabled={pending} onClick={() => reset(unsaved.map((entry) => entry.formId))}>
              {formMessages.cancel}
            </Button>
            <Button disabled={pending} onClick={saveAll}>
              {pending ? formMessages.saving : formMessages.save}
            </Button>
          </div>
        </section>
      )}
    </div>
  );
}
