import { servicesNote } from '../../content/services-page';

/** What the prices on the page mean, under the menu. */
export function ServicesNote() {
  return (
    <aside aria-labelledby="services-note-heading" className="rounded-2xl bg-site-soft px-6 py-5">
      <h2 id="services-note-heading" className="text-base font-semibold">
        {servicesNote.heading}
      </h2>
      <ul className="mt-2 list-disc space-y-1 pl-5 text-[15px] text-site-muted">
        {servicesNote.lines.map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ul>
    </aside>
  );
}
