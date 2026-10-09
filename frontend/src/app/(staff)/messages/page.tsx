import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getThread } from '@/features/messaging/api/getThread';
import { listThreads } from '@/features/messaging/api/listThreads';
import { ThreadList } from '@/features/messaging/components/thread-list';
import { ThreadStatusFilter } from '@/features/messaging/components/thread-status-filter';
import { ThreadView } from '@/features/messaging/components/thread-view';
import { messagesPage } from '@/features/messaging/content/messages-page';
import { readMessagesQuery } from '@/features/messaging/lib/messages-query';
import { authed } from '@/lib/auth/session';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: messagesPage.metaTitle };

/** How many threads the inbox column shows per page. Previews only: no messages are loaded for them. */
const THREADS_PER_PAGE = 30;

interface MessagesPageProps {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/** The customers' conversations: the inbox on the left, the open thread and its reply box on the right. */
export default async function MessagesPage({ searchParams }: MessagesPageProps) {
  const context = await loadStaffContext();

  if (!context?.can(Scope.MessageRead)) {
    return <Alert tone="info">{messagesPage.noAccess}</Alert>;
  }

  const query = readMessagesQuery(await searchParams);
  const [page, thread] = await Promise.all([
    authed(() => listThreads({ status: query.status, cursor: query.cursor, messages: 0, limit: THREADS_PER_PAGE })),
    query.thread ? authed(() => getThread(query.thread!)) : Promise.resolve(null),
  ]);

  return (
    <div className="space-y-5">
      <PageHeader title={messagesPage.title} subtitle={messagesPage.subtitle} />

      <div className="grid items-start gap-5 lg:grid-cols-[320px_minmax(0,1fr)]">
        <Card>
          <div className="border-b border-ink-100 px-4 py-3">
            <ThreadStatusFilter query={query} />
          </div>
          <ThreadList threads={page.data} query={query} nextCursor={page.page.next_cursor} />
        </Card>

        <div className="space-y-3">
          {thread ? (
            <ThreadView thread={thread} canReply={context.can(Scope.MessageWrite)} />
          ) : query.thread ? (
            <Alert tone="info">{messagesPage.thread.notFound}</Alert>
          ) : (
            <EmptyState title={messagesPage.thread.pick}>{messagesPage.howItStarts}</EmptyState>
          )}
        </div>
      </div>
    </div>
  );
}
