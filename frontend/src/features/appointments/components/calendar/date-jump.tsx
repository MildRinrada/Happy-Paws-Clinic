'use client';

import { useRouter } from 'next/navigation';
import { Input } from '@/components/ui/field';
import { appointmentCalendar } from '../../content/appointment-calendar';
import { appointmentPaths } from '../../lib/appointment-paths';

/** A date box that opens the chosen day as soon as it is picked. */
export function DateJump({ day }: { day: string }) {
  const router = useRouter();

  return (
    <label className="flex items-center gap-2">
      <span className="sr-only">{appointmentCalendar.nav.pick}</span>
      {/* Uncontrolled: picking a date navigates, and the key puts the new day in the box. */}
      <Input
        key={day}
        type="date"
        defaultValue={day}
        autoComplete="off"
        onChange={(event) => event.target.value && router.push(appointmentPaths.calendar(event.target.value))}
        className="max-w-44"
      />
    </label>
  );
}
