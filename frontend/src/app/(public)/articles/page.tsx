import type { Metadata } from 'next';
import { SiteContainer } from '@/components/layout/site-container';
import { ArticleCardList } from '@/features/articles/components/article-card-list';
import { loadArticles } from '@/features/articles/lib/load-articles';
import { SectionHeading } from '@/features/home/components/shared/section-heading';

export const metadata: Metadata = {
  title: 'บทความจากทีมสัตวแพทย์ | Happy Paws Clinic',
};

/** Every published article, newest first. The API caps a page at 50, which is plenty for now. */
export default async function ArticlesPage() {
  const articles = await loadArticles(50);

  return (
    <main className="py-12">
      <SiteContainer>
        <div className="max-w-[720px]">
          <SectionHeading id="articles-heading">บทความจากทีมสัตวแพทย์</SectionHeading>
          <ArticleCardList articles={articles} />
        </div>
      </SiteContainer>
    </main>
  );
}
