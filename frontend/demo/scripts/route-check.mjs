// Visits demo pages in headless Edge (or Chrome) and reports what went wrong on each: console
// errors, uncaught exceptions, API calls the mock does not answer, and pages that ended on the
// demo's error or not-found screen.
//
//   node demo/scripts/route-check.mjs [--base http://localhost:5199] [--as owner|vet|reception|customer|none]
//                                     [--shots <dir>] [--wait 1500] [--json] path [path …]
//   node demo/scripts/route-check.mjs --list routes.txt      (one path per line, "# role" lines switch role)
//   A path "list>>css" opens the list page, then the first link matching css (seeded ids differ per browser).
//
// Needs Node 22 (global WebSocket). Exit code 1 when any page had a problem.

import { spawn } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const args = process.argv.slice(2);
const option = (name, fallback) => {
  const index = args.indexOf(`--${name}`);
  if (index < 0) return fallback;
  const [value] = args.splice(index, 2).slice(1);
  return value;
};
const flag = (name) => {
  const index = args.indexOf(`--${name}`);
  if (index < 0) return false;
  args.splice(index, 1);
  return true;
};

const base = option('base', 'http://localhost:5199');
let role = option('as', 'none');
const shots = option('shots', '');
const wait = Number(option('wait', '2000'));
const listFile = option('list', '');
const asJson = flag('json');
// A real browser window (off-screen) instead of headless, which draws no scrollbars inside elements.
const headed = flag('headed');
const width = Number(option('width', '1366'));
const height = Number(option('height', '900'));
// An expression evaluated on each page once it has settled; its value is printed.
const probe = option('probe', '');
// "x,y": where the pointer rests for the screenshot, for anything drawn only on hover.
const mouse = option('mouse', '');

/** @type {{ role: string, path: string }[]} */
const visits = [];
if (listFile) {
  for (const line of readFileSync(listFile, 'utf8').split(/\r?\n/)) {
    const text = line.trim();
    if (!text) continue;
    if (text.startsWith('#')) {
      role = text.slice(1).trim() || 'none';
      continue;
    }
    visits.push({ role, path: text });
  }
}
for (const path of args) visits.push({ role, path });
if (!visits.length) visits.push({ role, path: '/' });

const browsers = [
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
  'C:/Program Files/Microsoft/Edge/Application/msedge.exe',
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  '/usr/bin/google-chrome',
  '/usr/bin/chromium',
];
const browser = browsers.find((path) => existsSync(path));
if (!browser) throw new Error('No Edge or Chrome found.');

const port = 9300 + Math.floor(Math.random() * 500);
const profile = mkdtempSync(join(tmpdir(), 'hp-demo-check-'));
const child = spawn(
  browser,
  [
    ...(headed ? ['--window-position=-3000,0'] : ['--headless=new']),
    `--remote-debugging-port=${port}`,
    `--user-data-dir=${profile}`,
    '--no-first-run',
    '--no-default-browser-check',
    '--disable-extensions',
    `--window-size=${width},${height}`,
    'about:blank',
  ],
  { stdio: 'ignore' },
);

async function json(url, init) {
  for (let attempt = 0; attempt < 50; attempt += 1) {
    try {
      const response = await fetch(url, init);
      return await response.json();
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 200));
    }
  }
  throw new Error(`Browser did not answer at ${url}`);
}

const target = await json(`http://127.0.0.1:${port}/json/new?about:blank`, { method: 'PUT' });
const socket = new WebSocket(target.webSocketDebuggerUrl);
await new Promise((resolve, reject) => {
  socket.onopen = resolve;
  socket.onerror = reject;
});

let nextId = 1;
const pending = new Map();
const handlers = new Set();
socket.onmessage = (message) => {
  const data = JSON.parse(String(message.data));
  if (data.id && pending.has(data.id)) {
    const { resolve, reject } = pending.get(data.id);
    pending.delete(data.id);
    if (data.error) reject(new Error(data.error.message));
    else resolve(data.result);
  } else if (data.method) {
    for (const handler of handlers) handler(data);
  }
};
const send = (method, params = {}) =>
  new Promise((resolve, reject) => {
    const id = nextId++;
    pending.set(id, { resolve, reject });
    socket.send(JSON.stringify({ id, method, params }));
  });

await send('Page.enable');
await send('Runtime.enable');
await send('Log.enable');
await send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: width < 600 });

