# The demo's in-browser backend

The demo runs the real product pages from `frontend/src` in the browser (see `demo/src/runtime`).
Every call they make to the API goes to `https://api.demo.happypaws.invalid/v1/...` and is answered
here, inside the page, from data kept in `localStorage`. Nothing reaches a network.

## Layout

```
mock/
  core/            shared plumbing — do not change without coordinating
    http.ts        route(), Reply, fail(), page(), requireStaff(), requireCustomer(), checkVersion(), matches()
    db.ts          table(name), singleton(), uuid(), nowIso(), day(n), at(n,'HH:mm'), minutesFromNow(), nextNumber()
    auth.ts        users, sessions, memberships, display tokens
    bus.ts         emit(topic, data) / on(): realtime, across tabs (queue stream listens to topic 'queue')
    outbox.ts      send({channel:'sms'|'email'|'line', to, subject?, body, code?}): the "messages sent" panel
    otp.ts         issueOtp(phone, purpose) / checkOtp(phone, code, purpose)
    files.ts       keepFile(blob) / signedUpload() / fileUrl(id): uploads kept as data URLs
    demo-accounts.ts  the four demo accounts with fixed user ids
  modules/<name>/
    handlers.ts    route(...) calls; imported automatically
    seed.ts        export const order = N; export function seed() {...}   (optional; run once per day)
    *.ts           anything else the module needs; other modules may import its exported helpers
```

## Rules

1. **Paths** are the backend's, without `/v1`: `route('GET', '/staff/customers/:id', handler)`.
   The authority for every path, method, query and body is the product's own client code in
   `frontend/src/features/<name>/api/*.ts` (and `lib/api/*`). The backend (`backend/src/HappyPaws.Api/Controllers`
   and `HappyPaws.Application/**/Dtos`) is the reference for behaviour when the frontend is not enough.
2. **Shapes** are the product's TypeScript types in `frontend/src/features/<name>/types`. Answer exactly
   those (snake_case). Lists are `{ data, page: { next_cursor? } }` via `page(rows, ctx.query)` unless the
   client says the endpoint answers a bare array.
3. **Rows** in a table are stored in the shape the owning module's main read answers with, plus any
   internal fields the module needs (e.g. `user_id`, `customer_id`). Other modules may read any table,
   but only the owner writes it — call the owner's exported helper if you need to change its rows.
4. **Ids** are `uuid()` (the product validates UUIDs in places). Versioned resources carry `version: 1`;
   `table.update()` bumps it. Use `checkVersion(ctx, row)` where the client sends `ifMatch`.
5. **Errors** go through `fail(status, code, thaiMessage, { field })`, e.g. `invalid('phone', '...')`,
   `notFound('ลูกค้า')`, `stateConflict('...')`. Codes the frontend branches on: `version_conflict`,
   `state_conflict`, `slot_unavailable`, `hold_expired`, `validation_failed`, `not_found`, `insufficient_scope`.
6. **Auth**: `requireStaff(ctx, 'customer:read')` / `requireCustomer(ctx)` / `requireAuth(ctx)`. Public
   endpoints need none. Scope names come from `backend/src/HappyPaws.Domain/Access/Scopes.cs`.
7. **Seeds** must not hard-code another module's ids. Look rows up: `table('branches').all()[0]`,
   `table('staff_members').where(s => s.staff_type === 'veterinarian')`. Dates are relative to today
   (`day(-3)`, `at(0, '10:30')`) so "today" always has appointments and a queue. Seeds are Thai,
   realistic, small enough for localStorage (aim < 300 KB per module), and make every page look alive.
   Seed order: auth -10, clinic/staff 0, customers/pets 10, appointments/queue 20, clinical 30,
   billing/inventory 40, crm/messaging 50, content 5, everything else 60.
8. **Side effects** the product would have: a message to a customer → `send()` to the outbox; a queue
   change → `emit('queue', { branch_id, type: 'ticket.updated' })`; money/stock movements → the owner
   module's helper.
9. Files are TypeScript, no new dependencies, comment density like the product's (short doc comments
   saying why). No emoji.

## Demo accounts (core/demo-accounts.ts)

| key | user id | sign-in | roles |
| --- | --- | --- | --- |
| owner | `7a1d0001-0000-4000-8000-000000000001` | owner@happypaws.test / demo1234, 2FA 123456 | clinic_owner |
| vet | `…0002` | mint@staff.happypaws.test / demo1234, 2FA 123456 | veterinarian |
| reception | `…0003` | siriporn@staff.happypaws.test / demo1234 | receptionist |
| customer | `…0004` | 0812345678 / demo1234 | (customer) |

The staff seed creates the `staff_members` rows for owner/vet/reception (`user_id` = these ids) and
the customers seed creates the customer record (`user_id` = the customer id) with two or three pets.

## Checking pages

`npm run dev:demo` serves http://localhost:5199. Then, from `frontend/`:

```
MSYS_NO_PATHCONV=1 node demo/scripts/route-check.mjs --as owner /customers /customers/new
MSYS_NO_PATHCONV=1 node demo/scripts/route-check.mjs --as customer --shots <dir> /portal
```

Each page prints `ok` or `FAIL` with console errors, exceptions and `[mock] unhandled METHOD /path`
lines. `--shots <dir>` saves a screenshot per page; look at them. A page is done when it is clean,
shows believable data, and its forms/buttons work (try the main actions through `window.__demo`
or by reading the action code and calling the endpoints the action calls).
