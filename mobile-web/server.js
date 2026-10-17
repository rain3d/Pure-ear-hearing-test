const http = require('http');
const fs = require('fs');
const path = require('path');
const os = require('os');

const PORT = 8080;
const BASE_DIR = __dirname;

const MIME_TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.ico': 'image/x-icon'
};

const server = http.createServer((req, res) => {
  let reqPath = req.url.split('?')[0];
  if (reqPath === '/' || reqPath === '') reqPath = '/index.html';

  const filePath = path.join(BASE_DIR, reqPath);

  fs.stat(filePath, (err, stats) => {
    if (err || !stats.isFile()) {
      res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end('404 Not Found');
      return;
    }

    const ext = path.extname(filePath).toLowerCase();
    const contentType = MIME_TYPES[ext] || 'application/octet-stream';

    res.writeHead(200, {
      'Content-Type': contentType,
      'Cache-Control': 'no-cache',
      'Access-Control-Allow-Origin': '*'
    });

    const stream = fs.createReadStream(filePath);
    stream.pipe(res);
  });
});

function getLocalIps() {
  const interfaces = os.networkInterfaces();
  const ips = [];
  for (const name of Object.keys(interfaces)) {
    for (const iface of interfaces[name]) {
      if (iface.family === 'IPv4' && !iface.internal) {
        ips.push({ name, ip: iface.address });
      }
    }
  }
  // Prioritize 192.168.x.x
  ips.sort((a, b) => {
    if (a.ip.startsWith('192.168.')) return -1;
    if (b.ip.startsWith('192.168.')) return 1;
    return 0;
  });
  return ips.length > 0 ? ips : [{ name: 'local', ip: '127.0.0.1' }];
}

server.on('error', (err) => {
  if (err.code === 'EADDRINUSE') {
    const localIps = getLocalIps();
    const primaryIp = localIps[0].ip;
    console.log('====================================================');
    console.log('  ⚠️ 伺服器已經在執行中！(連接埠 8080 正由現有服務提供)');
    console.log('====================================================');
    console.log(`  📱 請直接在手機瀏覽器打開：`);
    console.log(`     👉 http://${primaryIp}:${PORT}`);
    console.log(`  💻 本機電腦請打開：http://localhost:${PORT}`);
    console.log('====================================================');
    process.exit(0);
  } else {
    console.error('伺服器發生未預期錯誤:', err);
  }
});

server.listen(PORT, '0.0.0.0', () => {
  const localIps = getLocalIps();
  const primaryIp = localIps[0].ip;
  console.log('====================================================');
  console.log('  🎧 專業純音聽力測驗 (Mobile Web / PWA 伺服器已啟動)  ');
  console.log('  ✨ 版本: v2.1 (已包含 1000Hz 0dB 聽閾修復與 PWA 快取更新)');
  console.log('====================================================');
  console.log(`  📱 手機（Android / iPhone）請連線同一 Wi-Fi，打開瀏覽器：`);
  console.log(`     👉 http://${primaryIp}:${PORT}`);
  if (localIps.length > 1) {
    console.log(`  備用連線 IP：`);
    localIps.slice(1).forEach(item => console.log(`     • http://${item.ip}:${PORT} (${item.name})`));
  }
  console.log('');
  console.log(`  💻 本機電腦測試請打開：`);
  console.log(`     http://localhost:${PORT}`);
  console.log('====================================================');
  console.log('  提示：在手機 Chrome / Edge 點選「安裝應用程式」或「加到主螢幕」，即可像原生 App 一樣全螢幕使用！');
  console.log('  按 Ctrl + C 即可關閉伺服器。');
});