let problems = [];
handlers.add(({ method, params }) => {
  if (method === 'Runtime.consoleAPICalled') {
    const text = params.args.map((arg) => arg.value ?? arg.description ?? '').join(' ');
    if (params.type === 'error' || (params.type === 'warning' && text.includes('[mock] unhandled'))) {
      problems.push(`${params.type}: ${text.slice(0, 400)}`);
    }
  } else if (method === 'Runtime.exceptionThrown') {
    const details = params.exceptionDetails;
    problems.push(`exception: ${(details.exception?.description ?? details.text).slice(0, 400)}`);
  } else if (method === 'Log.entryAdded' && params.entry.level === 'error' && !/favicon|fonts\.g/.test(params.entry.url ?? '')) {
    problems.push(`log: ${params.entry.text.slice(0, 300)} ${params.entry.url ?? ''}`);
  }
});

const evaluate = async (expression) =>
  (await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true })).result.value;

async function settle() {
  // Wait for the page to stop fetching: the boot screen gone and nothing loading for a moment.
  const deadline = Date.now() + 15_000;
  await new Promise((resolve) => setTimeout(resolve, wait));
  while (Date.now() < deadline) {
    const busy = await evaluate(`Boolean(document.querySelector('[aria-busy="true"], #root .animate-pulse')) || !document.querySelector('#root')?.children.length`);
    if (!busy) break;
    await new Promise((resolve) => setTimeout(resolve, 300));
  }
}

let currentRole = null;
const results = [];
if (shots) mkdirSync(shots, { recursive: true });

await send('Page.navigate', { url: `${base}/` });
await settle();
// The guide opens by itself on a first visit; a check has read it already.
await evaluate(`localStorage.setItem('hp-demo-guided', '1')`);

for (const visit of visits) {
  if (visit.role !== currentRole) {
    await evaluate(`window.__demo ? window.__demo.signInAs(${JSON.stringify(visit.role)}) : null`);
    currentRole = visit.role;
  }
  problems = [];
  // "list>>css": open the list, then the first link matching css (for seeded ids unknown beforehand).
  const [listPath, follow] = visit.path.split('>>');
  await send('Page.navigate', { url: `${base}${listPath}` });
  await settle();
  if (follow) {
    const href = await evaluate(follow.startsWith('js:') ? follow.slice(3) : `document.querySelector(${JSON.stringify(follow)})?.getAttribute('href') ?? null`);
    if (href) {
      await send('Page.navigate', { url: `${base}${href}` });
      await settle();
    } else problems.push(`no link for ${follow}`);
  }
  const state = await evaluate(`({
    url: location.pathname + location.search,
    state: document.querySelector('[data-demo-state]')?.getAttribute('data-demo-state') ?? null,
    title: document.title,
    heading: document.querySelector('main h1, h1')?.textContent?.trim().slice(0, 80) ?? null,
    nextError: document.body.innerText.includes('เกิดข้อผิดพลาด') || document.body.innerText.includes('Something went wrong'),
  })`);
  // Before the screenshot, so a probe may also scroll to what it wants pictured.
  if (probe) state.probe = await evaluate(probe);
  if (mouse) {
    const [x, y] = mouse.split(',').map(Number);
    await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x, y });
    await new Promise((resolve) => setTimeout(resolve, 300));
  }
  if (shots) {
    const shot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    const name = `${visit.role}${visit.path.replace(/[^a-z0-9]+/gi, '_')}`.slice(0, 120);
    writeFileSync(join(shots, `${name}.png`), Buffer.from(shot.data, 'base64'));
  }
  const failed = problems.length > 0 || state.state === 'error' || state.state === 'not-found';
  results.push({ ...visit, ...state, problems, failed });
  if (!asJson) {
    console.log(`${failed ? 'FAIL' : 'ok  '} [${visit.role}] ${visit.path} → ${state.url}  ${state.state ?? ''} ${state.heading ? `“${state.heading}”` : ''}`);
    for (const problem of problems) console.log(`       ${problem}`);
    if (probe) console.log(`       probe: ${JSON.stringify(state.probe)}`);
  }
}

if (asJson) console.log(JSON.stringify(results, null, 2));
const failures = results.filter((result) => result.failed).length;
if (!asJson) console.log(`\n${results.length - failures}/${results.length} pages clean`);

socket.close();
child.kill();
process.exit(failures ? 1 : 0);
