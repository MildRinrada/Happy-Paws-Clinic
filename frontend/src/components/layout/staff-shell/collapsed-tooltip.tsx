/**
 * The name of a menu entry beside its icon while the sidebar is folded. Fixed to the viewport, so
 * the sidebar's own scrolling cannot clip it. Hidden from screen readers, which already read the
 * link's full name.
 */
export function CollapsedTooltip({ label, top, left }: { label: string; top: number; left: number }) {
  return (
    <span
      aria-hidden
      style={{ top, left }}
      className="pointer-events-none fixed z-50 -translate-y-1/2 whitespace-nowrap rounded-md bg-ink-900 px-2.5 py-1 text-xs font-medium text-white shadow-lg"
    >
      {label}
    </span>
  );
}
