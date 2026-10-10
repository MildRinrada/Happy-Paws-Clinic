/** A heading and a short bulleted list, for spelling out what a confirmation will do. */
export function EffectList({ title, items }: { title: string; items: readonly string[] }) {
  return (
    <div>
      <p className="text-sm font-medium text-ink-800">{title}</p>
      <ul className="mt-1 list-disc space-y-1 pl-5 text-sm text-ink-700">
        {items.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </div>
  );
}
