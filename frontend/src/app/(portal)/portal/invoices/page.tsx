import type { Metadata } from 'next';
import { InvoiceCard } from '@/features/portal/components/invoices/invoice-card';
import { InvoicePager } from '@/features/portal/components/invoices/invoice-pager';
import { InvoiceTabs } from '@/features/portal/components/invoices/invoice-tabs';
import { OutstandingBanner } from '@/features/portal/components/invoices/outstanding-banner';
import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { portalInvoices } from '@/features/portal/content/portal-invoices';
import { loadInvoices } from '@/features/portal/lib/load-invoices';

export const metadata: Metadata = { title: `${portalInvoices.title} · Happy Paws Clinic` };

interface InvoicesPageProps {
  searchParams: Promise<{ unpaid?: string; cursor?: string }>;
}

/** The owner's bills, newest first, 20 to a page, with what is still owed above them. */
export default async function PortalInvoicesPage({ searchParams }: InvoicesPageProps) {
  const { unpaid, cursor } = await searchParams;
  const unpaidOnly = unpaid === 'true';
  const view = await loadInvoices(unpaidOnly, cursor);
  const text = portalInvoices;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <PageHeading title={text.title} intro={text.intro} />

      <OutstandingBanner outstanding={view.outstanding} />

      <InvoiceTabs unpaidOnly={unpaidOnly} unpaidCount={view.outstanding.count} more={view.outstanding.more} />

      {view.invoices.length === 0 ? (
        <EmptyLine>{unpaidOnly ? text.emptyUnpaid : text.empty}</EmptyLine>
      ) : (
        <div className="space-y-3">
          {view.invoices.map((invoice) => (
            <InvoiceCard key={invoice.id} invoice={invoice} petName={invoice.pet_id ? view.petNames[invoice.pet_id] : undefined} />
          ))}
        </div>
      )}

      <InvoicePager unpaidOnly={unpaidOnly} nextCursor={view.nextCursor} paged={Boolean(cursor)} />
    </div>
  );
}
