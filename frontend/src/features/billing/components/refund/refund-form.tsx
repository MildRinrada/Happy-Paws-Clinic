'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { fieldErrors } from '@/lib/actions/field-errors';
import { formatBaht, parseBahtToSatang } from '@/lib/format/money';
import { refundPaymentAction } from '../../actions/billing-actions';
import { billingMessages } from '../../content/billing-messages';
import { refundForm } from '../../content/refund-form';
import { paymentMethodLabel } from '../../lib/invoice-labels';
import type { Invoice, PaymentMethod } from '../../types/billing.types';
import { RefundConfirm } from './refund-confirm';
import { RefundTenderFields } from './refund-tender-fields';

const toBahtText = (satang: number) => (satang / 100).toFixed(2);

/**
 * "คืนเงินบางส่วนหรือทั้งใบ ผูกเหตุผล". Money leaves the clinic, so it takes two presses: the
 * form, then a summary that has to be confirmed.
 *
 * Any refund, partial or whole, moves the bill to `refunded` and it stays there
 * (docs/assumptions.md § 22). On a bill that was only partly paid that also ends the chase for
 * the rest, so the summary says so before the button is pressed.
 *
 * One idempotency key per refund: a retry after a response lost on the way back reuses it, so the
 * money cannot go back twice. Changing anything on the form, or a refusal from the backend, mints
 * a new one: the same key with a different body is refused as `idempotency_key_reused`, and a
 * refusal is stored under its key and would be replayed to every retry.
 */
export function RefundForm({ invoice }: { invoice: Invoice }) {
  const text = refundForm;
  const refundable = Math.max(0, invoice.paid_satang - invoice.refunded_satang);

  const [open, setOpen] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [amount, setAmount] = useState(toBahtText(refundable));
  const [paymentId, setPaymentId] = useState('');
  const [method, setMethod] = useState<PaymentMethod | ''>('');
  const [reason, setReason] = useState('');
  const [key, setKey] = useState(() => crypto.randomUUID());
  const [tried, setTried] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string>();
  const [done, setDone] = useState<string>();
  const [pending, startTransition] = useTransition();

  const amountSatang = parseBahtToSatang(amount);
  const amountError =
    amountSatang === null || amountSatang <= 0
      ? text.errors.amount
      : amountSatang > refundable
        ? text.errors.over(formatBaht(refundable))
        : undefined;

  if (refundable === 0) {
    return <p className="text-sm text-ink-500">{text.nothingLeft}</p>;
  }

  function edit(apply: () => void) {
    setDone(undefined);
    setMessage(undefined);
    setErrors({});
    setKey(crypto.randomUUID());
    apply();
  }

  function review(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setTried(true);

    if (amountError || !reason.trim()) {
      return;
    }

    setMessage(undefined);
    setConfirming(true);
  }

  function confirm() {
    if (amountSatang === null || amountError) {
      return;
    }

    setMessage(undefined);
    startTransition(async () => {
      try {
        const result = await refundPaymentAction({
          invoiceId: invoice.id,
          version: invoice.version,
          amountSatang,
          reason: reason.trim(),
          method: method || undefined,
          paymentId: paymentId || undefined,
          idempotencyKey: key,
        });

        if (result.ok) {
          const left = Math.max(0, result.data.paid_satang - result.data.refunded_satang);
          setDone(text.saved(formatBaht(amountSatang)));
          setOpen(false);
          setConfirming(false);
          setTried(false);
          setAmount(toBahtText(left));
          setReason('');
          setPaymentId('');
          setMethod('');
          setKey(crypto.randomUUID());
          return;
        }

        // A settled refusal is stored under the key and would be replayed to a retry (the backend
        // keeps every non-5xx answer for 24 hours), so the next attempt needs a key of its own.
        if (result.error.status < 500) {
          setKey(crypto.randomUUID());
        }

        if (result.error.code === 'version_conflict') {
          // The page has re-read the bill. Back to the form, so the refundable figure is checked again.
          setMessage(billingMessages.conflict);
          setConfirming(false);
          return;
        }

        const byField = fieldErrors(result.error);
        if (Object.keys(byField).length > 0) {
          setErrors(byField);
          setConfirming(false);
        } else {
          setMessage(result.error.message);
        }
      } catch {
        setMessage(billingMessages.unreachable);
      }
    });
  }

  if (!open) {
    return (
      <div className="space-y-3">
        {done && <Alert tone="success">{done}</Alert>}
        <p className="text-sm text-ink-600">{text.refundable(formatBaht(refundable))}</p>
        <Button variant="danger" size="sm" onClick={() => edit(() => setOpen(true))}>
          {text.open}
        </Button>
      </div>
    );
  }

  const chosenPayment = invoice.payments.find((payment) => payment.id === paymentId);

  if (confirming && amountSatang !== null) {
    return (
      <RefundConfirm
        amount={formatBaht(amountSatang)}
        method={method ? paymentMethodLabel[method] : chosenPayment ? paymentMethodLabel[chosenPayment.method] : text.methodDefault}
        payment={
          chosenPayment
            ? text.paymentOption(paymentMethodLabel[chosenPayment.method], formatBaht(chosenPayment.amount_satang), chosenPayment.reference)
            : undefined
        }
        reason={reason.trim()}
        warning={invoice.status === 'partially_paid' ? text.partialWarning : text.terminalNote}
        pending={pending}
        message={message}
        onConfirm={confirm}
        onBack={() => setConfirming(false)}
      />
    );
  }

  return (
    <form onSubmit={review} className="space-y-4" noValidate>
      {message && <Alert>{message}</Alert>}

      <Field
        label={text.amount}
        htmlFor="refund-amount"
        error={(tried ? amountError : amountSatang !== null && amountSatang > refundable ? amountError : undefined) ?? errors.amount_satang}
        hint={text.refundable(formatBaht(refundable))}
      >
        <div className="flex gap-2">
          <Input
            id="refund-amount"
            inputMode="decimal"
            value={amount}
            aria-invalid={tried && amountError ? true : undefined}
            className="min-w-0 flex-1"
            onChange={(event) => edit(() => setAmount(event.target.value))}
          />
          <Button variant="secondary" onClick={() => edit(() => setAmount(toBahtText(refundable)))}>
            {text.full}
          </Button>
        </div>
      </Field>

      <RefundTenderFields
        payments={invoice.payments}
        paymentId={paymentId}
        method={method}
        errors={errors}
        onPayment={(value) => edit(() => setPaymentId(value))}
        onMethod={(value) => edit(() => setMethod(value))}
      />

      <Field
        label={text.reason}
        htmlFor="refund-reason"
        error={errors.reason ?? (tried && !reason.trim() ? text.errors.reason : undefined)}
        hint={text.reasonHint}
      >
        <Input id="refund-reason" value={reason} maxLength={500} onChange={(event) => edit(() => setReason(event.target.value))} />
      </Field>

      {invoice.status === 'partially_paid' && <Alert tone="info">{text.partialWarning}</Alert>}

      <div className="flex flex-wrap gap-2">
        <Button type="submit" variant="danger" size="sm">
          {text.review}
        </Button>
        <Button variant="ghost" size="sm" onClick={() => edit(() => setOpen(false))}>
          {text.cancel}
        </Button>
      </div>
    </form>
  );
}
