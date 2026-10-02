# การเข้าสู่ระบบของลูกค้า

ลูกค้าสมัครด้วยเบอร์โทรที่พิสูจน์ด้วยรหัส OTP แล้วเข้าสู่ระบบด้วยรหัส OTP หรือรหัสผ่าน ลืมรหัสผ่านขอลิงก์ตั้งรหัสใหม่ได้ ซึ่งพนักงานก็ใช้ได้เหมือนกัน

ส่วนที่ contract ไม่ได้เขียนไว้และเราตีความเอง อยู่ใน [ข้อสมมติ § 39](../assumptions.md) token เซสชัน และการเข้าสู่ระบบของพนักงานอยู่ใน [การยืนยันตัวตน](../authentication.md)

> **ยังไม่มีตัวส่ง SMS และอีเมลจริง** รหัส OTP และลิงก์รีเซ็ตรหัสผ่านไม่ได้ส่งถึงใคร ในเครื่อง dev เปิด `Auth:SignIn:LogUndeliveredSecrets` ไว้ รหัสจึงอยู่ใน log ของ API ในบรรทัดที่ขึ้นต้นว่า `Development only. OTP for` ห้ามเปิดค่านี้ในเครื่องที่ log ถูกส่งออกไปที่อื่น

## Endpoint

| Method | Endpoint | ผู้เรียก | ทำอะไร |
|---|---|---|---|
| POST | `/auth/otp/request` | ไม่ต้องเข้าสู่ระบบ | ส่งรหัส 6 หลักไปที่เบอร์ |
| POST | `/auth/register` | ไม่ต้องเข้าสู่ระบบ | สมัครด้วยเบอร์และรหัส แล้วเข้าสู่ระบบทันที |
| POST | `/auth/login` | ไม่ต้องเข้าสู่ระบบ | เข้าสู่ระบบด้วยรหัส OTP หรือรหัสผ่าน |
| POST | `/auth/password/reset` | ไม่ต้องเข้าสู่ระบบ | ขอลิงก์ แล้วตั้งรหัสใหม่ |

## ขอรหัส OTP

```http
POST /v1/auth/otp/request
{ "phone": "081-234-5678", "purpose": "register" }
```

```json
202 { "expires_at": "2026-10-02T10:05:00+00:00" }
```

| กติกา | ค่า |
|---|---|
| `purpose` | `register` หรือ `login` รหัสของจุดประสงค์หนึ่งใช้กับอีกจุดประสงค์ไม่ได้ |
| รหัส | ตัวเลข 6 หลัก อายุ 5 นาที ใช้ได้ครั้งเดียว |
| เดาผิด | ได้ 5 ครั้งต่อรหัส ครั้งที่ 6 แม้ใส่ถูกก็ไม่ผ่าน ต้องขอรหัสใหม่ |
| ขอได้กี่ครั้ง | 3 ครั้งต่อเบอร์ใน 10 นาที เกินตอบ `429 rate_limited` |
| เบอร์ | อ่านเฉพาะตัวเลข `081-234-5678` กับ `0812345678` คือเบอร์เดียวกัน |

**คำตอบเหมือนกันเสมอ** ไม่ว่าเบอร์จะมีบัญชีหรือไม่ เพื่อไม่ให้ใช้ endpoint นี้ตรวจว่าใครเป็นลูกค้า รหัส `login` ส่งจริงเฉพาะเบอร์ที่มีบัญชี รหัส `register` ส่งทุกครั้ง

## สมัคร

```http
POST /v1/auth/register
{ "phone": "0812345678", "code": "482913", "display_name": "คุณสมใจ ใจดี", "password": "ไม่ใส่ก็ได้", "email": "ไม่ใส่ก็ได้" }
```

ได้ `201` พร้อม access token และคุกกี้ refresh เหมือนการเข้าสู่ระบบ

- **บัญชีผูกกับเรกคอร์ดลูกค้าที่เคาน์เตอร์สร้างไว้แล้ว** ถ้าเบอร์ตรงกัน ลูกค้าที่ลงทะเบียนที่คลินิกเมื่อปีที่แล้วจึงเห็นสัตว์และบิลเดิมทันที ถ้าไม่มีเรกคอร์ดเดิม ระบบสร้างใหม่
- คำเชิญจาก `POST /staff/customers/{id}/invite` ถูกทำเครื่องหมายว่ารับแล้ว
- รหัสผ่านไม่บังคับ ไม่ตั้งไว้ก็เข้าสู่ระบบด้วยรหัส OTP ทุกครั้ง ถ้าตั้ง ยาว 8 ถึง 256 ตัวอักษร
- เบอร์หรืออีเมลที่มีบัญชีแล้วตอบ `409 contact_taken` ระบบบอกเรื่องนี้หลังพิสูจน์เบอร์แล้วเท่านั้น
- รหัสผิดหรือหมดอายุตอบ `422` ที่ช่อง `code`
- บันทึก audit `auth.registered`

## เข้าสู่ระบบ

