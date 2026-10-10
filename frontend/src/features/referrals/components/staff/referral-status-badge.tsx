import { Badge } from '@/components/ui/badge';
import { referralStatusLabels } from '../../content/staff-referrals';
import type { ReferralStatus } from '../../types/referral.types';

export function ReferralStatusBadge({ status }: { status: ReferralStatus }) {
  const label = referralStatusLabels[status];

  return <Badge tone={label.tone}>{label.text}</Badge>;
}
