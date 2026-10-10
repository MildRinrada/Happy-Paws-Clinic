/**
 * Links the refer-a-friend pages hand out (docs/contract-extensions.md § 39). The staff list is
 * `crmPaths.referrals`, beside the other CRM pages.
 */
export const referralPaths = {
  loyaltySettings: '/settings/loyalty',

  /** The booking page with a friend's code carried along, or without one. */
  book: (code?: string) => (code ? `/book?${new URLSearchParams({ ref: code })}` : '/book'),

  /** LINE's share sheet with the message already written. */
  lineShare: (message: string) => `https://line.me/R/share?text=${encodeURIComponent(message)}`,
};
