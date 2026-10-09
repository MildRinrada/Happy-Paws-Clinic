import type { Metadata } from 'next';
import { getMyThread } from '@/features/portal/api/getMyThread';
import { ChatCompose } from '@/features/portal/components/chat/chat-compose';
import { ChatMessageList } from '@/features/portal/components/chat/chat-message-list';
import { ChatPager } from '@/features/portal/components/chat/chat-pager';
import { MarkRepliesRead } from '@/features/portal/components/chat/mark-replies-read';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { LiveRefresh } from '@/features/portal/components/visit-card/live-refresh';
import { portalChat } from '@/features/portal/content/portal-chat';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: `${portalChat.title} · Happy Paws Clinic` };

/** How often the latest messages are re-read while the page is on screen, like the visit card. */
const REFRESH_SECONDS = 15;

interface MessagesPageProps {
  searchParams: Promise<{ cursor?: string }>;
}

/**
 * The owner's conversation with the clinic (contract extension § 24): the same thread the clinic
 * answers from "ข้อความลูกค้า". The latest page refreshes itself while visible and clears the
 * bell for answers on screen; an older page is a still view with the way back.
 */
export default async function PortalMessagesPage({ searchParams }: MessagesPageProps) {
  const { cursor } = await searchParams;
  const thread = await authed(() => getMyThread(cursor));
  const latest = !cursor;
  const latestReply = thread.data.find((message) => message.direction === 'outbound');

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <PageHeading title={portalChat.title} intro={portalChat.intro} />
      <p className="rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-900">{portalChat.urgent}</p>

      <section className="space-y-4 rounded-2xl bg-white p-5 shadow-sm">
        <ChatPager olderCursor={thread.page.next_cursor} paged={!latest} />
        <ChatMessageList messages={thread.data} />
        {latest && thread.status === 'awaiting_reply' && <p className="text-xs text-site-muted">{portalChat.awaiting}</p>}
      </section>

      {latest && (
        <section className="rounded-2xl bg-white p-5 shadow-sm">
          <ChatCompose />
        </section>
      )}

      {latest && <LiveRefresh seconds={REFRESH_SECONDS} />}
      {latest && <MarkRepliesRead latestReplyId={latestReply?.id} />}
    </div>
  );
}
