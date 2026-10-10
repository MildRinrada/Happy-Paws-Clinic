import { loneLink } from '../lib/policy-body';
import type { PolicyBlock as Block } from '../types/privacy-policy.types';
import { PolicyInlineText } from './policy-inline-text';
import { PolicyLinks } from './policy-links';
import { PolicyList } from './policy-list';
import { PolicyParagraph } from './policy-paragraph';

/** Draws one block of a section's text: a paragraph, a list, or a paragraph that is one link, as a button. */
export function PolicyBlock({ block }: { block: Block }) {
  if (block.kind === 'list') {
    return <PolicyList items={block.items} />;
  }

  const button = loneLink(block);

  if (button) {
    return <PolicyLinks items={[{ label: button.text, href: button.href }]} />;
  }

  return (
    <PolicyParagraph>
      <PolicyInlineText inlines={block.inlines} />
    </PolicyParagraph>
  );
}
