import { SectionCard } from '@/features/portal/components/shared/section-card';
import { portalReferral } from '../../content/portal-referral';
import type { MyReferral } from '../../types/referral.types';

/** The rules in plain words, with the clinic's current points and cap. */
export function ReferralHow({ referral }: { referral: MyReferral }) {
  const text = portalReferral;

  return (
    <SectionCard title={text.howHeading}>
      <ul className="list-disc space-y-1 pl-5 text-sm text-ink-700">
        {text.how(referral.referrer_points, referral.friend_points).map((line) => (
          <li key={line}>{line}</li>
        ))}
        <li>{text.cap(referral.monthly_cap)}</li>
      </ul>
    </SectionCard>
  );
}
