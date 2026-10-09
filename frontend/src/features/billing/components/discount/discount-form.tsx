'use client';

import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { formatBaht } from '@/lib/format/money';
import { applyCouponAction, applyDiscountAction, redeemPointsAction } from '../../actions/billing-actions';
import { billingMessages } from '../../content/billing-messages';
import { discountForm } from '../../content/discount-form';
import { previewDiscount, previewPoints, type DiscountKind, type DiscountPreview } from '../../lib/discount-preview';
import type { Invoice } from '../../types/billing.types';
import { ApproverField, OTHER_APPROVER, type StaffOption } from './approver-field';
import { CouponCodeField } from './coupon-code-field';
import { DiscountPreviewList } from './discount-preview-list';
import { PointsField } from './points-field';

interface Props {
  invoice: Invoice;
  self: StaffOption;
  staff: StaffOption[] | null;
  /** The customer's points, shown beside "แลกแต้ม" when this session holds `crm:read`. */
  pointsBalance?: number;
}

/** The two manual modes take the existing amount-or-percent form; a coupon and points have their own field. */
const isManual = (mode: DiscountKind): mode is 'amount' | 'percent' => mode === 'amount' || mode === 'percent';

/**
 * "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ": an amount or a percent, a reason, and who approved it. Or a
 * coupon code, or loyalty points, for which the backend records the reason and approver itself.
 *
 * Sent with the bill's version. A dispense upstairs can add a medicine line between the page
 * loading and the button being pressed; the backend then answers `version_conflict`, the action
 * re-reads the bill, and the form stays open with what was typed and the new figures beside it,
 * so the cashier decides again against the total the customer will actually see.
 */
