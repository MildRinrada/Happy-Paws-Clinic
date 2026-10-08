'use client';

import { Field, Select } from '@/components/ui/field';
import { formatBaht } from '@/lib/format/money';
import { refundForm } from '../../content/refund-form';
import { paymentMethodLabel } from '../../lib/invoice-labels';
import type { InvoicePayment, PaymentMethod } from '../../types/billing.types';

const methods: PaymentMethod[] = ['cash', 'card', 'promptpay'];

interface Props {
  payments: InvoicePayment[];
  paymentId: string;
  method: PaymentMethod | '';
  errors: Record<string, string>;
  onPayment: (value: string) => void;
  onMethod: (value: PaymentMethod | '') => void;
}

/**
 * Which tender is being reversed, and how the money goes back. Both optional: left blank, the
 * backend uses the chosen payment's method, else the bill's only method, else cash.
 */
export function RefundTenderFields({ payments, paymentId, method, errors, onPayment, onMethod }: Props) {
  const text = refundForm;

  return (
    <>
      {payments.length > 1 && (
        <Field label={text.payment} htmlFor="refund-payment" error={errors.payment_id} hint={text.paymentHint}>
          <Select id="refund-payment" value={paymentId} onChange={(event) => onPayment(event.target.value)}>
            <option value="">{text.paymentAny}</option>
            {payments.map((payment) => (
              <option key={payment.id} value={payment.id}>
                {text.paymentOption(paymentMethodLabel[payment.method], formatBaht(payment.amount_satang), payment.reference)}
              </option>
            ))}
          </Select>
        </Field>
      )}

      <Field label={text.method} htmlFor="refund-method" error={errors.method} hint={text.methodHint}>
        <Select id="refund-method" value={method} onChange={(event) => onMethod(event.target.value as PaymentMethod | '')}>
          <option value="">{text.methodDefault}</option>
          {methods.map((value) => (
            <option key={value} value={value}>
              {paymentMethodLabel[value]}
            </option>
          ))}
        </Select>
      </Field>
    </>
  );
}
