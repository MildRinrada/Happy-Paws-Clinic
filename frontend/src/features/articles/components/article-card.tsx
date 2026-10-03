import Link from 'next/link';
import { ImageSlot } from '@/components/shared/image-slot';
import { articleHref, articleMeta } from '../lib/article-text';
import type { ArticleSummary } from '../types/article.types';

/** One article in a list: thumbnail, title, and who wrote it. The whole card is the link. */
export function ArticleCard({ article }: { article: ArticleSummary }) {
  return (
    <li>
      <Link
        href={articleHref(article.slug)}
        className="flex items-center gap-5 rounded-2xl border border-site-border bg-white p-[18px] transition hover:border-site-teal"
      >
        <ImageSlot
          src={article.cover_image_path}
          alt=""
          label="รูป"
          sizes="110px"
          className="h-[94px] w-[109px] shrink-0 overflow-hidden rounded-lg"
        />

        <div>
          <h3 className="text-lg font-bold leading-snug">{article.title}</h3>
          <p className="mt-2.5 text-[13px] text-site-muted">{articleMeta(article)}</p>
        </div>
      </Link>
    </li>
  );
}
