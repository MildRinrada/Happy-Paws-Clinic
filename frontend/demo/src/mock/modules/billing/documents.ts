/**
 * Files the demo hands out, built in the page with no library: CSV, a real (uncompressed) .xlsx,
 * and a PDF whose pages are pictures drawn on a canvas, so Thai text shows in any viewer without
 * embedding a font. `renderPdf()` is exported for any module that answers a `postForFile` with a PDF.
 */

export type Cell = string | number | null | undefined;

// ---- CSV -----------------------------------------------------------------------------------------

export function toCsv(rows: Cell[][]): string {
  const cell = (value: Cell) => {
    const text = value === null || value === undefined ? '' : String(value);
    return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
  };
  // The byte-order mark makes Excel read the file as UTF-8, so the Thai is not garbled.
  return `﻿${rows.map((row) => row.map(cell).join(',')).join('\r\n')}\r\n`;
}

// ---- XLSX ----------------------------------------------------------------------------------------

const encoder = new TextEncoder();

const CRC_TABLE = (() => {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n += 1) {
    let c = n;
    for (let k = 0; k < 8; k += 1) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    table[n] = c >>> 0;
  }
  return table;
})();

function crc32(bytes: Uint8Array): number {
  let crc = 0xffffffff;
  for (const byte of bytes) crc = CRC_TABLE[(crc ^ byte) & 0xff]! ^ (crc >>> 8);
  return (crc ^ 0xffffffff) >>> 0;
}

/** A zip of stored (uncompressed) entries: all an .xlsx needs to open. */
function zip(files: Array<{ name: string; data: Uint8Array }>): Uint8Array {
  const chunks: Uint8Array[] = [];
  const central: Uint8Array[] = [];
  let offset = 0;

  for (const file of files) {
    const name = encoder.encode(file.name);
    const crc = crc32(file.data);
    const local = new DataView(new ArrayBuffer(30));
    local.setUint32(0, 0x04034b50, true);
    local.setUint16(4, 20, true);
    local.setUint16(6, 0x0800, true);
    local.setUint32(14, crc, true);
    local.setUint32(18, file.data.length, true);
    local.setUint32(22, file.data.length, true);
    local.setUint16(26, name.length, true);
    chunks.push(new Uint8Array(local.buffer), name, file.data);

    const entry = new DataView(new ArrayBuffer(46));
    entry.setUint32(0, 0x02014b50, true);
    entry.setUint16(4, 20, true);
    entry.setUint16(6, 20, true);
    entry.setUint16(8, 0x0800, true);
    entry.setUint32(16, crc, true);
    entry.setUint32(20, file.data.length, true);
    entry.setUint32(24, file.data.length, true);
    entry.setUint16(28, name.length, true);
    entry.setUint32(42, offset, true);
    central.push(new Uint8Array(entry.buffer), name);
    offset += 30 + name.length + file.data.length;
  }

  const size = central.reduce((total, chunk) => total + chunk.length, 0);
  const end = new DataView(new ArrayBuffer(22));
  end.setUint32(0, 0x06054b50, true);
  end.setUint16(8, files.length, true);
  end.setUint16(10, files.length, true);
  end.setUint32(12, size, true);
  end.setUint32(16, offset, true);
  return concat([...chunks, ...central, new Uint8Array(end.buffer)]);
}

function concat(chunks: Uint8Array[]): Uint8Array {
  const out = new Uint8Array(chunks.reduce((total, chunk) => total + chunk.length, 0));
  let at = 0;
  for (const chunk of chunks) {
    out.set(chunk, at);
    at += chunk.length;
  }
  return out;
}

const xml = (text: string) => text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

function column(index: number): string {
  let name = '';
  for (let n = index + 1; n > 0; n = Math.floor((n - 1) / 26)) name = String.fromCharCode(65 + ((n - 1) % 26)) + name;
  return name;
}

export function toXlsx(sheetName: string, rows: Cell[][]): Uint8Array {
  const sheetRows = rows
    .map((row, r) => {
      const cells = row
        .map((value, c) => {
          const ref = `${column(c)}${r + 1}`;
          if (value === null || value === undefined || value === '') return '';
          if (typeof value === 'number') return `<c r="${ref}"${r === 0 ? ' s="1"' : ''}><v>${value}</v></c>`;
          return `<c r="${ref}" t="inlineStr"${r === 0 ? ' s="1"' : ''}><is><t xml:space="preserve">${xml(value)}</t></is></c>`;
        })
        .join('');
      return `<row r="${r + 1}">${cells}</row>`;
    })
    .join('');
  const widths = (rows[0] ?? []).map((_, c) => `<col min="${c + 1}" max="${c + 1}" width="${c === 0 ? 30 : 16}" customWidth="1"/>`).join('');

  const files = {
    '[Content_Types].xml':
      '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>',
    '_rels/.rels':
      '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>',
    'xl/workbook.xml': `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="${xml(sheetName.slice(0, 31))}" sheetId="1" r:id="rId1"/></sheets></workbook>`,
    'xl/_rels/workbook.xml.rels':
      '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>',
    'xl/styles.xml':
      '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Tahoma"/></font><font><b/><sz val="11"/><name val="Tahoma"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf/></cellStyleXfs><cellXfs count="2"><xf fontId="0"/><xf fontId="1" applyFont="1"/></cellXfs></styleSheet>',
    'xl/worksheets/sheet1.xml': `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><cols>${widths}</cols><sheetData>${sheetRows}</sheetData></worksheet>`,
  };
  return zip(Object.entries(files).map(([name, text]) => ({ name, data: encoder.encode(text) })));
}

// ---- PDF -----------------------------------------------------------------------------------------

const PAGE = { width: 595, height: 842 };
const SCALE = 2;

