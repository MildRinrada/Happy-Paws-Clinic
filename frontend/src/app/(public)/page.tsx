import { ArticlesAndReviewsRow } from '@/features/home/components/articles-and-reviews-row';
import { BookingCtaSection } from '@/features/home/components/booking-cta/booking-cta-section';
import { CareSection } from '@/features/home/components/care/care-section';
import { HeroSection } from '@/features/home/components/hero/hero-section';
import { LineSection } from '@/features/home/components/line/line-section';
import { LocationSection } from '@/features/home/components/location/location-section';
import { ServicesSection } from '@/features/home/components/services/services-section';
import { StatsSection } from '@/features/home/components/stats/stats-section';
import { VetsSection } from '@/features/home/components/vets/vets-section';

/**
 * The clinic's home page: one section after another, top to bottom.
 *
 * Each section is a folder under `features/home/components`, and its words live in
 * `features/home/content`. To change what the page says, edit the content file; to change how a
 * section looks, edit that section's folder; to reorder the page, reorder the lines below.
 */
export default function HomePage() {
  return (
    <main>
      <HeroSection />
      <StatsSection />
      <ServicesSection />
      <CareSection />
      <VetsSection />
      <ArticlesAndReviewsRow />
      <LocationSection />
      <LineSection />
      <BookingCtaSection />
    </main>
  );
}
