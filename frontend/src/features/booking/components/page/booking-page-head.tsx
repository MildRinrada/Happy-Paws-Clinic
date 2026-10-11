import Link from 'next/link';
import type { ReactNode } from 'react';
import { sitePages } from '@/features/seo/content/seo';
import { bookingFrame } from '../../content/booking-frame';
import { BookingBreadcrumb } from './booking-breadcrumb';

interface BookingPageHeadProps {
  title: string;
  lead: string;
  /** At the right of the lead on a wide screen, under it on a phone: the "เข้าสู่ระบบก่อนจอง" line. */
  aside?: ReactNode;
}

/** The top of `/book`: where the page sits, the way back to the services, the title and the lead. */
export function BookingPageHead({ title, lead, aside }: BookingPageHeadProps) {
  return (
    <div>
      <BookingBreadcrumb />
      <Link
        href={sitePages.services.path}
        className="mt-6 inline-flex items-center gap-2 text-[15px] font-semibold text-site-forest hover:text-site-teal sm:mt-[30px]"
      >
        <span aria-hidden="true">←</span>
        {bookingFrame.head.backToServices}
      </Link>
      <h1 className="mt-3 text-[32px] font-bold leading-tight text-site-forest sm:text-[38px]">{title}</h1>
      <div className="mt-3 flex flex-col gap-2 lg:flex-row lg:items-baseline lg:justify-between lg:gap-8">
        <p className="text-[15px] leading-relaxed text-site-muted">{lead}</p>
        {aside}
      </div>
    </div>
  );
}
