import Link from 'next/link';
import { servicesPage } from '../../content/services-page';
import type { ServiceMenu as ServiceMenuData } from '../../types/service-menu.types';
import { CategoryNav } from './category-nav';
import { CategorySection } from './category-section';

/**
 * Every bookable service, one section per category, with a row of links to each.
 *
 * `null` means the menu could not be read (the API is down), which is said differently from a
 * clinic that has not opened any service for online booking yet. Both point to the contact page.
 */
export function ServiceMenu({ menu }: { menu: ServiceMenuData | null }) {
  if (menu === null || menu.categories.length === 0) {
    return (
      <p className="text-[15px] text-site-muted">
        {menu === null ? servicesPage.unavailable : servicesPage.empty}{' '}
        <Link href={servicesPage.contactLink.href} className="font-semibold text-site-teal underline">
          {servicesPage.contactLink.label}
        </Link>
      </p>
    );
  }

  return (
    <>
      <CategoryNav categories={menu.categories} />

      <div className="mt-10 space-y-12">
        {menu.categories.map((category) => (
          <CategorySection key={category.code} category={category} branchNames={menu.branchNames} />
        ))}
      </div>
    </>
  );
}