สองแบบ ส่งแบบใดแบบหนึ่ง

```http
POST /v1/auth/login
{ "phone": "0812345678", "code": "193847" }
```

```http
POST /v1/auth/login
{ "identifier": "0812345678", "password": "..." }
```

`identifier` เป็นอีเมลหรือเบอร์ก็ได้ คำตอบมี `capabilities` คือสิ่งที่ token ที่เพิ่งออกทำได้

```json
{
  "access_token": "...", "token_type": "Bearer", "expires_at": "...", "two_factor_satisfied": false,
  "capabilities": { "customer_id": "cus_...", "scopes": ["portal"], "branches": [] }
}
```

- บัญชีที่เป็นทั้งพนักงานและลูกค้าได้ทั้ง `staff_id` และ `customer_id` ใน token เดียว
- บัญชีที่มี 2FA ได้ `two_factor_required` และ `challenge` แทน แล้วต่อที่ `POST /auth/2fa/verify` เหมือนพนักงาน รหัส OTP พิสูจน์โทรศัพท์ ไม่ได้พิสูจน์แอป authenticator
- ผิดทุกแบบตอบ `401` ข้อความเดียวกัน ไม่บอกว่าบัญชีมีอยู่ไหม

## ลืมรหัสผ่าน

เรียก endpoint เดียวสองครั้ง แบบเดียวกับการรับคำเชิญพนักงาน

**ครั้งที่ 1** ขอลิงก์

```http
POST /v1/auth/password/reset
{ "identifier": "somjai@example.com" }
```

ได้ `202 { "status": "link_sent" }` เสมอ แม้ไม่มีบัญชีนั้น ลิงก์ไปทางอีเมลถ้าบัญชีมีอีเมล ไม่อย่างนั้นไปทาง SMS ลิงก์ชี้ไปหน้าเว็บตาม `Auth:SignIn:PasswordResetUrlTemplate` โดยวาง token ไว้หลัง `#` ขอได้ไม่เกิน 3 ลิงก์ต่อบัญชีใน 10 นาที

**ครั้งที่ 2** ตั้งรหัสใหม่

```http
POST /v1/auth/password/reset
{ "token": "...", "new_password": "..." }
```

ได้ `200 { "status": "reset" }`

- ลิงก์อายุ 30 นาที ใช้ได้ครั้งเดียว ลิงก์ผิด ใช้แล้ว หรือหมดอายุ ตอบ `422` ที่ช่อง `token` ข้อความเดียวกัน
- ทุกเซสชันของบัญชีถูกเพิกถอน เพราะเหตุผลที่คนรีเซ็ตรหัสผ่านมักเป็นว่ามีคนอื่นรู้รหัส
- ปัจจัยที่สองไม่ถูกแตะ การรีเซ็ตรหัสผ่านไม่ใช่ทางเลี่ยงแอป authenticator
- บันทึก audit `auth.password_reset`

## ลองบนเครื่องตัวเอง

1. เปิด API ตาม [เริ่มต้นใช้งาน](../getting-started.md)
2. `POST /v1/auth/otp/request` ด้วยเบอร์ใดก็ได้และ `purpose: "register"`
3. ดูรหัสใน Terminal ที่รัน API บรรทัด `Development only. OTP for 0812345678: 482913`
4. `POST /v1/auth/register` ด้วยเบอร์และรหัสนั้น

## ข้อจำกัดที่รู้อยู่

- ยังไม่มีตัวส่ง SMS และอีเมล
- ยังไม่มีหน้าเว็บสมัคร เข้าสู่ระบบ และตั้งรหัสใหม่ของลูกค้า
- รหัส OTP เก็บเป็น hash แต่มีแค่ล้านแบบ ถ้าฐานข้อมูลหลุดไปพร้อมรหัสที่ยังไม่หมดอายุ hash ไม่ได้กันการเดา สิ่งที่กันจริงคืออายุ 5 นาที
- แถวของรหัสที่หมดอายุไม่ถูกลบ จนกว่าจะมีงานเบื้องหลัง
- ไม่มีการล็อกบัญชีเมื่อใส่รหัสผ่านผิดซ้ำ ด่านเดียวคือ rate limit รวม

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| domain | `backend/src/HappyPaws.Domain/Identity/PhoneVerification.cs`, `PasswordResetRequest.cs`, `UserAccount.cs` (`CreateCustomer`) |
| use case | `backend/src/HappyPaws.Application/Identity/` (`RequestOtp`, `Register`, `Login`, `ResetPassword`, `CustomerSignIn.cs`) |
| ออก token พร้อม customer id | `backend/src/HappyPaws.Application/Identity/Sessions/SessionIssuer.cs` |
| ตัวส่งชั่วคราว | `backend/src/HappyPaws.Infrastructure/Identity/LoggingCustomerSignInSenders.cs` |
| endpoint | `backend/src/HappyPaws.Api/Controllers/Auth/AuthController.cs`, `OtpController.cs`, `PasswordController.cs` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Identity/CustomerSignInTests.cs` |
