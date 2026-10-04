import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { staffSettings } from '../../content/staff-settings';
import { staffPageHref } from '../../lib/staff-filters';
import type { StaffListFilters } from '../../types/staff.types';

interface StaffPagerProps {
  filters: StaffListFilters;
  /** From the page just read; absent on the last page. */
  nextCursor?: string;
}

/** Forward through the list. The cursor is opaque and only goes forward, so "back" is "first". */
export function StaffPager({ filters, nextCursor }: StaffPagerProps) {
  if (!nextCursor && !filters.cursor) return null;

  return (
    <nav className="flex flex-wrap justify-end gap-2 border-t border-ink-100 px-5 py-3">
      {filters.cursor && (
        <Link href={staffPageHref({ ...filters, cursor: undefined })} className={buttonClass('ghost', 'sm')}>
          {staffSettings.list.first}
        </Link>
      )}
      {nextCursor && (
        <Link href={staffPageHref({ ...filters, cursor: nextCursor })} className={buttonClass('secondary', 'sm')}>
          {staffSettings.list.next}
        </Link>
      )}
    </nav>
  );
}
