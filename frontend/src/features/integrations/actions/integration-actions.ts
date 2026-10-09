'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { createApiKey } from '../api/apiKeys';
import { configureLab, configurePayment } from '../api/providerSettings';
import { createWebhook } from '../api/webhooks';
import type { CreatedApiKey, CreatedWebhook, LabSettings, PaymentSettings } from '../types/integration.types';

/**
 * The "Webhooks and Integrations" writes, all `integration:manage`. The secret a create returns
 * goes back to the form that asked for it and is shown there once; nothing here keeps it.
 */

const webhookInput = z.object({
  url: z.string().trim().min(1).max(2000),
  events: z.array(z.string().max(60)).min(1).max(16),
  description: z.string().trim().max(200).optional(),
});

export async function createWebhookAction(values: z.infer<typeof webhookInput>): Promise<ActionResult<CreatedWebhook>> {
  return runAction(() => createWebhook(webhookInput.parse(values)));
}

const apiKeyInput = z.object({
  name: z.string().trim().min(1).max(100),
  scopes: z.array(z.string().max(60)).min(1).max(80),
  branch_ids: z.array(z.string().max(40)).max(50).optional(),
  expires_in_days: z.number().int().min(1).max(365),
});

export async function createApiKeyAction(values: z.infer<typeof apiKeyInput>): Promise<ActionResult<CreatedApiKey>> {
  const { expires_in_days: days, branch_ids: branches, ...rest } = apiKeyInput.parse(values);
  const expiresAt = new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString();

  return runAction(() =>
    createApiKey({ ...rest, branch_ids: branches && branches.length > 0 ? branches : undefined, expires_at: expiresAt }),
  );
}

const secret = z.string().trim().max(500).optional();

const paymentInput = z.object({
  version: z.number().int().nonnegative(),
  values: z.object({
    provider: z.string().trim().max(30).optional(),
    mode: z.enum(['test', 'live']).optional(),
    enabled: z.boolean().optional(),
    public_key: z.string().trim().max(200).optional(),
    secret_key: secret,
    webhook_secret: secret,
  }),
});

export async function configurePaymentAction(payload: z.infer<typeof paymentInput>): Promise<ActionResult<PaymentSettings>> {
  const { version, values } = paymentInput.parse(payload);
  const result = await runAction(() => configurePayment(version, values));

  revalidatePath('/settings/integrations/payment');
  return result;
}

const labInput = z.object({
  version: z.number().int().nonnegative(),
  values: z.object({
    provider: z.string().trim().max(30).optional(),
    enabled: z.boolean().optional(),
    account_id: z.string().trim().max(100).optional(),
    base_url: z.string().trim().max(2000).optional(),
    api_key: secret,
  }),
});

export async function configureLabAction(payload: z.infer<typeof labInput>): Promise<ActionResult<LabSettings>> {
  const { version, values } = labInput.parse(payload);
  const result = await runAction(() => configureLab(version, values));

  revalidatePath('/settings/integrations/lab');
  return result;
}
