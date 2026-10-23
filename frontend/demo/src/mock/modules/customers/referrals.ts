import type { ReferralChannel, ReferralRejectReason, StaffReferral } from '@/features/referrals/types/referral.types';
import { nowIso, table, type Row } from '../../core/db';
import { customers, referralCodes, type ReferralCodeRow } from './store';

/** Refer-a-friend (docs/contract-extensions.md § 39): each customer's code, and who came with one. */

export type ReferralRow = Row & StaffReferral;

export const referrals = () => table<ReferralRow>('referrals');

/** No 0, O, 1, I or L, as the backend issues them. */
const ALPHABET = '23456789ABCDEFGHJKMNPQRSTUVWXYZ';

function newCode(): string {
  for (;;) {
    let code = '';
    for (let index = 0; index < 8; index += 1) code += ALPHABET[Math.floor(Math.random() * ALPHABET.length)];
    if (!referralCodes().find((row) => row.code === code)) return code;
  }
}

/** The customer's code, made on first use. */
export function referralCodeOf(customerId: string, createdAt = nowIso()): ReferralCodeRow {
  return referralCodes().find((row) => row.customer_id === customerId) ?? referralCodes().insert({ customer_id: customerId, code: newCode(), created_at: createdAt });
}

export function findReferralCode(code: string): ReferralCodeRow | undefined {
  const cleaned = code.replace(/[\s-]/g, '').toUpperCase();
  return referralCodes().find((row) => row.code === cleaned);
}

/**
 * The programme's settings live with loyalty (the crm module's `GET /clinic/settings/loyalty`).
 * Read from its row when there is one; the demo runs the programme switched on otherwise.
 */
export function referralSettings() {
  const row = table('loyalty_settings').all()[0] ?? table('clinic_settings').all().find((entry) => 'referral_enabled' in entry);
  return {
    enabled: typeof row?.referral_enabled === 'boolean' ? (row.referral_enabled as boolean) : true,
    referrer_points: Number(row?.referral_referrer_points ?? 100),
    friend_points: Number(row?.referral_friend_points ?? 100),
    monthly_cap: Number(row?.referral_monthly_cap ?? 5),
  };
}

export function referralLink(code: string): string {
  return `${location.origin}/r/${code}`;
}

/**
 * Records that a new customer came with a friend's code, from a guest booking or a registration.
 * Refuses the obvious abuse as the backend does, recording the attempt as `rejected`. Answers the
 * row, or `undefined` for a code that does not exist.
 */
export function recordReferral(input: { code: string; friendCustomerId: string; channel: ReferralChannel }): ReferralRow | undefined {
  const owner = findReferralCode(input.code);
  if (!owner) return undefined;
  const settings = referralSettings();
  const referrer = customers().get(owner.customer_id);
  const friend = customers().get(input.friendCustomerId);
  let reason: ReferralRejectReason | undefined;
  if (!settings.enabled) reason = 'programme_off';
  else if (owner.customer_id === input.friendCustomerId) reason = 'self_referral';
  else if (referrer && friend && referrer.phone === friend.phone) reason = 'shared_contact';
  else if (referrals().find((row) => row.friend.customer_id === input.friendCustomerId && row.status !== 'rejected')) reason = 'not_new_customer';
  const now = nowIso();
  return referrals().insert({
    referrer: { customer_id: owner.customer_id, name: referrer?.display_name },
    friend: { customer_id: input.friendCustomerId, name: friend?.display_name },
    code: owner.code,
    channel: input.channel,
    status: reason ? 'rejected' : 'pending',
    ...(reason ? { reject_reason: reason, rejected_at: now } : {}),
    referrer_points: 0,
    friend_points: 0,
    over_monthly_cap: false,
    created_at: now,
  });
}

/** The local calendar month of an instant, `2026-10`. */
function monthOf(iso: string): string {
  const date = new Date(iso);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
}

/**
 * A customer's bill was paid in full (`ReferralRewarder.InvoicePaidAsync`): the pending referral
 * they came with is closed on that bill. Rewarded, with the friend's points and the referrer's
 * unless the referrer has already been paid for `monthlyCap` friends this calendar month (then
 * `over_monthly_cap`, no referrer points); or rejected `programme_off` while the programme is off.
 * A bill paid before the friend came with the code does not count.
 *
 * The loyalty rules are the CRM module's, so it passes them in, and it adds the points this answers
 * on the row (`referrer_points`, `friend_points`) to its ledger. Undefined when nothing was pending.
 */
export function rewardReferral(input: {
  friendCustomerId: string;
  invoiceId: string;
  paidAt: string;
  rules: { enabled: boolean; referrerPoints: number; friendPoints: number; monthlyCap: number };
}): ReferralRow | undefined {
  const referral = referrals().find((row) => row.friend.customer_id === input.friendCustomerId && row.status === 'pending' && row.created_at <= input.paidAt);
  if (!referral) return undefined;
  if (!input.rules.enabled) return referrals().update(referral.id, { status: 'rejected', reject_reason: 'programme_off', rejected_at: input.paidAt });
  const month = monthOf(input.paidAt);
  const paidThisMonth = referrals().where(
    (row) =>
      row.referrer.customer_id === referral.referrer.customer_id &&
      row.status === 'rewarded' &&
      row.referrer_points > 0 &&
      Boolean(row.rewarded_at) &&
      monthOf(row.rewarded_at!) === month,
  ).length;
  const overCap = paidThisMonth >= input.rules.monthlyCap;
  return referrals().update(referral.id, {
    status: 'rewarded',
    reward_invoice_id: input.invoiceId,
    rewarded_at: input.paidAt,
    referrer_points: overCap ? 0 : input.rules.referrerPoints,
    friend_points: input.rules.friendPoints,
    over_monthly_cap: overCap,
  });
}
