import { articleEditorText } from '../../content/article-editor';
import { articleMeta } from '../../lib/article-text';
import { ArticleBody } from '../article-body';

interface ArticlePreviewProps {
  title: string;
  summary: string;
  author: string;
  minutes: number;
  body: string;
}

/**
 * The article as the public page will show it, from what is typed now. The body goes through the
 * same `ArticleBody` as the public page: text nodes only, so markup shows as the characters typed.
 */
export function ArticlePreview({ title, summary, author, minutes, body }: ArticlePreviewProps) {
  const text = articleEditorText;

  return (
    <div className="space-y-4 rounded-lg border border-ink-200 bg-white p-5">
      <p className="text-xs text-ink-500">{text.previewNote}</p>
      <h2 className="text-2xl font-bold leading-tight text-ink-900">{title}</h2>
      {summary && <p className="text-lg font-light text-ink-600">{summary}</p>}
      <p className="text-sm text-ink-500">{articleMeta({ author_name: author, reading_minutes: minutes })}</p>
      {body.trim() ? <ArticleBody body={body} /> : <p className="text-sm text-ink-500">{text.previewEmpty}</p>}
    </div>
  );
}
