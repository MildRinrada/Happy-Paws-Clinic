/**
 * Hands a PDF the server sent as base64 to the browser as a download. Browser-only: it needs
 * `document`. A download rather than a new tab, because a tab opened after an `await` is what
 * pop-up blockers stop.
 */
export function savePdf(fileName: string, base64: string): void {
  const bytes = Uint8Array.from(atob(base64), (char) => char.charCodeAt(0));
  const url = URL.createObjectURL(new Blob([bytes], { type: 'application/pdf' }));
  const link = document.createElement('a');

  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();

  // The click has started the download by now; the URL is no longer needed.
  setTimeout(() => URL.revokeObjectURL(url), 10_000);
}
