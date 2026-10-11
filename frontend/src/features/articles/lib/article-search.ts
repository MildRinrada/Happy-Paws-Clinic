import type { ArticleSummary } from '../types/article.types';

/**
 * Text made comparable: compatibility forms folded (so a "ำ" typed as two marks matches the one
 * character), invisible joiners that Thai text often carries dropped, lower case for Latin
 * letters, and every run of spaces made one space.
 */
function normalise(text: string): string {
  return text
    .normalize('NFKC')
    .replace(/[​-‍⁠﻿]/g, '')
    .toLocaleLowerCase('th')
    .replace(/\s+/g, ' ')
    .trim();
}

/**
 * Whether an article's title or summary holds every word of the search. Thai is written without
 * spaces between words, so each word is matched anywhere inside the text, not at word starts.
 */
export function matchesArticleSearch(article: Pick<ArticleSummary, 'title' | 'summary'>, query: string): boolean {
  const text = normalise(`${article.title} ${article.summary}`);
  const words = normalise(query).split(' ').filter(Boolean);

  return words.every((word) => text.includes(word));
}
