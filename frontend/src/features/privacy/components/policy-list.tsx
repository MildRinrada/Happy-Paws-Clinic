/** A bulleted list of short points. */
export function PolicyList({ items }: { items: string[] }) {
  return (
    <ul className="list-disc space-y-2 pl-5 text-[15px] leading-relaxed text-ink-700 marker:text-site-teal">
      {items.map((item) => (
        <li key={item}>{item}</li>
      ))}
    </ul>
  );
}
