import Link from 'next/link';
import { emergencyPage } from '../content/emergency-page';
import type { CallNumber } from '../types/emergency.types';
import { CallCard } from './call-card';

/** The numbers to ring, or where else to look when the clinic has set none. */
export function CallList({ numbers }: { numbers: CallNumber[] }) {
  const text = emergencyPage.call;

  if (numbers.length === 0) {
    return (
      <p className="rounded-2xl bg-white/15 px-5 py-4 text-[15px]">
        {text.none}{' '}
        <Link href={text.contact.href} className="font-bold underline">
          {text.contact.label}
        </Link>
      </p>
    );
  }

  return (
    <ul className={`grid gap-4 ${numbers.length > 1 ? 'lg:grid-cols-2' : 'max-w-[560px]'}`}>
      {numbers.map((number) => (
        <CallCard key={number.id} number={number} />
      ))}
    </ul>
  );
}
