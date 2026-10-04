import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { staffSettings } from '../../content/staff-settings';
import { staffPageHref } from '../../lib/staff-filters';
import type { StaffListFilters, StaffStatus } from '../../types/staff.types';

const tabs: { status?: StaffStatus; label: string }[] = [
  { label: staffSettings.tabs.all },
  { status: 'active', label: staffSettings.tabs.active },
  { status: 'invited', label: staffSettings.tabs.invited },
  { status: 'deactivated', label: staffSettings.tabs.deactivated },
];

/** Status as links, so the list is a plain page per status and the back button works. */
export function StaffStatusTabs({ filters }: { filters: StaffListFilters }) {
  return (
    <nav aria-label={staffSettings.tabs.label} className="flex flex-wrap gap-2">
      {tabs.map((tab) => {
        const current = tab.status === filters.status;

        return (
          <Link
            key={tab.label}
            // Switching status starts again from the first page; the cursor belongs to the old list.
            href={staffPageHref({ ...filters, status: tab.status, cursor: undefined })}
            aria-current={current ? 'page' : undefined}
            className={buttonClass(current ? 'primary' : 'secondary', 'sm')}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}
