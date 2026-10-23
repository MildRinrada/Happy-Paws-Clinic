/**
 * The customers module: the register, the patients, the privacy notice with consents and data
 * requests, and refer-a-friend. Routes live in the files below; helpers for other modules are in
 * store.ts (customers, pets, consents), referrals.ts and privacy.ts.
 */
import './customers-handlers';
import './pets-handlers';
import './privacy-handlers';
import './referral-handlers';

export * from './store';
export { currentPolicyLabel } from './privacy';
export { findReferralCode, recordReferral, referralCodeOf } from './referrals';
