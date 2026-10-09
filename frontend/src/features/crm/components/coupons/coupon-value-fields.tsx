'use client';

import { Field, Input } from '@/components/ui/field';
import { couponPage } from '../../content/coupon-page';
import type { CouponValueType } from '../../lib/coupon-draft';
import { SegmentedChoice } from '../shared/segmented-choice';

interface Props {
  valueType: CouponValueType;
  value: string;
  error?: string;
  onType: (valueType: CouponValueType) => void;
  onValue: (value: string) => void;
}

/** How much the coupon takes off: a percent or a fixed amount, never both. */
export function CouponValueFields({ valueType, value, error, onType, onValue }: Props) {
  const text = couponPage;

  return (
    <div className="grid gap-3 sm:grid-cols-[auto_12rem] sm:items-start">
      <SegmentedChoice legend={text.valueType} options={text.valueTypes} value={valueType} onChange={onType} />
      <Field label={valueType === 'percent' ? text.percent : text.amount} htmlFor="coupon-value" error={error}>
        <Input
          id="coupon-value"
          inputMode="decimal"
          value={value}
          aria-invalid={error ? true : undefined}
          onChange={(event) => onValue(event.target.value)}
        />
      </Field>
    </div>
  );
}
