import Link from 'next/link';
import { footerLinks } from '@/config/site-footer';

/** The row of links under the footer's dividing line. */
export function FooterLinks() {
  return (
    <nav aria-label="ลิงก์ท้ายหน้า" className="mt-12 border-t border-white pt-8 lg:mt-[60px]">
      <ul className="flex flex-wrap gap-x-10 gap-y-3 text-[15px] font-bold">
        {footerLinks.map((link) => (
          <li key={link.label}>
            <Link href={link.href} className="hover:underline">
              {link.label}
            </Link>
          </li>
        ))}
      </ul>
    </nav>
  );
}
