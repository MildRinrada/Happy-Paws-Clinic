/** A procedure in a template. No price: one that names a service is priced by it on the day. */
export interface TemplateProcedure {
  name: string;
  quantity: number;
  service_id?: string;
  note?: string;
}

export interface TemplateMedicine {
  drug_name: string;
  dose: string;
  frequency: string;
  duration_days: number;
  quantity: number;
  instructions?: string;
}

/**
 * A treatment template: `GET /staff/templates/treatments`, "เทมเพลตแผนรักษาที่คลินิกทำไว้เอง".
 * `sessions` is how many visits a course takes; absent for a single visit.
 */
export interface TreatmentTemplate {
  id: string;
  name: string;
  species?: string;
  description?: string;
  procedures: TemplateProcedure[];
  medicines: TemplateMedicine[];
  plan?: string;
  sessions?: number;
  session_interval_days?: number;
}
