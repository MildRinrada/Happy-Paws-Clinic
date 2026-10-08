import { Badge } from '@/components/ui/badge';
import { labelOf, statusLabels, statusTones } from '../content/delivery-labels';

/** Sent, failed, skipped, in the colour the counter reads at a glance. */
export function DeliveryStatusBadge({ status }: { status: string }) {
  return <Badge tone={statusTones[status] ?? 'neutral'}>{labelOf(statusLabels, status)}</Badge>;
}
