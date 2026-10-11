import Link from 'next/link';
import { useId } from 'react';
import { bookingFrame } from '../../content/booking-frame';

/** "มีคำถามก่อนจอง?": the contact page and the team page, for someone not ready to book yet. */
export function QuestionsCard() {
  const text = bookingFrame.questions;
  const headingId = useId();

  return (
    <section aria-labelledby={headingId} className="rounded-[18px] bg-white p-5">
      <h2 id={headingId} className="text-sm font-bold text-site-forest">
        {text.title}
      </h2>
      <ul className="mt-3 space-y-2">
        {text.links.map((link) => (
          <li key={link.href}>
            <Link href={link.href} className="text-sm text-site-forest underline underline-offset-2 hover:text-site-teal">
              {link.label}
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
