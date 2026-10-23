import { day, nowIso, table, type Row } from '../../core/db';
import { fail } from '../../core/http';
import { couponRedemptions, packages, pointsEntries } from './store';

/**
 * Loyalty points, tiers and packages. Points are a ledger (`crm_points_entries`); the balance, the
 * tier and what expired are worked out from it on every read, oldest points spent first, so an
 * adjustment, a redemption or a reversal from any module reads back consistently.
 */

export type Tier = 'member' | 'silver' | 'gold';

export interface LoyaltySettingsRow {
  satang_per_point: number;
  point_value_satang: number;
  silver_min_points: number;
  gold_min_points: number;
  points_expire_after_months?: number;
  referral_enabled: boolean;
  referral_referrer_points: number;
  referral_friend_points: number;
  referral_monthly_cap: number;
  version: number;
  updated_at?: string;
}

/**
 * The programme's rules. They are clinic settings, kept and served by the clinic module
 * (`clinic_settings` row `loyalty`, `GET/PUT /clinic/settings/loyalty`); read fresh on every use so a
 * change there applies to the next bill. The defaults stand in until that row exists.
 */
export const loyaltySettings = {
  get(): LoyaltySettingsRow {
    const row = table('clinic_settings').get('loyalty');
    return {
      satang_per_point: 2_500,
      point_value_satang: 100,
      silver_min_points: 500,
      gold_min_points: 2_000,
      points_expire_after_months: 24,
      referral_enabled: true,
      referral_referrer_points: 100,
      referral_friend_points: 50,
      referral_monthly_cap: 10,
      version: 0,
      ...(row as Partial<LoyaltySettingsRow> | undefined),
    };
  },
};

export function rules() {
  const s = loyaltySettings.get();
  return {
    satang_per_point: s.satang_per_point,
    point_value_satang: s.point_value_satang,
    tiers: [
      { tier: 'member' as Tier, min_points: 0 },
      { tier: 'silver' as Tier, min_points: s.silver_min_points },
      { tier: 'gold' as Tier, min_points: s.gold_min_points },
    ],
    ...(s.points_expire_after_months ? { points_expire_after_months: s.points_expire_after_months } : {}),
  };
}

export function tierOf(lifetime: number): Tier {
  const s = loyaltySettings.get();
  return lifetime >= s.gold_min_points ? 'gold' : lifetime >= s.silver_min_points ? 'silver' : 'member';
}

export function entriesOf(customerId: string): Row[] {
  return pointsEntries()
    .where((e) => e.customer_id === customerId)
    .sort((a, b) => String(a.occurred_at).localeCompare(String(b.occurred_at)));
}

/** The balance, the lifetime total, what ran out, and the next points to run out. */
export function balanceOf(customerId: string) {
  const now = nowIso();
  const lots: Array<{ points: number; expires_at?: string }> = [];
  let expired = 0;
  let lifetime = 0;
  const expireBefore = (instant: string) => {
    for (const lot of lots) {
      if (lot.points > 0 && lot.expires_at && lot.expires_at <= instant) {
        expired += lot.points;
        lot.points = 0;
      }
    }
  };
  for (const entry of entriesOf(customerId)) {
    expireBefore(entry.occurred_at);
    const points = Number(entry.points) || 0;
    if (entry.kind === 'earn' || entry.kind === 'referral' || (entry.kind === 'adjust' && points > 0)) lifetime += points;
    if (entry.kind === 'reverse') lifetime += points;
    if (points > 0) {
      lots.push({ points, expires_at: entry.expires_at });
    } else {
      let owed = -points;
      for (const lot of lots) {
        if (owed <= 0) break;
        const take = Math.min(lot.points, owed);
        lot.points -= take;
        owed -= take;
      }
    }
  }
  expireBefore(now);
  const balance = lots.reduce((sum, lot) => sum + lot.points, 0);
  const next = lots
    .filter((lot) => lot.points > 0 && lot.expires_at)
    .sort((a, b) => a.expires_at!.localeCompare(b.expires_at!))[0];
  return {
    points_balance: balance,
    lifetime_points: Math.max(0, lifetime),
    expired_points: expired,
    ...(next ? { next_expiry: { points: next.points, expires_at: next.expires_at! } } : {}),
  };
}

