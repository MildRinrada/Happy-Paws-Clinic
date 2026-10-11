import Link from 'next/link';
import { useId } from 'react';
import { articleCards } from '../../content/articles-page';
import { articleHref } from '../../lib/article-text';
import type { ArticleSummary } from '../../types/article.types';
import { ArticleCover } from '../article-cover';
import { ArticleByline } from './article-byline';
import { ArticleTopicLine } from './article-topic-line';

/**
 * One article in the grid: cover, topics (when the article has some), title, summary and byline,
 * straight on the page with no card around them. The whole tile is one link, named by the title.
 */
export function ArticleTile({ article }: { article: ArticleSummary }) {
  const titleId = useId();
  const hasTopics = article.tags !== undefined && article.tags.length > 0;

  return (
    <li>
      <Link href={articleHref(article.slug)} aria-labelledby={titleId} className="group block">
        <ArticleCover
          article={article}
          label={articleCards.coverLabel}
          sizes="(min-width: 1024px) 315px, (min-width: 768px) 50vw, 100vw"
          className="aspect-[16/10] w-full overflow-hidden rounded-xl"
        />
        {hasTopics && <ArticleTopicLine tags={article.tags ?? []} className="mt-3" />}
        <h2
          id={titleId}
          className={`${hasTopics ? 'mt-2' : 'mt-4'} text-[19px] font-bold leading-[28px] text-ink-900 transition group-hover:text-site-teal`}
        >
          {article.title}
        </h2>
        <p className="mt-2.5 text-[14.5px] font-light leading-[23px] text-ink-700">{article.summary}</p>
        <ArticleByline article={article} className="mt-2.5 text-[13px] font-light text-ink-600" />
      </Link>
    </li>
  );
}
