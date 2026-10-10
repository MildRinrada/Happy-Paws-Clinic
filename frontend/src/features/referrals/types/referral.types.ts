/**
 * Shapes of the refer-a-friend routes (docs/contract-extensions.md § 39). Field names are the API's
 * snake_case ones.
 */

/** `GET /portal/referral`: the customer's own code and counts. No friend is ever named. */
export interface MyReferral {
  enabled: boolean;
  code: string;
  link: string;
  invited: number;
  rewarded: number;
  pending: number;
  points_earned: number;
  monthly_cap: number;
  rewarded_this_month: number;
  referrer_points: number;
  friend_points: number;
}

/** `GET /referrals/{code}`: valid or not, and nothing else. */
export interface ReferralCodeCheck {
  valid: boolean;
}

export type ReferralStatus = 'pending' | 'rewarded' | 'rejected';

export type ReferralRejectReason = 'self_referral' | 'not_new_customer' | 'shared_contact' | 'programme_off';

export type ReferralChannel = 'guest_booking' | 'register';

export interface ReferralParty {
  customer_id: string;
  name?: string;
}

/** One row of `GET /staff/referrals`. */
export interface StaffReferral {
  id: string;
  referrer: ReferralParty;
  friend: ReferralParty;
  code: string;
  channel: ReferralChannel;
  status: ReferralStatus;
  reject_reason?: ReferralRejectReason;
  referrer_points: number;
  friend_points: number;
  over_monthly_cap: boolean;
  reward_invoice_id?: string;
  created_at: string;
  rewarded_at?: string;
  rejected_at?: string;
}

export interface StaffReferralPage {
  data: StaffReferral[];
  page: { next_cursor: string | null };
}
