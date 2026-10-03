import { testimonials, testimonialsHeading } from '../../content/testimonials';
import { SectionHeading } from '../shared/section-heading';
import { TestimonialCard } from './testimonial-card';

/** The reviews column: a heading and the owners' words under it. */
export function TestimonialList() {
  return (
    <section id="reviews" aria-labelledby="reviews-heading" className="scroll-mt-6">
      <SectionHeading id="reviews-heading">{testimonialsHeading}</SectionHeading>

      <ul className="mt-7 space-y-[18px]">
        {testimonials.map((testimonial) => (
          <TestimonialCard key={testimonial.id} testimonial={testimonial} />
        ))}
      </ul>
    </section>
  );
}
