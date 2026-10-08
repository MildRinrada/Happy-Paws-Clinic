'use client';

import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { refundForm } from '../../content/refund-form';

interface Props {
  amount: string;
  method: string;
  payment?: string;
  reason: string;
  warning?: string;
  pending: boolean;
  message?: string;
  onConfirm: () => void;
  onBack: () => void;
}

/** The second look before money leaves the clinic: what, how, from which payment, and why. */
export function RefundConfirm({ amount, method, payment, reason, warning, pending, message, onConfirm, onBack }: Props) {
  const text = refundForm.confirm;
  const rows = [
    { label: text.amount, value: amount, strong: true },
    { label: text.method, value: method },
    ...(payment ? [{ label: text.payment, value: payment }] : []),
    { label: text.reason, value: reason },
  ];

  return (
    <div className="space-y-4" role="group" aria-labelledby="refund-confirm-heading">
      <div className="space-y-1">
        <h3 id="refund-confirm-heading" className="text-sm font-semibold text-red-800">
          {text.heading}
        </h3>
        <p className="text-sm text-ink-600">{text.question}</p>
      </div>

      {message && <Alert>{message}</Alert>}

      <dl className="space-y-1.5 rounded-lg border border-red-200 bg-red-50/50 px-3 py-2 text-sm">
        {rows.map((row) => (
          <div key={row.label} className="flex justify-between gap-4">
            <dt className="text-ink-500">{row.label}</dt>
            <dd className={`text-right ${row.strong ? 'font-semibold tabular-nums text-ink-900' : 'text-ink-800'}`}>{row.value}</dd>
          </div>
        ))}
      </dl>

      {warning && <Alert tone="info">{warning}</Alert>}

      <div className="flex flex-wrap gap-2">
        <Button variant="danger" size="sm" disabled={pending} onClick={onConfirm}>
          {pending ? text.saving : text.submit(amount)}
        </Button>
        <Button variant="ghost" size="sm" disabled={pending} onClick={onBack}>
          {text.back}
        </Button>
      </div>
    </div>
  );
}
