import { CtaBand } from '@/components/site/cta-band';
import { CtaLink } from '@/components/ui/cta-link';
import { articlesCta } from '../../content/articles-page';

/** The grey band that closes the page: book a visit, or get in touch first. */
export function ArticlesCta() {
  return (
    <CtaBand
      variant="muted"
      heading={articlesCta.heading}
      lead={articlesCta.lead}
      actions={
        <>
          <CtaLink href={articlesCta.book.href} variant="solidInk" size="lg">
            {articlesCta.book.label}
          </CtaLink>
          <CtaLink href={articlesCta.contact.href} variant="forest" size="lg">
            {articlesCta.contact.label}
          </CtaLink>
        </>
      }
    />
  );
}
