import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { petDocuments } from '../../content/pet-documents';
import { PrintDocumentForm } from './print-document-form';
import { UploadDocumentForm } from './upload-document-form';

interface DocumentsCardProps {
  petId: string;
  /** `record:write`: add a file to the record. */
  canUpload: boolean;
  /** `record:read`: print a document from the record. */
  canPrint: boolean;
}

/**
 * Files in and documents out. The contract has no read that lists a patient's files for staff,
 * so the card says so instead of showing an empty list that looks like "none".
 */
export function DocumentsCard({ petId, canUpload, canPrint }: DocumentsCardProps) {
  const text = petDocuments;

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody className="space-y-4">
        {canUpload && (
          <section className="space-y-2">
            <p className="text-xs text-ink-500">{text.noList}</p>
            <UploadDocumentForm petId={petId} />
          </section>
        )}
        {canUpload && canPrint && <hr className="border-ink-100" />}
        {canPrint && <PrintDocumentForm petId={petId} />}
      </CardBody>
    </Card>
  );
}
