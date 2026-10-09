import { formatDateTime } from '@/lib/format/datetime';
import { chatMessageMeta, chatMessageText } from '../../lib/chat-message-text';
import type { ChatMessage } from '../../types/chat.types';

/** One message: the owner's own on the right, the clinic's answers on the left. */
export function ChatBubble({ message }: { message: ChatMessage }) {
  const mine = message.direction === 'inbound';

  return (
    <li className={`flex flex-col ${mine ? 'items-end' : 'items-start'}`}>
      <p
        className={`max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-3.5 py-2 text-sm ${
          mine ? 'rounded-br-sm bg-site-teal text-white' : 'rounded-bl-sm bg-ink-100 text-ink-900'
        } ${message.kind === 'text' ? '' : 'italic'}`}
      >
        {chatMessageText(message)}
      </p>
      <span className="mt-1 text-xs text-site-muted">
        {chatMessageMeta(message)}{' '}
        <time dateTime={message.sent_at} className="tabular-nums">
          {formatDateTime(message.sent_at)}
        </time>
      </span>
    </li>
  );
}
