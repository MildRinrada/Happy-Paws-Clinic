'use client';

import { useState } from 'react';
import { rotateCalendarLinkAction } from '../actions/manage-integration-actions';
import { calendarFeedPage } from '../content/calendar-feed';
import { ConfirmButton } from './confirm-button';

/**
 * "สร้างลิงก์ใหม่": a new calendar link after a second press, for when the old one got out. The
 * old link stops at once; the page re-reads itself and shows the new one.
 */
export function RotateCalendarLinkButton() {
  const text = calendarFeedPage.rotate;
  const [done, setDone] = useState(false);

  return (
    <div className="space-y-2">
      <p className="text-sm text-ink-600">{text.hint}</p>
      <ConfirmButton
        label={text.button}
        confirmText={text.confirm}
        confirmLabel={text.confirmButton}
        pendingLabel={text.pending}
        cancelLabel={text.cancel}
        action={rotateCalendarLinkAction}
        onDone={() => setDone(true)}
      />
      {done && (
        <p role="status" className="text-sm text-green-700">
          {text.done}
        </p>
      )}
    </div>
  );
}
