import type { LoyaltySettings, ReferralSettingsInput } from '../types/crm.types';

/** The refer-a-friend part of the loyalty form, as typed (docs/contract-extensions.md § 39). */
export interface ReferralDraft {
  enabled: boolean;
  referrerPoints: string;
  friendPoints: string;
  monthlyCap: string;
}

export type ReferralNumberField = 'referrerPoints' | 'friendPoints' | 'monthlyCap';

export const REFERRAL_POINTS_MAX = 10_000;
export const REFERRAL_CAP_MAX = 100;

export function referralDraftFrom(settings: LoyaltySettings): ReferralDraft {
  return {
    enabled: settings.referral_enabled,
    referrerPoints: String(settings.referral_referrer_points),
    friendPoints: String(settings.referral_friend_points),
    monthlyCap: String(settings.referral_monthly_cap),
  };
}

function wholeBetween(value: string, min: number, max: number): number | null {
  const parsed = Number(value.trim());
  return value.trim() !== '' && Number.isInteger(parsed) && parsed >= min && parsed <= max ? parsed : null;
}

/** The referral values of the request, or the fields that do not parse. The backend checks the same bounds. */
export function toReferralInput(draft: ReferralDraft): { input?: ReferralSettingsInput; bad: ReferralNumberField[] } {
  const referrer = wholeBetween(draft.referrerPoints, 0, REFERRAL_POINTS_MAX);
  const friend = wholeBetween(draft.friendPoints, 0, REFERRAL_POINTS_MAX);
  const cap = wholeBetween(draft.monthlyCap, 1, REFERRAL_CAP_MAX);

  const bad: ReferralNumberField[] = [];
  if (referrer === null) bad.push('referrerPoints');
  if (friend === null) bad.push('friendPoints');
  if (cap === null) bad.push('monthlyCap');

  if (referrer === null || friend === null || cap === null) {
    return { bad };
  }

  return {
    bad,
    input: {
      referral_enabled: draft.enabled,
      referral_referrer_points: referrer,
      referral_friend_points: friend,
      referral_monthly_cap: cap,
    },
  };
}
