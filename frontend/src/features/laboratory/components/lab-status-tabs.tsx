import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { labPage } from '../content/lab-page';
import { LAB_TABS, labHref, type LabFilters } from '../lib/lab-filters';

/** Status as links, so each list is a plain page and the back button works. */
export function LabStatusTabs({ filters }: { filters: LabFilters }) {
  return (
    <nav aria-label={labPage.tabs.label} className="flex flex-wrap gap-2">
      {LAB_TABS.map((tab) => {
        const current = tab === filters.tab;

        return (
          <Link
            key={tab}
            href={labHref({ ...filters, tab, cursor: undefined })}
            aria-current={current ? 'page' : undefined}
            className={buttonClass(current ? 'primary' : 'secondary', 'sm')}
          >
            {labPage.tabs[tab]}
          </Link>
        );
      })}
    </nav>
  );
}
