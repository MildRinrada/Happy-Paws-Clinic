import Link from 'next/link';
import { seoSite, sitePages } from '@/features/seo/content/seo';
import { bookingFrame } from '../../content/booking-frame';
import { guestBookingPage } from '../../content/guest-booking-page';

/** "หน้าแรก > บริการและราคา > จองนัดหมาย" at the top of `/book`. */
export function BookingBreadcrumb() {
  const trail = [
    { name: seoSite.homeLabel, href: '/' },
    { name: sitePages.services.name, href: sitePages.services.path },
  ];

  return (
    <nav aria-label={bookingFrame.head.breadcrumbLabel}>
      <ol className="flex flex-wrap items-center gap-x-2.5 gap-y-1 text-sm text-ink-700">
        {trail.map((crumb) => (
          <li key={crumb.href} className="flex items-center gap-2.5">
            <Link href={crumb.href} className="underline underline-offset-2 hover:text-site-teal">
              {crumb.name}
            </Link>
            <span aria-hidden="true" className="text-ink-500">
              ›
            </span>
          </li>
        ))}
        <li aria-current="page" className="font-semibold text-site-forest">
          {guestBookingPage.title}
        </li>
      </ol>
    </nav>
  );
}
