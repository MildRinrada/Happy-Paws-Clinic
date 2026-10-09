'use client';

import { Field, Input } from '@/components/ui/field';
import { discountForm } from '../../content/discount-form';

interface Props {
  value: string;
  error?: string;
  /** The customer's points, when this session may read them (`crm:read`). Only a hint. */
  balance?: number;
  onChange: (value: string) => void;
}

/** "แลกแต้ม": how many of the customer's points to take off the bill. */
export function PointsField({ value, error, balance, onChange }: Props) {
  const text = discountForm;

  return (
    <Field
      label={text.points}
      htmlFor="discount-value"
      error={error}
      hint={balance === undefined ? text.pointsHint : text.pointsBalance(balance)}
    >
      <Input
        id="discount-value"
        type="number"
        min={1}
        step={1}
        value={value}
        aria-invalid={error ? true : undefined}
        onChange={(event) => onChange(event.target.value)}
      />
    </Field>
  );
}
