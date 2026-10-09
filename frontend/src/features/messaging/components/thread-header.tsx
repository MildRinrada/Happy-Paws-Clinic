import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { channelLabels, labelOf } from '@/features/notifications/content/delivery-labels';
import { threadStatusLabels, threadStatusTones } from '../content/message-labels';
import { messagesPage } from '../content/messages-page';
import type { MessageThread } from '../types/thread.types';

/** Whose conversation it is, whether it waits for an answer, and where a reply goes first. */
export function ThreadHeader({ thread }: { thread: MessageThread }) {
  return (
    <header className="space-y-1 border-b border-ink-100 px-5 py-3.5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="font-display text-base font-semibold text-ink-900">
          {thread.customer_name ?? messagesPage.list.unknownCustomer}
        </h2>
        <span className="flex items-center gap-3">
          <Badge tone={threadStatusTones[thread.status]}>{threadStatusLabels[thread.status]}</Badge>
          <Link href={customerPaths.detail(thread.customer_id)} className="text-sm font-medium text-brand-700 hover:underline">
            {messagesPage.thread.openCustomer}
          </Link>
        </span>
      </div>
      {thread.reply_channel && (
        <p className="text-xs text-ink-500">{messagesPage.thread.replyChannel(labelOf(channelLabels, thread.reply_channel))}</p>
      )}
    </header>
  );
}
