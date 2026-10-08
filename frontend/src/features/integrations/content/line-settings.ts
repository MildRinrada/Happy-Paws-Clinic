/** The LINE settings page. */
export const lineSettingsPage = {
  metaTitle: 'ตั้งค่า LINE · Happy Paws Clinic',
  title: 'LINE Official Account',
  subtitle: 'บัญชี LINE ของคลินิกเอง ใช้ส่งการแจ้งเตือนอย่างเดียว ไม่ใช้เข้าสู่ระบบหรือยืนยันตัวตน',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ตั้งค่า LINE',

  status: {
    heading: 'สถานะ',
    canPush: 'ส่งข้อความได้',
    cannotPush: 'ยังส่งข้อความไม่ได้',
    canVerify: 'ตรวจลายเซ็น webhook ได้',
    cannotVerify: 'ยังตรวจลายเซ็น webhook ไม่ได้',
    token: 'Channel access token',
    secret: 'Channel secret',
    sources: {
      database: 'ตั้งจากหน้านี้',
      configuration: 'ตั้งจาก environment ของเซิร์ฟเวอร์',
      none: 'ยังไม่ได้ตั้ง',
    } as Record<string, string>,
    webhook: 'Webhook URL ที่ต้องใส่ใน LINE Developers',
    webhookHint: 'ต่อท้าย URL ของ API ที่เปิดให้ LINE เข้าถึงได้ เช่น https://api.example.com/v1',
  },

  credentials: {
    heading: 'ข้อมูลช่องทาง',
    channelId: 'Channel ID',
    channelIdHint: 'ตัวเลขจากหน้า Basic settings ของช่องทาง Messaging API',
    channelSecret: 'Channel secret',
    channelAccessToken: 'Channel access token (long-lived)',
    storedPlaceholder: 'ตั้งไว้แล้ว เว้นว่างเพื่อใช้ค่าเดิม',
    emptyPlaceholder: 'ยังไม่ได้ตั้ง',
    secretHint: 'ระบบไม่แสดงค่าที่บันทึกแล้ว และเก็บในฐานข้อมูลแบบยังไม่เข้ารหัส ถ้าตั้งผ่าน environment ได้ ควรใช้ทางนั้น',
    clearStored: 'ลบค่าที่บันทึกไว้ในหน้านี้ (กลับไปใช้ค่าจาก environment ถ้ามี)',
  },

  sending: {
    heading: 'การส่ง',
    enabled: 'เปิดส่งการแจ้งเตือนทาง LINE',
    enabledHint: 'ปิดแล้วข้อความทุกชนิดจะไปทาง SMS หรืออีเมลแทน',
    quota: 'โควตาประกาศต่อเดือน (ข้อความ)',
    quotaHint: 'นับเฉพาะประกาศที่ส่งสำเร็จ ข้อความเรียกคิวและใบเสร็จไม่นับ แต่ยังกินโควตาของ LINE เอง',
    types: 'ชนิดข้อความที่ส่งทาง LINE',
    typesHint: 'ชนิดที่ปิดจะส่งทาง SMS หรืออีเมลแทน',
  },
};
