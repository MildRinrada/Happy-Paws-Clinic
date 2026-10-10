import type { ReactNode } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { PageLinks } from '@/features/crm/components/shared/page-links';
import { purchaseOrderListText } from '../content/purchase-order-list';
import type { PurchaseOrder } from '../types/inventory.types';
import { PurchaseOrderEntry } from './purchase-order-entry';

interface PurchaseOrderListProps {
  orders: PurchaseOrder[];
  filters: ReactNode;
  nextHref?: string;
  firstHref?: string;
}

/** The branch's orders, newest first, each with its lines and the cancel button while anything is outstanding. */
export function PurchaseOrderList({ orders, filters, nextHref, firstHref }: PurchaseOrderListProps) {
  const text = purchaseOrderListText;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <p className="text-sm text-ink-600">{text.hint}</p>
        {filters}
        {orders.length === 0 ? (
          <p className="text-sm text-ink-500">{text.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {orders.map((order) => (
              <PurchaseOrderEntry key={order.id} order={order} />
            ))}
          </ul>
        )}
        <PageLinks nextHref={nextHref} firstHref={firstHref} nextLabel={text.next} firstLabel={text.first} />
      </CardBody>
    </Card>
  );
}
