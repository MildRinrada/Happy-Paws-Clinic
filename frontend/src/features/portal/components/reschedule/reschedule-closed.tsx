import Link from 'next/link';
import { portalReschedule } from '../../content/portal-reschedule';
import { portalPaths } from '../../content/portal-navigation';

/** An appointment the owner can no longer move online: cancelled, checked in, or already past. */
export function RescheduleClosed() {
  const text = portalReschedule;

  return (
    <div className="space-y-3">
      <h2 className="text-base font-semibold text-ink-900">{text.closed.title}</h2>
      <p className="text-sm text-ink-700">{text.closed.body}</p>
      <div className="flex flex-wrap gap-4 text-sm font-semibold">
        <Link href={text.refused.contact.href} className="text-site-teal hover:underline">
          {text.refused.contact.label}
        </Link>
        <Link href={portalPaths.appointments} className="text-site-teal hover:underline">
          {text.refused.back}
        </Link>
      </div>
    </div>
  );
}
