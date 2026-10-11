import Link from 'next/link';
import { servicesPage } from '../../content/services-page';

/** Why there is no menu to show, and the way to the contact page instead. */
export function MenuMessage({ text }: { text: string }) {
  return (
    <p className="text-[15px] text-site-muted">
      {text}{' '}
      <Link href={servicesPage.contactLink.href} className="font-semibold text-site-teal underline">
        {servicesPage.contactLink.label}
      </Link>
    </p>
  );
}
