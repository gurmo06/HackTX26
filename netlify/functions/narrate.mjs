import { GoogleGenAI } from '@google/genai';
import { requestSchema, responseJsonSchema, validateResponse, fallback } from './server/contract.mjs';
import { systemPrompt } from './server/prompt.mjs';

export const config = { rateLimit: { windowLimit: 12, windowSize: 60, aggregateBy: ['ip', 'domain'], action: 'rate_limit' } };
const MAX_BODY = 16 * 1024;
const counters = new Map();
function take(key, limit, windowMs, now = Date.now()) {
  for (const [id, value] of counters) if (value.until <= now) counters.delete(id);
  if (!counters.has(key)) {
    if (counters.size >= 2500) return false;
    counters.set(key, { count: 0, until: now + windowMs });
  }
  return ++counters.get(key).count <= limit;
}
function cors(request, env) {
  const origin = request.headers.get('origin');
  if (!origin) return {};
  const local = env.NETLIFY_DEV === 'true' || env.NODE_ENV === 'development';
  const allowed = new Set([new URL(request.url).origin, env.SITE_URL, env.URL, env.DEPLOY_PRIME_URL].filter(Boolean));
  if (local) for (const host of ['localhost', '127.0.0.1']) for (const port of [8888, 8080, 8000]) allowed.add(`http://${host}:${port}`);
  if (!allowed.has(origin)) return null;
  return { 'Access-Control-Allow-Origin': origin, 'Access-Control-Allow-Methods': 'POST, OPTIONS', 'Access-Control-Allow-Headers': 'Content-Type', Vary: 'Origin' };
}
async function readBounded(request) {
  if (Number(request.headers.get('content-length')) > MAX_BODY) throw new RangeError('Payload exceeds 16KB');
  if (!request.body) return '';
  const reader = request.body.getReader(); const chunks = []; let size = 0;
  try {
    while (true) {
      const { done, value } = await reader.read(); if (done) break;
      size += value.byteLength;
      if (size > MAX_BODY) { await reader.cancel(); throw new RangeError('Payload exceeds 16KB'); }
      chunks.push(value);
    }
  } finally { reader.releaseLock(); }
  return Buffer.concat(chunks).toString('utf8');
}
export function createHandler({ env = process.env, generate, now = Date.now } = {}) {
  return async (request, context = {}) => {
    const headers = cors(request, env);
    const reply = (body, status = 200, extra = {}) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store', ...headers, ...extra } });
    if (!headers) return reply(fallback('forbidden_origin', 'This stage only accepts requests from its own theatre.'), 403);
    if (request.method === 'OPTIONS') return new Response(null, { status: 204, headers });
    if (request.method !== 'POST') return reply(fallback('method_not_allowed', 'Use POST to speak to the narrator.'), 405, { Allow: 'POST, OPTIONS' });
    if (!request.headers.get('content-type')?.toLowerCase().startsWith('application/json')) return reply(fallback('invalid_content_type', 'Send application/json.'), 415);
    let data;
    try { data = requestSchema.parse(JSON.parse(await readBounded(request))); }
    catch (error) { return reply(fallback('invalid_request', error instanceof RangeError ? 'Request exceeds 16KB.' : 'Invalid game request: check fields, actor roster, text length and limits.'), error instanceof RangeError ? 413 : 400); }
    const ip = context.ip || 'local';
    if (!take(`ip:${ip}`, 12, 60000, now()) || !take(`session:${data.sessionId}`, 200, 3600000, now()) || !take('instance', 60, 60000, now()))
      return reply(fallback('rate_limited', 'The narrator needs a breather. Your offline understudy can continue.'), 429, { 'Retry-After': '60' });
    if (env.MOCK_NARRATOR === 'true' || !env.GEMINI_API_KEY || env.ENABLE_LIVE_NARRATOR !== 'true') return reply(fallback('offline'));
    // In the installed SDK, omitting retryOptions uses one fetch and preserves ApiError.status.
    // Its attempts:1 wrapper discards HTTP status, preventing our bounded retry below.
    const client = generate ? null : new GoogleGenAI({ apiKey: env.GEMINI_API_KEY, httpOptions: { timeout: 25000 } });
    const call = generate || (options => client.models.generateContent(options));
    // Two attempts maximum, including the first; each is bounded to 25 seconds.
    for (let attempt = 0; attempt < 2; attempt++) {
      const abort = new AbortController(); let timer;
      try {
        const options = {
          model: env.GEMINI_MODEL || 'gemini-3.8-flash',
          contents: [{ role: 'user', parts: [{ text: `UNTRUSTED_GAME_DATA\n${JSON.stringify(data)}` }] }],
          config: { systemInstruction: systemPrompt(data.levelId), responseMimeType: 'application/json', responseJsonSchema,
            maxOutputTokens: 2048, temperature: .8, abortSignal: abort.signal,
            httpOptions: { timeout: 25000 } },
        };
        const result = await Promise.race([call(options), new Promise((_, reject) => { timer = setTimeout(() => { abort.abort(); reject(new Error('Narrator timeout')); }, 25000); })]);
        if (typeof result.text !== 'string' || result.text.length > 16000) return reply(fallback());
        let parsed; try { parsed = JSON.parse(result.text); } catch { return reply(fallback()); }
        return reply(validateResponse(parsed, data));
      } catch (error) {
        const transient = [429, 500, 502, 503, 504].includes(Number(error.status)) || error.name === 'AbortError' || error.message === 'Narrator timeout';
        if (!transient || attempt === 1) {
          const status = Number(error.status);
          const reason = status === 429 ? 'provider_quota' : status === 503 ? 'provider_busy'
            : [401, 403].includes(status) ? 'provider_auth' : status === 404 ? 'provider_model'
            : status === 400 ? 'provider_request' : error.name === 'AbortError' || error.message === 'Narrator timeout' || status === 504 ? 'provider_timeout' : 'provider_error';
          // Only a fixed diagnostic code crosses the boundary, never provider text or secrets.
          return reply(fallback(), 200, { 'X-Narrator-Error': reason, 'Access-Control-Expose-Headers': 'X-Narrator-Error' });
        }
      } finally { clearTimeout(timer); }
    }
    return reply(fallback());
  };
}
export default createHandler();
