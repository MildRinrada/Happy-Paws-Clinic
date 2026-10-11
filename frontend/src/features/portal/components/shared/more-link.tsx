import Link from 'next/link';

/** "ดูทั้งหมด" at the right of a section's title. */
export function MoreLink({ label, href }: { label: string; href: string }) {
  return (
    <Link
      href={href}
      className="shrink-0 font-display text-[13px] font-medium text-brand-600 underline decoration-1 underline-offset-4 hover:text-brand-700"
    >
      {label}
    </Link>
  );
}
