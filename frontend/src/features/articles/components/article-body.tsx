import { bodyParagraphs } from '../lib/article-text';

/**
 * The article's text, one `<p>` per paragraph.
 *
 * Rendered as text on purpose. The body is plain text from the API and is never parsed as HTML or
 * Markdown, so an article cannot put a script or a link into the page.
 */
export function ArticleBody({ body }: { body: string }) {
  return (
    <div className="space-y-5 text-[17px] leading-[1.8] text-ink-800">
      {bodyParagraphs(body).map((paragraph, index) => (
        <p key={index}>{paragraph}</p>
      ))}
    </div>
  );
}
