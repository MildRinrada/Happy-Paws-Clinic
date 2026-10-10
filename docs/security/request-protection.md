# การป้องกันระดับคำขอ

ส่วนหนึ่งของ [ความปลอดภัย](README.md)

## ลำดับ middleware

ลำดับใน `Program.cs` มีผลกับความปลอดภัย อย่าสลับโดยไม่อ่านเหตุผล

| ลำดับ | middleware | ทำไมอยู่ตรงนี้ |
|---|---|---|
| 1 | `UsePathBase("/v1")` | ให้ routing และ path ของคุกกี้ refresh เห็น `/v1` |
| 2 | `UseExceptionHandler` | error ทุกจุดหลังจากนี้ออกเป็น envelope เดียวกัน |
| 3 | `RequestContextMiddleware` | ตั้ง `request_id` ก่อน error ใดจะเกิด |
| 4 | `UseHttpsRedirection`, `UseHsts` | เฉพาะเมื่อไม่ใช่ `Development` |
| 5 | `UseCors` | |
| 6 | `UseAuthentication` | |
| 7 | `UseRateLimiter` | ต้องอยู่หลัง authentication จึงแยกถังตามผู้ใช้ได้ ถ้าอยู่ก่อน ทุกคำขอจะถูกนับตาม IP |
| 8 | `UseAuthorization` | |
| 9 | `IdempotencyMiddleware` | ต้องรู้ผู้เรียกแล้ว และคำขอที่ไม่มีสิทธิ์ต้องไม่ได้จอง key |

## การตรวจ input

- ทุก use case ที่รับ body เรียก `RequestValidator.ValidateAsync` เป็นบรรทัดแรก ใช้ FluentValidation ที่ลงทะเบียนต่อ command ไม่ได้ซ่อนเป็น pipeline เพื่อให้คนอ่าน handler เห็นว่าตรวจแล้ว
- request ที่ไม่มี validator ผ่านเสมอ ตั้งใจให้ใช้กับ input ที่ไม่มีรูปร่าง เช่นรหัสใน path ทุกอย่างที่มี body ควรมี validator
- body ที่ผูกค่าไม่ได้ (JSON ผิด ขาดช่องที่บังคับ) `ModelStateErrors` เปลี่ยนเป็น `ValidationFailedException` จึงได้ `422 validation_failed` รูปแบบเดียวกับที่ use case ตอบ ไม่ใช่ 400 ของ ASP.NET
- ชื่อช่องใน `field` เป็น snake_case ตามที่ client ส่งมา
- ข้อความจากตัวผูกค่าที่ว่าง ถูกแทนด้วย "ข้อมูลไม่ถูกต้อง" เพื่อไม่ให้ชื่อ type ของ .NET หลุดออกไป
- รายการแบบ cursor รับ `limit` ได้ 1 ถึง 100 ค่าเริ่มต้น 25 (`CursorPageRequest.SafeLimit`) ค่าที่เกินถูกตัด ไม่ error
- cursor เป็น base64url ของ `เวลา|รหัส` อ่านได้แต่ไม่ใช่ความลับ ทุก query ใส่ตัวกรองสิทธิ์ซ้ำทุกหน้า cursor ที่ถอดไม่ได้ตอบ `422` ที่ช่อง `cursor`
- คิวรีผ่าน EF Core แบบ parameter มี SQL ดิบ 2 จุด คือ advisory lock ของเลขคิว (`QueueRepositories.cs`) และเลขเอกสาร (`DocumentSequence.cs`) ค่าจากผู้ใช้ผูกเป็น parameter ส่วนชื่อตารางและคอลัมน์ที่ต่อเข้า SQL เป็นค่าคงที่ในโค้ดทุกจุดที่เรียก

ตัวอย่างค่าที่ validator จำกัด

| ช่อง | กติกา |
|---|---|
| อีเมลตอนเข้าสู่ระบบและเชิญ | รูปแบบอีเมล ยาวไม่เกิน 256 |
| รหัสผ่าน | 8 ถึง 256 ตัวอักษร ไม่มีกฎบังคับตัวอักษรพิเศษ |
| รหัส 2FA | ตัวเลข 6 ถึง 8 หลัก |
| challenge, token คำเชิญ | ไม่เกิน 128 ตัวอักษร |
| เหตุผลการยกเลิกนัด | ไม่เกิน 500 ตัวอักษร |

**ยังไม่มีการจำกัดขนาด body เอง** ใช้ค่าเริ่มต้นของ Kestrel และ ASP.NET Core

