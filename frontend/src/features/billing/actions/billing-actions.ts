'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { collectPayment } from '../api/collectPayment';
import { issueReceipt } from '../api/issueReceipt';
import { voidInvoice } from '../api/voidInvoice';

/**
 * What the cash desk can do, as server actions.
 *
 * As thin as the queue's, and for a stronger reason: nothing about money is decided here. Whether
 * an amount may be taken against a bill, whether a bill may be voided, what its status is
 * afterwards — the invoice aggregate on the backend answers all of it and refuses with a reason.
 * These parse the arguments, make the one call, and mark the pages stale.
 */

function revalidate(invoiceId: string) {
  revalidatePath('/billing');
  revalidatePath(`/billing/${invoiceId}`);
}

const payment = z.object({
  invoiceId: z.string().min(1),
  version: z.number().int().nonnegative(),
  method: z.enum(['cash', 'card', 'promptpay']),

  // Whole satang, and at least one. The upper bound is the backend's to judge against the bill.
  amountSatang: z.number().int().positive(),
  reference: z.string().trim().max(100).optional(),
  idempotencyKey: z.uuid(),
});

export async function collectPaymentAction(input: z.infer<typeof payment>) {
  const parsed = payment.parse(input);
  const result = await runAction(() => collectPayment(parsed));

  revalidate(parsed.invoiceId);

  return result;
}

const receipt = z.object({
  invoiceId: z.string().min(1),
  taxInvoice: z
    .object({
      taxId: z.string().trim().min(1).max(20),
      legalName: z.string().trim().min(1).max(200),
      billingAddress: z.string().trim().min(1).max(500),
    })
    .optional(),
});

export async function issueReceiptAction(input: z.infer<typeof receipt>) {
  const parsed = receipt.parse(input);
  const result = await runAction(() => issueReceipt(parsed));

  revalidate(parsed.invoiceId);

  return result;
}

const voiding = z.object({
  invoiceId: z.string().min(1),
  version: z.number().int().nonnegative(),
  reason: z.string().trim().min(1).max(300),
});

export async function voidInvoiceAction(input: z.infer<typeof voiding>) {
  const parsed = voiding.parse(input);
  const result = await runAction(() => voidInvoice(parsed.invoiceId, parsed.version, parsed.reason));

  revalidate(parsed.invoiceId);

  return result;
}
