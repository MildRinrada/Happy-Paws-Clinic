import { messagesPage } from '../content/messages-page';
import type { ThreadMessage } from '../types/thread.types';
import { MessageBubble } from './message-bubble';

interface MessageListProps {
  messages: ThreadMessage[];
  hasMore: boolean;
}

/** The conversation, oldest at the top. Only the newest messages are loaded. */
export function MessageList({ messages, hasMore }: MessageListProps) {
  return (
    <div className="space-y-3">
      {hasMore && <p className="text-center text-xs text-ink-500">{messagesPage.thread.olderHidden(messages.length)}</p>}
      <ol className="space-y-3">
        {messages.map((message) => (
          <MessageBubble key={message.id} message={message} />
        ))}
      </ol>
    </div>
  );
}