### ข้อความที่ขึ้นหน้าเว็บสาธารณะ

บทความที่พนักงานเขียน ([contract-extensions.md § 26](../contract-extensions.md)) เป็นข้อความเดียวที่คนในคลินิกเขียนแล้วคนนอกทุกคนเปิดอ่านได้ จึงตั้งใจไม่รับ HTML หรือ Markdown เลย

- เนื้อหาเป็นข้อความล้วน API ตอบเป็น JSON และหน้าเว็บเขียนแต่ละย่อหน้าเป็น text node ใน `<p>` (`ArticleBody`) ไม่มี `dangerouslySetInnerHTML` `<script>` ในบทความจึงแสดงเป็นตัวอักษร ไม่ถูกรัน มีเทสต์ยืนยันทั้งสองชั้น (`ArticleTextTests`, `StaffArticlesTests`)
- ก่อนเก็บ `ArticleText` ตัดอักขระควบคุม อักขระสลับทิศทาง (U+202A ถึง U+202E, U+2066 ถึง U+2069) BOM และ surrogate ที่ขาดครึ่ง ส่วน `<` และ `>` ไม่ถูกตัด เพราะไม่มีที่ไหนแปลเป็น markup
- slug รับแค่ตัวพิมพ์เล็กภาษาอังกฤษ ตัวเลข และขีดกลาง จึงพา `/`, `..` หรือ query string เข้าที่อยู่ของหน้าไม่ได้
- รูปปกรับแค่ JPEG, PNG และ WebP ไม่รับ SVG ซึ่งฝัง script ได้ ไฟล์ถูกส่งกลับด้วยชนิดที่ประกาศพร้อม `nosniff`

## Error envelope

ทุก error ไม่ว่าเกิดที่ middleware หรือใน use case ออกผ่าน `ApiErrorWriter` จุดเดียว client จึงอ่านรูปแบบเดียวเสมอ

```json
{
  "error": {
    "code": "insufficient_scope",
    "message": "สิทธิ์ไม่เพียงพอสำหรับการดำเนินการนี้",
    "message_en": "The token is missing one or more required scopes.",
    "field": null,
    "request_id": "req_8c41d02e9f7a",
    "details": { "missing_scopes": ["queue:call"] }
  }
}
```

| สถานะ | `code` | เมื่อไร |
|---|---|---|
| 400 | `idempotency_key_required` | endpoint ที่บังคับ key แต่ไม่ได้ส่ง |
| 401 | `unauthenticated` | ไม่มี token token ผิดหรือหมดอายุ รหัสผ่านหรือรหัส 2FA ผิด |
| 403 | `insufficient_scope` | ขาด scope บอกใน `details.missing_scopes` |
| 403 | `forbidden` | ไม่ผ่าน 2FA หรือไม่มีสิทธิ์กับข้อมูลแถวนั้น |
| 404 | `not_found` | ไม่มีอยู่ หรือมีแต่ผู้เรียกไม่ควรรู้ |
| 409 | `version_conflict`, `state_conflict`, `idempotency_key_reused`, `request_in_flight` และรหัสเฉพาะเรื่อง | ข้อมูลชนกัน |
| 422 | `validation_failed`, `slot_unavailable` | input ผิด |
| 423 | `record_locked` | เวชระเบียนลงนามแล้ว |
| 429 | `rate_limited` | ชนเพดาน |
| 500 | `internal_error` | ข้อผิดพลาดที่ไม่คาดคิด |

กติกาที่ต้องรักษา

- **error ที่ไม่คาดคิดไม่หลุดอะไรออกไป** `ApiExceptionHandler` ตอบเพียง `internal_error` กับ `request_id` ส่วน exception เต็มอยู่ใน log ใน API ที่ถือเวชระเบียน ข้อความ exception ที่ส่งกลับไปคือการเปิดเผยข้อมูล
- `NotFoundException` ไม่ส่งรหัสที่ถูกขอกลับไป `ForbiddenException` ไม่ส่งเหตุผลภายในกลับไป (มีเทสต์ทั้งสองข้อ) ตอนนี้ log ก็บันทึกเพียงข้อความทั่วไป ไม่ได้บันทึกเหตุผลภายในด้วย
- `401` หมายถึง token ใช้ไม่ได้ ให้ลอง refresh ส่วน `403` หมายถึง token ใช้ได้แต่ไม่มีสิทธิ์ ถ้าปนกัน หน้าเว็บจะ refresh วน
- JWT ที่ผิดรูปหรือหมดอายุก็ได้ envelope เดียวกัน เพราะ `OnChallenge` ของ JwtBearer เขียนผ่าน `ApiErrorWriter`
- client ที่ตัดการเชื่อมต่อเอง ไม่ถูกบันทึกเป็น error
- ข้อความภาษาไทยไม่ถูก escape เป็น `\uXXXX`

