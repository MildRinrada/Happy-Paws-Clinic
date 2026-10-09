import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { formatDateTime } from '@/lib/format/datetime';
import { messagesPage } from '../content/messages-page';
import { messagesHref, type MessagesQuery } from '../lib/messages-query';
import { messageText } from '../lib/message-text';
import type { MessageThread } from '../types/thread.types';

interface ThreadListItemProps {
  thread: MessageThread;
  query: MessagesQuery;
  selected: boolean;
}

/** One customer in the inbox: who, when, the newest line, and how many messages wait. */
export function ThreadListItem({ thread, query, selected }: ThreadListItemProps) {
  const awaiting = thread.status === 'awaiting_reply';

  return (
    <li>
      <Link
        href={messagesHref(query, { thread: thread.id })}
        aria-current={selected ? 'true' : undefined}
        className={`block px-4 py-3 hover:bg-ink-50 ${selected ? 'bg-brand-50' : ''}`}
      >
        <span className="flex items-center justify-between gap-2">
          <span className={`truncate text-sm ${awaiting ? 'font-semibold text-ink-900' : 'font-medium text-ink-700'}`}>
            {thread.customer_name ?? messagesPage.list.unknownCustomer}
          </span>
          <span className="shrink-0 text-xs tabular-nums text-ink-500">{formatDateTime(thread.last_message_at)}</span>
        </span>
        <span className="mt-0.5 flex items-center justify-between gap-2">
          <span className="truncate text-sm text-ink-600">{thread.last_message ? messageText(thread.last_message) : ''}</span>
          {awaiting && thread.unread_count > 0 && <Badge tone="amber">{messagesPage.list.unread(thread.unread_count)}</Badge>}
        </span>
      </Link>
    </li>
  );
}
