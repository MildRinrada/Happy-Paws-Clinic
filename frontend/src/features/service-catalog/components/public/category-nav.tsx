import { servicesPage } from '../../content/services-page';
import { categoryAnchor } from '../../lib/category-anchor';
import type { MenuCategory } from '../../types/service-menu.types';

/** A row of links, one per category, that jump down the page. Same chips as the team page. */
export function CategoryNav({ categories }: { categories: MenuCategory[] }) {
  return (
    <nav aria-label={servicesPage.navLabel}>
      <ul className="flex flex-wrap gap-2">
        {categories.map((category) => (
          <li key={category.code}>
            <a
              href={`#${categoryAnchor(category.code)}`}
              className="flex h-9 items-center gap-1.5 rounded-full border border-site-border bg-white px-4 text-sm hover:border-site-teal hover:text-site-teal"
            >
              {category.name}
              <span className="text-site-muted">{category.services.length}</span>
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}
