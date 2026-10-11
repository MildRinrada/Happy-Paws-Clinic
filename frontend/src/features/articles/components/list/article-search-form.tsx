import Link from 'next/link';
import { useId } from 'react';
import { articleSearch } from '../../content/articles-page';
import { articlesHref } from '../../lib/articles-href';
import { ARTICLE_SEARCH_MAX } from '../../lib/read-article-list-query';
import { ClearIcon } from './clear-icon';
import { SearchIcon } from './search-icon';

interface Props {
  /** The search on screen now, kept in the box. */
  q?: string;
  /** The topic on screen now, kept when searching. */
  tag?: string;
}

/**
 * The search box at the top right: a plain form that loads `/articles?q=`, so it works before any
 * script has run. Enter or the magnifier sends it; the cross, shown while searching, clears it.
 */
export function ArticleSearchForm({ q, tag }: Props) {
  const inputId = useId();

  return (
    <form action={articlesHref()} method="get" role="search" className="relative w-full sm:max-w-[400px] lg:w-[334px]">
      <label htmlFor={inputId} className="sr-only">
        {articleSearch.label}
      </label>
      {tag && <input type="hidden" name="tag" value={tag} />}
      <input
        id={inputId}
        type="search"
        name="q"
        defaultValue={q}
        placeholder={articleSearch.label}
        maxLength={ARTICLE_SEARCH_MAX}
        enterKeyHint="search"
        className="h-[46px] w-full rounded-[10px] border border-site-border bg-white pl-[42px] pr-11 text-base text-ink-900 transition placeholder:text-ink-500 focus:border-site-teal sm:text-[15px] [&::-webkit-search-cancel-button]:appearance-none"
      />
      <button
        type="submit"
        aria-label={articleSearch.submit}
        className="absolute inset-y-0 left-0 flex items-center pl-4 pr-1.5 text-ink-700 hover:text-site-teal"
      >
        <SearchIcon />
      </button>
      {q && (
        <Link
          href={articlesHref({ tag })}
          aria-label={articleSearch.clear}
          className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-ink-500 hover:text-site-teal"
        >
          <ClearIcon />
        </Link>
      )}
    </form>
  );
}
