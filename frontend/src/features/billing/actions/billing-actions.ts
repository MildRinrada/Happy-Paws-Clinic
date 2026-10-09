'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { applyDiscount } from '../api/applyDiscount';
import { collectPayment } from '../api/collectPayment';
import { issueReceipt } from '../api/issueReceipt';
import { refundPayment } from '../api/refundPayment';
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

const discount = z
  .object({
    invoiceId: z.string().min(1),
    version: z.number().int().nonnegative(),
    reason: z.string().trim().min(1).max(500),
    approvedByStaffId: z.string().trim().min(1).max(40),
    amountSatang: z.number().int().positive().optional(),
    percent: z.number().positive().max(100).optional(),
  })
  // The API's own rule, restated so a malformed call never leaves the server: one or the other.
  .refine((value) => (value.amountSatang === undefined) !== (value.percent === undefined));

export async function applyDiscountAction(input: z.infer<typeof discount>) {
  const parsed = discount.parse(input);
  const result = await runAction(() => applyDiscount(parsed));

  // Marked stale on a refusal too: after a `version_conflict` the page re-reads the bill, so the
  // figures beside the form are the new ones by the time the cashier reads the message.
  revalidate(parsed.invoiceId);

  return result;
}

const coupon = z.object({
  invoiceId: z.string().min(1),
  version: z.number().int().nonnegative(),
  couponCode: z.string().trim().min(1).max(20),
});

/** "ใช้คูปอง": the code alone. The backend checks it and fills in the reason and approver. */
export async function applyCouponAction(input: z.infer<typeof coupon>) {
  const parsed = coupon.parse(input);
  const result = await runAction(() => applyDiscount(parsed));

  revalidate(parsed.invoiceId);

  return result;
}

const points = z.object({
  invoiceId: z.string().min(1),
  version: z.number().int().nonnegative(),
  points: z.number().int().positive(),
});

/** "แลกแต้ม": whole points, 1 point = ฿1. The backend refuses more than the customer holds. */
export async function redeemPointsAction(input: z.infer<typeof points>) {
  const parsed = points.parse(input);
  const result = await runAction(() => applyDiscount(parsed));

  revalidate(parsed.invoiceId);

  return result;
}

const refund = z.object({
  invoiceId: z.string().min(1),
  version: z.number().int().nonnegative(),
  amountSatang: z.number().int().positive(),
  reason: z.string().trim().min(1).max(500),
  method: z.enum(['cash', 'card', 'promptpay']).optional(),
  paymentId: z.string().trim().min(1).max(40).optional(),
  idempotencyKey: z.uuid(),
});

export async function refundPaymentAction(input: z.infer<typeof refund>) {
  const parsed = refund.parse(input);
  const result = await runAction(() => refundPayment(parsed));

  revalidate(parsed.invoiceId);

  return result;
}
