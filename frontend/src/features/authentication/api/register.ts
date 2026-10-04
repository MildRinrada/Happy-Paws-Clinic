import { postRegister as postRegisterBody, type SignedIn } from '@/lib/api/auth-transport';

/**
 * `POST /auth/register`: the call that opens a customer's account and signs them straight in.
 *
 * The request goes through `lib/api/auth-transport.ts`, the one place allowed to read the refresh
 * token out of `Set-Cookie`. Server-side only.
 */

export interface RegisterInput {
  phone: string;
  code: string;
  displayName: string;
  password: string;
  email?: string;
}

/**
 * Registers, and answers the new session. Refusals are thrown as `ApiError`: a wrong or expired
 * code is `422` on `code`, a number or email that already has an account is `409 contact_taken`.
 * The answer type keeps `null` so the caller's "account made but no session" path stays valid if
 * the backend ever stops signing new customers in.
 */
export function postRegister(input: RegisterInput): Promise<SignedIn | null> {
  return postRegisterBody({
    phone: input.phone,
    code: input.code,
    display_name: input.displayName,
    password: input.password,
    email: input.email,
  });
}
