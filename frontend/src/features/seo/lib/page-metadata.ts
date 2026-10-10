import type { Metadata } from 'next';
import { ogImage, seoSite } from '../content/seo';

interface PageSeo {
  title: string;
  description?: string;
  /** The page's own path, such as `/services`. Becomes the canonical link and the Open Graph URL. */
  path: string;
  /** A picture under `/public` for link previews. Without it the site's default picture is used. */
  image?: { url: string; alt: string };
  /** `article` for an article page. */
  type?: 'website' | 'article';
  /** For an article: when it went live, as ISO 8601. */
  publishedTime?: string;
}

/**
 * One public page's `metadata`: title, description, canonical link, and the Open Graph and X
 * (Twitter) preview. Every public page goes through this, so a shared link always has a picture.
 *
 * The picture is spelled out on every page because a page's `openGraph` replaces its parent's
 * whole, images included. X falls back to the Open Graph title, description and picture.
 */
export function pageMetadata({ title, description, path, image, type = 'website', publishedTime }: PageSeo): Metadata {
  const images = [
    image
      ? { url: image.url, alt: image.alt }
      : { url: ogImage.path, width: ogImage.width, height: ogImage.height, alt: ogImage.alt },
  ];

  return {
    title,
    description,
    alternates: { canonical: path },
    openGraph: {
      title,
      description,
      url: path,
      siteName: seoSite.name,
      locale: seoSite.locale,
      images,
      ...(type === 'article' ? { type: 'article', publishedTime } : { type: 'website' }),
    },
    twitter: { card: 'summary_large_image' },
  };
}
