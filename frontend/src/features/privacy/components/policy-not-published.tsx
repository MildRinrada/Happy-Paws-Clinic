import { notPublished } from '../content/privacy-policy';
import { PolicyLinks } from './policy-links';

/**
 * What the page says while the clinic has not published a notice: plainly that, and how to reach
 * the clinic. Never the shipped template, which is the system developer's words and not the clinic's.
 */
export function PolicyNotPublished() {
  return (
    <aside role="note" className="space-y-3 rounded-2xl border border-site-border/60 bg-site-soft p-5">
      <p className="font-semibold text-ink-900">{notPublished.title}</p>
      <p className="text-[15px] leading-relaxed text-ink-700">{notPublished.body}</p>
      <PolicyLinks items={[notPublished.account]} />
    </aside>
  );
}
