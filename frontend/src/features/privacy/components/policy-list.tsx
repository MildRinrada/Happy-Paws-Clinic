import type { PolicyInline } from '../types/privacy-policy.types';
import { PolicyInlineText } from './policy-inline-text';

/** A bulleted list of short points. */
export function PolicyList({ items }: { items: PolicyInline[][] }) {
  return (
    <ul className="list-disc space-y-2 break-words pl-5 text-[15px] leading-relaxed text-ink-700 marker:text-site-teal">
      {items.map((item, index) => (
        <li key={index}>
          <PolicyInlineText inlines={item} />
        </li>
      ))}
    </ul>
  );
}
