/**
 * A colour for each branch, so the one in view is recognisable at a glance. Taken from the branch
 * id, so it stays the same on every page and every device without the clinic setting anything.
 * Full class names, so Tailwind sees them.
 */
const DOTS = [
  'bg-brand-500',
  'bg-amber-500',
  'bg-sky-500',
  'bg-violet-500',
  'bg-rose-500',
  'bg-emerald-600',
  'bg-orange-500',
  'bg-indigo-500',
];

export function branchDotClass(branchId: string): string {
  let hash = 0;

  for (const char of branchId) {
    hash = (hash * 31 + char.charCodeAt(0)) >>> 0;
  }

  return DOTS[hash % DOTS.length]!;
}
