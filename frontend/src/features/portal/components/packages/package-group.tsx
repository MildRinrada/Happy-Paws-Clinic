import type { PackageView } from '../../lib/load-packages';
import { PackageCard } from './package-card';

/** A heading and its packages, two to a row on a wide screen. Nothing at all when the group is empty. */
export function PackageGroup({ title, items }: { title: string; items: PackageView[] }) {
  if (items.length === 0) return null;

  return (
    <section className="space-y-3">
      <h2 className="text-base font-semibold text-ink-900">{title}</h2>
      <div className="grid gap-4 md:grid-cols-2">
        {items.map((item) => (
          <PackageCard key={item.id} item={item} />
        ))}
      </div>
    </section>
  );
}
