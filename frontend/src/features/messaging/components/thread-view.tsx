import { Card } from '@/components/ui/card';
import { messagesPage } from '../content/messages-page';
import type { MessageThread } from '../types/thread.types';
import { MessageList } from './message-list';
import { ReplyBox } from './reply-box';
import { ThreadHeader } from './thread-header';

interface ThreadViewProps {
  thread: MessageThread;
  canReply: boolean;
}

/** One conversation and, for someone who may answer, the reply box under it. */
export function ThreadView({ thread, canReply }: ThreadViewProps) {
  return (
    <Card>
      <ThreadHeader thread={thread} />
      <div className="max-h-[60vh] overflow-y-auto px-5 py-4">
        <MessageList messages={thread.messages} hasMore={thread.has_more_messages} />
      </div>
      {canReply ? (
        <ReplyBox key={thread.id} threadId={thread.id} />
      ) : (
        <p className="border-t border-ink-100 px-5 py-3 text-sm text-ink-600">{messagesPage.reply.noWrite}</p>
      )}
    </Card>
  );
}
