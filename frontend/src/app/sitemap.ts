import type { MetadataRoute } from 'next';
import { loadArticles } from '@/features/articles/lib/load-articles';
import { sitePages } from '@/features/seo/content/seo';
import { absoluteUrl } from '@/features/seo/lib/absolute-url';
import { articlePath } from '@/features/seo/lib/article-metadata';

// Never prerendered at build time: a build without the API would freeze a sitemap with no articles.
export const dynamic = 'force-dynamic';

/** The API's largest page of articles. Older articles are still reached from the articles list. */
const ARTICLE_LIMIT = 50;

/**
 * `/sitemap.xml`: the public pages, then every published article the API lists, newest first.
 * Built on each request, so a new article is in it at once. If the API is away the pages are
 * still listed and the articles are left out, rather than the sitemap failing.
 */
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const articles = (await loadArticles(ARTICLE_LIMIT)) ?? [];

  const pages: MetadataRoute.Sitemap = [
    { url: absoluteUrl('/'), changeFrequency: 'weekly', priority: 1 },
    { url: absoluteUrl(sitePages.services.path), changeFrequency: 'weekly', priority: 0.9 },
    { url: absoluteUrl(sitePages.vaccineSchedule.path), changeFrequency: 'monthly', priority: 0.8 },
    { url: absoluteUrl(sitePages.articles.path), changeFrequency: 'weekly', priority: 0.7 },
    { url: absoluteUrl(sitePages.about.path), changeFrequency: 'monthly', priority: 0.6 },
    { url: absoluteUrl(sitePages.team.path), changeFrequency: 'monthly', priority: 0.6 },
    { url: absoluteUrl(sitePages.contact.path), changeFrequency: 'monthly', priority: 0.6 },
    { url: absoluteUrl(sitePages.privacy.path), changeFrequency: 'yearly', priority: 0.2 },
  ];

  return [
    ...pages,
    ...articles.map((article) => ({
      url: absoluteUrl(articlePath(article.slug)),
      lastModified: article.published_at,
      changeFrequency: 'monthly' as const,
      priority: 0.6,
    })),
  ];
}
