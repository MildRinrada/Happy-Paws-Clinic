'use client';

import Link from 'next/link';
import { useState } from 'react';
import { Button, buttonClass } from '@/components/ui/button';
import { formatDateTime } from '@/lib/format/datetime';
import { webhookListText } from '../content/webhook-list';
import { webhookEventLabels } from '../content/webhook-settings';
import { integrationPaths } from '../lib/integration-paths';
import type { RotatedWebhookSecret, Webhook } from '../types/integration.types';
import { DeleteWebhookButton } from './delete-webhook-button';
import { RotateWebhookSecretButton } from './rotate-webhook-secret-button';
import { RotatedSecretCard } from './rotated-secret-card';
import { WebhookActiveToggle } from './webhook-active-toggle';
import { WebhookEditForm } from './webhook-edit-form';
import { WebhookStatusBadge } from './webhook-status-badge';

/**
 * One webhook: where it sends, what, and whether it is on, with its log, edit, switch, new secret
 * and delete. A new secret replaces the row until the person says they have kept it.
 */
export function WebhookListItem({ webhook }: { webhook: Webhook }) {
  const text = webhookListText;
  const [editing, setEditing] = useState(false);
  const [rotated, setRotated] = useState<RotatedWebhookSecret>();

  if (rotated) {
    return (
      <li className="py-4">
        <RotatedSecretCard rotated={rotated} onDone={() => setRotated(undefined)} />
      </li>
    );
  }

  return (
    <li className="space-y-3 py-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0 space-y-1">
          <p className="break-all font-mono text-sm text-ink-900">{webhook.url}</p>
          <p className="text-sm text-ink-600">{webhook.description ?? text.noDescription}</p>
          <p className="text-xs text-ink-500">
            {webhook.events.map((type) => webhookEventLabels[type] ?? type).join(', ')}
          </p>
          <p className="text-xs text-ink-400">
            <span className="font-mono">{webhook.id}</span> {text.created(formatDateTime(webhook.created_at))}
          </p>
        </div>
        <WebhookStatusBadge active={webhook.active} />
      </div>

      {editing ? (
        <WebhookEditForm webhook={webhook} onClose={() => setEditing(false)} />
      ) : (
        <div className="flex flex-wrap items-start gap-2">
          <Link href={integrationPaths.webhookLog(webhook.id)} className={buttonClass('secondary', 'sm')}>
            {text.log}
          </Link>
          <Button variant="secondary" size="sm" onClick={() => setEditing(true)}>
            {text.edit}
          </Button>
          <WebhookActiveToggle webhook={webhook} />
          <RotateWebhookSecretButton webhook={webhook} onRotated={setRotated} />
          <DeleteWebhookButton webhook={webhook} />
        </div>
      )}
    </li>
  );
}
