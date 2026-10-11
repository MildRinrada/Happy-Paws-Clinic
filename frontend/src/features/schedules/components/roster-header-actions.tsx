'use client';

import { Button } from '@/components/ui/button';
import { schedulePage } from '../content/schedule-page';
import { useRosterDialogs } from './dialogs/roster-dialogs-context';

/** "ลงเวร", "บันทึกวันลา" and "คัดลอกเวรสัปดาห์ก่อน" in the page header, each opening its dialog. Only with `schedule:write`. */
export function RosterHeaderActions() {
  const open = useRosterDialogs();
  const text = schedulePage.actions;

  if (!open) return null;

  return (
    <>
      <Button size="sm" onClick={() => open({ kind: 'addShift' })}>
        {text.addShift}
      </Button>
      <Button size="sm" variant="secondary" onClick={() => open({ kind: 'timeOff' })}>
        {text.timeOff}
      </Button>
      <Button size="sm" variant="secondary" onClick={() => open({ kind: 'copyWeek' })}>
        {text.copyWeek}
      </Button>
    </>
  );
}
