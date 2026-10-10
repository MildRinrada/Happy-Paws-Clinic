import type { Invoice } from '@/features/billing/types/billing.types';

/** Where a sold package stands. It is usable only once its sale invoice is paid. */
export type PackageStatus = 'pending_payment' | 'active' | 'used_up' | 'expired' | 'cancelled';

/** The membership levels, lowest first. */
export type LoyaltyTier = 'member' | 'silver' | 'gold';

/** A package as the staff API returns it. */
export interface CrmPackage {
  id: string;
  name: string;
  customer_id: string;
  pet_id: string | null;
  service_ids: string[];
  total_uses: number;
  used_count: number;
  remaining: number;
  price_satang: number;
  branch_id: string;
  invoice_id: string;
  purchased_at: string;
  expires_on: string | null;
  activated_at: string | null;
  cancelled_at: string | null;
  status: PackageStatus;
}

/** `POST /staff/packages`. */
export interface SellPackageInput {
  customer_id: string;
  pet_id?: string | null;
  name: string;
  service_ids: string[];
  total_uses: number;
  price_satang: number;
  expires_on?: string | null;
  branch_id?: string | null;
  note?: string | null;
}

/** The package and the bill raised for it in the same step. */
export interface PackageSale {
  package: CrmPackage;
  invoice: Invoice;
}

/** `POST /staff/coupons`. Exactly one of `percent` and `amount_satang`. */
export interface NewCouponInput {
  code?: string;
  name: string;
  percent?: number;
  amount_satang?: number;
  service_ids?: string[];
  max_redemptions?: number | null;
  per_customer_limit?: number | null;
  valid_from?: string;
  valid_until?: string | null;
  branch_id?: string | null;
}

export interface Coupon {
  id: string;
  code: string;
  name: string;
  percent: number | null;
  amount_satang: number | null;
  service_ids: string[];
  max_redemptions: number | null;
  per_customer_limit: number | null;
  valid_from: string;
  valid_until: string | null;
  branch_id: string | null;
  redeemed_count: number;
  /** What is left of `max_redemptions`. Absent when unlimited. docs/contract-extensions.md § 25. */
  remaining?: number;
  status: CouponStatus;
  created_by_staff_id: string;
  created_at: string;
  deactivated_at?: string;
  deactivated_by_staff_id?: string;
  deactivation_reason?: string;
}

/** Where a coupon stands today. docs/contract-extensions.md § 25. */
export type CouponStatus = 'active' | 'scheduled' | 'used_up' | 'expired' | 'deactivated';

/** What a segment asks of a customer. Every field left out is not a condition. */
export interface SegmentCriteriaInput {
  species?: string[];
  inactive_days?: number;
  visited_within_days?: number;
  min_spend_satang?: number;
  tiers?: LoyaltyTier[];
  has_active_package?: boolean;
  branch_id?: string | null;
}

/** `POST /staff/segments`. */
export interface NewSegmentInput {
  name: string;
  criteria: SegmentCriteriaInput;
}

export interface Segment {
  id: string;
  name: string;
  criteria: {
    species: string[];
    inactive_days: number | null;
    visited_within_days: number | null;
    min_spend_satang: number | null;
    tiers: string[];
    has_active_package: boolean | null;
    branch_id: string | null;
  };
  /** Customers matching now. */
  customer_count: number;
  /** Of those, who agreed to marketing and has a LINE address: who a broadcast would reach. */
  reachable_count: number;
  /** When the two counts were taken: at saving for a list row, now for a single read (§ 25). */
  counted_at: string;
  created_by_staff_id: string;
  created_at: string;
}

/** `referral` is a refer-a-friend reward (docs/contract-extensions.md § 39). */
export type PointsEntryKind = 'earn' | 'reverse' | 'redeem' | 'restore' | 'adjust' | 'referral';

export interface PointsEntry {
  id: string;
  kind: PointsEntryKind;
  /** Signed: what this entry added to or took from the balance. */
  points: number;
  invoice_id: string | null;
  invoice_number: string | null;
  note: string | null;
  occurred_at: string;
  /** When the points this row added stop counting. Absent when they never do (§ 25). */
  expires_at?: string;
  staff_id?: string;
}

export interface CouponRedemption {
  id: string;
  coupon_id: string;
  code: string;
  invoice_id: string;
  invoice_number: string | null;
  amount_satang: number;
  redeemed_at: string;
  released_at: string | null;
}

export interface LoyaltyRules {
  satang_per_point: number;
  point_value_satang: number;
  tiers: { tier: LoyaltyTier; min_points: number }[];
  /** Absent when points never expire (§ 25). */
  points_expire_after_months?: number;
}

/**
 * `GET` and `PUT /clinic/settings/loyalty`. docs/contract-extensions.md § 25. The `referral_*`
 * values are refer a friend (§ 39).
 */
export interface LoyaltySettings {
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

/** The referral values of a `PUT`. Optional on the wire: one not sent keeps its stored value (§ 39). */
export type ReferralSettingsInput = Pick<
  LoyaltySettings,
  'referral_enabled' | 'referral_referrer_points' | 'referral_friend_points' | 'referral_monthly_cap'
>;

export type LoyaltySettingsInput = Omit<
  LoyaltySettings,
  'version' | 'updated_at' | 'points_expire_after_months' | keyof ReferralSettingsInput
> & {
  points_expire_after_months: number | null;
} & Partial<ReferralSettingsInput>;

/** `POST /staff/loyalty/{customer_id}/adjustments`: the row written and the balance after (§ 25). */
export interface PointsAdjustment {
  entry: PointsEntry;
  points_balance: number;
  lifetime_points: number;
}

/** `GET /staff/loyalty/{customer_id}`. Lists are newest first, at most 50 each. */
export interface CustomerLoyalty {
  customer_id: string;
  points_balance: number;
  lifetime_points: number;
  tier: LoyaltyTier;
  next_tier: { tier: LoyaltyTier; points_needed: number } | null;
  rules: LoyaltyRules;
  /** Points that ran out unspent (§ 25). */
  expired_points: number;
  /** The spendable points that expire soonest. Absent when none expire (§ 25). */
  next_expiry?: { points: number; expires_at: string };
  history: PointsEntry[];
  coupon_redemptions: CouponRedemption[];
  packages: CrmPackage[];
}

/** A service a package or coupon can cover. */
export interface ServiceOption {
  id: string;
  name: string;
}

/** A branch a coupon or segment can be limited to. */
export interface BranchOption {
  id: string;
  name: string;
}
