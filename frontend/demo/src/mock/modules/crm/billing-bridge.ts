import { nowIso, type Row } from '../../core/db';
import { createInvoice, onInvoiceEvent } from '../billing/invoices';
import { rewardReferral } from '../customers/referrals';
import { fireWebhookEvent } from './integration-routes';
import { addPoints, earnPointsForInvoice, loyaltySettings, refreshPackage, reversePointsForInvoice } from './loyalty';
import { packages } from './store';

/**
 * Where the CRM meets billing. A package sale raises its bill through billing's own `createInvoice`;
 * billing's invoice events give points when a bill is settled, take them back on a refund or a
 * void, make a package usable once its sale bill is paid, and pay a referral when the friend's
 * first bill is paid. The same events go out to the webhooks subscribed to `invoice.issued`,
 * `payment.succeeded` and `payment.refunded`.
 */

export function raisePackageInvoice(input: {
  customerId: string;
  petId?: string;
  branchId: string;
  staffId: string;
  description: string;
  priceSatang: number;
  note?: string;
}): Row {
  return createInvoice({
    customerId: input.customerId,
    branchId: input.branchId,
    staffId: input.staffId,
    petId: input.petId,
    note: input.note,
    lines: [{ description: input.description, quantity: 1, unit_price_satang: input.priceSatang, kind: 'other' }],
  });
}

onInvoiceEvent((event, invoice) => {
  const ref = { invoice_id: invoice.id, number: invoice.number, branch_id: invoice.branch_id, total_satang: invoice.total_satang };
  if (event === 'created') fireWebhookEvent('invoice.issued', ref);
  if (event === 'payment') fireWebhookEvent('payment.succeeded', { ...ref, paid_satang: invoice.paid_satang });
  if (event === 'refunded') fireWebhookEvent('payment.refunded', { ...ref, refunded_satang: invoice.refunded_satang });
  if (event === 'payment' && invoice.status === 'paid') {
    const paidAt = invoice.payments.map((p) => p.collected_at).sort().pop() ?? nowIso();
    earnPointsForInvoice(invoice, paidAt);
    for (const row of packages().where((p) => p.invoice_id === invoice.id)) refreshPackage(row);
    if (invoice.paid_satang > 0) payReferral(invoice.customer_id, invoice.id, paidAt);
  }
  if (event === 'refunded' || event === 'voided') {
    reversePointsForInvoice(invoice.id);
    if (event === 'voided') {
      for (const row of packages().where((p) => p.invoice_id === invoice.id && p.status === 'pending_payment')) {
        packages().update(row.id, { status: 'cancelled', cancelled_at: nowIso() });
      }
    }
  }
});

/**
 * The friend's bill was paid: the customers module closes their pending referral on it, and the
 * points it settles on go on both ledgers (`ReferralRewarder`; the notes name neither person).
 */
function payReferral(friendCustomerId: string, invoiceId: string, paidAt: string) {
  const settings = loyaltySettings.get();
  const referral = rewardReferral({
    friendCustomerId,
    invoiceId,
    paidAt,
    rules: {
      enabled: settings.referral_enabled,
      referrerPoints: settings.referral_referrer_points,
      friendPoints: settings.referral_friend_points,
      monthlyCap: settings.referral_monthly_cap,
    },
  });
  if (referral?.status !== 'rewarded') return;
  if (referral.referrer_points > 0) addPoints(referral.referrer.customer_id, 'referral', referral.referrer_points, { note: 'แต้มชวนเพื่อน', occurred_at: paidAt });
  if (referral.friend_points > 0) addPoints(referral.friend.customer_id, 'referral', referral.friend_points, { note: 'แต้มต้อนรับจากการชวนของเพื่อน', occurred_at: paidAt });
}
