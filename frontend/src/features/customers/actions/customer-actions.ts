'use server';

import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { createCustomer } from '../api/createCustomer';
import { inviteCustomer } from '../api/inviteCustomer';
import { customerPaths } from '../lib/customer-paths';
import { REGISTERED_PARAM } from '../lib/registered-param';

/**
 * The counter's changes to the customer register. The backend owns the rules (what counts as a
 * duplicate, who may be invited); these check the shape, make the call and hand back the answer.
 */

const newCustomer = z.object({
  displayName: z.string().trim().min(1).max(200),
  phone: z.string().trim().min(1).max(32),
  email: z.string().trim().max(256),
  allowDuplicatePhone: z.boolean(),
});

/**
 * Registers the customer and opens their page, ready for the first pet. A `409
 * duplicate_customer` comes back as a result: its `details.existing` is what the form shows.
 */
export async function registerCustomerAction(values: z.infer<typeof newCustomer>): Promise<ActionResult> {
  const input = newCustomer.parse(values);
  const result = await runAction(() =>
    createCustomer({
      display_name: input.displayName,
      phone: input.phone,
      email: input.email || undefined,
      allow_duplicate_phone: input.allowDuplicatePhone || undefined,
    }),
  );

  if (!result.ok) {
    return result;
  }

  revalidatePath(customerPaths.list);
  redirect(`${customerPaths.detail(result.data.id)}?${REGISTERED_PARAM}=1`);
}

export async function inviteCustomerAction(values: { customerId: string }) {
  const { customerId } = z.object({ customerId: z.string().min(1).max(64) }).parse(values);
  const result = await runAction(() => inviteCustomer(customerId));

  revalidatePath(customerPaths.detail(customerId));
  return result;
}
