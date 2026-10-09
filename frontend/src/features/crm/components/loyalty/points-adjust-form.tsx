'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { adjustPointsAction } from '../../actions/crm-manage-actions';
import { crmArea } from '../../content/crm-tabs';
import { pointsAdjustText } from '../../content/points-adjust';
import { SegmentedChoice } from '../shared/segmented-choice';

type Direction = 'give' | 'take';

/**
 * Gives or takes points by hand with a reason. One `Idempotency-Key` per adjustment: a retry after
 * a lost answer sends the same key and cannot give twice; a saved adjustment starts a new key.
 */
export function PointsAdjustForm({ customerId }: { customerId: string }) {
  const text = pointsAdjustText;
  const id = useId();
  const [direction, setDirection] = useState<Direction>('give');
  const [points, setPoints] = useState('');
  const [reason, setReason] = useState('');
  const [key, setKey] = useState(() => crypto.randomUUID());
  const [balance, setBalance] = useState<number>();
  const [bad, setBad] = useState<'points' | 'reason'>();
  const { errors, message, pending, submit, touch } = useFormSubmit({ conflict: crmArea.conflict, unreachable: crmArea.unreachable });

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const amount = Number(points);

    if (!Number.isInteger(amount) || amount < 1 || amount > 100_000) return setBad('points');
    if (!reason.trim()) return setBad('reason');
    setBad(undefined);
    setBalance(undefined);

    submit(
      () => adjustPointsAction({ idempotencyKey: key, customerId, points: direction === 'give' ? amount : -amount, reason: reason.trim() }),
      (adjusted) => {
        setBalance(adjusted.points_balance);
        setPoints('');
        setReason('');
        setKey(crypto.randomUUID());
      },
    );
  }

  return (
    <form onSubmit={save} noValidate className="space-y-3">
      <p className="text-sm text-ink-600">{text.hint}</p>
      <SegmentedChoice<Direction>
        legend={text.direction}
        value={direction}
        options={{ give: text.give, take: text.take }}
        onChange={(value) => {
          touch();
          setDirection(value);
        }}
      />
      <Field label={text.points} htmlFor={`${id}-points`} error={bad === 'points' ? text.wholeNumber : errors.points}>
        <Input
          id={`${id}-points`}
          inputMode="numeric"
          value={points}
          className="h-9 max-w-32 tabular-nums"
          aria-invalid={bad === 'points' || Boolean(errors.points)}
          onChange={(event) => {
            touch();
            setPoints(event.target.value);
          }}
        />
      </Field>
      <Field label={text.reason} htmlFor={`${id}-reason`} error={bad === 'reason' ? text.needReason : errors.reason}>
        <Input
          id={`${id}-reason`}
          value={reason}
          maxLength={200}
          placeholder={text.reasonPlaceholder}
          aria-invalid={bad === 'reason' || Boolean(errors.reason)}
          onChange={(event) => {
            touch();
            setReason(event.target.value);
          }}
        />
      </Field>
      {balance !== undefined && (
        <p role="status" className="text-sm text-emerald-800">
          {text.done(balance)}
        </p>
      )}
      <div className="flex flex-wrap items-center justify-end gap-3">
        <FormStatus message={message} savedText="" />
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? text.submitting : text.submit}
        </Button>
      </div>
    </form>
  );
}
