import type { ArticleSummary } from './article.types';

/** What the reader asked the articles page for: `?q=&tag=&cursor=`. */
export interface ArticleListQuery {
  /** Words to look for in the title and summary. */
  q?: string;
  /** One topic, such as "แมว". */
  tag?: string;
  /** Where the next page of older articles starts, from the page before. */
  cursor?: string;
}

/** A topic and how many published articles are filed under it. */
export interface ArticleTagCount {
  tag: string;
  count: number;
}

/** One page of the articles list, as `loadArticleList` gives it. */
export interface ArticleList {
  /** Newest first. */
  items: ArticleSummary[];
  /** Every topic, for the row of chips. Empty until the API can list them. */
  tags: ArticleTagCount[];
  /** Present when there are older articles to show on another page. */
  nextCursor?: string;
}
