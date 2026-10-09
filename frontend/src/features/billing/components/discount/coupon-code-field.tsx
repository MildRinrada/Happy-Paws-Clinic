'use client';

import { Field, Input } from '@/components/ui/field';
import { discountForm } from '../../content/discount-form';

interface Props {
  value: string;
  error?: string;
  onChange: (value: string) => void;
}

/** "ใช้คูปอง": the code the customer brought. Typed in any case; codes are read case-insensitively. */
export function CouponCodeField({ value, error, onChange }: Props) {
  const text = discountForm;

  return (
    <Field label={text.coupon} htmlFor="discount-value" error={error} hint={text.couponHint}>
      <Input
        id="discount-value"
        value={value}
        maxLength={20}
        autoComplete="off"
        placeholder={text.couponPlaceholder}
        className="font-mono uppercase"
        aria-invalid={error ? true : undefined}
        onChange={(event) => onChange(event.target.value.toUpperCase())}
      />
    </Field>
  );
}
