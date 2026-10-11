import { servicesPage } from '../../content/services-page';
import type { AskChannels } from '../../types/ask-channels.types';

interface Props {
  categoryName: string;
  serviceCount: number;
  phone: AskChannels['phone'];
}

/**
 * The note at the right of a category's heading: how many services can be booked, or, with none,
 * "โทรสอบถาม" as a link that rings the clinic (or opens the contact page when no number is set).
 */
export function CategoryMeta({ categoryName, serviceCount, phone }: Props) {
  if (serviceCount > 0) {
    return <p>{servicesPage.serviceCount(serviceCount)}</p>;
  }

  return (
    <a href={phone?.href ?? servicesPage.contactLink.href} className="hover:text-site-teal hover:underline">
      {servicesPage.askByPhone}
      <span className="sr-only"> {servicesPage.askAbout(categoryName)}</span>
    </a>
  );
}
