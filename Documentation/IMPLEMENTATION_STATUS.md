# Current implementation status — story continuity update

The existing game was extended in place. The merchant/growth gameplay pair is now a deterministic prologue, followed by five playable chapters and their connecting sequences. The existing narrator HTTP client, Gemini handler, action response DTOs, art, fonts, URP assets and Unity package versions were preserved.

## Completed

- [x] Inspected the existing scene, builder, progression, narrator client/backend, inventory and action executor before modifying them.
- [x] Added the ordered story milestone ledger and aligned client/server chapter and actor definitions.
- [x] Implemented a parchment-only pre-growth prologue: approach, trade, merchant/cow departure, return home, planting, night and growth.
- [x] Revised gameplay order: sabotaged beanstalk → impossible climb → castle entrance → golden goose → great escape.
- [x] Added all six cutscenes through one reusable sequencer: prologue, climbing, cloud kingdom, treasure room, awakening and ending.
- [x] Added eased movement, sprite swapping, anchored cutout animation, breathing/hops/sway, growth, camera pans/zoom, fades, screen shake and object appearance/removal effects.
- [x] Added Next, Skip scene, optional typewriter/reveal controls, automatic objective transitions and an updated ending screen.
- [x] Enforced the inventory timeline: receive then plant beans; quiet goose acquisition only in the treasure chapter; giant pursuit only after awakening; victory only after the final ending.
- [x] Preserved world changes between requests within the current chapter, request deduplication, stale-response cancellation, live/free-form Gemini gameplay, fixed challenges and outage fallback.
- [x] Added a non-destructive story-upgrade menu that preserves existing scene/UI/API references and makes a scene backup. The full builder also supports the revised story and makes backups.
- [x] Updated Unity/backend tests, curl fixture, demo script, Editor instructions, deployment compatibility notes and change summary.

## Executed verification

- **12/12 Node tests passed**: request/response validation, HTTP errors/CORS, opt-in and outage fallback, structured model configuration, retry bounds, action sequencing, all revised goals, protected actors/items, milestone consistency, rate limits, C#/JS DTO parity, and identical chapter/actor/milestone definitions.
- **Unity tests in an isolated project copy:** scene upgrade preservation/idempotence, watched story to victory, and skip/restart/state restoration all passed. The harness uses standard Play-mode reload while running and restores the project's previous setting afterward. Final consolidated NUnit output is saved as `Documentation/Verification/StoryResults.xml`.
- **Graphics-enabled Unity smoke test:** the watched-story test passed. Batch screenshot capture did not produce usable review images; this does not establish visual layout quality or interactive mouse/touch feel.
- **C# compile check:** `python3 scripts/check-csharp.py` passed against installed Unity assemblies. Unity also compiled the updated assembly definitions and scripts during the test runs.
- **Backend fixture:** the revised `sabotaged-request.json` passed schema validation and returned HTTP 200 / `offline` through the actual handler without credentials.
- **Function bundling:** the updated Netlify function bundles with esbuild. No paid model request or publication is involved.
- All story sprite keys resolve to the existing 29-entry art catalog. No new image/font/package dependency was introduced. New code/UI does not add narrator artwork or change the existing text heading.

## Manual verification/configuration still required

- Your open Editor and actual `JackGame.unity` were not replaced during testing. Run **Tools > Jack > Upgrade Story and Cutscenes** in the real project, then save. The upgrade automatically backs up the previous scene and adds the new references.
- Watch the animations at normal speed in Game view and check 16:9, 4:3 and narrow layouts, scrolling text, mouse/touch, keyboard controls and typewriter pacing. Automated Play-mode tests execute the motion/state logic but do not replace this visual check.
- Retest live Gemini with your private existing key/model/quota. Backend tests use injected model replies; the cutscenes themselves require no API.
- Rebuild the Web player and deploy it together with the updated backend. No new Web export, browser session, or hosted deployment was performed in this update.

## Scope limits

No required story sequence is left as a placeholder. This remains a narrative puzzle with bounded movement and generic world actions, not a physics platformer or lethal timed chase. Pursuit is staged through the awakening sequence and escape setting. Page-turn transitions were optional and were not added; fades and pans provide transitions. Client snapshots remain non-authoritative at the server boundary; the actual Unity scene and local milestone ledger independently enforce progression.

For exact actions and a full walkthrough, see `UNITY_MANUAL_STEPS.md`, `STORY_UPDATE.md`, `DEMO_SCRIPT.md` and `DEPLOYMENT.md`. Older NUnit XML files in Verification describe the previous implementation; StoryResults is the evidence for this revision.
