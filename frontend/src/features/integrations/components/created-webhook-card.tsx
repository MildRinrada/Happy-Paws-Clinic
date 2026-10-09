import Link from 'next/link';
import { Button, buttonClass } from '@/components/ui/button';
import { webhookSettingsPage } from '../content/webhook-settings';
import { integrationPaths } from '../lib/integration-paths';
import type { CreatedWebhook } from '../types/integration.types';
import { SecretRevealCard } from './secret-reveal-card';

/** A webhook just made: its signing secret, how to check a delivery, and where its log is. */
export function CreatedWebhookCard({ webhook, onDone }: { webhook: CreatedWebhook; onDone: () => void }) {
  const text = webhookSettingsPage.created;

  return (
    <SecretRevealCard heading={text.heading} label={text.secret} secret={webhook.secret} warning={text.warning}>
      <p className="text-sm text-ink-600">{text.signing}</p>
      <p className="text-sm text-ink-700">
        {text.id} <span className="font-mono">{webhook.id}</span>
      </p>
      <div className="flex flex-wrap gap-3">
        <Link href={integrationPaths.webhookLog(webhook.id)} className={buttonClass('secondary', 'sm')}>
          {text.log}
        </Link>
        <Button variant="ghost" size="sm" onClick={onDone}>
          {webhookSettingsPage.form.heading}
        </Button>
      </div>
    </SecretRevealCard>
  );
}
