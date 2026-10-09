import { portalChat } from '../../content/portal-chat';
import type { ChatMessage } from '../../types/chat.types';
import { EmptyLine } from '../shared/empty-line';
import { ChatBubble } from './chat-bubble';

/** The conversation, oldest at the top. The API pages newest first, so the page is turned over here. */
export function ChatMessageList({ messages }: { messages: ChatMessage[] }) {
  if (messages.length === 0) {
    return <EmptyLine>{portalChat.empty}</EmptyLine>;
  }

  return (
    <ol aria-label={portalChat.listLabel} className="space-y-3">
      {[...messages].reverse().map((message) => (
        <ChatBubble key={message.id} message={message} />
      ))}
    </ol>
  );
}
