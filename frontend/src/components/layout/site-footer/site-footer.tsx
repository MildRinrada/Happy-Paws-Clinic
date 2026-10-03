import { loadSiteInfo } from '@/features/clinic/lib/load-site-info';
import { SiteContainer } from '../site-container';
import { CopyrightBar } from './copyright-bar';
import { EmergencyBar } from './emergency-bar';
import { FooterBrandColumn } from './footer-brand-column';
import { FooterContactColumn } from './footer-contact-column';
import { FooterHoursColumn } from './footer-hours-column';
import { FooterLegalColumn } from './footer-legal-column';
import { FooterLinks } from './footer-links';

/**
 * The public site's footer: the emergency line, four columns of who-and-where, a row of links and
 * the copyright bar. Each part is its own file beside this one. The wording is in
 * `config/site-footer.ts`; the phone numbers, chat accounts, map and hours come from the API, the
 * same read the contact page makes, so the two cannot disagree.
 */
export async function SiteFooter() {
  const info = await loadSiteInfo();

  return (
    <footer>
      {info.emergency && <EmergencyBar contact={info.emergency} />}

      <div className="bg-site-footer pb-10 pt-7 text-white">
        <SiteContainer>
          {/* The columns are as wide as the design draws them, which is not four equal parts. */}
          <div className="grid gap-x-7 gap-y-10 sm:grid-cols-2 xl:grid-cols-[650fr_480fr_560fr_704fr]">
            <FooterBrandColumn map={info.map} />
            <FooterContactColumn info={info} />
            <FooterHoursColumn hours={info.hours} />
            <FooterLegalColumn />
          </div>

          <FooterLinks />
        </SiteContainer>
      </div>

      <CopyrightBar />
    </footer>
  );
}
