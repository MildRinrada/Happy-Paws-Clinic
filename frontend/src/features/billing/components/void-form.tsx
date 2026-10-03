'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { voidInvoiceAction } from '../actions/billing-actions';

/**
 * Cancels a bill raised in error.
 *
 * Offered only while the bill is `open`, which is the backend's rule restated as a hidden button:
 * once money has been taken, the correction is a refund — which says what was given back and why
 * — and not a void, which says the bill should never have existed. A reason is required and goes
 * into the audit log with the bill's number and total.
 */
export function VoidForm({ invoiceId, version }: { invoiceId: string; version: number }) {
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  if (!open) {
    return (
      <Button variant="danger" size="sm" onClick={() => setOpen(true)}>
        ยกเลิกบิลนี้
      </Button>
    );
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    startTransition(async () => {
      const result = await voidInvoiceAction({ invoiceId, version, reason });

      if (!result.ok) {
        setError(result.error.message);
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-3">
      {error && <Alert>{error}</Alert>}

      <Field label="เหตุผลที่ยกเลิก" htmlFor="void-reason" hint="เช่น ออกบิลผิดคน · บันทึกไว้ใน audit log">
        <Input id="void-reason" value={reason} onChange={(event) => setReason(event.target.value)} />
      </Field>

      <div className="flex gap-2">
        <Button type="submit" variant="danger" size="sm" disabled={pending || reason.trim() === ''}>
          {pending ? 'กำลังยกเลิก…' : 'ยืนยันยกเลิกบิล'}
        </Button>
        <Button variant="ghost" size="sm" onClick={() => setOpen(false)}>
          ไม่ยกเลิก
        </Button>
      </div>
    </form>
  );
}
