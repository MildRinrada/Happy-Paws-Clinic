import { staffApi } from '@/lib/api/server-client';
import type { Me, NotificationChannels } from '../types/account.types';

export interface MeChanges {
  display_name?: string;
  email?: string;
  /** An empty string removes the phone number. */
  phone?: string;
  /** The language messages to this person are written in. */
  language?: 'th' | 'en';
  notifications?: NotificationChannels;
  /** Required by the backend whenever the email or phone actually changes. */
  current_password?: string;
}

/**
 * `PATCH /me` — the only way to change one's own name, email or phone. Absent fields are left as
 * they are. `version` goes in `If-Match`, so an edit from another device is not overwritten.
 */
export function updateMe(version: number | undefined, changes: MeChanges): Promise<Me> {
  return staffApi().patch<Me>('/me', { ifMatch: version, body: changes });
}
