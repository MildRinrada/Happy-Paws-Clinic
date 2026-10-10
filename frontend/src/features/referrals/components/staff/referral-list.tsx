import { Card, CardBody } from '@/components/ui/card';
import { PageLinks } from '@/features/crm/components/shared/page-links';
import { staffReferralsText } from '../../content/staff-referrals';
import type { StaffReferral } from '../../types/referral.types';
import { ReferralRow } from './referral-row';

interface ReferralListProps {
  referrals: StaffReferral[];
  nextHref?: string;
  firstHref?: string;
}

/** Referrals newest first, with paging. */
export function ReferralList({ referrals, nextHref, firstHref }: ReferralListProps) {
  const text = staffReferralsText;

  return (
    <Card>
      <CardBody className="space-y-4">
        {referrals.length === 0 ? (
          <p className="text-sm text-ink-500">{text.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {referrals.map((referral) => (
              <ReferralRow key={referral.id} referral={referral} />
            ))}
          </ul>
        )}
        <PageLinks nextHref={nextHref} firstHref={firstHref} nextLabel={text.next} firstLabel={text.first} />
      </CardBody>
    </Card>
  );
}
