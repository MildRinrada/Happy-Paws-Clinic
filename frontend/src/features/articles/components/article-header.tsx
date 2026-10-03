import Link from 'next/link';
import { formatLongDate } from '@/lib/format/datetime';
import { articleMeta } from '../lib/article-text';
import type { Article } from '../types/article.types';

/** The top of an article page: a way back, the title, the summary and the byline. */
export function ArticleHeader({ article }: { article: Article }) {
  return (
    <header>
      <Link href="/articles" className="text-sm text-site-teal hover:underline">
        ← บทความทั้งหมด
      </Link>

      <h1 className="mt-4 text-3xl font-bold leading-tight sm:text-[38px] sm:leading-tight">{article.title}</h1>
      <p className="mt-3 text-lg font-light text-site-muted">{article.summary}</p>

      <p className="mt-4 text-sm text-site-muted">
        {articleMeta(article)} | เผยแพร่ {formatLongDate(article.published_at)}
      </p>
    </header>
  );
}
