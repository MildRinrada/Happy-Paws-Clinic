'use client';

import { useState, useTransition } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { formatDateTime } from '@/lib/format/datetime';
import { formatBaht } from '@/lib/format/money';
import { issueReceiptAction } from '../actions/billing-actions';
import type { Receipt } from '../types/billing.types';

interface Props {
  invoiceId: string;
  receipt?: Receipt;
  /** A receipt is for money received, so there has to have been some. */
  hasPayments: boolean;
}

/**
 * The customer's copy — "ออกใบเสร็จ/ใบกำกับภาษี".
 *
 * One bill has one receipt. Pressing the button a second time returns the document already
 * issued rather than a second original, which matters for a ใบกำกับภาษี: two originals of one tax
 * document is a problem in the clinic's books.
 */
export function ReceiptPanel({ invoiceId, receipt, hasPayments }: Props) {
  const [wantsTaxInvoice, setWantsTaxInvoice] = useState(false);
  const [taxId, setTaxId] = useState('');
  const [legalName, setLegalName] = useState('');
  const [billingAddress, setBillingAddress] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  if (receipt) {
    return (
      <div className="space-y-3 text-sm">
        <dl className="space-y-1.5">
          <div className="flex justify-between gap-4">
            <dt className="text-ink-500">{receipt.is_tax_invoice ? 'ใบกำกับภาษีเลขที่' : 'ใบเสร็จเลขที่'}</dt>
            <dd className="font-medium text-ink-900">{receipt.number}</dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-ink-500">ออกเมื่อ</dt>
            <dd className="text-ink-800">{formatDateTime(receipt.issued_at)}</dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-ink-500">จำนวนเงิน</dt>
            <dd className="tabular-nums text-ink-800">{formatBaht(receipt.amount_satang)}</dd>
          </div>
          {receipt.tax_id && (
            <div className="flex justify-between gap-4">
              <dt className="text-ink-500">เลขประจำตัวผู้เสียภาษี</dt>
              <dd className="text-ink-800">{receipt.tax_id}</dd>
            </div>
          )}
        </dl>
        <Button variant="secondary" className="no-print w-full" onClick={() => window.print()}>
          พิมพ์
        </Button>
      </div>
    );
  }

  if (!hasPayments) {
    return <p className="text-sm text-ink-500">ออกใบเสร็จได้หลังรับชำระเงินแล้ว</p>;
  }

  const taxInvoiceReady = taxId.trim() !== '' && legalName.trim() !== '' && billingAddress.trim() !== '';

  function issue() {
    setError(null);
    startTransition(async () => {
      const result = await issueReceiptAction({
        invoiceId,
        taxInvoice: wantsTaxInvoice
          ? { taxId: taxId.trim(), legalName: legalName.trim(), billingAddress: billingAddress.trim() }
          : undefined,
      });

      if (!result.ok) {
        setError(result.error.message);
      }
    });
  }

  return (
    <div className="space-y-4">
      {error && <Alert>{error}</Alert>}

      <label className="flex items-center gap-2 text-sm text-ink-800">
        <input
          type="checkbox"
          className="size-4 rounded border-ink-300"
          checked={wantsTaxInvoice}
          onChange={(event) => setWantsTaxInvoice(event.target.checked)}
        />
        ออกเป็นใบกำกับภาษี
      </label>

      {wantsTaxInvoice && (
        <>
          <Field label="เลขประจำตัวผู้เสียภาษี" htmlFor="receipt-tax-id">
            <Input id="receipt-tax-id" inputMode="numeric" value={taxId} onChange={(e) => setTaxId(e.target.value)} />
          </Field>
          <Field label="ชื่อในใบกำกับภาษี" htmlFor="receipt-legal-name">
            <Input id="receipt-legal-name" value={legalName} onChange={(e) => setLegalName(e.target.value)} />
          </Field>
          <Field label="ที่อยู่" htmlFor="receipt-address">
            <Input id="receipt-address" value={billingAddress} onChange={(e) => setBillingAddress(e.target.value)} />
          </Field>
        </>
      )}

      <Button className="w-full" disabled={pending || (wantsTaxInvoice && !taxInvoiceReady)} onClick={issue}>
        {pending ? 'กำลังออกเอกสาร…' : wantsTaxInvoice ? 'ออกใบกำกับภาษี' : 'ออกใบเสร็จ'}
      </Button>
    </div>
  );
}
