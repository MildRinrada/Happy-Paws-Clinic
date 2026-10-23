import { DEMO_ACCOUNTS, DEMO_PASSWORD } from '../../core/demo-accounts';
import { users } from '../../core/auth';
import { at } from '../../core/db';

/** The demo bar's accounts. Their memberships come from the staff and customers seeds. */
export const order = -10;

export function seed() {
  for (const account of Object.values(DEMO_ACCOUNTS)) {
    users().insert({
      id: account.user_id,
      display_name: account.display_name,
      email: account.email ?? null,
      phone: account.phone ?? null,
      password: DEMO_PASSWORD,
      status: 'active',
      two_factor: { enabled: account.two_factor, method: account.two_factor ? 'totp' : 'none', ...(account.two_factor ? { recovery_codes_left: 10 } : {}) },
      language: 'th',
      notifications: { email: true, sms: true, line: account.key === 'customer' },
      version: 1,
      created_at: at(-400, '09:00'),
    });
  }
}
