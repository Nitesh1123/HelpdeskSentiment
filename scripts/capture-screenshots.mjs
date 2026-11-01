import fs from 'node:fs';
import path from 'node:path';
import { spawn } from 'node:child_process';

const edgePath = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const userDataDir = path.join(process.env.TEMP, 'edge-cdp-' + Date.now());
const screenshotsDir = path.resolve('docs', 'screenshots');

if (!fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true });
}

console.log('Launching headless Edge for automated evidence screenshot capture...');
const edgeProc = spawn(edgePath, [
  '--headless=new',
  '--remote-debugging-port=9222',
  `--user-data-dir=${userDataDir}`,
  '--disable-gpu',
  '--window-size=1280,900',
  'about:blank'
]);

try {
  // Allow Edge debugging port to start
  await new Promise(r => setTimeout(r, 2000));

  console.log('Opening target page: http://localhost:5153/Account/Login');
  const targetRes = await fetch('http://127.0.0.1:9222/json/new?http://localhost:5153/Account/Login', { method: 'PUT' });
  const target = await targetRes.json();
  const ws = new WebSocket(target.webSocketDebuggerUrl);

  await new Promise((resolve, reject) => {
    ws.onopen = resolve;
    ws.onerror = reject;
  });

  let msgId = 1;
  const callbacks = new Map();
  ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
    if (data.id && callbacks.has(data.id)) {
      const { resolve, reject } = callbacks.get(data.id);
      callbacks.delete(data.id);
      if (data.error) reject(data.error);
      else resolve(data.result);
    }
  };

  function send(method, params = {}) {
    return new Promise((resolve, reject) => {
      const id = msgId++;
      callbacks.set(id, { resolve, reject });
      ws.send(JSON.stringify({ id, method, params }));
    });
  }

  async function evaluate(expression) {
    const res = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
    return res.result?.value;
  }

  async function screenshot(filename) {
    const { data } = await send('Page.captureScreenshot', { format: 'png' });
    const buffer = Buffer.from(data, 'base64');
    const filepath = path.join(screenshotsDir, filename);
    fs.writeFileSync(filepath, buffer);
    console.log(`[PASS] Saved screenshot: ${filename} (${buffer.length} bytes)`);
  }

  async function navigate(url) {
    await send('Page.navigate', { url });
    await new Promise(r => setTimeout(r, 2000));
  }

  await send('Page.enable');
  await send('Runtime.enable');
  await send('Network.enable');
  await new Promise(r => setTimeout(r, 2000));

  // --- EVIDENCE 1: The Login Page ---
  console.log('Capturing Evidence 1: Login page...');
  await screenshot('01-login-page.png');

  // --- EVIDENCE 2: Log in as agent -> Open Dashboard -> Access Denied ---
  console.log('Logging in as agent (agent / Agent@123)...');
  await evaluate(`
    document.querySelector('input[name="Username"]').value = 'agent';
    document.querySelector('input[name="Password"]').value = 'Agent@123';
    document.querySelector('button[type="submit"]').click();
  `);
  await new Promise(r => setTimeout(r, 3000));

  console.log('Navigating to http://localhost:5153/Dashboard as agent (expect redirect to /Account/AccessDenied)...');
  await navigate('http://localhost:5153/Dashboard');
  const currentUrl = await evaluate('window.location.href');
  console.log(`Current URL after agent dashboard access: ${currentUrl}`);
  console.log('Capturing Evidence 2: Access Denied page...');
  await screenshot('02-access-denied.png');

  // --- EVIDENCE 3: Log in as admin -> Supervisor Dashboard ---
  console.log('Clearing session cookies to log in fresh as admin...');
  await send('Network.clearBrowserCookies');
  await navigate('http://localhost:5153/Account/Login');
  console.log('Logging in as admin (admin / Admin@123)...');
  await evaluate(`
    document.querySelector('input[name="Username"]').value = 'admin';
    document.querySelector('input[name="Password"]').value = 'Admin@123';
    document.querySelector('button[type="submit"]').click();
  `);
  await new Promise(r => setTimeout(r, 3000));

  console.log('Navigating to http://localhost:5153/Dashboard as admin...');
  await navigate('http://localhost:5153/Dashboard');
  const adminUrl = await evaluate('window.location.href');
  console.log(`Current URL after admin login: ${adminUrl}`);
  console.log('Capturing Evidence 3: Admin Dashboard page...');
  await screenshot('03-admin-dashboard.png');

  ws.close();
} finally {
  edgeProc.kill('SIGKILL');
  console.log('Headless Edge closed.');
}
