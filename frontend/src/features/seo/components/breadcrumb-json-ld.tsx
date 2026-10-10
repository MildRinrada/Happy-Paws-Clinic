import { seoSite } from '../content/seo';
import { absoluteUrl } from '../lib/absolute-url';
import { JsonLd } from './json-ld';

export interface Crumb {
  name: string;
  path: string;
}

/**
 * Where an inner page sits under the home page, as a schema.org `BreadcrumbList`. Pass the trail
 * after home: `[{ name: 'บทความ', path: '/articles' }, { name: title, path: '/articles/slug' }]`.
 */
export function BreadcrumbJsonLd({ trail }: { trail: Crumb[] }) {
  const crumbs = [{ name: seoSite.homeLabel, path: '/' }, ...trail];

  return (
    <JsonLd
      data={{
        '@context': 'https://schema.org',
        '@type': 'BreadcrumbList',
        itemListElement: crumbs.map((crumb, index) => ({
          '@type': 'ListItem',
          position: index + 1,
          name: crumb.name,
          item: absoluteUrl(crumb.path),
        })),
      }}
    />
  );
}
