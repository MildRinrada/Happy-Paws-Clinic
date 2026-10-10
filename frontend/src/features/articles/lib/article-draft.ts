import type { ArticleInput, StaffArticle } from '../types/staff-article.types';

/** The editor's fields as typed, before they are checked. */
export interface ArticleDraft {
  title: string;
  slug: string;
  summary: string;
  author_name: string;
  minutes: string;
  body: string;
}

export const emptyDraft: ArticleDraft = { title: '', slug: '', summary: '', author_name: '', minutes: '', body: '' };

export function draftFromArticle(article: StaffArticle): ArticleDraft {
  return {
    title: article.title,
    slug: article.slug,
    summary: article.summary,
    author_name: article.author_name,
    minutes: String(article.reading_minutes),
    body: article.body,
  };
}

/**
 * What goes to the API, or `null` when reading time is not a whole number from 1 to 120. Everything
 * else is left for the API to judge, so the rules live in one place and its messages are the ones shown.
 */
export function toArticleInput(draft: ArticleDraft): ArticleInput | null {
  const minutesText = draft.minutes.trim();
  const minutes = minutesText === '' ? undefined : Number(minutesText);

  if (minutes !== undefined && (!Number.isInteger(minutes) || minutes < 1 || minutes > 120)) {
    return null;
  }

  return {
    title: draft.title,
    slug: draft.slug.trim() || undefined,
    summary: draft.summary,
    body: draft.body,
    author_name: draft.author_name,
    reading_minutes: minutes,
  };
}
