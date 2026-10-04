/**
 * The page-shaped grey blocks shown the moment a link is clicked, until the next page's data
 * arrives. Without them a click that waits on the server looks like a click that missed.
 */
export function PageLoading({ label }: { label: string }) {
  return (
    <div role="status" aria-live="polite" className="animate-pulse space-y-5">
      <span className="sr-only">{label}</span>
      <div className="space-y-2">
        <div className="h-7 w-56 rounded-lg bg-ink-200/70" />
        <div className="h-4 w-80 max-w-full rounded bg-ink-100" />
      </div>
      <div className="h-44 rounded-xl border border-ink-100 bg-white" />
      <div className="h-28 rounded-xl border border-ink-100 bg-white" />
    </div>
  );
}
