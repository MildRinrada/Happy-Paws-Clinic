import { footerLegal } from '@/config/site-footer';
import { FooterColumn, FooterColumnTitle } from './footer-column';
import { FooterDetailList } from './footer-detail';

/** The fourth column: the clinic's licence, its responsible vets and its company registration. */
export function FooterLegalColumn() {
  return (
    <FooterColumn heading={<FooterColumnTitle>{footerLegal.title}</FooterColumnTitle>}>
      <FooterDetailList details={footerLegal.details} />
    </FooterColumn>
  );
}
