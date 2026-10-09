'use client';

import { Field, Input } from '@/components/ui/field';
import { couponPage } from '../../content/coupon-page';

interface Props {
  validFrom: string;
  validUntil: string;
  errors: { validFrom?: string; validUntil?: string };
  onChange: (patch: { validFrom?: string; validUntil?: string }) => void;
}

/** From which day the code works, and until which day. No end date is allowed. */
export function CouponPeriodFields({ validFrom, validUntil, errors, onChange }: Props) {
  const text = couponPage;

  return (
    <div className="grid gap-3 sm:grid-cols-2">
      <Field label={text.validFrom} htmlFor="coupon-from" error={errors.validFrom}>
        <Input id="coupon-from" type="date" value={validFrom} onChange={(event) => onChange({ validFrom: event.target.value })} />
      </Field>
      <Field label={text.validUntil} htmlFor="coupon-until" error={errors.validUntil}>
        <Input
          id="coupon-until"
          type="date"
          min={validFrom || undefined}
          value={validUntil}
          onChange={(event) => onChange({ validUntil: event.target.value })}
        />
      </Field>
    </div>
  );
}
