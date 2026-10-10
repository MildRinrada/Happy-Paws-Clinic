import type { Metadata } from 'next';
import type { ArticleSummary } from '@/features/articles/types/article.types';
import { seoSite } from '../content/seo';
import { pageMetadata } from './page-metadata';

export const articlePath = (slug: string) => `/articles/${encodeURIComponent(slug)}`;

/**
 * The cover for link previews: only a picture under `/public`. An uploaded cover is a signed link
 * that stops working within the hour, and LINE and Facebook keep a preview far longer than that,
 * so such an article is shared with the site's default picture instead.
 */
export const articlePreviewImage = (article: Pick<ArticleSummary, 'cover_image_path'>) =>
  article.cover_image_path?.startsWith('/') ? article.cover_image_path : undefined;

/** One article's `metadata`: its title and summary, canonical link, and an article-type preview. */
export function articleMetadata(article: ArticleSummary): Metadata {
  const cover = articlePreviewImage(article);

  return pageMetadata({
    title: `${article.title} | ${seoSite.name}`,
    description: article.summary,
    path: articlePath(article.slug),
    image: cover ? { url: cover, alt: article.title } : undefined,
    type: 'article',
    publishedTime: article.published_at,
  });
}
