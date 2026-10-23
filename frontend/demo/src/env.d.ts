/// <reference types="vite/client" />

/** `?original` reads a product module the demo overrides (see vite.config.mts), as it is on disk. */
declare module '@/features/home/content/hero.ts?original' {
  export * from '@/features/home/content/hero';
}

declare module '@/features/home/content/care.ts?original' {
  export * from '@/features/home/content/care';
}

declare module '@/features/home/content/services.ts?original' {
  export * from '@/features/home/content/services';
}

declare module '@/features/site-banners/content/page-banners.ts?original' {
  export * from '@/features/site-banners/content/page-banners';
}

declare module '*.jpg' {
  const src: string;
  export default src;
}

declare module '*.png' {
  const src: string;
  export default src;
}
