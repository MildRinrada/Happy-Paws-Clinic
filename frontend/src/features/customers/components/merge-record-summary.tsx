import { Badge } from '@/components/ui/badge';
import { mergeCustomer } from '../content/merge-customer';
import type { Customer } from '../types/customer.types';

interface MergeRecordSummaryProps {
  label: string;
  note?: string;
  customer: Customer;
  tone: 'keep' | 'absorb';
}

/** One side of the merge: which record, and what it is going to be. */
export function MergeRecordSummary({ label, note, customer, tone }: MergeRecordSummaryProps) {
  const frame = tone === 'keep' ? 'border-emerald-200 bg-emerald-50/60' : 'border-red-200 bg-red-50/60';

  return (
    <div className={`rounded-lg border px-3 py-2 text-sm ${frame}`}>
      <p className="text-xs font-medium text-ink-600">
        {label}
        {note && <span className="font-normal text-ink-500"> ({note})</span>}
      </p>
      <p className="font-medium text-ink-900">{customer.display_name}</p>
      <p className="text-ink-600">{[customer.phone, customer.email].filter(Boolean).join(' / ')}</p>
      {customer.has_portal_account && <Badge tone="green">{mergeCustomer.search.hasAccount}</Badge>}
    </div>
  );
}
