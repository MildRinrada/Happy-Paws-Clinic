import { Badge } from '@/components/ui/badge';
import { reminderStatusLabels, reminderStatusTones } from '../content/automation-labels';
import type { ReminderStatus } from '../types/automation.types';

/** What the run does with one reminder: send it, have the consent gate skip it, or nothing (already sent). */
export function ReminderStatusBadge({ status }: { status: ReminderStatus }) {
  return <Badge tone={reminderStatusTones[status]}>{reminderStatusLabels[status]}</Badge>;
}
