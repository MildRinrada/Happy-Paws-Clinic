'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { formatBaht, parseBahtToSatang } from '@/lib/format/money';
import { collectPaymentAction } from '../actions/billing-actions';
import { paymentMethodLabel } from '../lib/invoice-labels';
import type { PaymentMethod } from '../types/billing.types';

interface Props {
  invoiceId: string;
  version: number;
  balanceSatang: number;
}

const methods: PaymentMethod[] = ['cash', 'card', 'promptpay'];

/**
 * Takes one tender against a bill — "เงินสด บัตร PromptPay · แบ่งจ่ายหลายวิธีได้".
 *
 * One submission is one tender. A customer paying part in cash and the rest by card is two
 * submissions, which is how the API models it and why the amount starts at the whole balance and
 * can be lowered.
 *
 * The idempotency key is the part that matters most on this screen. It is minted once per tender
 * and reused for every attempt at it, so a double-click, or a retry after a response that never
 * arrived, cannot take the customer's money twice. It changes only after a tender has succeeded.
 */
export function PaymentForm({ invoiceId, version, balanceSatang }: Props) {
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  const [method, setMethod] = useState<PaymentMethod>('cash');
  const [amount, setAmount] = useState((balanceSatang / 100).toFixed(2));
  const [reference, setReference] = useState('');
  const [error, setError] = useState<{ message: string; field?: string } | null>(null);
  const [pending, startTransition] = useTransition();

  const amountSatang = parseBahtToSatang(amount);
  const amountError =
    amountSatang === null || amountSatang <= 0
      ? 'กรอกจำนวนเงินเป็นตัวเลข เช่น 1460 หรือ 1460.50'
      : amountSatang > balanceSatang
        ? `เกินยอดค้างชำระ (${formatBaht(balanceSatang)})`
        : undefined;

  function submit(event: FormEvent) {
    event.preventDefault();

    if (amountSatang === null || amountError) {
      return;
    }

    setError(null);
    startTransition(async () => {
      const result = await collectPaymentAction({
        invoiceId,
        version,
        method,
        amountSatang,
        reference: reference.trim() || undefined,
        idempotencyKey,
      });

      if (result.ok) {
        // A new tender gets a new key. The page re-renders with the new balance and version.
        setIdempotencyKey(crypto.randomUUID());
        setAmount((result.data.balance_satang / 100).toFixed(2));
        setReference('');
      } else {
        setError(result.error);
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      {error && <Alert>{error.message}</Alert>}

      <Field label="ช่องทางชำระ" htmlFor="payment-method">
        <Select id="payment-method" value={method} onChange={(event) => setMethod(event.target.value as PaymentMethod)}>
          {methods.map((value) => (
            <option key={value} value={value}>
              {paymentMethodLabel[value]}
            </option>
          ))}
        </Select>
      </Field>

      <Field label="จำนวนเงิน (บาท)" htmlFor="payment-amount" error={amountError}>
        <Input
          id="payment-amount"
          inputMode="decimal"
          value={amount}
          aria-invalid={amountError ? true : undefined}
          aria-describedby={amountError ? 'payment-amount-error' : undefined}
          onChange={(event) => setAmount(event.target.value)}
        />
      </Field>

      {method !== 'cash' && (
        <Field
          label="เลขอ้างอิง"
          htmlFor="payment-reference"
          hint="เลขสลิปจากเครื่องรูดบัตร หรือเลขอ้างอิงการโอน — ระบบบันทึกไว้ ไม่ได้ตัดบัตรเอง"
        >
          <Input id="payment-reference" value={reference} onChange={(event) => setReference(event.target.value)} />
        </Field>
      )}

      <Button type="submit" className="w-full" disabled={pending || Boolean(amountError)}>
        {pending ? 'กำลังบันทึก…' : `รับชำระ ${amountSatang && !amountError ? formatBaht(amountSatang) : ''}`}
      </Button>
    </form>
  );
}
