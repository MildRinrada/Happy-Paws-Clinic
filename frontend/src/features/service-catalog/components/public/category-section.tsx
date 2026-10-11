import { SectionHeader } from '@/components/site/section-header';
import { categoryBlurbs } from '../../content/services-page';
import { categoryAnchor } from '../../lib/category-anchor';
import type { AskChannels } from '../../types/ask-channels.types';
import type { MenuCategory } from '../../types/service-menu.types';
import { CategoryMeta } from './category-meta';
import { ServiceCard } from './service-card';

interface Props {
  category: MenuCategory;
  branchNames: Record<string, string>;
  phone: AskChannels['phone'];
}

/**
 * One category: its name, one line on what it covers, how many services (or "โทรสอบถาม" when
 * none can be booked online), and a card per service.
 */
export function CategorySection({ category, branchNames, phone }: Props) {
  const id = categoryAnchor(category.code);
  const headingId = `${id}-heading`;
  const hasServices = category.services.length > 0;

  return (
    <section id={id} aria-labelledby={headingId} className={`scroll-mt-6 ${hasServices ? '' : 'pb-5'}`}>
      <SectionHeader
        id={headingId}
        title={category.name}
        blurb={categoryBlurbs[category.code]}
        meta={<CategoryMeta categoryName={category.name} serviceCount={category.services.length} phone={phone} />}
      />

      {hasServices && (
        <ul className="mt-5 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {category.services.map((service) => (
            <ServiceCard key={service.id} service={service} branchNames={branchNames} />
          ))}
        </ul>
      )}
    </section>
  );
}
