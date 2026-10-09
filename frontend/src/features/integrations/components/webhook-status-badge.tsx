import { Badge } from '@/components/ui/badge';
import { webhookListText } from '../content/webhook-list';

/** On or off, read at a glance. */
export function WebhookStatusBadge({ active }: { active: boolean }) {
  return active ? (
    <Badge tone="green">{webhookListText.status.active}</Badge>
  ) : (
    <Badge tone="neutral">{webhookListText.status.inactive}</Badge>
  );
}
