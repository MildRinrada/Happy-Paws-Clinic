import type { CSSProperties, ImgHTMLAttributes } from 'react';

/** `next/image`: a plain image. `fill` stretches it over its positioned parent, as Next does. */

interface StaticImage {
  src: string;
  width?: number;
  height?: number;
}

interface ImageProps extends Omit<ImgHTMLAttributes<HTMLImageElement>, 'src'> {
  src: string | StaticImage;
  fill?: boolean;
  priority?: boolean;
  quality?: number;
  placeholder?: string;
  blurDataURL?: string;
  unoptimized?: boolean;
  loader?: unknown;
}

export default function Image({
  src,
  fill,
  priority,
  quality: _quality,
  placeholder: _placeholder,
  blurDataURL: _blur,
  unoptimized: _unoptimized,
  loader: _loader,
  style,
  ...rest
}: ImageProps) {
  const url = typeof src === 'string' ? src : src.src;
  const filled: CSSProperties | undefined = fill
    ? { position: 'absolute', inset: 0, width: '100%', height: '100%' }
    : undefined;

  return (
    <img
      {...rest}
      src={url}
      loading={priority ? 'eager' : (rest.loading ?? 'lazy')}
      decoding="async"
      style={{ ...filled, ...style }}
    />
  );
}