export function tierOfCustomer(customerId: string): Tier {
  return tierOf(balanceOf(customerId).lifetime_points);
}

function expiryFrom(instant: string): string | undefined {
  const months = loyaltySettings.get().points_expire_after_months;
  if (!months) return undefined;
  const date = new Date(instant);
  date.setMonth(date.getMonth() + months);
  return date.toISOString();
}

/** Writes one ledger row. `points` is signed. */
export function addPoints(
  customerId: string,
  kind: 'earn' | 'reverse' | 'redeem' | 'restore' | 'adjust' | 'referral',
  points: number,
  extra: { invoice_id?: string | null; invoice_number?: string | null; note?: string | null; staff_id?: string; occurred_at?: string } = {},
): Row {
  const occurred = extra.occurred_at ?? nowIso();
  const expires = points > 0 && kind !== 'restore' ? expiryFrom(occurred) : undefined;
  return pointsEntries().insert({
    customer_id: customerId,
    kind,
    points,
    invoice_id: extra.invoice_id ?? null,
    invoice_number: extra.invoice_number ?? null,
    note: extra.note ?? null,
    occurred_at: occurred,
    ...(expires ? { expires_at: expires } : {}),
    ...(extra.staff_id ? { staff_id: extra.staff_id } : {}),
  });
}

/**
 * Points for a paid bill, once per bill: what billing calls when an invoice is paid in full. Answers
 * the ledger row, or undefined when the bill earned nothing or was already counted.
 */
export function earnPointsForInvoice(invoice: { id: string; number?: string; customer_id: string; paid_satang?: number; total_satang?: number }, occurredAt?: string): Row | undefined {
  if (pointsEntries().find((e) => e.invoice_id === invoice.id && e.kind === 'earn')) return undefined;
  const paid = Number(invoice.paid_satang ?? invoice.total_satang) || 0;
  const points = Math.floor(paid / loyaltySettings.get().satang_per_point);
  if (points <= 0) return undefined;
  return addPoints(invoice.customer_id, 'earn', points, { invoice_id: invoice.id, invoice_number: invoice.number ?? null, occurred_at: occurredAt });
}

/**
 * Points spent on a bill, for billing's points discount: refused with `409 insufficient_points` past
 * the balance. Answers the ledger row and the baht they are worth.
 */
export function redeemPoints(customerId: string, points: number, invoice: { id: string; number?: string }): { entry: Row; amount_satang: number } {
  const balance = balanceOf(customerId).points_balance;
  if (!Number.isInteger(points) || points <= 0) fail(422, 'validation_failed', 'จำนวนแต้มไม่ถูกต้อง', { field: 'points' });
  if (points > balance) fail(409, 'insufficient_points', `ลูกค้ามีแต้มคงเหลือ ${balance.toLocaleString('th-TH')} แต้ม`);
  const entry = addPoints(customerId, 'redeem', -points, { invoice_id: invoice.id, invoice_number: invoice.number ?? null, note: 'แลกแต้มเป็นส่วนลด' });
  return { entry, amount_satang: points * loyaltySettings.get().point_value_satang };
}

/** The points of a bill taken back, for a refund or a void. */
export function reversePointsForInvoice(invoiceId: string): Row | undefined {
  const earned = pointsEntries().where((e) => e.invoice_id === invoiceId && (e.kind === 'earn' || e.kind === 'reverse'));
  const net = earned.reduce((sum, e) => sum + (Number(e.points) || 0), 0);
  if (net <= 0) return undefined;
  const first = earned[0]!;
  return addPoints(first.customer_id, 'reverse', -net, { invoice_id: invoiceId, invoice_number: first.invoice_number, note: 'คืนแต้มจากการคืนเงิน' });
}

// ---- Packages ---------------------------------------------------------------------------------------

/**
 * A package's status as of today: usable once its sale bill is paid (read from billing's invoice),
 * finished when used up, past its date when expired. Stored back when it changed.
 */
