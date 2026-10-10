import type { PolicySection } from '../types/privacy-policy.types';

/** A section in the editor: the stored fields plus a key that survives reordering. */
export interface SectionDraft extends PolicySection {
  key: string;
}

let next = 0;

function key(): string {
  next += 1;
  return `section-${next}`;
}

export function toDrafts(sections: PolicySection[]): SectionDraft[] {
  return sections.map((section) => ({ ...section, key: key() }));
}

export function toSections(drafts: SectionDraft[]): PolicySection[] {
  return drafts.map(({ anchor, heading, body }) => ({ anchor, heading, body }));
}

/** A blank section with an anchor no other section has yet, so the first save is not refused for it. */
export function blankSection(existing: SectionDraft[], heading: string): SectionDraft {
  const taken = new Set(existing.map((section) => section.anchor));
  let index = existing.length + 1;

  while (taken.has(`section-${index}`)) index += 1;

  return { key: key(), anchor: `section-${index}`, heading, body: '' };
}

/** Moves the section at `from` by one place up (-1) or down (+1). */
export function moved(drafts: SectionDraft[], from: number, by: -1 | 1): SectionDraft[] {
  const to = from + by;
  if (to < 0 || to >= drafts.length) return drafts;

  const copy = [...drafts];
  const [taken] = copy.splice(from, 1);
  if (taken) copy.splice(to, 0, taken);

  return copy;
}

/** True when the editor's sections differ from what was last saved. */
export function changed(drafts: SectionDraft[], saved: PolicySection[]): boolean {
  return JSON.stringify(toSections(drafts)) !== JSON.stringify(saved);
}
