import { listDueBack } from '@/features/crm/api/listDueBack';
import { getCustomer } from '@/features/customers/api/getCustomer';
import type { DueBackPreviewRow } from '../types/today.types';
import { tolerant } from './tolerant-read';
import { DUE_BACK_PREVIEW } from './today-settings';

export interface DueBackPreviewView {
  rows: DueBackPreviewRow[];
}

/**
 * The first few customers whose pet's vaccine is overdue (docs/contract-extensions.md § 33), on
 * `crm:read`. The call list carries no phone number, so each of the few is looked up on the
 * customer record when the session may read customers; without that, the row has the name only.
 */
export async function loadDueBackPreview(branchId: string, canReadCustomers: boolean): Promise<DueBackPreviewView> {
  const page = await listDueBack('vaccine_overdue', branchId);
  const first = page.data.slice(0, DUE_BACK_PREVIEW);

  const customers = canReadCustomers
    ? await Promise.all(first.map((row) => tolerant(() => getCustomer(row.customer_id))))
    : [];

  return {
    rows: first.map((row, index) => ({
      customerId: row.customer_id,
      customerName: row.customer_name,
      phone: customers[index]?.customer.phone || undefined,
      petNames: row.pets.map((pet) => pet.name),
      daysOverdue: row.days_overdue,
    })),
  };
}
