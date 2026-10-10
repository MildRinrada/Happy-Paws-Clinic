'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { findCustomersAction, type CustomerChoice } from '@/features/billing/actions/create-invoice-actions';
import { crmArea } from '../../content/crm-tabs';
import { loyaltyPage } from '../../content/loyalty-page';
import { crmPaths } from '../../lib/crm-paths';

/**
 * Finds the customer whose points to show, then opens `?customer_id=`. Searches the register
 * under `customer:read`; without it the customer id is typed instead. `target` sends the choice to
 * another page, such as the package lookup. It is a name, not a path function, because a server
 * page renders this and functions cannot cross into a client component.
 */
export function CustomerLookup({
  canSearch,
  target = 'loyalty',
}: {
  canSearch: boolean;
  target?: 'loyalty' | 'customerPackages';
}) {
  const pathFor = crmPaths[target];
  const text = loyaltyPage.lookup;
  const router = useRouter();
  const [query, setQuery] = useState('');
  const [found, setFound] = useState<CustomerChoice[]>();
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const q = query.trim();
    if (!q) return;

    if (!canSearch) {
      router.push(pathFor(q));
      return;
    }

    setError(undefined);
    startTransition(async () => {
      try {
        const result = await findCustomersAction({ q });
        if (result.ok) setFound(result.data);
        else setError(result.error.message);
      } catch {
        setError(crmArea.unreachable);
      }
    });
  }

  return (
    <div className="space-y-2">
      <form onSubmit={search} role="search" className="flex max-w-xl gap-2">
        <label htmlFor="loyalty-customer" className="sr-only">
          {canSearch ? text.label : text.idLabel}
        </label>
        <Input
          id="loyalty-customer"
          type={canSearch ? 'search' : 'text'}
          value={query}
          maxLength={100}
          placeholder={canSearch ? text.placeholder : text.idPlaceholder}
          autoComplete="off"
          className="min-w-0 flex-1"
          onChange={(event) => setQuery(event.target.value)}
        />
        <Button type="submit" variant="secondary" disabled={pending}>
          {!canSearch ? text.idSubmit : pending ? text.searching : text.submit}
        </Button>
      </form>

      {!canSearch && <p className="text-sm text-ink-500">{text.noSearch}</p>}

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}

      {found && found.length === 0 && <p className="text-sm text-ink-500">{text.empty}</p>}

      {found && found.length > 0 && (
        <ul className="max-w-xl divide-y divide-ink-100 rounded-lg border border-ink-200 text-sm">
          {found.map((customer) => (
            <li key={customer.id} className="flex items-center justify-between gap-3 px-3 py-2">
              <span>
                <span className="font-medium text-ink-900">{customer.name}</span>
                <span className="ml-2 tabular-nums text-ink-500">{customer.phone}</span>
              </span>
              <Button size="sm" variant="secondary" onClick={() => router.push(pathFor(customer.id))}>
                {text.open}
              </Button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
