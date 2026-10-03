import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  reactStrictMode: true,
  // Produces .next/standalone, which the Dockerfile runs directly.
  output: 'standalone',
  // The browser never talks to the API directly with a raw token: session cookies are
  // httpOnly, so authenticated calls go through the server-side API client in src/lib/api.
  // NEXT_PUBLIC_API_BASE_URL is only used for the public, unauthenticated reads.
  env: {
    NEXT_PUBLIC_API_BASE_URL: process.env.NEXT_PUBLIC_API_BASE_URL,
  },
};

export default nextConfig;
