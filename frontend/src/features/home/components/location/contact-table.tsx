import type { ContactRow } from '../../types/home.types';

/** Opening hours, phone and the like, as label–value rows in a bordered box. */
export function ContactTable({ rows }: { rows: ContactRow[] }) {
  return (
    <dl className="divide-y divide-site-border rounded-[14px] border border-site-border">
      {rows.map((row) => (
        <div key={row.label} className="flex min-h-14 items-center justify-between gap-6 px-5 py-2 sm:px-[30px]">
          <dt className="text-site-muted">{row.label}</dt>
          <dd className="text-right">
            {row.href ? (
              <a href={row.href} className="hover:underline">
                {row.value}
              </a>
            ) : (
              row.value
            )}
          </dd>
        </div>
      ))}
    </dl>
  );
}
