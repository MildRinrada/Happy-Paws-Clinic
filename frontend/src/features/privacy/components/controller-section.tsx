import Link from 'next/link';
import { controllerSection } from '../content/privacy-policy';
import type { ControllerInfo } from '../lib/load-controller';
import { ControllerContacts } from './controller-contacts';
import { ControllerDetails } from './controller-details';
import { PolicyParagraph } from './policy-paragraph';
import { PolicySection } from './policy-section';

/**
 * Who the data controller is and how to reach them. The details come from the clinic's own
 * settings; when the API cannot be reached the section says so and points to the contact page.
 */
export function ControllerSection({ info }: { info: ControllerInfo | null }) {
  const text = controllerSection;

  return (
    <PolicySection id={text.id} heading={text.heading}>
      <PolicyParagraph>{text.lead}</PolicyParagraph>

      {info ? (
        <>
          <ControllerDetails info={info} />
          <PolicyParagraph>{text.contactLead}</PolicyParagraph>
          {info.contacts.length > 0 && <ControllerContacts contacts={info.contacts} />}
        </>
      ) : (
        <PolicyParagraph>{text.unavailable}</PolicyParagraph>
      )}

      <Link href={text.contactPage.href} className="inline-block text-[15px] font-semibold text-site-teal hover:underline">
        {text.contactPage.label}
      </Link>
    </PolicySection>
  );
}
