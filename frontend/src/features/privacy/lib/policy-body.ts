import type { PolicyBlock, PolicyInline } from '../types/privacy-policy.types';

/**
 * Reads a privacy notice section's body into paragraphs and lists (docs/contract-extensions.md
 * § 35). The same three rules as the backend's `PrivacyPolicyText.Parse`, which checks the text
 * when it is saved; the two must agree, so change them together.
 *
 * 1. A blank line starts a new paragraph. Lines next to each other are one paragraph.
 * 2. A line starting with `- ` is a list item. Items next to each other are one list.
 * 3. `[label](/path)` links to a page of the clinic's own site. Anything else in brackets is text.
 *
 * Nothing here produces markup: the components draw text nodes and `next/link`, so a `<script>`
 * typed into a draft is shown as those characters in the preview.
 */

const link = /\[([^[\]\n]{1,120})\]\(([^()\s]{1,300})\)/g;
const sitePath = /^\/[A-Za-z0-9\-._~/?#=&%+]*$/;

/** True for a path on the clinic's own site: one leading `/`, no scheme, no host, no spaces. */
export function isSitePath(path: string): boolean {
  return path.length <= 300 && sitePath.test(path) && !path.startsWith('//');
}

function inlinesOf(text: string): PolicyInline[] {
  const inlines: PolicyInline[] = [];
  let at = 0;

  for (const match of text.matchAll(link)) {
    const [whole, label = '', path = ''] = match;
    const index = match.index ?? 0;

    if (!isSitePath(path)) {
      continue;
    }

    if (index > at) {
      inlines.push({ text: text.slice(at, index) });
    }

    inlines.push({ text: label.trim(), href: path });
    at = index + whole.length;
  }

  if (at < text.length) {
    inlines.push({ text: text.slice(at) });
  }

  return inlines;
}

function listItem(line: string): string | null {
  if (line === '-') return '';
  return line.startsWith('- ') ? line.slice(2).trim() : null;
}

export function parsePolicyBody(body: string): PolicyBlock[] {
  const blocks: PolicyBlock[] = [];
  let paragraph: string[] = [];
  let list: PolicyInline[][] = [];

  const flushParagraph = () => {
    if (paragraph.length > 0) {
      blocks.push({ kind: 'paragraph', inlines: inlinesOf(paragraph.join(' ')) });
      paragraph = [];
    }
  };

  const flushList = () => {
    if (list.length > 0) {
      blocks.push({ kind: 'list', items: list });
      list = [];
    }
  };

  for (const raw of body.replace(/\r\n?/g, '\n').split('\n')) {
    const line = raw.trim();

    if (line.length === 0) {
      flushParagraph();
      flushList();
      continue;
    }

    const item = listItem(line);

    if (item !== null) {
      flushParagraph();
      if (item.length > 0) list.push(inlinesOf(item));
      continue;
    }

    flushList();
    paragraph.push(line);
  }

  flushParagraph();
  flushList();

  return blocks;
}

/** A paragraph that is nothing but one link is drawn as a button: "จัดการข้อมูลของฉัน". */
export function loneLink(block: PolicyBlock): { text: string; href: string } | null {
  if (block.kind !== 'paragraph' || block.inlines.length !== 1) return null;
  const [only] = block.inlines;
  return only?.href ? { text: only.text, href: only.href } : null;
}
