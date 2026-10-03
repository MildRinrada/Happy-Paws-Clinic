/** The yellow note: a short bold lead (a time, a queue number, a state) and a line of advice. */
export function VisitNotice({ lead, text }: { lead?: string; text: string }) {
  return (
    <div className="flex items-center gap-4 rounded-xl bg-site-sun px-4 py-3 text-ink-900">
      {lead && <p className="shrink-0 font-display text-xl font-bold">{lead}</p>}
      <p className="text-sm leading-snug">{text}</p>
    </div>
  );
}
