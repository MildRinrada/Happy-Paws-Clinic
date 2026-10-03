/** The one quiet line a section shows when it has nothing in it. */
export function EmptyLine({ children }: { children: string }) {
  return <p className="text-sm text-site-muted">{children}</p>;
}
