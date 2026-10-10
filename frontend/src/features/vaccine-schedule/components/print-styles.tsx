/**
 * On paper, only the page's own content: the site header, footer and chat button around it are
 * hidden while printing. They are the siblings of the element carrying `data-print-root`, which the
 * shared layout renders, so this reaches them without changing the layout.
 */
export function PrintStyles() {
  return (
    <style media="print">{`
      :has(> [data-print-root]) > :not([data-print-root]) { display: none !important; }
      @page { margin: 14mm; }
    `}</style>
  );
}
