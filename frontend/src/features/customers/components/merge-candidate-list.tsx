import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { mergeCustomer } from '../content/merge-customer';
import type { Customer } from '../types/customer.types';

/** The matches, one row each, to choose the duplicate from. */
export function MergeCandidateList({ customers, onPick }: { customers: Customer[]; onPick: (customer: Customer) => void }) {
  const text = mergeCustomer.search;

  return (
    <ul className="divide-y divide-ink-100 rounded-lg border border-ink-200">
      {customers.map((customer) => (
        <li key={customer.id} className="flex items-center justify-between gap-2 px-3 py-2">
          <div className="min-w-0 text-sm">
            <p className="truncate font-medium text-ink-900">{customer.display_name}</p>
            <p className="truncate text-ink-600">{[customer.phone, customer.email].filter(Boolean).join(' / ')}</p>
            {customer.has_portal_account && <Badge tone="green">{text.hasAccount}</Badge>}
          </div>
          <Button variant="secondary" size="sm" onClick={() => onPick(customer)}>
            {text.pick}
          </Button>
        </li>
      ))}
    </ul>
  );
}
