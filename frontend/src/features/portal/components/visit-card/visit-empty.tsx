import Link from 'next/link';
import { visitCard } from '../../content/visit-card';
import { solidButton } from './visit-button-styles';

/** The card when there is no appointment ahead and no ticket today. */
export function VisitEmpty() {
  const { title, body, book } = visitCard.empty;

  return (
    <section className="max-w-[420px] space-y-3 rounded-2xl bg-site-teal p-5 text-white shadow-sm">
      <h2 className="text-base font-semibold">{title}</h2>
      <p className="text-sm text-white/90">{body}</p>
      <Link href={book.href} className={`${solidButton} w-full`}>
        {book.label}
      </Link>
    </section>
  );
}
