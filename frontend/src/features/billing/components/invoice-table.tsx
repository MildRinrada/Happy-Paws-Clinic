import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { EmptyState } from '@/components/ui/empty-state';
import { formatDateTime } from '@/lib/format/datetime';
import { formatBaht } from '@/lib/format/money';
import { invoiceStatusLabel } from '../lib/invoice-labels';
import type { InvoiceRow } from '../lib/load-billing-view';

/** The counter's list of bills. Each row opens the bill it names. */
export function InvoiceTable({ rows }: { rows: InvoiceRow[] }) {
  if (rows.length === 0) {
    return <EmptyState title="ไม่มีบิลในรายการนี้">บิลจะถูกสร้างอัตโนมัติเมื่อสัตวแพทย์ปิดเคส</EmptyState>;
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
      <table className="w-full min-w-[640px] text-sm">
        <thead className="border-b border-ink-100 text-left text-ink-500">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">เลขที่บิล</th>
            <th scope="col" className="px-4 py-3 font-medium">ลูกค้า</th>
            <th scope="col" className="px-4 py-3 font-medium">ออกเมื่อ</th>
            <th scope="col" className="px-4 py-3 text-right font-medium">ยอดรวม</th>
            <th scope="col" className="px-4 py-3 text-right font-medium">ค้างชำระ</th>
            <th scope="col" className="px-4 py-3 font-medium">สถานะ</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {rows.map(({ invoice, customerName }) => {
            const status = invoiceStatusLabel[invoice.status];

            return (
              <tr key={invoice.id} className="hover:bg-ink-50">
                <td className="px-4 py-3">
                  <Link href={`/billing/${invoice.id}`} className="font-medium text-brand-700 hover:underline">
                    {invoice.number}
                  </Link>
                </td>
                <td className="px-4 py-3 text-ink-800">{customerName ?? invoice.customer_id}</td>
                <td className="px-4 py-3 text-ink-600">{formatDateTime(invoice.issued_at)}</td>
                <td className="px-4 py-3 text-right tabular-nums text-ink-800">{formatBaht(invoice.total_satang)}</td>
                <td className="px-4 py-3 text-right font-medium tabular-nums text-ink-900">
                  {formatBaht(invoice.balance_satang)}
                </td>
                <td className="px-4 py-3">
                  <Badge tone={status.tone}>{status.text}</Badge>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
