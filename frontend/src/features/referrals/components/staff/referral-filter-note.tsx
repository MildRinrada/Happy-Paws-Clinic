import Link from 'next/link';
import { crmPaths } from '@/features/crm/lib/crm-paths';
import { staffReferralsText } from '../../content/staff-referrals';

/** Says the list is narrowed to one customer, with the way back to all of them. */
export function ReferralFilterNote({ customerId }: { customerId: string }) {
  const text = staffReferralsText;

  return (
    <p className="flex flex-wrap items-center gap-3 text-sm text-ink-700">
      <span className="break-all">{text.showingFor(customerId)}</span>
      <Link href={crmPaths.referrals()} className="text-brand-700 hover:underline">
        {text.showAll}
      </Link>
    </p>
  );
}
