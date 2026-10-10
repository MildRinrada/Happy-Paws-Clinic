import { Badge, type BadgeTone } from '@/components/ui/badge';
import { queueDisplays } from '../../content/queue-displays';
import type { QueueDisplayStatus } from '../../types/queue.types';

const tones: Record<QueueDisplayStatus, BadgeTone> = {
  pending_setup: 'amber',
  setup_expired: 'neutral',
  active: 'green',
  expired: 'neutral',
  revoked: 'red',
};

/** Where a screen is in its life: waiting for the TV, working, or stopped. */
export function DisplayStatusBadge({ status }: { status: QueueDisplayStatus }) {
  return <Badge tone={tones[status]}>{queueDisplays.status[status]}</Badge>;
}
