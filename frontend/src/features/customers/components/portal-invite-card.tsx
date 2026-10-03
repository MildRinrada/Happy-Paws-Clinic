import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { formatDateTime } from '@/lib/format/datetime';
import { customerDetail } from '../content/customer-detail';
import type { Customer, PortalInvitation } from '../types/customer.types';
import { InviteButton } from './invite-button';
import { PortalStatusBadge } from './portal-status-badge';

interface PortalInviteCardProps {
  customer: Customer;
  invitation?: PortalInvitation;
  canInvite: boolean;
}

/** Whether the customer has an online account, and if not, whether they have been asked. */
export function PortalInviteCard({ customer, invitation, canInvite }: PortalInviteCardProps) {
  const text = customerDetail.portal;
  const linked = customer.has_portal_account;

  let status = text.never;
  if (linked) status = text.linked;
  else if (invitation?.is_live) status = text.live(invitation.sent_to, formatDateTime(invitation.expires_at));
  else if (invitation) status = text.expired(formatDateTime(invitation.sent_at));

  return (
    <Card>
      <CardHeader title={text.title} aside={<PortalStatusBadge linked={linked} />} />
      <CardBody className="space-y-3">
        <p className="text-sm text-ink-700">{status}</p>
        {!linked && canInvite && !customer.merged_into_customer_id && (
          <InviteButton customerId={customer.id} again={Boolean(invitation)} />
        )}
      </CardBody>
    </Card>
  );
}
