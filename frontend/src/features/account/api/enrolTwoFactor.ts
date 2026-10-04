import { staffApi } from '@/lib/api/server-client';
import type { TwoFactorEnrolment } from '../types/account.types';

/**
 * `POST /me/2fa/enroll`, called twice. Nothing is switched on by the first call: the new factor
 * only replaces the old one once a code from the app has proved it works, so someone who closes
 * the page before scanning is not locked out.
 */

/** Step 1: the current password in, a fresh secret out. Calling it again returns the same secret. */
export function beginTwoFactorEnrolment(currentPassword: string): Promise<TwoFactorEnrolment> {
  return staffApi().post<TwoFactorEnrolment>('/me/2fa/enroll', { body: { current_password: currentPassword } });
}

/** Step 2: a code from the app. Answers with the recovery codes, which the backend keeps only as hashes. */
export function confirmTwoFactorEnrolment(code: string): Promise<TwoFactorEnrolment> {
  return staffApi().post<TwoFactorEnrolment>('/me/2fa/enroll', { body: { code } });
}
