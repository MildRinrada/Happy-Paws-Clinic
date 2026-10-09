import Link from 'next/link';
import { portalChat } from '../../content/portal-chat';
import { portalPaths } from '../../content/portal-navigation';

interface ChatPagerProps {
  /** The cursor of the next, older page, when there is one. */
  olderCursor?: string | null;
  /** True when this is an older page, so the way back to the newest messages shows. */
  paged: boolean;
}

/** "ดูข้อความก่อนหน้า" above the conversation, and the way back to the latest messages. */
export function ChatPager({ olderCursor, paged }: ChatPagerProps) {
  if (!olderCursor && !paged) return null;

  const link = 'text-sm font-medium text-site-teal hover:underline';

  return (
    <div className="flex flex-wrap justify-between gap-3">
      {olderCursor ? (
        <Link href={`${portalPaths.messages}?cursor=${encodeURIComponent(olderCursor)}`} className={link}>
          {portalChat.older}
        </Link>
      ) : (
        <span />
      )}
      {paged && (
        <Link href={portalPaths.messages} className={link}>
          {portalChat.newest}
        </Link>
      )}
    </div>
  );
}
