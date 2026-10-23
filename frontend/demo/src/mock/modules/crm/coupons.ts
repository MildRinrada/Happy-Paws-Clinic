import { day, nowIso, type Row } from '../../core/db';
import { fail } from '../../core/http';
import { couponRedemptions, coupons } from './store';

/**
 * Coupons and their redemptions. Billing's `POST /staff/invoices/{id}/discounts` with a
 * `coupon_code` calls `couponDiscountFor` to price it and `recordCouponRedemption` once the
 * discount is on the bill; a void or refund calls `releaseCouponRedemption`.
 */

export function couponStatus(row: Row): string {
  if (row.deactivated_at) return 'deactivated';
  if (row.valid_from > day(0)) return 'scheduled';
  if (row.valid_until && row.valid_until < day(0)) return 'expired';
  if (row.max_redemptions && row.redeemed_count >= row.max_redemptions) return 'used_up';
  return 'active';
}

export function couponView(row: Row) {
  return {
    ...row,
    status: couponStatus(row),
    ...(row.max_redemptions ? { remaining: Math.max(0, row.max_redemptions - row.redeemed_count) } : {}),
  };
}

export function findCoupon(code: string): Row | undefined {
  const cleaned = code.trim().toUpperCase();
  return coupons().find((c) => c.code === cleaned);
}

/**
 * What a coupon takes off a bill, or a refusal in the backend's words: unknown, not yet valid,
 * expired, switched off, used up, used too often by this customer, or another branch's.
 */
export function couponDiscountFor(
  code: string,
  invoice: { customer_id: string; branch_id: string; lines: Array<{ line_total_satang: number; source_id?: string }>; total_satang?: number },
): { coupon: Row; amount_satang: number; reason: string } {
  const coupon = findCoupon(code);
  if (!coupon) fail(422, 'coupon_not_found', 'ไม่พบคูปองรหัสนี้', { field: 'coupon_code' });
  const status = couponStatus(coupon);
  if (status === 'deactivated') fail(409, 'coupon_deactivated', 'คูปองนี้ถูกปิดการใช้งานแล้ว');
  if (status === 'scheduled') fail(409, 'coupon_not_started', `คูปองนี้เริ่มใช้ได้วันที่ ${coupon.valid_from}`);
  if (status === 'expired') fail(409, 'coupon_expired', 'คูปองนี้หมดอายุแล้ว');
  if (status === 'used_up') fail(409, 'coupon_used_up', 'คูปองนี้ถูกใช้ครบจำนวนแล้ว');
  if (coupon.branch_id && coupon.branch_id !== invoice.branch_id) fail(409, 'coupon_other_branch', 'คูปองนี้ใช้ได้เฉพาะอีกสาขา');
  if (coupon.per_customer_limit) {
    const used = couponRedemptions().where((r) => r.coupon_id === coupon.id && r.customer_id === invoice.customer_id && !r.released_at).length;
    if (used >= coupon.per_customer_limit) fail(409, 'coupon_customer_limit', 'ลูกค้าคนนี้ใช้คูปองนี้ครบสิทธิ์แล้ว');
  }
  const base = coupon.service_ids.length
    ? invoice.lines.filter((l) => l.source_id && coupon.service_ids.includes(l.source_id)).reduce((s, l) => s + l.line_total_satang, 0)
    : invoice.total_satang ?? invoice.lines.reduce((s, l) => s + l.line_total_satang, 0);
  if (base <= 0) fail(409, 'coupon_not_applicable', 'บิลนี้ไม่มีรายการที่คูปองครอบคลุม');
  const amount = coupon.percent ? Math.round((base * coupon.percent) / 100) : Math.min(base, coupon.amount_satang);
  return { coupon, amount_satang: amount, reason: `คูปอง ${coupon.code} · ${coupon.name}` };
}

export function recordCouponRedemption(input: { couponId: string; customerId: string; invoiceId: string; invoiceNumber?: string | null; amountSatang: number; at?: string }): Row {
  const coupon = coupons().get(input.couponId)!;
  coupons().update(coupon.id, { redeemed_count: coupon.redeemed_count + 1 });
  return couponRedemptions().insert({
    coupon_id: coupon.id,
    code: coupon.code,
    customer_id: input.customerId,
    invoice_id: input.invoiceId,
    invoice_number: input.invoiceNumber ?? null,
    amount_satang: input.amountSatang,
    redeemed_at: input.at ?? nowIso(),
    released_at: null,
  });
}

/** Gives a bill's coupon use back, for a void or a full refund. Answers how many were released. */
export function releaseCouponRedemption(invoiceId: string): number {
  let count = 0;
  for (const r of couponRedemptions().where((x) => x.invoice_id === invoiceId && !x.released_at)) {
    couponRedemptions().update(r.id, { released_at: nowIso() });
    const coupon = coupons().get(r.coupon_id);
    if (coupon) coupons().update(coupon.id, { redeemed_count: Math.max(0, coupon.redeemed_count - 1) });
    count++;
  }
  return count;
}