### Request id

- ถ้า client ส่ง `X-Request-Id` มา ระบบใช้ค่านั้น ตัดเหลือ 64 ตัวอักษร และเก็บเฉพาะ `A-Z a-z 0-9 - _` กันการปลอมบรรทัด log และการแยก header
- ถ้าไม่ส่ง ระบบสร้าง `req_` ตามด้วยเลขฐานสิบหก 12 ตัว
- ใส่กลับใน header ของทุกคำตอบ รวมถึงคำตอบ error และอยู่ใน `error.request_id` กับ audit log

## การจำกัดคำขอ

`Extensions/RateLimitingExtensions.cs` ใช้ fixed window ไม่มีคิว ส่วนใหญ่ช่วงละ 1 นาที สอง policy ของการจองโดยไม่ต้องสมัครก่อน และ policy ของการตั้งจอห้องรอ ช่วงละ 1 ชั่วโมง

| policy | เพดาน | ใช้กับ |
|---|---|---|
| global | 600 ครั้งต่อนาที (`RateLimits:PerTokenPerMinute`) | ทุก endpoint |
| `messaging` | 60 ครั้งต่อนาที (`RateLimits:MessagingPerMinute`) | `POST /staff/notifications/send` และ `POST /staff/threads/{id}/messages` |
| API key | 600 ครั้งต่อนาที (`RateLimits:PerApiKeyPerMinute` ค่าเริ่มต้นเท่ากับ global) | ทุก endpoint เมื่อเรียกด้วย API key แยกถังต่อ key ไม่ใช้ถังของผู้สร้าง |
| `guest-hold` | 30 ครั้งต่อชั่วโมงต่อ IP (`RateLimits:GuestHoldsPerIpPerHour`) | `POST /booking/guest/start` นับตาม IP เสมอ แม้มี token (ส่วนขยาย § 27) |
| `guest-verify` | 60 ครั้งต่อชั่วโมงต่อ IP (`RateLimits:GuestVerifiesPerIpPerHour`) | `POST /booking/guest/verify` (ส่วนขยาย § 27) |
| `display-claim` | 30 ครั้งต่อชั่วโมงต่อ IP (`RateLimits:DisplayClaimsPerIpPerHour`) | `POST /queue/displays/claim` นับทุกครั้งที่ลอง ถูกหรือผิด (ส่วนขยาย § 30) |
| จอห้องรอ | 120 ครั้งต่อนาที (`RateLimits:PerDisplayPerMinute`) | ทุกคำขอที่ใช้ token ของจอ แยกถังต่อจอ ไม่ใช้ถังตาม IP (ส่วนขยาย § 30) |

- แยกถังตาม `sub` ของ token เพราะทั้งคลินิกมักออกอินเทอร์เน็ตผ่าน IP เดียว ถ้านับตาม IP สคริปต์ตัวเดียวจะทำให้ทุกคนที่เคาน์เตอร์ใช้งานไม่ได้
- คำขอที่ไม่มี token ใช้ IP ของ connection
- ชนเพดานตอบ `429 rate_limited` พร้อม header `Retry-After` หน้าเว็บแสดง "พยายามเข้าสู่ระบบบ่อยเกินไป กรุณารอสักครู่"
- ตัวนับอยู่ในหน่วยความจำ เริ่มนับใหม่ทุกครั้งที่เปิด API ใหม่

