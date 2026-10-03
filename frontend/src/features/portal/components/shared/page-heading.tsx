/** A portal page's title and the line under it. */
export function PageHeading({ title, intro }: { title: string; intro?: string }) {
  return (
    <div>
      <h1 className="text-2xl font-semibold text-ink-900">{title}</h1>
      {intro && <p className="mt-1 text-sm text-site-muted">{intro}</p>}
    </div>
  );
}
