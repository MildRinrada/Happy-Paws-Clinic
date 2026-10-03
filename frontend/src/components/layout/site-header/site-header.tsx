import { HeaderActions } from './header-actions';
import { MainNav } from './main-nav';
import { MobileNav } from './mobile-nav';
import { SiteLogo } from './site-logo';

/**
 * The public site's header: logo, menu, sign in. Each part is its own file beside this one, and
 * the links themselves are in `config/site-navigation.ts`.
 */
export function SiteHeader() {
  return (
    <header className="bg-white">
      <div className="mx-auto flex h-16 max-w-[1120px] items-center gap-3 px-5 sm:px-6 xl:h-[72px] xl:gap-12">
        <SiteLogo />
        <MainNav />

        <div className="ml-auto flex items-center gap-3">
          <HeaderActions />
          <MobileNav />
        </div>
      </div>
    </header>
  );
}
