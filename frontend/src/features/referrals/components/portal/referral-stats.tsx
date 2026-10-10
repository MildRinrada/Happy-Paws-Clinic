import { SectionCard } from '@/features/portal/components/shared/section-card';
import { portalReferral } from '../../content/portal-referral';
import type { MyReferral } from '../../types/referral.types';
import { ReferralStat } from './referral-stat';

/** What came of the link, in counts only. No friend is named, here or anywhere in the portal. */
export function ReferralStats({ referral }: { referral: MyReferral }) {
  const text = portalReferral.stats;

  return (
    <SectionCard title={portalReferral.statsHeading}>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <ReferralStat label={text.invited} value={referral.invited} />
        <ReferralStat label={text.pending} value={referral.pending} />
        <ReferralStat label={text.rewarded} value={referral.rewarded} />
        <ReferralStat label={text.points} value={referral.points_earned} />
      </div>
      <p className="mt-3 text-sm text-ink-700">{text.thisMonth(referral.rewarded_this_month, referral.monthly_cap)}</p>
      <p className="mt-1 text-xs text-site-muted">{portalReferral.privacy}</p>
    </SectionCard>
  );
}
