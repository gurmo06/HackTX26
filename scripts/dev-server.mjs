// Dependency-light localhost API runner; Netlify dev is the production-routing test.
import http from 'node:http';
import { createHandler } from '../netlify/functions/narrate.mjs';
const handler = createHandler({ env: { ...process.env, NODE_ENV: 'development' } });
http.createServer(async (req, res) => {
  try {
    if (req.url !== '/api/narrate') { res.writeHead(404); res.end('POST /api/narrate'); return; }
    const request = new Request(`http://localhost:8888${req.url}`, { method: req.method, headers: req.headers, ...(['GET', 'HEAD'].includes(req.method) ? {} : { body: req, duplex: 'half' }) });
    const response = await handler(request, { ip: req.socket.remoteAddress });
    res.writeHead(response.status, Object.fromEntries(response.headers)); res.end(await response.text());
  } catch { res.writeHead(500); res.end('Local server error'); }
}).listen(8888, '127.0.0.1', () => console.log('Jack API: http://localhost:8888/api/narrate (Ctrl+C to stop)'));
