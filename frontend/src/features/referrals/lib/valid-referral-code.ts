import { checkReferralCode } from '../api/checkReferralCode';
import { readReferralCode } from './read-referral-code';

/**
 * The code, when it is well formed and the backend says it can be used, otherwise undefined. A
 * failed check (the API down, the per-address cap reached) counts as not valid: the visitor still
 * books, just without the code, and never sees an error for it.
 */
export async function validReferralCode(value: string | string[] | undefined): Promise<string | undefined> {
  const code = readReferralCode(value);
  if (!code) return undefined;

  try {
    return (await checkReferralCode(code)).valid ? code : undefined;
  } catch {
    return undefined;
  }
}
