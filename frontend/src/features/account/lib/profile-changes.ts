import type { MeChanges } from '../api/updateMe';
import type { Me, NotificationChannels } from '../types/account.types';

/** The profile form's state. Every field is a plain value, so an empty box is `''`, not missing. */
export interface ProfileValues {
  display_name: string;
  email: string;
  phone: string;
  language: 'th' | 'en';
  notifications: NotificationChannels;
}

export function profileValues(me: Me): ProfileValues {
  return {
    display_name: me.display_name,
    email: me.email ?? '',
    phone: me.phone ?? '',
    language: me.language ?? 'th',
    notifications: me.notifications ?? { email: true, sms: true, line: true },
  };
}

const digits = (value: string) => value.replace(/\D/g, '');

/** Compared the way the backend stores them, so "081-234-5678" against "0812345678" is no change. */
export function emailChanged(saved: ProfileValues, values: ProfileValues): boolean {
  return values.email.trim().toLowerCase() !== saved.email.trim().toLowerCase();
}

export function phoneChanged(saved: ProfileValues, values: ProfileValues): boolean {
  return digits(values.phone) !== digits(saved.phone);
}

/**
 * Only the fields that differ from what was loaded. `PATCH /me` leaves absent fields alone and
 * records the names of the ones sent in its audit entry, so an untouched name must not be sent.
 */
export function profileChanges(saved: ProfileValues, values: ProfileValues): Omit<MeChanges, 'current_password'> {
  const changes: Omit<MeChanges, 'current_password'> = {};
  const channels = Object.keys(values.notifications) as (keyof NotificationChannels)[];

  if (values.display_name.trim() !== saved.display_name.trim()) changes.display_name = values.display_name;
  if (emailChanged(saved, values)) changes.email = values.email;
  if (phoneChanged(saved, values)) changes.phone = values.phone;
  if (values.language !== saved.language) changes.language = values.language;
  if (channels.some((channel) => values.notifications[channel] !== saved.notifications[channel])) {
    changes.notifications = values.notifications;
  }

  return changes;
}
