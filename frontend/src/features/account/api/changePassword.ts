import { staffApi } from '@/lib/api/server-client';

/**
 * `POST /me/password`. Answers 204 and signs out every other device. A wrong current password is
 * `401`, the same status as an expired session, so the caller must not treat that 401 as "signed
 * out" (see `changePasswordAction`).
 */
export function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  return staffApi().post<void>('/me/password', {
    body: { current_password: currentPassword, new_password: newPassword },
  });
}
