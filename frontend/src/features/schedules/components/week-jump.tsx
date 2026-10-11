'use client';

import { useRouter } from 'next/navigation';
import { Input } from '@/components/ui/field';
import { schedulePage } from '../content/schedule-page';

/** Typing a year digit by digit passes through dates like 0002-10-12; only a plausible one moves the page. */
const PLAUSIBLE = /^(19|20)\d{2}-\d{2}-\d{2}$/;

/** A date box that opens the week holding the date picked, any day of it. */
export function WeekJump({ monday }: { monday: string }) {
  const router = useRouter();

  return (
    <span className="w-40">
      <Input
        key={monday}
        type="date"
        defaultValue={monday}
        aria-label={schedulePage.week.jump}
        title={schedulePage.week.jump}
        className="h-8"
        onChange={(event) => PLAUSIBLE.test(event.target.value) && router.push(`/schedules?week=${event.target.value}`)}
      />
    </span>
  );
}
