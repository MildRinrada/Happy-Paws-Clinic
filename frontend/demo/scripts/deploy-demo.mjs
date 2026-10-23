// Builds the demo and pushes it to Clinic-Web-Demo, the public repo Vercel deploys from, kept next to this project folder.
// That repo gets only the built site (what every visitor's browser downloads anyway); the source stays here.
import { execFileSync, execSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, readdirSync, rmSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const frontend = resolve(here, '../..');
const site = resolve(frontend, '../../Clinic-Web-Demo');
const git = (args, cwd = site) => execFileSync('git', args, { cwd, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
// fs.cpSync crashes Node on Windows for some non-ASCII paths; a plain walk does not.
const copyDir = (from, to) => {
  mkdirSync(to, { recursive: true });
  for (const entry of readdirSync(from, { withFileTypes: true })) {
    if (entry.isDirectory()) copyDir(join(from, entry.name), join(to, entry.name));
    else copyFileSync(join(from, entry.name), join(to, entry.name));
  }
};

if (!existsSync(join(site, '.git'))) {
  console.error(
    `ไม่พบ repo ของเว็บเดโมที่ ${site}\n` +
      'clone มาไว้ข้างโฟลเดอร์ของระบบก่อน (ครั้งแรกครั้งเดียว):\n' +
      '  git clone https://github.com/MildRinrada/Clinic-Web-Demo.git',
  );
  process.exit(1);
}

execSync('npm run build:demo', { cwd: frontend, stdio: ['ignore', 'inherit', 'inherit'] });

// The site repo is rebuilt from scratch each time, so a file removed from the demo leaves the site too.
for (const name of readdirSync(site)) if (name !== '.git') rmSync(join(site, name), { recursive: true, force: true });
copyDir(join(here, 'demo-site'), site);
copyDir(join(frontend, 'demo', 'dist'), join(site, 'public'));

if (!git(['status', '--porcelain'])) {
  console.log('\nเว็บเดโมเหมือนฉบับที่ขึ้นไปแล้ว ไม่มีอะไรต้องส่ง');
  process.exit(0);
}

let from = '';
try {
  from = ` from ${git(['describe', '--always', '--dirty'], frontend)}`;
} catch {
  // The product folder is not a git repo; the commit just goes without its source revision.
}
git(['add', '-A']);
git(['commit', '-m', `Demo build${from}`]);
execFileSync('git', ['push', '-u', 'origin', 'HEAD'], { cwd: site, stdio: ['ignore', 'inherit', 'inherit'] });
console.log('\nส่งขึ้น GitHub แล้ว Vercel จะ deploy ให้เองภายในราวหนึ่งนาที');
