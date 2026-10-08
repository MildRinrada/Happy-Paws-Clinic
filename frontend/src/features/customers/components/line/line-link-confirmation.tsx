'use client';

import Link from 'next/link';
import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import type { LineFollower } from '@/features/integrations/types/line.types';
import { linkLineAction } from '../../actions/line-actions';
import { customerLine } from '../../content/customer-line';
import { customerPaths } from '../../lib/customer-paths';
import { LineAvatar } from './line-avatar';

interface LineLinkConfirmationProps {
  customerId: string;
  customerName: string;
  version: number;
  follower: LineFollower;
  onBack: () => void;
  onLinked: () => void;
}

/**
 * The second press before a LINE address is saved: the counter checks with the person that this is
 * their account, because the clinic's messages about their pet will go there.
 */
export function LineLinkConfirmation({ customerId, customerName, version, follower, onBack, onLinked }: LineLinkConfirmationProps) {
  const text = customerLine;
  const [error, setError] = useState<string>();
  const [takenBy, setTakenBy] = useState<string>();
  const [pending, startTransition] = useTransition();

  function link() {
    setError(undefined);
    setTakenBy(undefined);
    startTransition(async () => {
      try {
        const result = await linkLineAction({ customerId, version, lineUserId: follower.line_user_id });

        if (result.ok) {
          onLinked();
          return;
        }

        if (result.error.code === 'line_address_taken') {
          setError(text.confirm.taken);
          setTakenBy(typeof result.error.details?.customer_id === 'string' ? result.error.details.customer_id : undefined);
        } else {
          setError(result.error.code === 'version_conflict' ? text.conflict : result.error.message);
        }
      } catch {
        setError(text.unreachable);
      }
    });
  }

  return (
    <div className="space-y-3 rounded-lg border border-ink-100 bg-ink-50 p-3">
      <div className="flex items-center gap-3">
        <LineAvatar name={follower.display_name} pictureUrl={follower.picture_url} />
        <p className="text-sm font-medium text-ink-900">{text.confirm.question(follower.display_name ?? text.noName, customerName)}</p>
      </div>
      <p className="text-xs text-ink-600">{text.confirm.check}</p>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}{' '}
          {takenBy && (
            <Link href={customerPaths.detail(takenBy)} className="font-medium underline">
              {text.confirm.openTaken}
            </Link>
          )}
        </p>
      )}
      <div className="flex flex-wrap justify-end gap-2">
        <Button variant="ghost" size="sm" disabled={pending} onClick={onBack}>
          {text.confirm.back}
        </Button>
        <Button size="sm" disabled={pending} onClick={link}>
          {pending ? text.confirm.linking : text.confirm.confirm}
        </Button>
      </div>
    </div>
  );
}
