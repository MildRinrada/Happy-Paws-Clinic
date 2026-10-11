import { articleSearch } from '../../content/articles-page';

/** The line over the results of a search: what was searched for and how many articles matched. */
export function ArticleSearchSummary({ q, count }: { q: string; count: number }) {
  return <p className="text-[15px] text-ink-700">{articleSearch.results(q, count)}</p>;
}