/** One line of a PDF page: plain text, or a table row whose cells are spread over the width. */
export type PdfLine = string | { cells: Cell[]; bold?: boolean };

function drawPages(title: string, subtitle: string, lines: PdfLine[]): HTMLCanvasElement[] {
  const pages: HTMLCanvasElement[] = [];
  const font = '"Noto Sans Thai", "IBM Plex Sans Thai", Tahoma, "Leelawadee UI", sans-serif';
  const margin = 40 * SCALE;
  const lineHeight = 18 * SCALE;
  let canvas!: HTMLCanvasElement;
  let context!: CanvasRenderingContext2D;
  let y = 0;

  const newPage = () => {
    canvas = document.createElement('canvas');
    canvas.width = PAGE.width * SCALE;
    canvas.height = PAGE.height * SCALE;
    context = canvas.getContext('2d')!;
    context.fillStyle = '#ffffff';
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.fillStyle = '#1f2937';
    context.textBaseline = 'top';
    y = margin;
    if (pages.length === 0) {
      context.font = `600 ${18 * SCALE}px ${font}`;
      context.fillText(title, margin, y);
      y += 28 * SCALE;
      context.font = `${10 * SCALE}px ${font}`;
      context.fillStyle = '#6b7280';
      context.fillText(subtitle, margin, y);
      context.fillStyle = '#1f2937';
      y += 26 * SCALE;
    }
    pages.push(canvas);
  };
  newPage();

  for (const line of lines) {
    if (y + lineHeight > canvas.height - margin) newPage();
    if (typeof line === 'string') {
      context.font = `${10.5 * SCALE}px ${font}`;
      context.fillText(line, margin, y);
    } else {
      const width = canvas.width - margin * 2;
      const first = width * 0.34;
      const rest = (width - first) / Math.max(1, line.cells.length - 1);
      context.font = `${line.bold ? '600 ' : ''}${10 * SCALE}px ${font}`;
      line.cells.forEach((value, index) => {
        const text = value === null || value === undefined ? '' : String(value);
        if (index === 0) context.fillText(text, margin, y, first - 8);
        else {
          const right = margin + first + rest * index;
          context.fillText(text, right - context.measureText(text).width, y, rest - 6);
        }
      });
      if (line.bold) {
        context.fillRect(margin, y + lineHeight - 4 * SCALE, width, 1 * SCALE);
      }
    }
    y += lineHeight;
  }
  pages.forEach((page, index) => {
    const ctx = page.getContext('2d')!;
    ctx.font = `${8 * SCALE}px ${font}`;
    ctx.fillStyle = '#9ca3af';
    ctx.fillText(`Happy Paws Clinic · หน้า ${index + 1}/${pages.length}`, margin, page.height - margin / 1.5);
  });
  return pages;
}

function jpegBytes(canvas: HTMLCanvasElement): Uint8Array {
  const base64 = canvas.toDataURL('image/jpeg', 0.82).split(',')[1]!;
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) bytes[i] = binary.charCodeAt(i);
  return bytes;
}

/** A PDF of A4 pages with a title, a line under it, and the lines given. */
export function renderPdf(title: string, subtitle: string, lines: PdfLine[]): Uint8Array {
  const pages = drawPages(title, subtitle, lines);
  const parts: Uint8Array[] = [];
  const offsets: number[] = [];
  let length = 0;
  const push = (chunk: Uint8Array | string) => {
    const bytes = typeof chunk === 'string' ? encoder.encode(chunk) : chunk;
    parts.push(bytes);
    length += bytes.length;
  };
  const object = (id: number, body: (Uint8Array | string)[]) => {
    offsets[id] = length;
    push(`${id} 0 obj\n`);
    body.forEach(push);
    push('\nendobj\n');
  };

  push('%PDF-1.4\n%\xE2\xE3\xCF\xD3\n');
  const pageIds = pages.map((_, index) => 3 + index * 3);
  object(1, ['<< /Type /Catalog /Pages 2 0 R >>']);
  object(2, [`<< /Type /Pages /Count ${pages.length} /Kids [${pageIds.map((id) => `${id} 0 R`).join(' ')}] >>`]);
  pages.forEach((page, index) => {
    const pageId = pageIds[index]!;
    const image = jpegBytes(page);
    const content = `q ${PAGE.width} 0 0 ${PAGE.height} 0 0 cm /Im0 Do Q`;
    object(pageId, [`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${PAGE.width} ${PAGE.height}] /Resources << /XObject << /Im0 ${pageId + 1} 0 R >> >> /Contents ${pageId + 2} 0 R >>`]);
    object(pageId + 1, [`<< /Type /XObject /Subtype /Image /Width ${page.width} /Height ${page.height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length ${image.length} >>\nstream\n`, image, '\nendstream']);
    object(pageId + 2, [`<< /Length ${content.length} >>\nstream\n${content}\nendstream`]);
  });

  const count = 3 + pages.length * 3;
  const xrefAt = length;
  push(`xref\n0 ${count}\n0000000000 65535 f \n`);
  for (let id = 1; id < count; id += 1) push(`${String(offsets[id]).padStart(10, '0')} 00000 n \n`);
  push(`trailer\n<< /Size ${count} /Root 1 0 R >>\nstartxref\n${xrefAt}\n%%EOF\n`);
  return concat(parts);
}

/** Bytes as a `data:` URL, for an `<a href download>`. */
export function dataUrl(bytes: Uint8Array | string, type: string): string {
  if (typeof bytes === 'string') return `data:${type};charset=utf-8,${encodeURIComponent(bytes)}`;
  let binary = '';
  for (let i = 0; i < bytes.length; i += 0x8000) binary += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
  return `data:${type};base64,${btoa(binary)}`;
}
