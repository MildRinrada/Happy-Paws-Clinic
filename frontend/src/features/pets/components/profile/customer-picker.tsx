'use client';

import { useEffect, useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import type { Customer } from '@/features/customers/types/customer.types';
import { searchCarerCandidatesAction } from '../../actions/carer-actions';
import { carers } from '../../content/carers';

interface CustomerPickerProps {
  picked: Customer | null;
  onPick: (customer: Customer | null) => void;
  /** Customers already linked to the pet: listed, but not offered. */
  linkedIds: string[];
  error?: string;
}

/** Finds a customer by name, phone or HN. Searches once the typing stops. */
export function CustomerPicker({ picked, onPick, linkedIds, error }: CustomerPickerProps) {
  const text = carers;
  const [query, setQuery] = useState('');
  const [matches, setMatches] = useState<Customer[] | null>(null);
  const [refusal, setRefusal] = useState<string>();
  const [searching, startSearch] = useTransition();

  useEffect(() => {
    if (picked || query.trim().length < 2) {
      setMatches(null);
      setRefusal(undefined);
      return;
    }

    const timer = setTimeout(() => {
      startSearch(async () => {
        const result = await searchCarerCandidatesAction(query);
        setMatches(result.ok ? result.data : []);
        setRefusal(result.ok ? undefined : result.error.message);
      });
    }, 250);

    return () => clearTimeout(timer);
  }, [query, picked]);

  if (picked) {
    return (
      <div className="flex items-center justify-between gap-2 rounded-lg border border-brand-200 bg-brand-50 px-3 py-2">
        <div className="min-w-0 text-sm">
          <p className="font-medium text-ink-900">{picked.display_name}</p>
          <p className="text-ink-600">{picked.phone}</p>
        </div>
        <Button size="sm" variant="ghost" onClick={() => onPick(null)}>
          {text.change}
        </Button>
      </div>
    );
  }

  return (
    <Field label={text.search} htmlFor="carer-search" hint={text.searchHint} error={refusal ?? error}>
      <Input id="carer-search" value={query} autoComplete="off" autoFocus onChange={(event) => setQuery(event.target.value)} />
      {searching && <p className="text-sm text-ink-500">{text.searching}</p>}
      {matches?.length === 0 && !searching && !refusal && <p className="text-sm text-ink-500">{text.none}</p>}
      {matches && matches.length > 0 && (
        <ul className="max-h-56 divide-y divide-ink-100 overflow-auto rounded-lg border border-ink-200 bg-white">
          {matches.map((match) => {
            const linked = linkedIds.includes(match.id);
            return (
              <li key={match.id}>
                <button
                  type="button"
                  disabled={linked}
                  className="flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-sm hover:bg-ink-50 disabled:cursor-not-allowed disabled:text-ink-400 disabled:hover:bg-white"
                  onClick={() => onPick(match)}
                >
                  <span>
                    <span className="font-medium">{match.display_name}</span> <span className="text-ink-500">{match.phone}</span>
                  </span>
                  {linked && <span className="whitespace-nowrap text-xs">{text.alreadyLinked}</span>}
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </Field>
  );
}
