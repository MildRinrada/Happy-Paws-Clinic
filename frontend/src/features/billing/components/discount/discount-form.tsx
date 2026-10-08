'use client';

import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { formatBaht } from '@/lib/format/money';
import { applyDiscountAction } from '../../actions/billing-actions';
import { billingMessages } from '../../content/billing-messages';
import { discountForm } from '../../content/discount-form';
import { previewDiscount, type DiscountMode } from '../../lib/discount-preview';
import type { Invoice } from '../../types/billing.types';
import { ApproverField, OTHER_APPROVER, type StaffOption } from './approver-field';
import { DiscountPreviewList } from './discount-preview-list';

interface Props {
  invoice: Invoice;
  self: StaffOption;
  staff: StaffOption[] | null;
}

/**
 * "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ": an amount or a percent, a reason, and who approved it.
 *
 * Sent with the bill's version. A dispense upstairs can add a medicine line between the page
 * loading and the button being pressed; the backend then answers `version_conflict`, the action
 * re-reads the bill, and the form stays open with what was typed and the new figures beside it,
 * so the cashier decides again against the total the customer will actually see.
 */
export function DiscountForm({ invoice, self, staff }: Props) {
  const text = discountForm;
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<DiscountMode>('amount');
  const [value, setValue] = useState('');
  const [reason, setReason] = useState('');
  const [approver, setApprover] = useState(self.id);
  const [otherId, setOtherId] = useState('');
  const [done, setDone] = useState<string>();
  const [tried, setTried] = useState(false);
  const { errors, message, pending, submit, touch } = useFormSubmit(billingMessages);

  const preview = previewDiscount(invoice, mode, value);
  const approverId = approver === OTHER_APPROVER ? otherId.trim() : approver;

  const valueError = !value.trim()
    ? undefined
    : preview.problem === 'format'
      ? mode === 'amount' ? text.errors.amount : text.errors.percent
      : preview.problem === 'tooLarge' || preview.problem === 'belowPaid'
        ? `${text.errors.tooLarge(formatBaht(preview.roomSatang))}${preview.problem === 'belowPaid' ? ` ${text.errors.belowPaid}` : ''}`
        : undefined;

  function edit(apply: () => void) {
    touch();
    setDone(undefined);
    apply();
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setTried(true);

    if (preview.problem || preview.amountSatang === null || !reason.trim() || !approverId) {
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
          <Select id="discount-mode" value={mode} onChange={(event) => edit(() => setMode(event.target.value as DiscountMode))}>
            <option value="amount">{text.modes.amount}</option>
            <option value="percent">{text.modes.percent}</option>
          </Select>
        </Field>
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
      </div>

      {preview.amountSatang !== null && !preview.problem && (
        <DiscountPreviewList
          amountSatang={preview.amountSatang}
          totalSatang={invoice.total_satang}
          balanceSatang={invoice.balance_satang}
        />
      )}

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

      <div className="flex flex-wrap items-center gap-2">
        <Button type="submit" size="sm" disabled={pending}>
          {pending
            ? text.saving
            : preview.amountSatang !== null && !preview.problem
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
