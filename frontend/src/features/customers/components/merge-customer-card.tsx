import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { mergeCustomer } from '../content/merge-customer';
import type { Customer } from '../types/customer.types';
import { MergePanel } from './merge-panel';

/**
 * Folding a duplicate into the customer on screen. Shown only to `customer:merge`, and never on a
 * record that is itself merged: the backend refuses a tombstone as the survivor.
 */
export function MergeCustomerCard({ customer }: { customer: Customer }) {
  return (
    <Card>
      <CardHeader title={mergeCustomer.title} />
      <CardBody className="space-y-3">
        <p className="text-sm text-ink-600">{mergeCustomer.intro}</p>
        <MergePanel customer={customer} />
      </CardBody>
    </Card>
  );
}
