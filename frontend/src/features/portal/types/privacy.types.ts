/** The four things a customer can say yes or no to. Only `service_messages` is yes by default. */
export type ConsentPurpose = 'service_messages' | 'marketing' | 'medical_record_sharing' | 'extended_data_processing';

export interface ConsentState {
  purpose: ConsentPurpose;
  granted: boolean;
  /** True while the customer has never decided, and the default stands. */
  is_default: boolean;
  recorded_at?: string | null;
}

/** `GET /portal/consents`. */
export interface ConsentSummary {
  customer_id: string;
  consents: ConsentState[];
}

export type DataRequestType = 'access' | 'correction' | 'erasure' | 'portability';
