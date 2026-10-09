import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { messagesPage } from '../content/messages-page';
import { messagesHref, type MessagesQuery } from '../lib/messages-query';
import type { MessageThread } from '../types/thread.types';
import { ThreadListItem } from './thread-list-item';

interface ThreadListProps {
  threads: MessageThread[];
  query: MessagesQuery;
  nextCursor?: string;
}

/** The inbox column: threads awaiting a reply first, as the backend orders them. */
export function ThreadList({ threads, query, nextCursor }: ThreadListProps) {
  if (threads.length === 0) {
    return (
      <p className="px-4 py-8 text-center text-sm text-ink-500">
        {query.status === 'awaiting_reply' ? messagesPage.list.emptyAwaiting : messagesPage.list.empty}
      </p>
    );
  }

  return (
    <div>
      <ul aria-label={messagesPage.list.label} className="divide-y divide-ink-100">
        {threads.map((thread) => (
          <ThreadListItem key={thread.id} thread={thread} query={query} selected={thread.id === query.thread} />
        ))}
      </ul>
      {nextCursor && (
        <div className="border-t border-ink-100 p-3">
          <Link href={messagesHref(query, { cursor: nextCursor })} className={buttonClass('secondary', 'sm', 'w-full')}>
            {messagesPage.list.next}
          </Link>
        </div>
      )}
    </div>
  );
}
