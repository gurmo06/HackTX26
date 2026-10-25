# Story continuity and cutscene update

## Changes

The playable merchant encounter and bean-growth objective are replaced by the **Worst Trade in History** prologue. Its procedural parchment backdrop does not load the post-beanstalk village illustration. Beans enter the satchel only at the trade, are consumed on planting, and the plant first appears during the night/growth shot.

The playable order is now:

| Chapter ID | Objective | Illustrated environment |
| --- | --- | --- |
| `sabotaged` | Stabilize the beanstalk or establish a safe climbing aid | `01_Merchant_Village_Post_Beanstalk.png` |
| `climb` | Cross the missing middle | `02_Beanstalk_Climbing.png` |
| `castle` | Bypass the sealed entrance | `03_Giant_Castle.png` |
| `treasure` | Silence the goose ward and take it while the giant sleeps | `03_Giant_Castle.png`, cooler tint, altered camera and actors |
| `escape` | Bring the goose through the final obstacle | `04_NightEscape.png` |

Completing an objective automatically starts its connecting sequence. Narrator text and `advance` never set story milestones or bypass chapters. The goose cannot be granted, spawned or transformed into existence early. It must be taken from the unlocked treasure-room ward while the giant is sleeping. Only then can the awakening sequence enable pursuit. A visible goose follows Jack in escape gameplay.

## Added systems

- `StoryState.cs`: ordered milestone ledger, inventory changes for the prologue, phase-entry checks and idempotent cutscene finalizers.
- `CutsceneDirector.cs`: shared sequential step runner, synchronized dialogue, animation/read-time separation, Next/Skip/cancellation, world/control locking, camera ownership, stage cleanup and state-safe completion.
- `StorySequences.cs`: deterministic composition of the six scenes using existing art and the reusable runner.
- `StoryMotion.cs`: smooth easing/tween, position and scale primitives with unscaled coroutine timing.
- `StoryActor.cs`: anchored cutout rendering, sprite swapping, walking/climbing, bobbing/hopping, sleeping breath and sway.

The six sequences are prologue/trade/planting/growth; initial ascent; cloud kingdom approach; treasure-room introduction; giant awakening/chase; homecoming/sunrise/ending. Connecting visual action is roughly 9–10 seconds at default speed; reading time is controlled separately. Skip completes required story state exactly once. Restart cancels without completing the interrupted scene, then resets the ledger and inventory.

## Modified existing systems

- `GameManager.cs`, `LevelDefinition.cs`, `LevelController.cs`: revised chapter definitions, mandatory transitions, automatic goal completion and cinematic camera ownership.
- `ActionExecutor.cs`, `NarratorContract.cs`, `NarratorMock.cs`: milestone and treasure-ward restrictions, typed `storyMilestones` request array, and a complete deterministic path through the new order. The generic action response contract is unchanged.
- `NarratorUI.cs`: optional skippable typewriter, Ask/Next/Skip/Show text controls, cutscene status, explicit COMPLETE state and updated ending text. The same parchment panel and text-only Narrator heading remain.
- `JackController.cs`, `WorldEntity.cs`, `WorldRegistry.cs`: carried goose, grounded scaling, breathing/hopping/sway and eased object changes/appearance/removal. Current-chapter world changes persist between requests.
- `JackDemoBuilder.cs`: existing full builder now includes story support and backup; new **Upgrade Story and Cutscenes** preserves existing objects/references and adds only the new wiring/UI controls. Scene backups are outside Assets and gitignored.
- `netlify/functions/server/contract.mjs`, `prompt.mjs`, backend tests and curl fixture: revised level/actor rosters, canonical milestone validation, story-aware prompts and protected acquisition. `NarratorClient.cs` and the Gemini HTTP handler keep their existing transport, secret handling, structured-output configuration, limits and fallback.
- Unity tests and documentation updated for watched/skipped paths and scene preservation.

No existing Unity packages, URP assets, art files, font files or external dependencies were replaced. No additional Unity scenes are needed. Live gameplay remains free-form; deterministic dialogue is confined to cutscenes and offline fallback.

## Verification and limits

Current executed results are in `IMPLEMENTATION_STATUS.md`. No paid Gemini request or deployment is performed by this update. The game remains a story puzzle with bounded walking and compositional object actions; the giant's pursuit is staged narrative pressure, not a timed lethal platformer. Connections remain best-effort visual ropes. Page-turn effects were optional and are not added; transitions use fades and camera motion instead.
