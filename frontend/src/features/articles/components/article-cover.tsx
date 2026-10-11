/* eslint-disable @next/next/no-img-element -- cover_image_url is a short-lived signed link from the API, not a file next/image can optimise. */
import { ImageSlot } from '@/components/shared/image-slot';
import type { ArticleSummary } from '../types/article.types';

interface ArticleCoverProps {
  article: Pick<ArticleSummary, 'cover_image_path' | 'cover_image_url'>;
  /** Says what picture belongs here when there is none. */
  label: string;
  sizes: string;
  /** Size and shape of the frame. It must give the frame a height. */
  className: string;
  /** `onDark` when the frame sits on a dark card, so the empty frame stays visible. */
  tone?: 'onLight' | 'onDark';
  /** Loads the picture at once rather than when scrolled near: for a cover at the top of the page. */
  priority?: boolean;
}

/**
 * An article's cover: the picture staff uploaded (a signed link that works for an hour, minted for
 * this page), else a seeded picture under `/public`, else the empty frame.
 */
export function ArticleCover({ article, label, sizes, className, tone = 'onLight', priority = false }: ArticleCoverProps) {
  if (article.cover_image_url) {
    return (
      <div className={`relative ${className}`}>
        <img
          src={article.cover_image_url}
          alt=""
          loading={priority ? 'eager' : 'lazy'}
          className="absolute inset-0 size-full object-cover"
        />
      </div>
    );
  }

  return (
    <ImageSlot
      src={article.cover_image_path}
      alt=""
      label={label}
      sizes={sizes}
      tone={tone}
      priority={priority}
      className={className}
    />
  );
}
