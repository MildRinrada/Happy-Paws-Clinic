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
  created_by_staff_id: string;
  created_at: string;
}

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
  created_by_staff_id: string;
  created_at: string;
}

export type PointsEntryKind = 'earn' | 'reverse' | 'redeem' | 'restore';

export interface PointsEntry {
  id: string;
  kind: PointsEntryKind;
  /** Signed: what this entry added to or took from the balance. */
  points: number;
  invoice_id: string | null;
  invoice_number: string | null;
  note: string | null;
  occurred_at: string;
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
}

/** `GET /staff/loyalty/{customer_id}`. Lists are newest first, at most 50 each. */
export interface CustomerLoyalty {
  customer_id: string;
  points_balance: number;
  lifetime_points: number;
  tier: LoyaltyTier;
  next_tier: { tier: LoyaltyTier; points_needed: number } | null;
  rules: LoyaltyRules;
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
