import { parseBahtToSatang } from '@/lib/format/money';
import type { NewCouponInput } from '../types/crm.types';
import { parseCount } from './parse-count';

export type CouponValueType = 'percent' | 'amount';

/** A coupon as typed. */
export interface CouponDraft {
  code: string;
  name: string;
  valueType: CouponValueType;
  value: string;
  serviceIds: string[];
  maxRedemptions: string;
  perCustomerLimit: string;
  validFrom: string;
  validUntil: string;
  /** '' for every branch. */
  branchId: string;
}

export type CouponProblem = 'code' | 'name' | 'percent' | 'amount' | 'maxRedemptions' | 'perCustomerLimit' | 'validUntil';

export const emptyCouponDraft = (today: string, branchId: string): CouponDraft => ({
  code: '',
  name: '',
  valueType: 'percent',
  value: '',
  serviceIds: [],
  maxRedemptions: '',
  perCustomerLimit: '',
  validFrom: today,
  validUntil: '',
  branchId,
});

function parsePercent(input: string): number | null {
  const cleaned = input.replace(/[\s%]/g, '');
  const value = /^\d+(\.\d{1,2})?$/.test(cleaned) ? Number(cleaned) : NaN;

  return value > 0 && value <= 100 ? value : null;
}

function parseAmount(input: string): number | null {
  const satang = parseBahtToSatang(input);

  return satang !== null && satang > 0 ? satang : null;
}

/** A limit box: empty is "no limit", otherwise a whole number in range. `undefined` when it is neither. */
function parseLimit(input: string, max: number): number | null | undefined {
  if (!input.trim()) return null;
  const value = parseCount(input);

  return value !== null && value >= 1 && value <= max ? value : undefined;
}

/** Which parts break the API's own rules. The backend checks them again. */
export function couponProblems(draft: CouponDraft): CouponProblem[] {
  const problems: CouponProblem[] = [];
  const code = draft.code.trim();

  if (code && !/^[A-Za-z0-9-]{4,20}$/.test(code)) problems.push('code');
  if (!draft.name.trim()) problems.push('name');
  if (draft.valueType === 'percent' && parsePercent(draft.value) === null) problems.push('percent');
  if (draft.valueType === 'amount' && parseAmount(draft.value) === null) problems.push('amount');
  if (parseLimit(draft.maxRedemptions, 100_000) === undefined) problems.push('maxRedemptions');
  if (parseLimit(draft.perCustomerLimit, 100) === undefined) problems.push('perCustomerLimit');
  if (draft.validUntil && draft.validFrom && draft.validUntil < draft.validFrom) problems.push('validUntil');

  return problems;
}

/** The request body, once `couponProblems` is empty. */
export function toCouponInput(draft: CouponDraft): NewCouponInput {
  const code = draft.code.trim().toUpperCase();

  return {
    code: code || undefined,
    name: draft.name.trim(),
    ...(draft.valueType === 'percent'
      ? { percent: parsePercent(draft.value) ?? undefined }
      : { amount_satang: parseAmount(draft.value) ?? undefined }),
    service_ids: draft.serviceIds,
    max_redemptions: parseLimit(draft.maxRedemptions, 100_000) ?? null,
    per_customer_limit: parseLimit(draft.perCustomerLimit, 100) ?? null,
    valid_from: draft.validFrom || undefined,
    valid_until: draft.validUntil || null,
    branch_id: draft.branchId || null,
  };
}
