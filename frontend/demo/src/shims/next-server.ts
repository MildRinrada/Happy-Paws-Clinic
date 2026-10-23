/**
 * `next/server`, `next/og` and `next`: only route handlers, middleware and the share picture use
 * these, and the demo has none of them. Types and harmless stand-ins keep a stray import compiling.
 */
export type NextRequest = Request & { nextUrl: URL };
export type Metadata = Record<string, unknown>;
export type NextConfig = Record<string, unknown>;
export type MetadataRoute = Record<string, unknown>;

export const NextResponse = {
  next: () => new Response(null),
  json: (body: unknown, init?: ResponseInit) => new Response(JSON.stringify(body), init),
  redirect: (url: string | URL) => Response.redirect(String(url)),
};

export class ImageResponse extends Response {}
