import Link from 'next/link';
import { copyright } from '@/config/site-footer';
import { SiteContainer } from '../site-container';

/** The black bar at the very bottom: the copyright line and the privacy policy. */
export function CopyrightBar() {
  return (
    <div className="bg-black py-5 text-white lg:py-[22px]">
      <SiteContainer>
        <p className="text-center text-[13px]">
          {copyright.text}{' '}
          <Link href={copyright.privacyLink.href} className="font-bold text-site-orange hover:underline">
            {copyright.privacyLink.label}
          </Link>
        </p>
      </SiteContainer>
    </div>
  );
}
