'use client';

import { useCallback, useEffect, useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import type { LineFollower } from '@/features/integrations/types/line.types';
import { listLineFollowersAction } from '../../actions/line-actions';
import { customerLine } from '../../content/customer-line';
import { LineFollowerRow } from './line-follower-row';
import { LineLinkConfirmation } from './line-link-confirmation';

interface LineFollowerPickerProps {
  customerId: string;
  customerName: string;
  version: number;
  onClose: () => void;
}

/**
 * The recent followers of the clinic's LINE account who are on no customer's record yet, newest
 * first, read when the picker opens and again on "โหลดใหม่". The person at the counter has usually
 * just added the account, so they are at the top.
 */
export function LineFollowerPicker({ customerId, customerName, version, onClose }: LineFollowerPickerProps) {
  const text = customerLine.picker;
  const [query, setQuery] = useState('');
  const [followers, setFollowers] = useState<LineFollower[]>([]);
  const [cursor, setCursor] = useState<string>();
  const [error, setError] = useState<string>();
  const [chosen, setChosen] = useState<LineFollower>();
  const [pending, startTransition] = useTransition();

  const load = useCallback((q: string, after?: string) => {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await listLineFollowersAction({ q: q.trim() || undefined, cursor: after });

        if (!result.ok) {
          setError(result.error.message);
          return;
        }

        setFollowers((current) => (after ? [...current, ...result.data.data] : result.data.data));
        setCursor(result.data.page?.next_cursor);
      } catch {
        setError(customerLine.unreachable);
      }
    });
  }, []);

  useEffect(() => load(''), [load]);

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    load(query);
  }

  if (chosen) {
    return (
      <LineLinkConfirmation
        customerId={customerId}
        customerName={customerName}
        version={version}
        follower={chosen}
        onBack={() => setChosen(undefined)}
        onLinked={onClose}
      />
    );
  }

  return (
    <div className="space-y-3 rounded-lg border border-ink-100 p-3">
      <div className="flex items-center justify-between gap-2">
        <p className="text-sm font-medium text-ink-900">{text.heading}</p>
        <Button variant="ghost" size="sm" onClick={onClose}>
          {text.close}
        </Button>
      </div>
      <p className="text-xs text-ink-500">{text.hint}</p>

      <form onSubmit={search} className="flex gap-2">
        <Input aria-label={text.search} placeholder={text.search} value={query} maxLength={100} onChange={(event) => setQuery(event.target.value)} className="h-8" />
        <Button type="submit" variant="secondary" size="sm" disabled={pending}>
          {pending ? text.loading : text.refresh}
        </Button>
      </form>

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}

      {followers.length === 0 && !pending && !error ? (
        <p className="text-sm text-ink-500">{text.empty}</p>
      ) : (
        <ul className="max-h-72 divide-y divide-ink-100 overflow-y-auto">
          {followers.map((follower) => (
            <LineFollowerRow key={follower.line_user_id} follower={follower} onChoose={setChosen} />
          ))}
        </ul>
      )}

      {cursor && (
        <Button variant="ghost" size="sm" disabled={pending} onClick={() => load(query, cursor)}>
          {text.more}
        </Button>
      )}
    </div>
  );
}
