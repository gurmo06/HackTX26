# Backend and Web deployment

## Local setup

Use Node 22 LTS (minimum Node 20.19 for this toolchain). In the project root:

```sh
npm install
cp .env.example .env
npm test
npm run dev
```

Netlify dev serves port 8888 and rewrites `/api/narrate` to `/.netlify/functions/narrate`. A Web build is not needed to exercise the API; the static root may be empty until Unity builds it. If Netlify dev needs account configuration, use `npm run serve:mock` for the standalone localhost API. Both use `.env` and the same handler; run only one at a time.

Default environment is offline. The handler returns a typed zero-action response with `explanation=offline`; Unity uses its deterministic narrator to keep gameplay moving. To enable Gemini, create a private key at https://aistudio.google.com/apikey and edit `.env`:

```dotenv
GEMINI_API_KEY=your-private-value
GEMINI_MODEL=gemini-3.8-flash
ENABLE_LIVE_NARRATOR=true
MOCK_NARRATOR=false
```

Do not commit `.env`, copy it into Assets/web-build, or put a key in `netlify.toml`. Restart the local server after changes. The default fast model is configurable because availability and quotas differ by account. Run `npm run models` with the private key to list accessible models, then select a text model supporting structured output. Check https://ai.google.dev/gemini-api/docs/models and https://ai.google.dev/gemini-api/docs/rate-limits and your AI Studio dashboard. If a default model is unavailable, change only `GEMINI_MODEL`; an unavailable model produces safe offline fallback.

Implementation uses `@google/genai` with `models.generateContent`, `config.responseMimeType`, `config.responseJsonSchema`, `abortSignal`, and HTTP retry options verified against the installed SDK declarations. Structured-output reference: https://ai.google.dev/gemini-api/docs/generate-content/structured-output . Server-side Zod validation runs again before actions reach Unity.

## HTTP check

`tests/fixtures/sabotaged-request.json` is a complete client-contract example. With the local server running:

```sh
curl -i http://localhost:8888/api/narrate \
  -H 'Content-Type: application/json' \
  --data-binary @tests/fixtures/sabotaged-request.json
```

Expect HTTP 200 and the fields `narration`, `mood`, `twist`, `actions`, `challengeId`, `explanation`. With live environment enabled, verify a real action response. An `offline` or `api_unavailable` explanation is a fallback, not evidence that Gemini worked. `GET` returns 405; malformed requests return 400, >16KB return 413, wrong media type returns 415, and throttling returns 429. Production CORS is same-origin. Local CORS additionally permits localhost/127.0.0.1 ports 8888, 8080 and 8000 only in dev mode. Unity Editor sends no browser Origin header.

For Editor live testing: GameManager > Narrator Client > Use Mock Narrator **off**, Api Url **`http://localhost:8888/api/narrate`**. For deployment, change Api Url to **`/api/narrate`**, save, and rebuild.

## Build the Unity player

Use `Documentation/UNITY_MANUAL_STEPS.md`, the Web build section. Output must be:

```text
web-build/
  index.html
  Build/
  TemplateData/
```

Initial deployment uses **Compression Format: Disabled**, avoiding mismatched gzip/Brotli response headers. The provided Netlify configuration explicitly serves `.wasm` with `application/wasm`; uncompressed JavaScript/data need no Content-Encoding header. Do not enable Brotli/gzip without adding matching Content-Encoding and Content-Type rules for the actual emitted extensions, or enabling Unity's decompression fallback and testing it. Threading is off, so COOP/COEP headers are not required. No SPA catch-all redirect is present; API requests cannot be swallowed by `index.html`.

Build output is gitignored. A Git-connected Netlify deploy cannot run Unity to create it and will not have your local ignored files. Use a **CLI upload from the local machine after building**, or arrange your own CI artifact upload; do not assume Git auto-deploy includes the Unity player.

## Netlify deployment

