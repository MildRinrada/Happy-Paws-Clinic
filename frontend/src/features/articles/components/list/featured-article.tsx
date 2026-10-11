import { useId } from 'react';
import { MetaPill } from '@/components/site/meta-pill';
import { SiteCard } from '@/components/site/site-card';
import { ctaLinkLook } from '@/components/ui/cta-link';
import { articleCards } from '../../content/articles-page';
import { articleHref } from '../../lib/article-text';
import type { ArticleSummary } from '../../types/article.types';
import { ArticleCover } from '../article-cover';
import { ArticleByline } from './article-byline';

/**
 * The newest article, large: its cover at the left and, on dark green at the right, "บทความล่าสุด",
 * the title, summary, byline and an "อ่านบทความ" button. The cover goes on top on a phone. The
 * whole card is one link, named by the title.
 */
export function FeaturedArticle({ article }: { article: ArticleSummary }) {
  const titleId = useId();

  return (
    <SiteCard
      href={articleHref(article.slug)}
      tone="forest"
      padding="none"
      radius="xl"
      ariaLabelledBy={titleId}
      className="group overflow-hidden"
    >
      <div className="grid md:grid-cols-[13fr_12fr]">
        <ArticleCover
          article={article}
          label={articleCards.coverLabel}
          tone="onDark"
          priority
          sizes="(min-width: 1024px) 515px, (min-width: 768px) 52vw, 100vw"
          className="aspect-[16/10] w-full md:aspect-auto md:h-full md:min-h-[300px]"
        />

        <div className="flex flex-col items-start px-6 py-7 sm:py-[35px] sm:pl-[34px] sm:pr-12">
          <MetaPill tone="sun">{articleCards.latest}</MetaPill>
          <h2 id={titleId} className="mt-4 text-2xl font-semibold leading-[1.3] group-hover:underline lg:text-[30px] lg:leading-[38px]">
            {article.title}
          </h2>
          <p className="mt-3.5 text-[15px] font-light leading-6 text-white/85">{article.summary}</p>
          <ArticleByline article={article} className="mt-4 text-sm font-light text-white/75" />
          <span aria-hidden="true" className={`mt-4 ${ctaLinkLook('solidInk', 'lg')}`}>
            {articleCards.read}
          </span>
        </div>
      </div>
    </SiteCard>
  );
}
