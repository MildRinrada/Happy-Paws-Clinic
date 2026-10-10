/* eslint-disable @next/next/no-img-element -- the cover is a short-lived signed link from the API, not a file next/image can optimise. */
import { articleEditorText } from '../../content/article-editor';
import type { StaffArticle } from '../../types/staff-article.types';

/** The cover as it stands: the uploaded picture, a note for a seeded one, or a note that there is none. */
export function CoverPreview({ article }: { article: StaffArticle }) {
  const text = articleEditorText.cover;

  if (article.cover_image_url) {
    return (
      <div className="relative aspect-[16/9] w-full max-w-md overflow-hidden rounded-lg border border-ink-200 bg-ink-50">
        <img src={article.cover_image_url} alt="" className="absolute inset-0 size-full object-cover" />
      </div>
    );
  }

  return (
    <p className="text-sm text-ink-600">
      {article.cover_image_path ? `${text.seeded} (${article.cover_image_path})` : text.none}
    </p>
  );
}
