import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { formatDateTime } from '@/lib/format/datetime';
import { consultationPage } from '../../content/consultation-page';

interface ClosedNoticeProps {
  /** Shown only right after closing, from the address the close panel set. */
  justClosed: boolean;
  invoice?: { id: string; number: string };
  followUpAt?: string;
}

/** A closed case: read-only from here on, and, just after closing, the bill and follow-up it produced. */
export function ClosedNotice({ justClosed, invoice, followUpAt }: ClosedNoticeProps) {
  const text = consultationPage.close;

  if (!justClosed) {
    return <Alert tone="info">{consultationPage.readOnly}</Alert>;
  }

  return (
    <Alert tone="success">
      <p className="font-semibold">{text.done}</p>
      {invoice && (
        <p>
          {text.invoice(invoice.number)}{' '}
          <Link href={`/billing/${encodeURIComponent(invoice.id)}`} className="font-medium underline">
            {text.openInvoice}
          </Link>
        </p>
      )}
      {followUpAt && <p>{text.followUpBooked(formatDateTime(followUpAt))}</p>}
    </Alert>
  );
}
