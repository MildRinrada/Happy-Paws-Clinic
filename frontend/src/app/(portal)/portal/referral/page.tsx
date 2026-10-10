import type { Metadata } from 'next';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { getMyReferral } from '@/features/referrals/api/myReferral';
import { ReferralOverview } from '@/features/referrals/components/portal/referral-overview';
import { portalReferral } from '@/features/referrals/content/portal-referral';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: portalReferral.metaTitle };

/**
 * The owner's own invite link (docs/contract-extensions.md § 39): copy it or share it on LINE, the
 * rules with the clinic's current points, and counts of what came of it. The code is made the first
 * time this page is opened. Friends are never named here.
 */
export default async function PortalReferralPage() {
  const referral = await authed(getMyReferral);

  return (
    <div className="space-y-6">
      <PageHeading title={portalReferral.title} intro={portalReferral.intro} />
      <ReferralOverview referral={referral} />
    </div>
  );
}