**ข้อจำกัด** API ไม่อ่าน `X-Forwarded-For` และหน้าเว็บก็ไม่ส่ง IP ของเบราว์เซอร์ต่อ ทุกคำขอเข้าสู่ระบบจึงนับเป็น IP ของเซิร์ฟเวอร์ Next.js ถังเดียว 600 ครั้งต่อนาทีสำหรับทั้งคลินิก เพดาน `guest-hold` และ `guest-verify` ก็เป็นถังเดียวของทุกคนที่จองผ่านเว็บด้วยเหตุผลเดียวกัน และ `display-claim` เป็นถังเดียวของทุกทีวีที่ตั้งผ่านเว็บ คนที่ลองรหัสผิดจนเต็มทำให้ตั้งจอใหม่ไม่ได้จนครบชั่วโมง จอที่ตั้งไว้แล้วไม่กระทบ ค่าเริ่มต้นจึงตั้งให้พอสำหรับทั้งคลินิก ไม่ใช่ต่อคน ไม่มีเพดานแยกสำหรับการเข้าสู่ระบบด้วยรหัสผ่านหรือ 2FA ส่วนรหัส OTP ของลูกค้ามีเพดานของตัวเองที่ไม่ขึ้นกับ IP คือ `RateLimits:OtpPerPhonePer10Minutes` (3 ครั้งต่อเบอร์ใน 10 นาที) นับจากตาราง `phone_verifications` เกินแล้วตอบ `429 rate_limited` และรหัสหนึ่งตัวเดาผิดได้ไม่เกิน 5 ครั้ง ลิงก์รีเซ็ตรหัสผ่านไม่เกิน 3 ลิงก์ต่อบัญชีใน 10 นาที เกินแล้วตอบเหมือนเดิมแต่ไม่ส่ง

## CORS

- อนุญาตเฉพาะ origin ใน `Cors:AllowedOrigins` ค่าเริ่มต้นใน `appsettings.json` เป็นรายการว่าง `appsettings.Development.json` และ `docker-compose.yml` ใส่ `http://localhost:3000`
- อนุญาตทุก header ทุก method และส่ง credential ได้
- เปิดให้ client อ่าน header `ETag`, `X-Request-Id` และ `Idempotent-Replay`

ในทางปฏิบัติเบราว์เซอร์ไม่ได้เรียก API ตรงเลย แม้แต่การอ่านข้อมูลสาธารณะก็ทำฝั่งเซิร์ฟเวอร์ผ่าน `publicApi()` ค่า `NEXT_PUBLIC_API_BASE_URL` ยังถูกประกาศไว้แต่ไม่มีโค้ดฝั่งเบราว์เซอร์ใช้

## Idempotency

contract กำหนดว่า POST ที่สร้างเงิน นัด หรือคิว ต้องมี `Idempotency-Key` endpoint ที่ติด `[Idempotent]` ตอนนี้มี

| Endpoint | scope |
|---|---|
| `POST /staff/appointments` | `appointment:write` |
| `POST /portal/appointments` | `portal` |
| `POST /appointments/{id}/check-in` | ตรวจใน use case |
| `POST /staff/queue/tickets` | `queue:write` |
| `POST /staff/consultations` | `consult:write` |
| `POST /staff/consultations/{id}/treatments` | `consult:write` |
| `POST /staff/consultations/{id}/prescriptions` | `rx:write` |
| `POST /staff/consultations/{id}/close` | `record:sign` |
| `POST /staff/invoices` | `invoice:write` |
| `POST /staff/invoices/{id}/payments` | `payment:collect` |
| `POST /staff/invoices/{id}/refunds` | `payment:refund` |
| `POST /staff/inventory/movements` | `inventory:write` |
| `POST /staff/inventory/purchase-orders` | `inventory:purchase` |

`IdempotencyMiddleware` กับ `EfIdempotencyStore` ทำงานดังนี้

- ไม่ส่ง key ตอบ `400 idempotency_key_required` ไม่ปล่อยผ่านแบบไม่ป้องกัน
- key ผูกกับ `(key, endpoint, ผู้เรียก)` สองบัญชีใช้ key เดียวกันได้โดยไม่ชนกัน
- เก็บ SHA-256 ของ body ส่ง key เดิมกับ body ต่างกันตอบ `409 idempotency_key_reused` แทนการส่งผลเก่า
- การจองคือ `INSERT` ที่ชน primary key ได้ คำขอซ้ำที่มาพร้อมกันจึงทำจริงได้ครั้งเดียว อีกคำขอได้ `409 request_in_flight`
- ส่งซ้ำหลังเสร็จแล้วได้สถานะและ body เดิม พร้อม header `Idempotent-Replay: true`
- คำตอบ 5xx หรือ exception คืนการจองทันที client จึงลองซ้ำด้วย key เดิมได้
- response ที่ใหญ่เกิน 256 KB ยังเก็บ แต่เขียน warning

**ข้อจำกัด** แต่ละแถวมี `expires_at` 24 ชั่วโมง แต่ไม่มีงานใดลบแถวที่หมดอายุ และการค้นหาไม่ได้ดูวันหมดอายุ key จึงถูกจำตลอดไป คอลัมน์ key รับได้ 80 ตัวอักษร middleware ไม่ได้ตรวจความยาว จากการอ่านโค้ด key ที่ยาวกว่านั้นจะทำให้ `INSERT` ล้ม แล้ว `TryClaimAsync` ตีความว่าแพ้การแข่งขันแต่หาแถวไม่เจอ คำขอจึงทำงานต่อโดยไม่ได้รับการป้องกัน ยังไม่มีเทสต์ครอบกรณีนี้

