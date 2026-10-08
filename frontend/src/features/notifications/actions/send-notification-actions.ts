'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { searchCustomers } from '@/features/customers/api/searchCustomers';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { sendNotification } from '../api/sendNotification';
import type { Recipient, SendResult } from '../types/notification.types';

/**
 * The send page's two calls: finding customers to send to (`GET /staff/customers?q=`, which needs
 * `customer:read`) and sending (`notify:send`). The backend checks both scopes again.
 */

/** How many matches the picker shows. A recipient is found by a precise query, not by paging. */
const SEARCH_LIMIT = 10;

const searchInput = z.object({ q: z.string().trim().min(1).max(100) });

export async function findRecipientsAction(values: z.infer<typeof searchInput>): Promise<ActionResult<Recipient[]>> {
  const { q } = searchInput.parse(values);
  const result = await runAction(() => searchCustomers({ q, limit: SEARCH_LIMIT }));

  return result.ok
    ? { ok: true, data: result.data.data.map((customer) => ({ id: customer.id, name: customer.display_name, phone: customer.phone })) }
    : result;
}

const sendInput = z.object({
  template: z.string().min(1).max(60),
  customer_ids: z.array(z.string().min(1).max(40)).min(1).max(100),
  channel: z.enum(['auto', 'line', 'sms', 'email']),
  variables: z.record(z.string().max(60), z.string().max(1000)),
});

/** Sends, then refreshes the log so the new rows are there when the person looks. */
export async function sendNotificationAction(values: z.infer<typeof sendInput>): Promise<ActionResult<SendResult>> {
  const input = sendInput.parse(values);

  // Empty optional values are left out rather than sent as blanks.
  const variables = Object.fromEntries(Object.entries(input.variables).filter(([, value]) => value.trim() !== ''));
  const result = await runAction(() => sendNotification({ ...input, variables }));

  revalidatePath('/notifications/log');
  return result;
}
