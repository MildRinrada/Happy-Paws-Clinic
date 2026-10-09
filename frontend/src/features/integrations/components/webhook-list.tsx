import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { webhookListText } from '../content/webhook-list';
import type { Webhook } from '../types/integration.types';
import { WebhookListItem } from './webhook-list-item';

/** Every webhook that is not deleted, newest first. */
export function WebhookList({ webhooks }: { webhooks: Webhook[] }) {
  const text = webhookListText;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        {webhooks.length === 0 ? (
          <EmptyState title={text.empty}>{text.emptyHint}</EmptyState>
        ) : (
          <ul className="divide-y divide-ink-100">
            {webhooks.map((webhook) => (
              // Keyed by id alone: a new secret must stay on screen when the list re-reads itself.
              <WebhookListItem key={webhook.id} webhook={webhook} />
            ))}
          </ul>
        )}
      </CardBody>
    </Card>
  );
}
