'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { findMergeCandidatesAction } from '../actions/merge-actions';
import { mergeCustomer } from '../content/merge-customer';
import type { Customer } from '../types/customer.types';
import { MergeCandidateList } from './merge-candidate-list';

interface MergeSearchProps {
  customerId: string;
  onPick: (customer: Customer) => void;
  onCancel: () => void;
}

/** The register's own search, inside the card, without the record being kept. */
export function MergeSearch({ customerId, onPick, onCancel }: MergeSearchProps) {
  const text = mergeCustomer.search;
  const [query, setQuery] = useState('');
  const [found, setFound] = useState<Customer[]>();
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const q = query.trim();
    if (!q) return;

    setError(undefined);
    startTransition(async () => {
      try {
        const result = await findMergeCandidatesAction({ q, customerId });
        if (result.ok) setFound(result.data);
        else setError(result.error.message);
      } catch {
        setError(mergeCustomer.unreachable);
      }
    });
  }

  return (
    <div className="space-y-3">
      <form onSubmit={search} role="search" className="flex gap-2">
        <label htmlFor="merge-search" className="sr-only">
          {text.label}
        </label>
        <Input
          id="merge-search"
          type="search"
          value={query}
          maxLength={100}
          placeholder={text.placeholder}
          autoComplete="off"
          autoFocus
          className="min-w-0 flex-1"
          onChange={(event) => setQuery(event.target.value)}
        />
        <Button type="submit" size="md" disabled={pending}>
          {pending ? text.searching : text.submit}
        </Button>
      </form>

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
      {found && (found.length === 0 ? <p className="text-sm text-ink-500">{text.empty}</p> : <MergeCandidateList customers={found} onPick={onPick} />)}

      <Button variant="ghost" size="sm" onClick={onCancel}>
        {mergeCustomer.cancel}
      </Button>
    </div>
  );
}
