import { ChipNav } from '@/components/site/chip-nav';
import { DotChip } from '@/components/site/dot-chip';
import { categoryDots, servicesPage } from '../../content/services-page';
import { categoryAnchor } from '../../lib/category-anchor';
import type { MenuCategory } from '../../types/service-menu.types';

/** The bar under the banner: one chip per category, each with its colour, that jumps down the page. */
export function CategoryNav({ categories }: { categories: MenuCategory[] }) {
  return (
    <ChipNav label={servicesPage.navLabel}>
      {categories.map((category) => (
        <DotChip
          key={category.code}
          href={`#${categoryAnchor(category.code)}`}
          label={category.name}
          dot={categoryDots[category.code] ?? 'brand'}
        />
      ))}
    </ChipNav>
  );
}
