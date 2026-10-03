import type { ArticleSummary } from '../types/article.types';

/** The byline under a title: how long it takes to read, and who wrote it. */
export function articleMeta(article: Pick<ArticleSummary, 'reading_minutes' | 'author_name'>): string {
  return `อ่าน ${article.reading_minutes} นาที | โดย ${article.author_name}`;
}

/** Splits a plain-text body into paragraphs at each blank line. */
export function bodyParagraphs(body: string): string[] {
  return body
    .split(/\r?\n\s*\r?\n/)
    .map((paragraph) => paragraph.trim())
    .filter(Boolean);
}

/** Where an article lives on the public site. */
export function articleHref(slug: string): string {
  return `/articles/${slug}`;
}
