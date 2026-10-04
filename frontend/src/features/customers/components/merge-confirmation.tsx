'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { mergeCustomerAction } from '../actions/merge-actions';
import { mergeCustomer } from '../content/merge-customer';
import type { Customer, MergeResult } from '../types/customer.types';
import { MergeRecordSummary } from './merge-record-summary';

interface MergeConfirmationProps {
  surviving: Customer;
  duplicate: Customer;
  onBack: () => void;
  onDone: (result: MergeResult) => void;
}

/**
 * The stop before the one change in the register that cannot be taken back: which record stays,
 * which one closes, what moves, and a tick to say it was read. The wording follows the backend's
 * merge (`MergeCustomersHandler`): the open record survives, the duplicate is kept as a closed
 * record that points here, and two portal logins refuse the merge.
 */
export function MergeConfirmation({ surviving, duplicate, onBack, onDone }: MergeConfirmationProps) {
  const text = mergeCustomer.confirm;
  const [understood, setUnderstood] = useState(false);
  const { message, pending, submit } = useFormSubmit(mergeCustomer);
  const bothLinked = surviving.has_portal_account && duplicate.has_portal_account;
  const accountMoves = duplicate.has_portal_account && !surviving.has_portal_account;

  function merge() {
    submit(
      () => mergeCustomerAction({ survivingId: surviving.id, version: surviving.version, duplicateId: duplicate.id }),
      onDone,
    );
  }

  return (
    <div className="space-y-3">
      <p className="text-sm font-medium text-ink-900">{text.title}</p>

      <div className="space-y-2">
        <MergeRecordSummary label={text.keep} note={text.keepNote} customer={surviving} tone="keep" />
        <MergeRecordSummary label={text.absorb} customer={duplicate} tone="absorb" />
      </div>

      <ul className="list-disc space-y-1 pl-5 text-sm text-ink-700">
        <li>{text.moves}</li>
        <li>{text.contact}</li>
        <li>{text.tombstone(duplicate.display_name)}</li>
        {accountMoves && <li>{text.accountMoves}</li>}
      </ul>

      {bothLinked ? (
        <p role="alert" className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
          {text.bothLinked}
        </p>
      ) : (
        <>
          <p className="text-sm font-medium text-red-700">{text.irreversible}</p>
          <Checkbox label={text.acknowledge} checked={understood} onChange={(event) => setUnderstood(event.target.checked)} />
        </>
      )}

      {message && (
        <p role="alert" className="text-sm text-red-700">
          {message}
        </p>
      )}

      <div className="flex flex-wrap gap-2">
        {!bothLinked && (
          <Button variant="danger" size="sm" disabled={!understood || pending} onClick={merge}>
            {pending ? text.submitting : text.submit}
          </Button>
        )}
        <Button variant="ghost" size="sm" disabled={pending} onClick={onBack}>
          {text.back}
        </Button>
      </div>
    </div>
  );
}
