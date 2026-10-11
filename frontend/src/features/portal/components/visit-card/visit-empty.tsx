import Link from 'next/link';
import { visitCard } from '../../content/visit-card';
import { solidButton } from './visit-button-styles';

/** The card when there is no appointment ahead and no ticket today. */
export function VisitEmpty() {
  const { title, body, book } = visitCard.empty;

  return (
    <section className="rounded-2xl bg-brand-800 p-5">
      <h2 className="font-display text-base font-semibold leading-6 text-white">{title}</h2>
      <p className="mt-2 text-sm leading-[22px] text-brand-100">{body}</p>
      <Link href={book.href} className={`${solidButton} mt-4 w-full`}>
        {book.label}
      </Link>
    </section>
  );
}
