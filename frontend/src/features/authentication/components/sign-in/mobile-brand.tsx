import { SiteLogo } from '@/components/layout/site-header/site-logo';

/** The logo above the form on a phone, where the green panel is hidden. */
export function MobileBrand() {
  return (
    <div className="mb-10 lg:hidden">
      <SiteLogo />
    </div>
  );
}
