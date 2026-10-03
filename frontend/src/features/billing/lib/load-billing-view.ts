import { getCustomer } from '@/features/customers/api/getCustomer';
import type { CustomerDetail } from '@/features/customers/types/customer.types';
import { optional } from '@/lib/api/api-error';
import { getInvoice } from '../api/getInvoice';
import { listInvoices, type ListInvoicesQuery } from '../api/listInvoices';
import type { Invoice } from '../types/billing.types';

export interface InvoiceRow {
  invoice: Invoice;
  customerName?: string;
}

export interface InvoiceListView {
  rows: InvoiceRow[];
  nextCursor?: string;
}

/**
 * The counter's list of bills, with the customer's name beside each.
 *
 * A bill carries a customer id. The name is read from the customer register under `customer:read`
 * — which a cashier, as the contract defines the role, does not hold. So it is optional: their
 * list shows the bill number and the amount, which is what they match against the slip in the
 * customer's hand, and a receptionist's shows the name as well.
 */
export async function loadInvoiceList(query: ListInvoicesQuery): Promise<InvoiceListView> {
  const page = await listInvoices(query);

  const customerIds = [...new Set(page.data.map((invoice) => invoice.customer_id))];
  const customers = await Promise.all(customerIds.map((id) => optional(getCustomer(id))));
  const nameById = new Map(customerIds.map((id, index) => [id, customers[index]?.customer.display_name]));

  return {
    rows: page.data.map((invoice) => ({ invoice, customerName: nameById.get(invoice.customer_id) })),
    nextCursor: page.page.next_cursor,
  };
}

export interface InvoiceDetailView {
  invoice: Invoice;
  customer: CustomerDetail | null;
}

export async function loadInvoiceDetail(invoiceId: string): Promise<InvoiceDetailView> {
  const invoice = await getInvoice(invoiceId);

  return { invoice, customer: await optional(getCustomer(invoice.customer_id)) };
}
