namespace HappyPaws.Domain.Access;

/// <summary>
/// The catalogue of <c>resource:action</c> scopes defined by the API contract.
/// <para>
/// This is the authoritative list served by <c>GET /staff/permissions</c> and validated against by
/// <c>PUT /staff/roles/{id}/permissions</c> ("ตรวจ scope ที่ไม่มีจริง" — reject unknown scopes).
/// Every scope here is taken verbatim from <c>API Reference.dc.html</c>; do not invent new ones
/// without a corresponding change to the contract.
/// </para>
/// </summary>
public static class Scopes
{
    /// <summary>Wildcard granted to <c>clinic_owner</c>. Also used as the suffix in <c>consult:*</c>.</summary>
    public const string Wildcard = "*";

    public static class User
    {
        public const string Manage = "user:manage";
    }

    public static class Settings
    {
        public const string Read = "settings:read";
        public const string Write = "settings:write";

        /// <summary>Withheld from <c>clinic_admin</c>; owner only.</summary>
        public const string Danger = "settings:danger";
    }

    public static class Staff
    {
        public const string Read = "staff:read";
        public const string Manage = "staff:manage";
    }

    public static class Role
    {
        public const string Read = "role:read";
        public const string Manage = "role:manage";
    }

    public static class Customer
    {
        public const string Read = "customer:read";
        public const string Write = "customer:write";
        public const string Merge = "customer:merge";
    }

    public static class Patient
    {
        public const string Read = "patient:read";
        public const string Write = "patient:write";
    }

    public static class Record
    {
        public const string Read = "record:read";
        public const string Write = "record:write";
        public const string Sign = "record:sign";
        public const string Amend = "record:amend";
        public const string Audit = "record:audit";
    }

    public static class Service
    {
        public const string Manage = "service:manage";
    }

    public static class Appointment
    {
        public const string Read = "appointment:read";
        public const string Write = "appointment:write";
        public const string CheckIn = "appointment:checkin";
    }

    public static class Queue
    {
        public const string Read = "queue:read";
        public const string Write = "queue:write";
        public const string Call = "queue:call";
    }

    public static class Consult
    {
        public const string Read = "consult:read";
        public const string Write = "consult:write";
    }

    public static class Vitals
    {
        public const string Write = "vitals:write";
    }

    public static class Prescription
    {
        public const string Read = "rx:read";
        public const string Write = "rx:write";
        public const string Dispense = "rx:dispense";
    }

    public static class Lab
    {
        public const string Read = "lab:read";
        public const string Request = "lab:request";
        public const string Write = "lab:write";
    }

    public static class Schedule
    {
        public const string Read = "schedule:read";
        public const string Write = "schedule:write";
    }

    public static class Notify
    {
        public const string Read = "notify:read";
        public const string Send = "notify:send";
        public const string Manage = "notify:manage";
        public const string Broadcast = "notify:broadcast";
    }

    public static class Message
    {
        public const string Read = "message:read";
        public const string Write = "message:write";
    }

    public static class Integration
    {
        public const string Manage = "integration:manage";
    }

    public static class Dashboard
    {
        public const string Read = "dashboard:read";
    }

    public static class Stats
    {
        public const string Read = "stats:read";
    }

    public static class Analytics
    {
        public const string Read = "analytics:read";
        public const string Hr = "analytics:hr";
    }

    public static class Invoice
    {
        public const string Write = "invoice:write";
        public const string Discount = "invoice:discount";

        /// <summary>Granted to <c>receptionist</c>; see docs/assumptions.md on its overlap with <c>payment:collect</c>.</summary>
        public const string Collect = "invoice:collect";
    }

    public static class Payment
    {
        public const string Collect = "payment:collect";
        public const string Refund = "payment:refund";

        /// <summary>Withheld from <c>clinic_admin</c>; owner only.</summary>
        public const string Payout = "billing:payout";
    }

    public static class Receipt
    {
        public const string Issue = "receipt:issue";
    }

    public static class Inventory
    {
        public const string Read = "inventory:read";
        public const string Write = "inventory:write";
        public const string Adjust = "inventory:adjust";
        public const string Purchase = "inventory:purchase";
    }

    public static class Crm
    {
        public const string Read = "crm:read";
        public const string Manage = "crm:manage";
    }

    public static class Automation
    {
        public const string Read = "automation:read";
        public const string Manage = "automation:manage";
    }

    public static class Report
    {
        public const string Export = "report:export";
    }

    public static class Audit
    {
        public const string Read = "audit:read";
    }

    /// <summary>
    /// Marker for the customer-facing surface. Customers hold no <c>resource:action</c> staff
    /// scopes; <c>/portal/*</c> is additionally constrained to the pets this account cares for
    /// (resource-level authorization, not scope-level).
    /// </summary>
    public const string Portal = "portal";
}
