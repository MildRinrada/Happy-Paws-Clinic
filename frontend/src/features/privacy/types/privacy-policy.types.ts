/**
 * The clinic's privacy notice as the API keeps it (docs/contract-extensions.md § 35), and the
 * pieces a section's body is read into for drawing.
 */

/** A table-of-contents entry, or a button to a page of the site. */
export interface PolicyLink {
  label: string;
  href: string;
}

/** A term with an explanation under it. Used for the clinic's own details in the first section. */
export interface PolicyDefinition {
  term: string;
  detail: string;
}

/** One numbered part of the notice, as stored. */
export interface PolicySection {
  /** The anchor in the address bar (`/privacy#cookies`) and the table of contents' target. */
  anchor: string;
  heading: string;
  /** Plain text: a blank line, `- ` list items and `[label](/path)` links. See `lib/policy-body.ts`. */
  body: string;
}

/** A run of text, or a link to a page of the clinic's own site. */
export type PolicyInline = { text: string; href?: undefined } | { text: string; href: string };

/** A paragraph, or a list whose items are runs of inlines. */
export type PolicyBlock = { kind: 'paragraph'; inlines: PolicyInline[] } | { kind: 'list'; items: PolicyInline[][] };

/** A published version in the list of older ones. */
export interface PolicyHistoryEntry {
  label: string;
  effective_date: string;
  published_at: string;
}

/** `GET /privacy-policy` and `GET /privacy-policy/versions/{label}`. */
export interface PublicPrivacyPolicy {
  /** False when the clinic has not published a notice yet; nothing else but `history` is sent then. */
  published: boolean;
  label?: string;
  effective_date?: string;
  published_at?: string;
  legal_review_done?: boolean;
  is_current?: boolean;
  sections?: PolicySection[];
  history: PolicyHistoryEntry[];
}
