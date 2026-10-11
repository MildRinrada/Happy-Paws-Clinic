import { SiteCard } from '@/components/site/site-card';
import { articleHref, articleMeta } from '../lib/article-text';
import type { ArticleSummary } from '../types/article.types';
import { ArticleCover } from './article-cover';

/** One article in a list: thumbnail, title, and who wrote it. The whole card is the link. */
export function ArticleCard({ article }: { article: ArticleSummary }) {
  return (
    <li>
      <SiteCard href={articleHref(article.slug)} padding="none" className="flex items-center gap-5 p-[18px]">
        <ArticleCover
          article={article}
          label="รูป"
          sizes="110px"
          className="h-[94px] w-[109px] shrink-0 overflow-hidden rounded-lg"
        />

        <div>
          <h3 className="text-lg font-bold leading-snug">{article.title}</h3>
          <p className="mt-2.5 text-[13px] text-site-muted">{articleMeta(article)}</p>
        </div>
      </SiteCard>
    </li>
  );
}
