'use client';

import { useRouter } from 'next/navigation';
import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Input } from '@/components/ui/field';
import { webhookSettingsPage } from '../content/webhook-settings';
import { integrationPaths, isWebhookId } from '../lib/integration-paths';

/**
 * Opens one webhook's delivery log by its id. Live webhooks are reached from the list; this is the
 * way to a deleted one's log, which stays readable for 30 days (docs/contract-extensions.md § 22).
 */
export function WebhookLogLookup() {
  const text = webhookSettingsPage.lookup;
  const id = useId();
  const router = useRouter();
  const [value, setValue] = useState('');

  function open(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isWebhookId(value)) {
      router.push(integrationPaths.webhookLog(value.trim()));
    }
  }

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        <form onSubmit={open} className="flex flex-wrap items-end gap-3">
          <div className="min-w-64 flex-1">
            <Field label={text.label} htmlFor={id} hint={text.hint}>
              <Input id={id} className="font-mono" value={value} onChange={(event) => setValue(event.target.value)} />
            </Field>
          </div>
          <Button type="submit" variant="secondary" disabled={!isWebhookId(value)}>
            {text.submit}
          </Button>
        </form>
      </CardBody>
    </Card>
  );
}
