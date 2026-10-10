import Link from 'next/link';
import { invitationPage } from '../../content/invitation-page';
import { reviewPaths } from '../../lib/review-paths';

/** A link that can no longer be used: already used, past its 30 days, or not one of ours. */
export function InvitationClosed({ title, body }: { title: string; body: string }) {
  return (
    <div className="space-y-3 rounded-2xl bg-site-soft px-5 py-6 text-sm text-ink-800">
      <h2 className="text-lg font-semibold text-ink-900">{title}</h2>
      <p>{body}</p>
      <div className="flex flex-wrap gap-x-5 gap-y-2 font-medium">
        <Link href={reviewPaths.list} className="text-site-teal hover:underline">
          {invitationPage.seeReviews}
        </Link>
        <Link href="/" className="text-site-teal hover:underline">
          {invitationPage.home}
        </Link>
      </div>
    </div>
  );
}
