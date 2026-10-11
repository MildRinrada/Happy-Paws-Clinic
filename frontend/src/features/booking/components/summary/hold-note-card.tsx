/** The white note under the summary on how long the time is kept once the visitor moves on. */
export function HoldNoteCard({ children }: { children: string }) {
  return (
    <p role="note" className="rounded-[18px] bg-white px-5 py-5 text-sm leading-relaxed text-ink-700">
      {children}
    </p>
  );
}
