/** One paragraph of the policy. */
export function PolicyParagraph({ text }: { text: string }) {
  return <p className="text-[15px] leading-relaxed text-ink-700">{text}</p>;
}
