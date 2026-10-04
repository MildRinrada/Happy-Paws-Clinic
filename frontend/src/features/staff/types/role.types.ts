/** A role as `GET /staff/roles` lists it. */
export interface Role {
  id: string;
  /** Echo in `If-Match` when replacing the scope set. */
  version: number;
  /** The key the rest of the API speaks, e.g. `veterinarian`. */
  name: string;
  /** True for the contract's seven starter roles. Their scopes are still editable. */
  is_system: boolean;
  /** As stored, wildcards unexpanded: `invoice:*` is one grant. */
  scopes: string[];
  /** False for `customer`, which describes the portal and is never given to staff or edited. */
  assignable: boolean;
}

/** One entry of `GET /staff/permissions`, the closed scope catalogue. */
export interface Permission {
  scope: string;
  resource: string;
  action: string;
}

/** One resource of the catalogue and its actions, for drawing a group of checkboxes. */
export interface PermissionGroup {
  resource: string;
  scopes: Permission[];
}
