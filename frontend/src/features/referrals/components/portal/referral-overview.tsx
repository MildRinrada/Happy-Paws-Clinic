import { Alert } from '@/components/ui/alert';
import { portalReferral } from '../../content/portal-referral';
import type { MyReferral } from '../../types/referral.types';
import { ReferralHow } from './referral-how';
import { ReferralLinkCard } from './referral-link-card';
import { ReferralStats } from './referral-stats';

/**
 * The customer's referral page. With the programme off the link still works as a booking link, so
 * it is still shown, under a note that nothing is earned for now; the rules and counts are not.
 */
export function ReferralOverview({ referral }: { referral: MyReferral }) {
  return (
    <div className="space-y-4">
      {!referral.enabled && <Alert tone="info">{portalReferral.off}</Alert>}
      <ReferralLinkCard referral={referral} />
      {referral.enabled && <ReferralHow referral={referral} />}
      {referral.enabled && <ReferralStats referral={referral} />}
    </div>
  );
}
