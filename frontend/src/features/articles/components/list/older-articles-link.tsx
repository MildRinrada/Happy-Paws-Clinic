import { CtaLink } from '@/components/ui/cta-link';
import { articlesList } from '../../content/articles-page';

/** "บทความก่อนหน้านี้", centred under the grid: the next page of older articles. */
export function OlderArticlesLink({ href }: { href: string }) {
  return (
    <div className="mt-10 flex justify-center">
      <CtaLink href={href} variant="outlineForest" size="lg">
        {articlesList.older}
      </CtaLink>
    </div>
  );
}
