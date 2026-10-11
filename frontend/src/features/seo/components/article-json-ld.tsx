import type { ArticleSummary } from '@/features/articles/types/article.types';
import { logoSrcOf } from '@/features/clinic/lib/brand-image-src';
import { ogImage, seoSite, sitePages } from '../content/seo';
import { absoluteUrl } from '../lib/absolute-url';
import { articlePath, articlePreviewImage } from '../lib/article-metadata';
import { clinicId } from '../lib/clinic-schema';
import { compact } from '../lib/json-ld';
import { loadClinicProfile } from '../lib/load-clinic-profile';
import { BreadcrumbJsonLd } from './breadcrumb-json-ld';
import { JsonLd } from './json-ld';

/**
 * One article as a schema.org `Article`, published by the clinic, plus where it sits on the site.
 * Everything comes from the article itself (`GET /articles/{slug}`) and the clinic's name and logo.
 */
export async function ArticleJsonLd({ article }: { article: ArticleSummary }) {
  const profile = await loadClinicProfile();
  const path = articlePath(article.slug);
  // An uploaded logo is a `storage:` name; its address is the API's fixed public one (§ 50).
  const logoSrc = logoSrcOf(profile?.logo_url);
  const logo = logoSrc ? absoluteUrl(logoSrc) : undefined;

  return (
    <>
      <JsonLd
        data={compact({
          '@context': 'https://schema.org',
          '@type': 'Article',
          headline: article.title,
          description: article.summary,
          datePublished: article.published_at,
          inLanguage: seoSite.language,
          image: [absoluteUrl(articlePreviewImage(article) ?? ogImage.path)],
          author: article.author_name ? { '@type': 'Person', name: article.author_name } : undefined,
          publisher: compact({
            '@type': 'VeterinaryCare',
            '@id': clinicId(),
            name: profile?.name || seoSite.name,
            logo,
          }),
          mainEntityOfPage: { '@type': 'WebPage', '@id': absoluteUrl(path) },
        })}
      />
      <BreadcrumbJsonLd trail={[sitePages.articles, { name: article.title, path }]} />
    </>
  );
}
