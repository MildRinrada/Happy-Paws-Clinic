import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { customerList } from '../content/customer-list';
import { customerPaths } from '../lib/customer-paths';
import type { Customer } from '../types/customer.types';
import { PortalStatusBadge } from './portal-status-badge';

/** Matching customers, one row each. The name opens their page. */
export function CustomerTable({ customers }: { customers: Customer[] }) {
  const columns = customerList.columns;

  return (
    <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
      <table className="w-full min-w-[560px] text-sm">
        <thead className="border-b border-ink-100 text-left text-ink-500">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">{columns.name}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.phone}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.email}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.portal}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {customers.map((customer) => (
            <tr key={customer.id} className="hover:bg-ink-50">
              <td className="px-4 py-3">
                <Link href={customerPaths.detail(customer.id)} className="font-medium text-brand-700 hover:underline">
                  {customer.display_name}
                </Link>
                {!customer.is_active && (
                  <span className="ml-2">
                    <Badge tone="amber">{customerList.inactive}</Badge>
                  </span>
                )}
              </td>
              <td className="px-4 py-3 tabular-nums text-ink-800">{customer.phone}</td>
              <td className="px-4 py-3 text-ink-600">{customer.email ?? '-'}</td>
              <td className="px-4 py-3">
                <PortalStatusBadge linked={customer.has_portal_account} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