export function DiscountForm({ invoice, self, staff, pointsBalance }: Props) {
  const text = discountForm;
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<DiscountKind>('amount');
  const [value, setValue] = useState('');
  const [reason, setReason] = useState('');
  const [approver, setApprover] = useState(self.id);
  const [otherId, setOtherId] = useState('');
  const [done, setDone] = useState<string>();
  const [tried, setTried] = useState(false);
  const { errors, message, pending, submit, touch } = useFormSubmit(billingMessages);

  // None for a coupon: what it takes off depends on its terms, which only the backend knows.
  const preview: (DiscountPreview & { points?: number }) | null =
    mode === 'coupon' ? null : mode === 'points' ? previewPoints(invoice, value) : previewDiscount(invoice, mode, value);
  const approverId = approver === OTHER_APPROVER ? otherId.trim() : approver;

  const valueError = !value.trim() || !preview
    ? undefined
    : preview.problem === 'format'
      ? mode === 'amount' ? text.errors.amount : mode === 'points' ? text.errors.points : text.errors.percent
      : preview.problem === 'tooLarge' || preview.problem === 'belowPaid'
        ? `${text.errors.tooLarge(formatBaht(preview.roomSatang))}${preview.problem === 'belowPaid' ? ` ${text.errors.belowPaid}` : ''}`
        : undefined;

  function edit(apply: () => void) {
    touch();
    setDone(undefined);
    apply();
  }

  function changeMode(next: DiscountKind) {
    edit(() => {
      // A figure typed for one kind means nothing to another, except between amount and percent.
      if (isManual(next) !== isManual(mode) || (!isManual(next) && next !== mode)) setValue('');
      setMode(next);
    });
  }

  /** After a coupon or points: the amount is the backend's, read off the bill it answered with. */
  function applied(updated: Invoice) {
    setDone(text.saved(formatBaht(updated.discount_satang - invoice.discount_satang)));
    setOpen(false);
    setValue('');
    setTried(false);
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setTried(true);

    if (mode === 'coupon') {
      const couponCode = value.trim();
      if (couponCode) submit(() => applyCouponAction({ invoiceId: invoice.id, version: invoice.version, couponCode }), applied);
      return;
    }

    if (mode === 'points') {
      const points = preview?.points;
      if (points && !preview?.problem) {
        submit(() => redeemPointsAction({ invoiceId: invoice.id, version: invoice.version, points }), applied);
      }
      return;
    }

    if (!preview || preview.problem || preview.amountSatang === null || !reason.trim() || !approverId) {
      return;
    }

    const amount = preview.amountSatang;

    submit(
      () =>
        applyDiscountAction({
          invoiceId: invoice.id,
          version: invoice.version,
          reason: reason.trim(),
          approvedByStaffId: approverId,
          ...(mode === 'amount' ? { amountSatang: amount } : { percent: preview.percent }),
        }),
      () => {
        setDone(text.saved(formatBaht(amount)));
        setOpen(false);
        setValue('');
        setReason('');
        setTried(false);
      },
    );
  }

  if (!open) {
    return (
      <div className="space-y-3">
        {done && <Alert tone="success">{done}</Alert>}
        <Button variant="secondary" size="sm" onClick={() => edit(() => setOpen(true))}>
          {text.open}
        </Button>
      </div>
    );
  }

  return (
    <form onSubmit={save} className="space-y-4" noValidate>
      {message && <Alert>{message}</Alert>}

      <div className="grid grid-cols-[7rem_1fr] gap-3">
        <Field label={text.mode} htmlFor="discount-mode">
          <Select id="discount-mode" value={mode} onChange={(event) => changeMode(event.target.value as DiscountKind)}>
            <option value="amount">{text.modes.amount}</option>
            <option value="percent">{text.modes.percent}</option>
            <option value="coupon">{text.modes.coupon}</option>
            <option value="points">{text.modes.points}</option>
          </Select>
        </Field>
        {mode === 'coupon' && (
          <CouponCodeField
            value={value}
            error={errors.coupon_code ?? (tried && !value.trim() ? text.errors.coupon : undefined)}
            onChange={(next) => edit(() => setValue(next))}
          />
        )}
        {mode === 'points' && (
          <PointsField
            value={value}
            balance={pointsBalance}
            error={valueError ?? errors.points ?? (tried && !value.trim() ? text.errors.points : undefined)}
            onChange={(next) => edit(() => setValue(next))}
          />
        )}
        {isManual(mode) && (
          <Field
            label={mode === 'amount' ? text.amount : text.percent}
            htmlFor="discount-value"
            error={valueError ?? errors.amount_satang ?? errors.percent ?? (tried && !value.trim() ? (mode === 'amount' ? text.errors.amount : text.errors.percent) : undefined)}
            hint={mode === 'percent' ? text.percentHint : undefined}
          >
            <Input
              id="discount-value"
              inputMode="decimal"
              value={value}
              aria-invalid={valueError ? true : undefined}
              onChange={(event) => edit(() => setValue(event.target.value))}
            />
          </Field>
        )}
      </div>

      {preview && preview.amountSatang !== null && !preview.problem && (
        <DiscountPreviewList
          amountSatang={preview.amountSatang}
          totalSatang={invoice.total_satang}
          balanceSatang={invoice.balance_satang}
        />
      )}

      {/* A coupon or points carry their own reason and approver, recorded by the backend. */}
      {isManual(mode) && (
        <>
          <Field
            label={text.reason}
            htmlFor="discount-reason"
            error={errors.reason ?? (tried && !reason.trim() ? text.errors.reason : undefined)}
            hint={text.reasonHint}
          >
            <Input id="discount-reason" value={reason} maxLength={500} onChange={(event) => edit(() => setReason(event.target.value))} />
          </Field>

          <ApproverField
            self={self}
            staff={staff}
            choice={approver}
            otherId={otherId}
            error={errors.approved_by_staff_id ?? (tried && !approverId ? text.errors.approver : undefined)}
            onChoice={(next) => edit(() => setApprover(next))}
            onOtherId={(next) => edit(() => setOtherId(next))}
          />
        </>
      )}

      <div className="flex flex-wrap items-center gap-2">
        <Button type="submit" size="sm" disabled={pending}>
          {pending
            ? text.saving
            : mode === 'coupon'
              ? text.submitCoupon
              : preview && preview.amountSatang !== null && !preview.problem
                ? text.submit(formatBaht(preview.amountSatang))
                : text.submitEmpty}
        </Button>
        <Button variant="ghost" size="sm" onClick={() => edit(() => setOpen(false))}>
          {text.cancel}
        </Button>
      </div>
    </form>
  );
}
