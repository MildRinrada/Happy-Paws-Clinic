import type { Me } from '@/features/account/types/account.types';
import { firstNameOf } from '@/features/portal/lib/first-name';
import { roleLabel, staffTypeLabel } from '@/features/staff/content/staff-labels';

/** Professional titles a vet's name may start with, which the portal's list of titles has no use for. */
const STAFF_TITLES = ['น.สพ.', 'สพ.ญ.', 'สพ.ช.', 'สพ.'];

/** A vowel written before its consonant: an initial of "เ" alone reads as nothing. */
const LEADING_VOWEL = /^[เ-ไ]/;

/**
 * The letters in the account button's circle: the first letter of the first name, after any title.
 * A Latin name gets two letters ("JS"), a Thai name one, as a Thai monogram is usually written.
 */
export function initialsOf(displayName: string): string {
  let name = displayName.trim();

  for (const title of STAFF_TITLES) {
    if (name.startsWith(title)) {
      name = name.slice(title.length).trim();
      break;
    }
  }

  const first = firstNameOf(name);

  if (/^[A-Za-z]/.test(first)) {
    const words = name.split(/\s+/);
    const last = words.length > 1 ? (words[words.length - 1] ?? '') : '';
    return (first.charAt(0) + (/^[A-Za-z]/.test(last) ? last.charAt(0) : '')).toUpperCase();
  }

  return (LEADING_VOWEL.test(first) ? first.slice(0, 2) : first.charAt(0)) || '?';
}

/** The position under the name in the account menu: the role the clinic gave, else the staff type. */
export function staffRoleOf(me: Me): string | undefined {
  const staff = me.memberships.find((membership) => membership.type === 'staff');
  const role = staff?.roles[0];

  if (role) return roleLabel(role);
  return staff?.staff_type ? staffTypeLabel(staff.staff_type) : undefined;
}
