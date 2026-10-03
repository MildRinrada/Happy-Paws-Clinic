/**
 * Sends a file's bytes to the signed URL `POST /portal/documents` returned, from the browser.
 *
 * Plain `fetch`, not the shared client: this body is the raw file, not JSON, and the URL carries
 * its own permission in the token, so there is no session header to add. The storage endpoint
 * answers 204 when the file is in, and a bare 403 for a lapsed URL or a file that does not match
 * what was declared.
 */
export async function uploadToSignedUrl(
  upload: { url: string; headers: Record<string, string> },
  file: File,
): Promise<boolean> {
  try {
    const response = await fetch(upload.url, { method: 'PUT', headers: upload.headers, body: file });
    return response.ok;
  } catch {
    return false;
  }
}
