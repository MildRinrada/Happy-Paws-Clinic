/**
 * A one-page PDF built by hand, for `POST /staff/documents/generate`. The product renders Thai
 * with an embedded font on the server; the demo has no font to embed, so its page is written in
 * the standard Helvetica and says so. Enough to show the print path works end to end.
 */

/** Keeps what Helvetica can draw; Thai and other characters are dropped. */
function ascii(text: string): string {
  return text
    .replace(/[^\x20-\x7e]/g, '')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/([\\()])/g, '\\$1');
}

export interface PdfLine {
  text: string;
  size?: number;
  bold?: boolean;
  gap?: number;
}

export function buildPdf(lines: PdfLine[]): Uint8Array {
  let y = 790;
  const content = lines
    .map((line) => {
      const size = line.size ?? 11;
      y -= line.gap ?? size + 7;
      return `BT /${line.bold ? 'F2' : 'F1'} ${size} Tf 56 ${y} Td (${ascii(line.text)}) Tj ET`;
    })
    .join('\n');

  const objects = [
    '<< /Type /Catalog /Pages 2 0 R >>',
    '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>',
    '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>',
    '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>',
    `<< /Length ${content.length} >>\nstream\n${content}\nendstream`,
  ];

  let out = '%PDF-1.4\n';
  const offsets: number[] = [];
  objects.forEach((body, index) => {
    offsets.push(out.length);
    out += `${index + 1} 0 obj\n${body}\nendobj\n`;
  });
  const xref = out.length;
  out += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  for (const offset of offsets) out += `${String(offset).padStart(10, '0')} 00000 n \n`;
  out += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF\n`;
  return new TextEncoder().encode(out);
}
