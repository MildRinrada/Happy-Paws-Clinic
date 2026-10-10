import Link from 'next/link';
import { crmPaths } from '@/features/crm/lib/crm-paths';
import { staffReferralsText } from '../../content/staff-referrals';
import type { ReferralParty as Party } from '../../types/referral.types';

/** One side of a referral: who, linking to their points. */
export function ReferralParty({ role, party }: { role: string; party: Party }) {
  return (
    <p className="min-w-0 text-sm">
      <span className="text-ink-500">{role} </span>
      <Link href={crmPaths.loyalty(party.customer_id)} className="break-words font-medium text-brand-700 hover:underline">
        {party.name ?? staffReferralsText.noName}
      </Link>
    </p>
  );
}
