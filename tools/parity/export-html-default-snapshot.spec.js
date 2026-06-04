const fs = require('fs');
const path = require('path');
const http = require('http');
const { test, expect } = require('@playwright/test');

test.describe.configure({ mode: 'serial' });
test.setTimeout(120000);

test('export html default parity snapshot', async ({ page }) => {
  const repoRoot = path.resolve(__dirname, '..', '..');
  const outputPath = path.resolve(repoRoot, process.env.STFU_HTML_SNAPSHOT_OUT || 'artifacts/html-default-parity-snapshot.json');
  const assetPath = (process.env.STFU_HTML_ASSET_PATH || 'assets/suzanne.obj').replace(/\\/g, '/');
  const width = parsePositiveInt(process.env.STFU_HTML_VIEWPORT_WIDTH, 1240);
  const height = parsePositiveInt(process.env.STFU_HTML_VIEWPORT_HEIGHT, 600);

  const server = await startStaticServer(repoRoot);

  try {
    await page.setViewportSize({ width, height });
    await page.goto(`${server.origin}/index.html`, { waitUntil: 'networkidle' });

    await page.waitForFunction(() =>
      typeof window.__prepareDefaultParitySnapshot === 'function' &&
      typeof window.__loadDefaultParityAsset === 'function' &&
      typeof window.__exportDefaultParitySnapshot === 'function');

    await page.evaluate(() => window.__prepareDefaultParitySnapshot({
      drawProgress: 1,
      cameraX: 0,
      cameraY: 0,
      cameraZ: 4,
      targetX: 0,
      targetY: 0,
      targetZ: 0,
      fov: 45,
      near: 0.01,
      far: 1000
    }));

    await page.evaluate(
      ({ assetPath }) => window.__loadDefaultParityAsset(assetPath, 'Suzanne'),
      { assetPath });

    await page.evaluate(() => window.__prepareDefaultParitySnapshot({
      drawProgress: 1,
      cameraX: 0,
      cameraY: 0,
      cameraZ: 4,
      targetX: 0,
      targetY: 0,
      targetZ: 0,
      fov: 45,
      near: 0.01,
      far: 1000
    }));

    await page.waitForFunction(() => {
      const snapshot = window.__exportDefaultParitySnapshot?.();
      return snapshot && snapshot.counts && snapshot.counts.triangles > 0 && snapshot.counts.vertices > 0;
    });

    const snapshot = await page.evaluate(() => window.__exportDefaultParitySnapshot());
    expect(snapshot).toBeTruthy();
    expect(snapshot.counts.vertices).toBeGreaterThan(0);
    expect(snapshot.counts.triangles).toBeGreaterThan(0);

    fs.mkdirSync(path.dirname(outputPath), { recursive: true });
    fs.writeFileSync(outputPath, JSON.stringify(snapshot, null, 2));
  } finally {
    await stopServer(server);
  }
});

function parsePositiveInt(value, fallback) {
  const parsed = Number.parseInt(value || '', 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function startStaticServer(rootDirectory) {
  return new Promise((resolve, reject) => {
    const server = http.createServer((request, response) => {
      try {
        const requestUrl = new URL(request.url || '/', 'http://127.0.0.1');
        const relativePath = decodeURIComponent(requestUrl.pathname === '/' ? '/index.html' : requestUrl.pathname);
        const filePath = path.resolve(rootDirectory, `.${relativePath}`);

        if (!filePath.startsWith(rootDirectory)) {
          response.writeHead(403);
          response.end('Forbidden');
          return;
        }

        if (!fs.existsSync(filePath) || fs.statSync(filePath).isDirectory()) {
          response.writeHead(404);
          response.end('Not Found');
          return;
        }

        response.writeHead(200, {
          'Content-Type': contentTypeFor(filePath),
          'Cache-Control': 'no-store'
        });
        fs.createReadStream(filePath).pipe(response);
      } catch (error) {
        response.writeHead(500);
        response.end(String(error));
      }
    });

    server.once('error', reject);
    server.listen(0, '127.0.0.1', () => {
      const address = server.address();
      resolve({
        server,
        origin: `http://127.0.0.1:${address.port}`
      });
    });
  });
}

function stopServer(handle) {
  return new Promise((resolve, reject) => {
    handle.server.close(error => {
      if (error) {
        reject(error);
        return;
      }

      resolve();
    });
  });
}

function contentTypeFor(filePath) {
  switch (path.extname(filePath).toLowerCase()) {
    case '.html': return 'text/html; charset=utf-8';
    case '.js': return 'text/javascript; charset=utf-8';
    case '.mjs': return 'text/javascript; charset=utf-8';
    case '.css': return 'text/css; charset=utf-8';
    case '.json': return 'application/json; charset=utf-8';
    case '.obj': return 'text/plain; charset=utf-8';
    case '.glb': return 'model/gltf-binary';
    case '.gltf': return 'model/gltf+json; charset=utf-8';
    default: return 'application/octet-stream';
  }
}
