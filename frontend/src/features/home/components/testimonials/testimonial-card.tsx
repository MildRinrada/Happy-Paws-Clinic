import type { Testimonial } from '../../types/home.types';

/** What one owner said, and who they are. */
export function TestimonialCard({ testimonial }: { testimonial: Testimonial }) {
  const lastLine = testimonial.quote.length - 1;

  return (
    <li className="rounded-2xl bg-site-soft px-[22px] py-6 text-site-muted">
      <figure>
        <blockquote className="text-[15px] leading-[19px]">
          {testimonial.quote.map((line, index) => (
            <span key={line} className="block">
              {index === 0 && '“'}
              {line}
              {index === lastLine && '”'}
            </span>
          ))}
        </blockquote>

        <figcaption className="mt-4 flex items-center gap-3.5 text-[15px]">
          <span aria-hidden="true" className="size-10 shrink-0 rounded-full bg-site-orange" />
          {testimonial.author} | {testimonial.relation}
        </figcaption>
      </figure>
    </li>
  );
}