1. Create/sign into a Netlify account at https://app.netlify.com/ and create or select a project. From this repository run `npx netlify login`, then `npx netlify init` for a new site or `npx netlify link` for an existing one. Choose manual deploy if offered. Do not publish until the local build and API checks are ready.
2. In Netlify **Project configuration > Environment variables**, add `GEMINI_API_KEY` as a secret scoped to **Functions/runtime** for the intended deploy context. Add `GEMINI_MODEL`, `ENABLE_LIVE_NARRATOR=true`, `MOCK_NARRATOR=false`, and `SITE_URL=https://your-site.netlify.app` (no trailing slash). The key must be available at function runtime, not merely in the build environment. Use the UI so the key is not saved in shell history. Do not send credentials to the assistant.
3. Build the Web player into `web-build/`. Run `npm test`. Deploy a draft with `npx netlify deploy --dir=web-build --functions=netlify/functions`. This uploads the Web build and bundles the function. Test its generated HTTPS URL. Set environment variables for the draft context as well if testing live there.
4. When satisfied, publish with `npx netlify deploy --prod --dir=web-build --functions=netlify/functions`. Re-deploy after changing function environment if Netlify indicates that a new deploy is required.
5. Open the site, confirm the title and all art, play the complete story, check Network requests to `/api/narrate`, and resize to 4:3/narrow view. Ensure there are no failed Build files or WebAssembly MIME errors. Send the curl fixture to the HTTPS API URL. Test invalid actions through the automated tests, and simulate an outage by disabling live mode in a draft environment; gameplay should continue offline.

## Abuse limits and trust boundary

- JSON payload at most 16KB, player input 1–400 characters, six history messages of at most 350 characters, 24 world entities, five inventory entries, three actions, and 350 narration characters. All fields, enums, IDs and numeric bounds are validated.
- Netlify function config requests **12 requests / 60 seconds per IP and domain**. Verify the rule is active in the deployed project's rate-limit configuration; platform availability/enforcement must be checked on your site. Reference: https://docs.netlify.com/manage/security/secure-access-to-sites/rate-limiting/ .
- Additional in-memory limits: 12 requests/minute/IP, 200 requests/hour/session and 60 requests/minute/function instance, with at most 2,500 counter keys. These reset on cold starts and do **not** enforce a global account budget across serverless instances. Session IDs and client snapshots are untrusted and spoofable; CORS is not authentication. This is a bounded public game endpoint, not a general-purpose Gemini proxy.
- Enable live use explicitly. Configure provider-side project quotas, monitor usage in AI Studio/Cloud, and restrict/private-host the judging site if you need a hard audience boundary. For broader public traffic, add durable shared quotas or authenticated sessions. Client turn counts are a gameplay limit, not a reliable billing control.
- Maximum two model calls per accepted request, each with a five-second deadline (about ten seconds total), 2,048 output tokens per call. SDK automatic retries are disabled (`attempts: 1`). Cancellation stops waiting locally but may not cancel provider billing. Transient errors retry once; invalid output and permanent API errors do not. No keys, raw provider errors, or user text are logged by the handler.
- Server checks required actor rosters and action references, but cannot prove the submitted world matches an actual browser game. Unity validates actions again against its instantiated state and current phase. This is appropriate for a single-player demo, not an authoritative multiplayer economy.

## Verification and remaining limits

See `IMPLEMENTATION_STATUS.md` for checks actually executed. Automated backend tests inject model responses; they do not spend quota or prove account/model access. Manual live Gemini, Web export, hosting, and browser UI checks require your credentials and Unity build.

`npm audit --omit=dev` found no production dependency advisories during implementation. The current Netlify CLI retains a development-only `toml` parser advisory (two high findings including the parent CLI) after compatible fixes. No breaking forced downgrade was applied. Recheck before updating tooling, and run the CLI only with the repository's trusted configuration. Runtime backend bundles do not include the CLI.


## Story update compatibility

Deploy the updated function and rebuilt Web player together. Requests now use level IDs `sabotaged`, `climb`, `castle`, `treasure`, `escape` and a required `storyMilestones` string array. Responses retain the existing action/narration contract. Old player exports do not supply the new request fields and will fall back offline until rebuilt. The cutscenes never call Gemini; only free-form requests during gameplay use the API.

The client owns milestone state and revalidates all actions. Beans are received and planted in the prologue; the server cannot grant them. Goose acquisition requires the treasure chapter, castle-entry milestone, an unlocked goose ward, and a sleeping giant. Requests claiming future or inconsistent milestones are rejected. As before, submitted snapshots are not authenticated multiplayer state; milestone validation prevents gameplay bypass in the actual client, not cheating in an external fabricated HTTP request.
