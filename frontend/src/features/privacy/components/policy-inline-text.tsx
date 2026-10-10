import Link from 'next/link';
import type { PolicyInline } from '../types/privacy-policy.types';

/**
 * A run of a paragraph or a list item: text nodes, and links to pages of the clinic's own site.
 * The paths were checked when the text was saved and again when it was read (`lib/policy-body.ts`).
 */
export function PolicyInlineText({ inlines }: { inlines: PolicyInline[] }) {
  return (
    <>
      {inlines.map((inline, index) =>
        inline.href ? (
          <Link key={index} href={inline.href} className="font-semibold text-site-teal hover:underline">
            {inline.text}
          </Link>
        ) : (
          <span key={index}>{inline.text}</span>
        ),
      )}
    </>
  );
}
