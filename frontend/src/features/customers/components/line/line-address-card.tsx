import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { customerLine } from '../../content/customer-line';
import type { CustomerLine } from '../../types/customer.types';
import { LineAddressManager } from './line-address-manager';
import { LineLinkedSummary } from './line-linked-summary';
import { LineStatusBadge } from './line-status-badge';

interface LineAddressCardProps {
  line: CustomerLine;
  customerName: string;
  /** `customer:write`, and the record is not a merged one. */
  canEdit: boolean;
}

/**
 * Where the customer's LINE messages go. LINE is a channel to send on, like an email address: the
 * counter picks the person from the clinic's recent LINE followers, and nothing here signs anyone in.
 */
export function LineAddressCard({ line, customerName, canEdit }: LineAddressCardProps) {
  return (
    <Card>
      <CardHeader title={customerLine.title} aside={<LineStatusBadge line={line} />} />
      <CardBody className="space-y-3">
        <LineLinkedSummary line={line} />
        {canEdit && <LineAddressManager key={line.version} line={line} customerName={customerName} />}
      </CardBody>
    </Card>
  );
}
