'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { addContact, removeContact, updateContact } from '../api/contacts';

/**
 * The settings page's changes to the contact list. What counts as a valid entry is the backend's
 * rule; these check the shape, make one call, and mark every page that shows contacts stale.
 */

function refresh() {
  revalidatePath('/settings/contacts');
  // The footer is on every public page, so the whole public site is stale.
  revalidatePath('/', 'layout');
}

const input = z.object({
  kind: z.enum(['phone', 'emergency', 'email', 'line', 'facebook', 'instagram', 'website', 'map', 'other']),
  label: z.string().trim().max(100),
  value: z.string().trim().max(500),
  url: z.string().trim().max(1000),
  embed_url: z.string().trim().max(1000),
  branch_id: z.string().trim().max(40),
  sort_order: z.number().int().min(-100000).max(1000000),
});

export async function addContactAction(values: z.infer<typeof input>) {
  const parsed = input.parse(values);
  const result = await runAction(() => addContact(parsed));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const change = z.object({ id: z.string().min(1).max(40), version: z.number().int().nonnegative(), values: input });

export async function updateContactAction(payload: z.infer<typeof change>) {
  const parsed = change.parse(payload);
  const result = await runAction(() => updateContact(parsed.id, parsed.version, parsed.values));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const removal = z.object({ id: z.string().min(1).max(40), version: z.number().int().nonnegative() });

export async function removeContactAction(payload: z.infer<typeof removal>) {
  const parsed = removal.parse(payload);
  const result = await runAction(() => removeContact(parsed.id, parsed.version));

  refresh();
  return result;
}
