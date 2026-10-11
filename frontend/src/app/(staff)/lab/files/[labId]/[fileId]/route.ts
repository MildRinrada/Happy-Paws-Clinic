import { NextResponse, type NextRequest } from 'next/server';
import { getLabFileLink } from '@/features/laboratory/api/lab-requests';
import { labPage } from '@/features/laboratory/content/lab-page';
import { ApiError } from '@/lib/api/api-error';
import { SIGN_IN_PATH } from '@/lib/auth/session';

export const dynamic = 'force-dynamic';

/**
 * Opens one file from the lab (docs/contract-extensions.md § 54). The file list links here rather
 * than to the file: each click asks the API for a fresh five-minute link, as the signed-in member,
 * and sends the browser on to it. So no page ever carries a working file address, and every
 * opening is in the access log. A PDF arrives as a download; a picture opens in the tab.
 */
export async function GET(request: NextRequest, { params }: { params: Promise<{ labId: string; fileId: string }> }) {
  const { labId, fileId } = await params;

  try {
    const link = await getLabFileLink(labId, fileId);
    return NextResponse.redirect(link.url, { status: 303, headers: { 'Cache-Control': 'no-store' } });
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return NextResponse.redirect(new URL(SIGN_IN_PATH, request.url), { status: 303 });
    }
    if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
      return new NextResponse(labPage.files.notFound, { status: 404, headers: { 'Content-Type': 'text/plain; charset=utf-8' } });
    }
    throw error;
  }
}
