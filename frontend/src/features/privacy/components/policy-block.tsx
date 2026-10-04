import type { PolicyBlock as Block } from '../types/privacy-policy.types';
import { PolicyDefinitions } from './policy-definitions';
import { PolicyLinks } from './policy-links';
import { PolicyList } from './policy-list';
import { PolicyParagraph } from './policy-paragraph';

/** Draws one block of a section's text by its kind. */
export function PolicyBlock({ block }: { block: Block }) {
  switch (block.kind) {
    case 'paragraph':
      return <PolicyParagraph text={block.text} />;
    case 'list':
      return <PolicyList items={block.items} />;
    case 'definitions':
      return <PolicyDefinitions items={block.items} />;
    case 'links':
      return <PolicyLinks items={block.items} />;
  }
}
