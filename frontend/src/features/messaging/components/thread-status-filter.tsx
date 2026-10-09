import Link from 'next/link';
import { messagesPage } from '../content/messages-page';
import { messagesHref, type MessagesQuery } from '../lib/messages-query';

const linkClass = 'rounded-full px-3 py-1 text-sm font-medium';
const active = 'bg-brand-100 text-brand-800';
const idle = 'text-ink-600 hover:bg-ink-100';

/** All threads, or only those waiting for an answer. Keeps the open thread open. */
export function ThreadStatusFilter({ query }: { query: MessagesQuery }) {
  const awaiting = query.status === 'awaiting_reply';

  return (
    <nav aria-label={messagesPage.filters.label} className="flex gap-1">
      <Link
        href={messagesHref(query, { status: undefined, cursor: undefined })}
        aria-current={awaiting ? undefined : 'page'}
        className={`${linkClass} ${awaiting ? idle : active}`}
      >
        {messagesPage.filters.all}
      </Link>
      <Link
        href={messagesHref(query, { status: 'awaiting_reply', cursor: undefined })}
        aria-current={awaiting ? 'page' : undefined}
        className={`${linkClass} ${awaiting ? active : idle}`}
      >
        {messagesPage.filters.awaiting}
      </Link>
    </nav>
  );
}
