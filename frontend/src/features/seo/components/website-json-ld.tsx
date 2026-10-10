import { seoSite } from '../content/seo';
import { absoluteUrl } from '../lib/absolute-url';
import { JsonLd } from './json-ld';

/** The site itself, for the home page: helps search engines show the site's name over its address. */
export function WebsiteJsonLd() {
  return (
    <JsonLd
      data={{
        '@context': 'https://schema.org',
        '@type': 'WebSite',
        name: seoSite.name,
        url: absoluteUrl('/'),
        inLanguage: seoSite.language,
      }}
    />
  );
}
