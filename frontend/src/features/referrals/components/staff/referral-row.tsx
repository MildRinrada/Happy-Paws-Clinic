import Link from 'next/link';
import { crmPaths } from '@/features/crm/lib/crm-paths';
import { formatDateTime } from '@/lib/format/datetime';
import { referralChannelLabels, referralRejectLabels, staffReferralsText } from '../../content/staff-referrals';
import type { StaffReferral } from '../../types/referral.types';
import { ReferralParty } from './referral-party';
import { ReferralStatusBadge } from './referral-status-badge';

/** One referral: both sides, the status with its reason, the points each side got, and when. */
export function ReferralRow({ referral }: { referral: StaffReferral }) {
  const text = staffReferralsText;

  return (
    <li className="space-y-1.5 py-3">
      <div className="flex flex-wrap items-center gap-2">
        <ReferralStatusBadge status={referral.status} />
        <span className="text-xs text-ink-500">{referralChannelLabels[referral.channel]}</span>
        <span className="font-mono text-xs text-ink-500">{text.code(referral.code)}</span>
      </div>

      <div className="grid gap-1 sm:grid-cols-2">
        <ReferralParty role={text.referrer} party={referral.referrer} />
        <ReferralParty role={text.friend} party={referral.friend} />
      </div>

      <p className="text-xs text-ink-500">{text.joined(formatDateTime(referral.created_at))}</p>

      {referral.status === 'rewarded' && (
        <div className="space-y-0.5 text-sm text-ink-700">
          <p>
            {text.points(referral.referrer_points, referral.friend_points)}
            {referral.rewarded_at && <span className="text-xs text-ink-500"> ({text.rewardedOn(formatDateTime(referral.rewarded_at))})</span>}
          </p>
          {referral.over_monthly_cap && <p className="text-xs text-amber-800">{text.overCap}</p>}
          {referral.reward_invoice_id && (
            <Link href={crmPaths.invoice(referral.reward_invoice_id)} className="text-xs text-brand-700 hover:underline">
              {text.bill}
            </Link>
          )}
        </div>
      )}

      {referral.status === 'rejected' && referral.reject_reason && (
        <p className="text-sm text-ink-700">
          {referralRejectLabels[referral.reject_reason]}
          {referral.rejected_at && <span className="text-xs text-ink-500"> ({text.rejectedOn(formatDateTime(referral.rejected_at))})</span>}
        </p>
      )}
    </li>
  );
}
