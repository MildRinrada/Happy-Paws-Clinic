import { formatDateTime } from '@/lib/format/datetime';
import { customerLine } from '../../content/customer-line';
import type { CustomerLine } from '../../types/customer.types';
import { LineAvatar } from './line-avatar';

/** Whose LINE the record carries, and whether a message would reach it now. */
export function LineLinkedSummary({ line }: { line: CustomerLine }) {
  const text = customerLine;

  if (!line.linked) {
    return <p className="text-sm text-ink-700">{text.notLinked}</p>;
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-3">
        <LineAvatar name={line.display_name} pictureUrl={line.picture_url} />
        <p className="text-sm font-medium text-ink-900">{line.display_name ?? text.noName}</p>
      </div>
      <p className="text-sm text-ink-700">
        {line.can_receive || !line.unfollowed_at ? text.canReceive : text.blockedSince(formatDateTime(line.unfollowed_at))}
      </p>
    </div>
  );
}
