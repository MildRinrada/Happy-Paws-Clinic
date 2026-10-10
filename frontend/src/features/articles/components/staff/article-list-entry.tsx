import Link from 'next/link';
import { formatLongDate } from '@/lib/format/datetime';
import { staffArticlesText } from '../../content/staff-articles';
import { articlePaths } from '../../lib/article-paths';
import { articleHref } from '../../lib/article-text';
import type { StaffArticleSummary } from '../../types/staff-article.types';
import { ArticleStatusBadge } from './article-status-badge';

/** One article in the staff list: title, status, byline and dates, with the way into the editor. */
export function ArticleListEntry({ article }: { article: StaffArticleSummary }) {
  const text = staffArticlesText;

  return (
    <li className="flex flex-wrap items-start justify-between gap-3 py-4">
      <div className="min-w-0 space-y-1">
        <div className="flex flex-wrap items-center gap-2">
          <Link href={articlePaths.edit(article.id)} className="font-medium text-ink-900 hover:underline">
            {article.title}
          </Link>
          <ArticleStatusBadge status={article.status} />
        </div>
        <p className="text-sm text-ink-600">{article.summary}</p>
        <p className="text-xs text-ink-500">
          /articles/{article.slug} | {text.meta(article.author_name, article.reading_minutes)} |{' '}
          {article.published_at ? text.publishedOn(formatLongDate(article.published_at)) : text.createdOn(formatLongDate(article.created_at))}
          {!article.has_cover && ` | ${text.noCover}`}
        </p>
      </div>
      <div className="flex shrink-0 items-center gap-3 text-sm">
        {article.status === 'published' && (
          <Link href={articleHref(article.slug)} target="_blank" className="text-ink-600 hover:underline">
            {text.view}
          </Link>
        )}
        <Link href={articlePaths.edit(article.id)} className="text-brand-700 hover:underline">
          {text.edit}
        </Link>
      </div>
    </li>
  );
}
