/**
 * The building blocks of the privacy policy's text. The content file is a list of sections, each
 * a list of these, so a section can be reworded or reordered without touching a component.
 */

export interface PolicyLink {
  label: string;
  href: string;
}

export interface PolicyDefinition {
  term: string;
  detail: string;
}

export type PolicyBlock =
  | { kind: 'paragraph'; text: string }
  | { kind: 'list'; items: string[] }
  | { kind: 'definitions'; items: PolicyDefinition[] }
  | { kind: 'links'; items: PolicyLink[] };

export interface PolicySection {
  /** The anchor in the address bar (`/privacy#cookies`) and the table of contents' target. */
  id: string;
  heading: string;
  blocks: PolicyBlock[];
}
