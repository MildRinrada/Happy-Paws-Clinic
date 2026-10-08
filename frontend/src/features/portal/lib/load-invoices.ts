import { authed } from '@/lib/auth/session';
import { listMyInvoices } from '../api/listMyInvoices';
import type { Invoice } from '../types/invoice.types';
import { loadPortalHome } from './load-portal-home';

/** How many bills one page of the list shows. */
const PAGE_SIZE = 20;

/** The most the API gives in one read. Enough outstanding bills for any one household. */
const OUTSTANDING_LIMIT = 100;

export interface InvoicesView {
  /** This page of the list: every bill, or only the unpaid ones. */
  invoices: Invoice[];
  nextCursor?: string;
  /** What is still owed, summed from the backend's own `balance_satang` of each unpaid bill. */
  outstanding: { count: number; balanceSatang: number; more: boolean };
  /** Pet id to name, for the line under each bill. */
  petNames: Record<string, string>;
}

/**
 * The bills page: one page of the list asked for, and the outstanding total shown above it. The
 * unpaid list is read again for the total even on the "ค้างชำระ" tab, so the banner is the same
 * whichever page of that tab is open.
 */
export async function loadInvoices(unpaidOnly: boolean, cursor?: string): Promise<InvoicesView> {
  const [page, unpaid, home] = await Promise.all([
    authed(() => listMyInvoices(unpaidOnly, PAGE_SIZE, cursor)),
    authed(() => listMyInvoices(true, OUTSTANDING_LIMIT)),
    loadPortalHome(),
  ]);

  return {
    invoices: page.data,
    nextCursor: page.page.next_cursor ?? undefined,
    outstanding: {
      count: unpaid.data.length,
      balanceSatang: unpaid.data.reduce((sum, invoice) => sum + invoice.balance_satang, 0),
      more: Boolean(unpaid.page.next_cursor),
    },
    petNames: Object.fromEntries(home.pets.map((pet) => [pet.id, pet.name])),
  };
}
