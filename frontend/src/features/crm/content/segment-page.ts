/** `/crm/segments`: picking out a group of customers by conditions. */
export const segmentPage = {
  title: 'กลุ่มลูกค้า',
  subtitle:
    'ตั้งเงื่อนไขเพื่อคัดลูกค้าเป็นกลุ่ม เช่น เจ้าของแมวที่ไม่มาเกิน 180 วัน แล้วส่งประกาศ LINE ถึงกลุ่มนั้น ระบบยังไม่มีหน้ารายการกลุ่มที่สร้างไว้ จึงต้องส่งประกาศจากลิงก์ที่ขึ้นหลังสร้างกลุ่ม ลิงก์นั้นพารหัสกลุ่มไปให้เอง',

  name: 'ชื่อกลุ่ม',
  namePlaceholder: 'เช่น แมวที่ไม่มาเกิน 6 เดือน',
  criteriaHeading: 'เงื่อนไข',
  criteriaHint: 'ต้องมีอย่างน้อย 1 เงื่อนไข ลูกค้าต้องตรงทุกเงื่อนไขที่กรอกจึงอยู่ในกลุ่ม',

  species: 'ชนิดสัตว์',
  speciesHint: 'ลูกค้าที่มีสัตว์ชนิดใดชนิดหนึ่งที่เลือก ไม่เกิน 10 ชนิด',
  speciesQuick: ['สุนัข', 'แมว', 'กระต่าย', 'นก'],
  speciesOther: 'ชนิดอื่น',
  speciesOtherPlaceholder: 'พิมพ์ชนิดสัตว์',
  speciesAdd: 'เพิ่ม',
  speciesRemove: (name: string) => `เอา ${name} ออก`,

  inactiveDays: 'ไม่มาเกิน (วัน)',
  inactiveHint: 'เคยมาแล้ว แต่ครั้งล่าสุดนานกว่านี้',
  quickDays: [90, 180, 365],
  quickDay: (days: number) => `${days} วัน`,
  visitedWithinDays: 'มาภายใน (วันที่ผ่านมา)',
  visitedWithinHint: 'ถ้ากรอกคู่กับไม่มาเกิน ตัวเลขนี้ต้องมากกว่า',
  minSpend: 'ยอดใช้จ่ายขั้นต่ำ (บาท)',
  minSpendHint: 'ยอดชำระหักเงินคืนใน 365 วันที่ผ่านมา',
  tiers: 'ระดับสมาชิก',
  activePackage: 'แพ็กเกจที่ใช้ได้',
  activePackageOptions: { any: 'ไม่สนใจ', yes: 'มีแพ็กเกจ', no: 'ไม่มีแพ็กเกจ' },
  branch: 'สาขา',
  branchHint: 'เฉพาะลูกค้าที่เคยมาสาขานี้ และนับการมากับยอดใช้จ่ายที่สาขานี้เท่านั้น',
  allBranches: 'ทุกสาขา',

  submit: 'สร้างกลุ่ม',
  saving: 'กำลังคัดลูกค้า…',

  errors: {
    name: 'ต้องตั้งชื่อกลุ่ม',
    inactiveDays: 'กรอกจำนวนวัน 1 ถึง 3,650',
    visitedWithinDays: 'กรอกจำนวนวัน 1 ถึง 3,650 และต้องมากกว่าช่องไม่มาเกิน',
    minSpend: 'กรอกยอดเงินมากกว่า 0 เช่น 3000',
    noCriteria: 'กรอกอย่างน้อย 1 เงื่อนไข',
  },

  result: {
    heading: (name: string) => `สร้างกลุ่ม ${name} แล้ว`,
    customers: 'ลูกค้าที่ตรงเงื่อนไขตอนนี้',
    reachable: 'ส่ง LINE ถึงได้',
    reachableHint: 'คือลูกค้าที่ยินยอมรับข่าวสาร และมี LINE ในข้อมูลลูกค้า ตัวเลขอาจเปลี่ยนตอนส่งจริง เพราะระบบคัดใหม่ตอนส่ง',
    count: (count: number) => `${count.toLocaleString('th-TH')} คน`,
    broadcast: 'ส่งประกาศ LINE ถึงกลุ่มนี้',
    noneReachable: 'ยังไม่มีลูกค้าในกลุ่มนี้ที่ส่ง LINE ถึงได้',
    another: 'สร้างกลุ่มใหม่',
  },
};
