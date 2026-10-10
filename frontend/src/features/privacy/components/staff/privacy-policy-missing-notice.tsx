import Link from 'next/link';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { Scope } from '@/lib/auth/scopes';
import { getCurrentPrivacyPolicy } from '../../api/getPrivacyPolicy';
import { dashboardNotice } from '../../content/staff-privacy-policy';
import { policySettingsPaths } from '../../lib/privacy-paths';

/**
 * A one-line reminder on the dashboard, for the owner only (`settings:danger`), while the clinic
 * has not published its privacy notice (docs/contract-extensions.md § 35). Draws nothing for
 * anyone else, once a notice is published, or when the API cannot be read.
 */
export async function PrivacyPolicyMissingNotice() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.SettingsDanger)) {
    return null;
  }

  const policy = await getCurrentPrivacyPolicy().catch(() => null);

  if (!policy || policy.published) {
    return null;
  }

  return (
    <p role="status" className="flex flex-wrap items-center gap-x-2 rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2 text-sm text-amber-900">
      <span>{dashboardNotice.text}</span>
      <Link href={policySettingsPaths.list} className="font-medium underline">
        {dashboardNotice.link}
      </Link>
    </p>
  );
}
