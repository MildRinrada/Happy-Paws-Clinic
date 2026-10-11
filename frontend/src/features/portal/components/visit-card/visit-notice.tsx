/** The yellow note: a short bold lead (a time, a queue number, a state) and a line of advice. */
export function VisitNotice({ lead, text }: { lead?: string; text: string }) {
  return (
    <p className="rounded-xl bg-site-sun px-3 py-2.5 text-sm leading-5 text-ink-900">
      {lead && <strong className="font-bold">{lead} </strong>}
      {text}
    </p>
  );
}
