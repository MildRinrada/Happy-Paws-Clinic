'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { findRecipientsAction } from '../actions/send-notification-actions';
import { sendPage } from '../content/send-page';
import type { Recipient } from '../types/notification.types';

interface RecipientSearchProps {
  chosen: string[];
  onAdd: (recipient: Recipient) => void;
}

/**
 * The customer register's own search, for picking who to send to. Its own form, so pressing Enter
 * here searches rather than sending the message.
 */
export function RecipientSearch({ chosen, onAdd }: RecipientSearchProps) {
  const text = sendPage.search;
  const [query, setQuery] = useState('');
  const [found, setFound] = useState<Recipient[]>();
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const q = query.trim();
    if (!q) return;

    setError(undefined);
    startTransition(async () => {
      try {
        const result = await findRecipientsAction({ q });
        if (result.ok) setFound(result.data);
        else setError(result.error.message);
      } catch {
        setError(sendPage.unreachable);
      }
    });
  }

  return (
    <div className="space-y-2">
      <form onSubmit={search} role="search" className="flex gap-2">
        <label htmlFor="recipient-search" className="sr-only">
          {text.label}
        </label>
        <Input
          id="recipient-search"
          type="search"
          value={query}
          maxLength={100}
          placeholder={text.placeholder}
          autoComplete="off"
          className="min-w-0 flex-1"
          onChange={(event) => setQuery(event.target.value)}
        />
        <Button type="submit" variant="secondary" disabled={pending}>
          {pending ? text.searching : text.submit}
        </Button>
      </form>

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}

      {found && found.length === 0 && <p className="text-sm text-ink-500">{text.empty}</p>}

      {found && found.length > 0 && (
        <ul className="divide-y divide-ink-100 rounded-lg border border-ink-200 text-sm">
          {found.map((recipient) => {
            const picked = chosen.includes(recipient.id);

            return (
              <li key={recipient.id} className="flex items-center justify-between gap-3 px-3 py-2">
                <span>
                  <span className="font-medium text-ink-900">{recipient.name}</span>
                  <span className="ml-2 tabular-nums text-ink-500">{recipient.phone}</span>
                </span>
                <Button size="sm" variant="secondary" disabled={picked} onClick={() => onAdd(recipient)}>
                  {picked ? text.added : text.add}
                </Button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
