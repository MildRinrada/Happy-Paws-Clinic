'use client';

import { Field, Input } from '@/components/ui/field';
import { couponPage } from '../../content/coupon-page';

interface Props {
  maxRedemptions: string;
  perCustomerLimit: string;
  errors: { maxRedemptions?: string; perCustomerLimit?: string };
  onChange: (patch: { maxRedemptions?: string; perCustomerLimit?: string }) => void;
}

/** How many times the code works in all, and per customer. Empty is no limit. */
export function CouponLimitFields({ maxRedemptions, perCustomerLimit, errors, onChange }: Props) {
  const text = couponPage;

  return (
    <div className="grid gap-3 sm:grid-cols-2">
      <Field label={text.maxRedemptions} htmlFor="coupon-max" hint={text.unlimitedHint} error={errors.maxRedemptions}>
        <Input
          id="coupon-max"
          type="number"
          min={1}
          max={100000}
          step={1}
          value={maxRedemptions}
          onChange={(event) => onChange({ maxRedemptions: event.target.value })}
        />
      </Field>
      <Field label={text.perCustomerLimit} htmlFor="coupon-per-customer" hint={text.unlimitedHint} error={errors.perCustomerLimit}>
        <Input
          id="coupon-per-customer"
          type="number"
          min={1}
          max={100}
          step={1}
          value={perCustomerLimit}
          onChange={(event) => onChange({ perCustomerLimit: event.target.value })}
        />
      </Field>
    </div>
  );
}
