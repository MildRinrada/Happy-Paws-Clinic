/** Characters a referral code is made of: no 0, O, 1, I or L (docs/contract-extensions.md § 39). */
const CODE = /^[2-9A-HJKMNP-Z]{8}$/;

/**
 * A code from an address (`/r/{code}` or `?ref=`) in the form the backend stores, or undefined when
 * it cannot be one, so a malformed value is never sent anywhere. Case, spaces and dashes are
 * forgiven, as on the backend.
 */
export function readReferralCode(value: string | string[] | undefined): string | undefined {
  if (typeof value !== 'string' || value.length > 32) return undefined;

  const cleaned = value.replace(/[\s-]/g, '').toUpperCase();
  return CODE.test(cleaned) ? cleaned : undefined;
}
