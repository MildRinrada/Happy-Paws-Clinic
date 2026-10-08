import { staffApi } from '@/lib/api/server-client';
import type { Invoice, InvoiceLineInput } from '../types/billing.types';

export interface CreateInvoiceInput {
  customerId: string;
  branchId?: string;
  petId?: string;
  note?: string;
  lines: InvoiceLineInput[];
  /** One key per bill, reused on every retry, so a double-click cannot raise two bills. */
  idempotencyKey: string;
}

/**
 * `POST /staff/invoices`: "สร้างเองหรือถูกสร้างอัตโนมัติตอนปิดเคส". The manual half, for a counter
 * sale or a charge with no case behind it. Needs `invoice:write` and an `Idempotency-Key`.
 */
export function createInvoice(input: CreateInvoiceInput): Promise<Invoice> {
  return staffApi().post<Invoice>('/staff/invoices', {
    idempotencyKey: input.idempotencyKey,
    body: {
      customer_id: input.customerId,
      branch_id: input.branchId,
      pet_id: input.petId,
      note: input.note,
      lines: input.lines,
    },
  });
}
