import { publicApi } from '@/lib/api/server-client';

/** `link_sent` after the first call, `reset` after the second. */
export interface ResetPasswordResult {
  status: 'link_sent' | 'reset';
}

/**
 * `POST /auth/password/reset`, first call: asks for a link to be sent to an email or phone.
 * Always `202 link_sent`, whether or not anyone has that email or number, so a stranger cannot
 * use it to learn who is a customer. At most three links per account per ten minutes; the rest
 * are quietly not sent.
 */
export function requestPasswordResetLink(identifier: string): Promise<ResetPasswordResult> {
  return publicApi().post<ResetPasswordResult>('/auth/password/reset', { body: { identifier } });
}

/**
 * `POST /auth/password/reset`, second call: the link's token and the new password. A token that is
 * wrong, used or past its thirty minutes is `422` on the field `token`, one message for all three.
 * Every session the account had is ended.
 */
export function setPasswordFromLink(token: string, newPassword: string): Promise<ResetPasswordResult> {
  return publicApi().post<ResetPasswordResult>('/auth/password/reset', {
    body: { token, new_password: newPassword },
  });
}