export function refreshPackage(row: Row): Row {
  let next = row;
  if (row.status === 'pending_payment') {
    const invoice = table('invoices').get(row.invoice_id);
    if (invoice && (invoice.status === 'paid' || (invoice.balance_satang === 0 && invoice.total_satang > 0))) {
      next = { ...next, status: 'active', activated_at: invoice.paid_at ?? invoice.updated_at ?? nowIso() };
    }
  }
  if (next.status === 'active' && next.remaining <= 0) next = { ...next, status: 'used_up' };
  if ((next.status === 'active' || next.status === 'pending_payment') && next.expires_on && next.expires_on < day(0)) {
    next = { ...next, status: 'expired' };
  }
  if (next !== row) packages().put(next);
  return next;
}

const STATUS_ORDER: Record<string, number> = { active: 0, pending_payment: 1, used_up: 2, expired: 3, cancelled: 4 };

/** A customer's packages, usable first and soonest to expire first, then the finished ones. */
export function packagesOfCustomer(customerId: string): Row[] {
  return packages()
    .where((p) => p.customer_id === customerId)
    .map(refreshPackage)
    .sort(
      (a, b) =>
        (STATUS_ORDER[a.status] ?? 9) - (STATUS_ORDER[b.status] ?? 9) ||
        String(a.expires_on ?? '9999').localeCompare(String(b.expires_on ?? '9999')) ||
        String(b.purchased_at).localeCompare(String(a.purchased_at)),
    );
}

export function packageView(row: Row) {
  const { note: _note, version: _version, ...rest } = row;
  return rest;
}

/** `GET /portal/packages`'s entry: the owner's view of a package. */
export function portalPackageView(row: Row) {
  return {
    id: row.id,
    name: row.name,
    pet_id: row.pet_id ?? null,
    service_ids: row.service_ids,
    total_uses: row.total_uses,
    used_count: row.used_count,
    remaining: row.remaining,
    purchased_at: row.purchased_at,
    expires_on: row.expires_on ?? null,
    status: row.status,
  };
}

/**
 * The usable package that covers a service for a pet, if any: what billing asks when a bill line
 * could be paid from a package.
 */
export function usablePackageFor(customerId: string, serviceId: string, petId?: string | null): Row | undefined {
  return packagesOfCustomer(customerId).find(
    (p) => p.status === 'active' && p.remaining > 0 && p.service_ids.includes(serviceId) && (!p.pet_id || !petId || p.pet_id === petId),
  );
}

/** Takes one use off a package. Answers the package after, or undefined when it had none left. */
export function usePackage(packageId: string): Row | undefined {
  const row = packages().get(packageId);
  if (!row || row.status !== 'active' || row.remaining <= 0) return undefined;
  const used = row.used_count + 1;
  return refreshPackage(packages().update(packageId, { used_count: used, remaining: row.total_uses - used })!);
}

/** The redemptions of coupons on a customer's bills, newest first. */
export function redemptionsOf(customerId: string): Row[] {
  return couponRedemptions()
    .where((r) => r.customer_id === customerId)
    .sort((a, b) => String(b.redeemed_at).localeCompare(String(a.redeemed_at)));
}

/** `GET /staff/loyalty/{customer_id}`. */
export function loyaltyView(customerId: string) {
  const balance = balanceOf(customerId);
  const tier = tierOf(balance.lifetime_points);
  const s = loyaltySettings.get();
  const next =
    tier === 'member'
      ? { tier: 'silver' as Tier, points_needed: s.silver_min_points - balance.lifetime_points }
      : tier === 'silver'
        ? { tier: 'gold' as Tier, points_needed: s.gold_min_points - balance.lifetime_points }
        : null;
  return {
    customer_id: customerId,
    points_balance: balance.points_balance,
    lifetime_points: balance.lifetime_points,
    tier,
    next_tier: next,
    rules: rules(),
    expired_points: balance.expired_points,
    ...(balance.next_expiry ? { next_expiry: balance.next_expiry } : {}),
    history: entriesOf(customerId)
      .reverse()
      .slice(0, 50)
      .map(({ customer_id: _c, ...entry }) => entry),
    coupon_redemptions: redemptionsOf(customerId)
      .slice(0, 50)
      .map(({ customer_id: _c, ...r }) => r),
    packages: packagesOfCustomer(customerId).slice(0, 50).map(packageView),
  };
}
