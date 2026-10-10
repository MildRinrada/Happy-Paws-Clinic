import { referredNote } from '../../content/referred-note';

/**
 * One line on `/book` for a visitor who came through a friend's link (docs/contract-extensions.md
 * § 39). Says only that someone invited them, never who.
 */
export function ReferredNote() {
  return (
    <p role="note" className="rounded-xl bg-site-soft px-4 py-3 text-sm text-ink-800">
      {referredNote.text}
    </p>
  );
}
