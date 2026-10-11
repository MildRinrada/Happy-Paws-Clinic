import { Alert } from '@/components/ui/alert';
import { schedulePage } from '../content/schedule-page';
import type { TimeOffResult as Result } from '../types/schedule.types';
import { AppointmentConflicts } from './appointment-conflicts';

interface TimeOffResultProps {
  result: Result;
  /** Staff id to name, for the suggested substitutes. */
  names: Record<string, string>;
}

/** What the leave did: shifts taken off, and each appointment someone now has to call about. */
export function TimeOffResult({ result, names }: TimeOffResultProps) {
  const text = schedulePage.timeOff;

  return (
    <div className="space-y-2">
      <Alert tone="success">{text.result(result.shifts_cancelled)}</Alert>
      <AppointmentConflicts conflicts={result.conflicts} names={names} heading={text.conflicts} emptyText={text.noConflicts} />
    </div>
  );
}
