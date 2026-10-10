/** A run of summary text, or a citation marker such as `[R3]` inside it. */
export type SummaryPart = { kind: 'text'; text: string } | { kind: 'ref'; ref: string };

/** Splits the summary into its words and its `[R3]` markers, so the markers can be drawn as links. */
export function splitCitations(text: string): SummaryPart[] {
  const parts: SummaryPart[] = [];
  let last = 0;

  for (const match of text.matchAll(/\[(R\d+)\]/g)) {
    if (match.index > last) parts.push({ kind: 'text', text: text.slice(last, match.index) });
    parts.push({ kind: 'ref', ref: match[1]! });
    last = match.index + match[0].length;
  }

  if (last < text.length) parts.push({ kind: 'text', text: text.slice(last) });

  return parts;
}
