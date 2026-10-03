/**
 * The backend's cursor pagination envelope, shared by every list endpoint.
 * Feature-specific types live with their feature (`features/<name>/types/`), not here —
 * this file holds only cross-cutting transport shapes.
 */
export interface CursorPage<T> {
  data: T[];
  page: {
    /** Absent on the last page: the API omits null members rather than sending them. */
    next_cursor?: string;
  };
}

export interface CursorPageQuery {
  limit?: number;
  cursor?: string;
}
