import type { ArticleSummary } from '../types/article.types';
import { ArticleCard } from './article-card';

/**
 * A column of article cards, or a short note when there are none to show.
 *
 * `null` means the articles could not be read (the API is down), which is said differently from a
 * clinic that simply has not published any yet.
 */
export function ArticleCardList({ articles }: { articles: ArticleSummary[] | null }) {
  if (articles === null) {
    return <p className="mt-7 text-[15px] text-site-muted">ยังโหลดบทความไม่ได้ ลองใหม่อีกครั้งภายหลัง</p>;
  }

  if (articles.length === 0) {
    return <p className="mt-7 text-[15px] text-site-muted">ยังไม่มีบทความ</p>;
  }

  return (
    <ul className="mt-7 space-y-[15px]">
      {articles.map((article) => (
        <ArticleCard key={article.id} article={article} />
      ))}
    </ul>
  );
}
