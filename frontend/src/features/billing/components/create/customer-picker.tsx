'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { findCustomersAction, type CustomerChoice } from '../../actions/create-invoice-actions';
import { billingMessages } from '../../content/billing-messages';
import { createInvoicePage } from '../../content/create-invoice-page';

interface Props {
  /** `customer:read`: without it the register cannot be searched and the id is typed instead. */
  canSearch: boolean;
  chosen: CustomerChoice | null;
  error?: string;
  onChoose: (customer: CustomerChoice | null) => void;
}

/**
 * Who the bill is for. The register's own search (`GET /staff/customers?q=`) in a form of its
 * own, so Enter here searches rather than raising the bill.
 */
export function CustomerPicker({ canSearch, chosen, error, onChoose }: Props) {
  const text = createInvoicePage.customer;
  const [query, setQuery] = useState('');
  const [found, setFound] = useState<CustomerChoice[]>();
  const [searchError, setSearchError] = useState<string>();
  const [pending, startTransition] = useTransition();

  if (!canSearch) {
    return (
      <Field label={text.idLabel} htmlFor="invoice-customer-id" error={error} hint={text.noSearch}>
        <Input
          id="invoice-customer-id"
          value={chosen?.id ?? ''}
          maxLength={40}
          placeholder={text.idPlaceholder}
          autoComplete="off"
          onChange={(event) => onChoose(event.target.value.trim() ? { id: event.target.value.trim(), name: '', phone: '' } : null)}
        />
      </Field>
    );
  }

  if (chosen) {
    return (
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-ink-200 px-3 py-2 text-sm">
        <span>
          <span className="font-medium text-ink-900">{chosen.name}</span>
          <span className="ml-2 tabular-nums text-ink-500">{chosen.phone}</span>
        </span>
        <Button size="sm" variant="ghost" onClick={() => onChoose(null)}>
          {text.change}
        </Button>
      </div>
    );
  }

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const q = query.trim();
    if (!q) return;

    setSearchError(undefined);
    startTransition(async () => {
      try {
        const result = await findCustomersAction({ q });
        if (result.ok) setFound(result.data);
        else setSearchError(result.error.message);
      } catch {
        setSearchError(billingMessages.unreachable);
      }
    });
  }

  return (
    <div className="space-y-2">
      <form onSubmit={search} role="search" className="flex gap-2">
        <label htmlFor="invoice-customer-search" className="sr-only">
          {text.label}
        </label>
        <Input
          id="invoice-customer-search"
          type="search"
          value={query}
          maxLength={100}
          placeholder={text.placeholder}
          autoComplete="off"
          aria-invalid={error ? true : undefined}
          className="min-w-0 flex-1"
          onChange={(event) => setQuery(event.target.value)}
        />
        <Button type="submit" variant="secondary" disabled={pending}>
          {pending ? text.searching : text.submit}
        </Button>
      </form>

      {(searchError ?? error) && (
        <p role="alert" className="text-sm text-red-700">
          {searchError ?? error}
        </p>
      )}

      {found && found.length === 0 && <p className="text-sm text-ink-500">{text.empty}</p>}

      {found && found.length > 0 && (
        <ul className="divide-y divide-ink-100 rounded-lg border border-ink-200 text-sm">
          {found.map((customer) => (
            <li key={customer.id} className="flex items-center justify-between gap-3 px-3 py-2">
              <span>
                <span className="font-medium text-ink-900">{customer.name}</span>
                <span className="ml-2 tabular-nums text-ink-500">{customer.phone}</span>
              </span>
              <Button size="sm" variant="secondary" onClick={() => onChoose(customer)}>
                {text.pick}
              </Button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
