import { formatDateTime } from '@/lib/format/datetime';
import { queueDisplays } from '../../content/queue-displays';
import type { QueueDisplaySummary } from '../../types/queue.types';
import { DisplayStatusBadge } from './display-status-badge';
import { RevokeDisplayButton } from './revoke-display-button';

const text = queueDisplays.list;

/** One screen: its name, its state, when it was set up and last seen, and the revoke button while it can still work. */
export function DisplayRow({ display }: { display: QueueDisplaySummary }) {
  const stoppable = display.status === 'active' || display.status === 'pending_setup';

  return (
    <tr className="align-top">
      <td className="px-4 py-3 text-ink-900">{display.name}</td>
      <td className="px-4 py-3">
        <DisplayStatusBadge status={display.status} />
      </td>
      <td className="whitespace-nowrap px-4 py-3 text-xs tabular-nums text-ink-600">
        <span className="block">{text.createdAt(formatDateTime(display.created_at))}</span>
        {display.claimed_at && <span className="block">{text.claimedAt(formatDateTime(display.claimed_at))}</span>}
        {display.status === 'pending_setup' && display.setup_expires_at && (
          <span className="block">{text.setupUntil(formatDateTime(display.setup_expires_at))}</span>
        )}
        <span className="block">{display.last_seen_at ? text.lastSeen(formatDateTime(display.last_seen_at)) : text.neverSeen}</span>
        <span className="block">{display.expires_at ? text.expires(formatDateTime(display.expires_at)) : text.noExpiry}</span>
        {display.revoked_at && <span className="block text-red-700">{text.revokedAt(formatDateTime(display.revoked_at))}</span>}
      </td>
      <td className="px-4 py-3">{stoppable && <RevokeDisplayButton id={display.id} />}</td>
    </tr>
  );
}
