import { servicesNote } from '../../content/services-page';

/** One line under the last category: what the prices mean, and that only bookable services are listed. */
export function ServicesNote() {
  return <p className="mt-6 text-sm text-site-muted">{servicesNote}</p>;
}
