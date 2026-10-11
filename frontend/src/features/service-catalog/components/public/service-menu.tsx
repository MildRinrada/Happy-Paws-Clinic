import { SiteContainer } from '@/components/layout/site-container';
import { servicesPage } from '../../content/services-page';
import type { AskChannels } from '../../types/ask-channels.types';
import type { ServiceMenu as ServiceMenuData } from '../../types/service-menu.types';
import { CategoryNav } from './category-nav';
import { CategorySection } from './category-section';
import { MenuMessage } from './menu-message';
import { ServicesNote } from './services-note';

interface Props {
  menu: ServiceMenuData | null;
  phone: AskChannels['phone'];
}

/**
 * The bar of category chips, then every category in the contract's order with its bookable
 * services, then one line on what the prices mean.
 *
 * `null` means the menu could not be read (the API is down), which is said differently from a
 * clinic that has not opened any service for online booking yet. Both point to the contact page.
 */
export function ServiceMenu({ menu, phone }: Props) {
  if (menu === null || menu.categories.length === 0) {
    return (
      <SiteContainer className="py-12">
        <MenuMessage text={menu === null ? servicesPage.unavailable : servicesPage.empty} />
      </SiteContainer>
    );
  }

  const bookable = menu.categories.some((category) => category.services.length > 0);

  return (
    <>
      <CategoryNav categories={menu.categories} />

      <SiteContainer className="pt-12">
        <div className="space-y-14">
          {!bookable && <MenuMessage text={servicesPage.empty} />}
          {menu.categories.map((category) => (
            <CategorySection key={category.code} category={category} branchNames={menu.branchNames} phone={phone} />
          ))}
        </div>
        <ServicesNote />
      </SiteContainer>
    </>
  );
}
