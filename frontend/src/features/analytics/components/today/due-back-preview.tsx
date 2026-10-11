import Link from 'next/link';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { crmPaths } from '@/features/crm/lib/crm-paths';
import { dueBackPreview } from '../../content/due-back-preview';
import type { DueBackPreviewView } from '../../lib/load-due-back-preview';
import { DueBackPreviewRow } from './due-back-preview-row';

/**
 * "ลูกค้าที่ถึงเวลากลับมา": the first few of the CRM call list, for a quiet moment at the counter.
 * The full list, and the customers who are late against their own rhythm, are on the CRM page.
 */
export function DueBackPreview({ view, canOpenCustomer }: { view: DueBackPreviewView; canOpenCustomer: boolean }) {
  const text = dueBackPreview;

  return (
    <Card>
      <CardHeader
        title={text.heading}
        aside={
          <Link href={crmPaths.dueBack('vaccine_overdue')} className="shrink-0 text-sm text-brand-700 hover:underline">
            {text.all}
          </Link>
        }
      />
      <CardBody className="space-y-2">
        <p className="text-xs text-ink-500">{text.hint}</p>
        {view.rows.length === 0 ? (
          <p className="text-sm text-ink-600">{text.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {view.rows.map((row) => (
              <DueBackPreviewRow key={row.customerId} row={row} canOpenCustomer={canOpenCustomer} />
            ))}
          </ul>
        )}
        <Link href={crmPaths.dueBack('lapsed')} className="inline-block text-sm text-brand-700 hover:underline">
          {text.lapsed}
        </Link>
      </CardBody>
    </Card>
  );
}