## If-Match และ ETag

ข้อมูลที่แก้ได้มี `version` (ค่า `xmin` ของ PostgreSQL) ส่งเป็น `ETag` แบบมีเครื่องหมายคำพูด เช่น `"8412"` client ส่งกลับใน `If-Match`

- ค่าไม่ตรงตอบ `409 version_conflict` พร้อม `expected_version` และ `current_version`
- header ที่อ่านไม่ออก หรือเป็น `*` ถือว่าไม่ได้ส่ง (`Http/IfMatch.cs`)
- use case ส่วนใหญ่ใช้ `ConcurrencyGuard.Ensure` คือตรวจเมื่อส่งมาเท่านั้น มีเพียง `PATCH /staff/appointments/{id}` ที่ใช้ `EnsureRequired` บังคับให้ส่ง
- ถ้าชนกันในระดับ EF โดยที่ use case ไม่ได้ตรวจเอง ก็ยังได้ 409 พร้อม warning ใน log ให้ไปปิดช่องนั้น

## Header และ HTTPS

| เรื่อง | สถานะ |
|---|---|
| HTTPS redirect และ HSTS | เปิดเมื่อไม่ใช่ `Development` (ค่า HSTS เป็นค่าเริ่มต้นของ ASP.NET Core) |
| OpenAPI | เปิดเฉพาะ `Development` |
| `Content-Security-Policy` | ไม่มี ทั้ง API และหน้าเว็บ |
| `X-Frame-Options` หรือ `frame-ancestors` | ไม่มี |
| `X-Content-Type-Options`, `Referrer-Policy` | ไม่มี |
| `Cache-Control` ของคำตอบ API | ไม่ได้ตั้งเอง ยกเว้นสตรีมคิวที่เป็น `no-cache` |
| `AllowedHosts` | `*` |

`next.config.ts` ไม่มี `headers()` ใดเลย ข้อดีที่มีอยู่คือหน้าเว็บไม่ใช้ `dangerouslySetInnerHTML` และ token อยู่ในคุกกี้ httpOnly script ที่แทรกเข้ามาจึงอ่าน token ไม่ได้ แต่ยังสั่งงานในนามผู้ใช้ผ่านหน้าเว็บได้ และยังอ่าน `challenge` ระหว่างเข้าสู่ระบบได้

## การเรียก API จากหน้าเว็บ

- ESLint ห้ามเรียก `fetch` ตรงนอก `src/lib/api/` (`eslint.config.mjs`) ทุกการเรียกผ่าน `createHttpClient` ที่ใส่ token, `Idempotency-Key` และ `If-Match` ให้ที่เดียว
- `auth-transport.ts` เป็นไฟล์เดียวที่จับ refresh token ใช้ได้เฉพาะฝั่งเซิร์ฟเวอร์
- `API_BASE_URL` เป็นค่าฝั่งเซิร์ฟเวอร์ ชี้ไปที่ API ในเครือข่ายภายในได้

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| ลำดับ pipeline, CORS | `backend/src/HappyPaws.Api/Program.cs` |
| error envelope | `backend/src/HappyPaws.Api/Errors/`, `backend/src/HappyPaws.Application/Common/Errors/` |
| request id | `backend/src/HappyPaws.Api/Middleware/RequestContextMiddleware.cs`, `backend/src/HappyPaws.Api/Http/RequestContext.cs` |
| rate limit | `backend/src/HappyPaws.Api/Extensions/RateLimitingExtensions.cs` |
| idempotency | `backend/src/HappyPaws.Api/Idempotency/`, `backend/src/HappyPaws.Infrastructure/Persistence/Idempotency/` |
| If-Match | `backend/src/HappyPaws.Api/Http/IfMatch.cs`, `backend/src/HappyPaws.Application/Common/Concurrency/ConcurrencyGuard.cs` |
| การตรวจ input | `backend/src/HappyPaws.Application/Common/Validation/RequestValidator.cs`, `backend/src/HappyPaws.Api/Errors/ModelStateErrors.cs` |
| หน้าเว็บ | `frontend/src/lib/api/`, `frontend/eslint.config.mjs`, `frontend/next.config.ts` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/ErrorEnvelopeTests.cs`, `IdempotencyTests.cs` |
