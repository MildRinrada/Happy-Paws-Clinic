import { publicApi } from '@/lib/api/server-client';

/** What the backend answers: when the code it sent (or would have sent) stops working. */
export interface OtpRequested {
  expires_at: string;
}

/**
 * `POST /auth/otp/request` with purpose `register`. Anonymous. The answer is the same whether or
 * not the number has an account; an existing account is only reported later, at
 * `POST /auth/register`, once the caller has proved they hold the number.
 *
 * Three codes per number per ten minutes. The fourth is `429 rate_limited`, with a Thai message
 * that the page shows as it is.
 */
export function requestRegisterOtp(phone: string): Promise<OtpRequested> {
  return publicApi().post<OtpRequested>('/auth/otp/request', { body: { phone, purpose: 'register' } });
}
