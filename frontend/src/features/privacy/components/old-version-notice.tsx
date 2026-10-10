import Link from 'next/link';
import { oldVersionNotice } from '../content/privacy-policy';

/** The banner on an older version's page, with the way to the current one. */
export function OldVersionNotice({ label }: { label: string }) {
  return (
    <aside role="note" className="rounded-2xl border border-sky-200 bg-sky-50 p-5 text-sky-900">
      <p className="font-semibold">{oldVersionNotice.title(label)}</p>
      <p className="mt-1 text-[15px] leading-relaxed">{oldVersionNotice.body}</p>
      <Link href="/privacy" className="mt-2 inline-block text-[15px] font-semibold text-site-teal hover:underline">
        {oldVersionNotice.link}
      </Link>
    </aside>
  );
}
