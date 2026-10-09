import { Badge } from '@/components/ui/badge';
import { deliveryStatusLabels, deliveryStatusTones } from '../content/webhook-settings';

/** Pending, succeeded or failed, in the colour read at a glance. */
export function WebhookDeliveryStatusBadge({ status }: { status: string }) {
  return <Badge tone={deliveryStatusTones[status] ?? 'neutral'}>{deliveryStatusLabels[status] ?? status}</Badge>;
}
