import { CtaLink } from '@/components/ui/cta-link';
import { vaccinePage } from '../content/vaccine-schedule-page';

/** Book the vaccination: the booking page, with a vaccination service picked when there is one. */
export function BookVaccineCard({ href }: { href: string }) {
  const text = vaccinePage.actions.book;

  return (
    <li className="flex flex-col rounded-2xl bg-site-orange p-5 text-white sm:p-6">
      <h3 className="text-xl font-bold leading-tight">{text.title}</h3>
      <p className="mt-2 flex-1 text-[15px] font-semibold leading-relaxed">{text.detail}</p>
      <CtaLink href={href} variant="violet" size="lg" className="mt-4 w-full sm:w-auto sm:self-start">
        {text.button}
      </CtaLink>
    </li>
  );
}
