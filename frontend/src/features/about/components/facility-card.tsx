/** One room or piece of equipment, and what it means for the visit. */
export function FacilityCard({ title, detail }: { title: string; detail: string }) {
  return (
    <li className="rounded-2xl border border-site-border/60 p-5">
      <h3 className="flex items-center gap-2 font-bold text-ink-900">
        <span aria-hidden="true" className="size-2 rounded-full bg-site-teal" />
        {title}
      </h3>
      <p className="mt-1.5 text-[15px] text-site-muted">{detail}</p>
    </li>
  );
}
