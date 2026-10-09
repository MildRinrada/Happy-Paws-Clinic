import { labelOf } from '@/features/notifications/content/delivery-labels';
import { formatDateTime } from '@/lib/format/datetime';
import { inboundChannelLabels } from '../content/message-labels';
import { messagesPage } from '../content/messages-page';
import { messageText } from '../lib/message-text';
import type { ThreadMessage } from '../types/thread.types';
import { MessageDelivery } from './message-delivery';

/** One message: the customer's on the left, the clinic's replies on the right with their result. */
export function MessageBubble({ message }: { message: ThreadMessage }) {
  const outbound = message.direction === 'outbound';
  const meta = outbound
    ? message.sent_by_name && messagesPage.thread.from(message.sent_by_name)
    : message.channel && labelOf(inboundChannelLabels, message.channel);

  return (
    <li className={`flex flex-col ${outbound ? 'items-end' : 'items-start'}`}>
      <p
        className={`max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-3.5 py-2 text-sm ${
          outbound ? 'rounded-br-sm bg-brand-600 text-white' : 'rounded-bl-sm bg-ink-100 text-ink-900'
        } ${message.kind === 'text' ? '' : 'italic'}`}
      >
        {messageText(message)}
      </p>
      <span className="mt-1 text-xs text-ink-500">
        <time dateTime={message.sent_at} className="tabular-nums">
          {formatDateTime(message.sent_at)}
        </time>
        {meta && ` ${meta}`}
      </span>
      {outbound && message.delivery && <MessageDelivery delivery={message.delivery} />}
    </li>
  );
}
