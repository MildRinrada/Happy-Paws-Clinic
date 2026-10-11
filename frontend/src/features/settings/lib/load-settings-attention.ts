import type { StaffContext } from '@/features/account/lib/staff-context';
import { getCurrentPrivacyPolicy } from '@/features/privacy/api/getPrivacyPolicy';
import { Scope } from '@/lib/auth/scopes';

/**
 * The settings pages that still need setting up, as far as one cheap read can tell: today only the
 * privacy notice, from the same public read the dashboard's reminder uses (kept for a minute). A
 * read that fails marks nothing; the dot is a hint, never a reason for the page not to draw.
 */
export async function loadSettingsAttention(context: StaffContext | null): Promise<Set<string>> {
  const attention = new Set<string>();

  if (context?.can(Scope.SettingsRead)) {
    const policy = await getCurrentPrivacyPolicy().catch(() => null);
    if (policy && !policy.published) attention.add('/settings/privacy-policy');
  }

  return attention;
}
