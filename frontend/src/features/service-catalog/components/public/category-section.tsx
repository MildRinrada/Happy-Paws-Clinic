import { categoryBlurbs } from '../../content/services-page';
import { categoryAnchor } from '../../lib/category-anchor';
import type { MenuCategory } from '../../types/service-menu.types';
import { ServiceCard } from './service-card';

interface Props {
  category: MenuCategory;
  branchNames: Record<string, string>;
}

/** One category: its name, one line on what it covers, and a card per service. */
export function CategorySection({ category, branchNames }: Props) {
  const id = categoryAnchor(category.code);
  const blurb = categoryBlurbs[category.code];

  return (
    <section id={id} aria-labelledby={`${id}-heading`} className="scroll-mt-6">
      <h2 id={`${id}-heading`} className="text-xl font-semibold sm:text-[22px]">
        {category.name}
      </h2>
      {blurb && <p className="mt-1.5 text-[15px] text-site-muted">{blurb}</p>}

      <ul className="mt-5 grid gap-5 md:grid-cols-2 lg:grid-cols-3">
        {category.services.map((service) => (
          <ServiceCard key={service.id} service={service} branchNames={branchNames} />
        ))}
      </ul>
    </section>
  );
}
