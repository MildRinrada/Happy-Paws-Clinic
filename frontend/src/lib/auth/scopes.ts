/**
 * "Does this session hold this scope", with the contract's two wildcard forms — the same rule the
 * backend's `ScopeSet` applies: `*` is everything and `queue:*` is every action on a resource.
 *
 * This decides which menus to draw and nothing else. A menu hidden here is a convenience; the
 * backend re-checks the scope on every request and is the only thing that actually refuses one
 * (docs/authorization.md § 7).
 */
export function allows(granted: readonly string[], required: string): boolean {
  if (granted.includes('*') || granted.includes(required)) {
    return true;
  }

  const separator = required.indexOf(':');

  return separator > 0 && granted.includes(`${required.slice(0, separator)}:*`);
}

/** The scopes the console's screens are built on. Names are the contract's. */
export const Scope = {
  AppointmentRead: 'appointment:read',
  AppointmentWrite: 'appointment:write',
  AppointmentCheckIn: 'appointment:checkin',
  QueueRead: 'queue:read',
  QueueWrite: 'queue:write',
  QueueCall: 'queue:call',
  PatientRead: 'patient:read',
  PatientWrite: 'patient:write',
  RecordRead: 'record:read',
  RecordWrite: 'record:write',
  RecordSign: 'record:sign',
  ConsultRead: 'consult:read',
  ConsultWrite: 'consult:write',
  VitalsWrite: 'vitals:write',
  PrescriptionWrite: 'rx:write',
  PrescriptionRead: 'rx:read',
  PrescriptionDispense: 'rx:dispense',
  LabRequest: 'lab:request',
  LabRead: 'lab:read',
  LabWrite: 'lab:write',
  StaffRead: 'staff:read',
  StaffManage: 'staff:manage',
  RoleRead: 'role:read',
  RoleManage: 'role:manage',
  UserManage: 'user:manage',
  CustomerRead: 'customer:read',
  CustomerWrite: 'customer:write',
  CustomerMerge: 'customer:merge',
  InvoiceWrite: 'invoice:write',
  SettingsRead: 'settings:read',
  SettingsWrite: 'settings:write',
  ServiceManage: 'service:manage',
  ScheduleRead: 'schedule:read',
  ScheduleWrite: 'schedule:write',
  NotifySend: 'notify:send',
  NotifyManage: 'notify:manage',
  NotifyRead: 'notify:read',
  NotifyBroadcast: 'notify:broadcast',
  IntegrationManage: 'integration:manage',
  AutomationRead: 'automation:read',
  AutomationManage: 'automation:manage',
  DashboardRead: 'dashboard:read',
  StatsRead: 'stats:read',
  AnalyticsRead: 'analytics:read',
  AnalyticsHr: 'analytics:hr',
  ReportExport: 'report:export',
  InventoryRead: 'inventory:read',
  InventoryWrite: 'inventory:write',
  InventoryAdjust: 'inventory:adjust',
  InventoryPurchase: 'inventory:purchase',
  PaymentCollect: 'payment:collect',
  ReceiptIssue: 'receipt:issue',
} as const;
