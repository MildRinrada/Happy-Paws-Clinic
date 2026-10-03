import Link from 'next/link';
import { Button, buttonClass } from '@/components/ui/button';
import { registerCustomer } from '../content/register-customer';
import { customerPaths } from '../lib/customer-paths';
import type { Customer } from '../types/customer.types';

interface DuplicateCustomerNoticeProps {
  existing: Customer[];
  pending: boolean;
  onCreateAnyway: () => void;
}

/**
 * The stop a `409 duplicate_customer` asks for. The counter either opens the record that is
 * already there or says this really is a second person on the same number.
 */
export function DuplicateCustomerNotice({ existing, pending, onCreateAnyway }: DuplicateCustomerNoticeProps) {
  const text = registerCustomer.duplicate;

  return (
    <div role="alert" className="space-y-3 rounded-xl border border-amber-200 bg-amber-50 p-4">
      <div>
        <p className="font-medium text-amber-900">{text.title}</p>
        <p className="mt-1 text-sm text-amber-900/80">{text.body}</p>
      </div>

      <ul className="divide-y divide-amber-200 rounded-lg border border-amber-200 bg-white">
        {existing.map((customer) => (
          <li key={customer.id} className="flex flex-wrap items-center justify-between gap-2 px-3.5 py-2.5">
            <div className="min-w-0">
              <p className="font-medium text-ink-900">{customer.display_name}</p>
              <p className="text-sm text-ink-600">{[customer.phone, customer.email].filter(Boolean).join(' / ')}</p>
            </div>
            <Link href={customerPaths.detail(customer.id)} className={buttonClass('secondary', 'sm')}>
              {text.open}
            </Link>
          </li>
        ))}
      </ul>

      <Button variant="ghost" size="sm" disabled={pending} onClick={onCreateAnyway}>
        {text.createAnyway}
      </Button>
    </div>
  );
}
