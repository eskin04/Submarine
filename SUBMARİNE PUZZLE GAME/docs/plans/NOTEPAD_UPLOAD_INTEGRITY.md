# C04 — Notepad upload integrity and lifetime

Reviewed on current local main, commit 028874b, 2026-10-04. All required repository guidance and the original plan were read. This replaces the earlier conditional design. The approved implementation now changes NotepadModule.cs and a narrow InventoryManager.cs bootstrap surface; implementation verification status is recorded below. The Unity encoder characterization was completed before coding.

## Actual payload and evidence

NotepadModule.InitializeDrawingPages creates square RGBA32 textures. Assets/Prefabs/NoteBook/Notepad.prefab configures textureResolution 512, four pages, brushSize 4 and opaque white/black colors. Notepads.prefab nests two instances. Asset/source searches found no resolution override, runtime resize/setter or AddComponent/new NotepadModule use. The script's 1024 initializer has no demonstrated gameplay use. Support the actual 512 configuration; do not invent 1024 compatibility or silently resize drawings.

HandlePageTearing uses currentTex.EncodeToJPG(50). JPEG has no alpha channel; no Unity alpha-specific experiment was run. SendImageInChunksRoutine creates a 36-character GUID, prepares declared length/target, yields a frame, then sends one 1000-byte chunk per frame at i * 1000. Only the last can be short; a divisible payload has a full final chunk. Installed ServerRpc defaults are ReliableOrdered/runLocally false; calls require no ownership.

Installed packers give the GUID 321 bits (presence + 32-bit count + 8 bits/character), offset 32 bits and byte-array metadata 32 bits. Chunk argument overhead is 385 bits, approximately 49 rounded bytes, excluding RPC envelope/transport. Preparation additionally carries declared size and identity presence/network ID/scene ID. Do not claim an exact total packet size.

Source: Assets/Scripts/Notepad/NotepadModule.cs:99-247; TornPageItem.cs:49-100; Assets/Scripts/InventorySystem/InventoryManager.cs:569-691. Framework evidence: installed PurrNet 1.19.1 RPCInfo/attributes, string/integer/collection/identity packers, PlayersManager and NetworkIdentity.ResetIdentity.

## Supported protocol ceiling

Choose one concrete maximum: **262144 bytes (256 KiB)** for supported 512x512 JPEG quality 50. This is a product constraint, not an absolute JPEG theorem or raw RGBA bound. Unsupported host dimensions fail explicitly; future 1024 assets require policy review.

### Actual Unity measurement — 2026-10-04

Measured in this project's normal Windows Unity Editor 6000.3.10f1 session, using a temporary Editor-only InitializeOnLoad/delayCall script. No standalone ILPP invocation. Each in-memory source was new Texture2D(512, 512, TextureFormat.RGBA32, false), populated with SetPixels32 and Apply, then encoded with source.EncodeToJPG(50). Each JPEG was parsed for SOF dimensions and loaded into a separate 2x2 Texture2D with LoadImage; all temporary textures were DestroyImmediate'd in finally. Results were collected from Unity's Editor.log. No image assets or output files were saved in the repository.

| Input | Raw RGBA32 dimensions | Actual Unity JPEG bytes | % of 262144 | LoadImage | Decoded dimensions | JPEG SOF content |
| --- | --- | ---: | ---: | --- | --- | --- |
| Blank white | 512x512 | 4723 | 1.801682% | true | 512x512 | SOF0 (0xC0), 512x512 |
| Sparse black drawing | 512x512 | 21335 | 8.138657% | true | 512x512 | SOF0 (0xC0), 512x512 |
| Dense black drawing | 512x512 | 51773 | 19.749832% | true | 512x512 | SOF0 (0xC0), 512x512 |
| 4-pixel checkerboard | 512x512 | 84081 | 32.074356% | true | 512x512 | SOF0 (0xC0), 512x512 |
| Deterministic RGB noise | 512x512 | 107989 | 41.194534% | true | 512x512 | SOF0 (0xC0), 512x512 |
| Additional binary noise | 512x512 | 140177 | 53.473282% | true | 512x512 | SOF0 (0xC0), 512x512 |

Reproduction: every sample starts opaque white. Seed is uint 104, reset per sample, using xorshift32: state ^= state << 13; state ^= state >> 17; state ^= state << 5. Sparse/dense use 12/180 black strokes: four successive random values modulo 512 select endpoints; steps=max(abs(dx),abs(dy)); integer interpolation for k=0..steps stamps a radius-4 disk (dx*dx+dy*dy <=16), clipped to the image. Checker pixels are ((x/4+y/4)%2)*255. RGB noise uses three successive random values' high bytes per pixel; binary noise uses the next value's top bit times 255. Alpha is always 255. These definitions, rather than the previous GDI+ strokes/noise generator, specify the measured samples.

**Final decision: KEEP 256 KiB (262144 bytes).** Largest actual sample was binary noise, 140177 bytes (141 chunks): 121967 bytes / 46.526718% of the cap remain unused. Cap is approximately 1.8701 times that sample, or 87.01% above it. This is useful representative margin, not a proof for every theoretical JPEG. Previous GDI+ proxies are superseded as decision evidence. No remaining encoder-size/header/decode characterization gate.

Two buffers consume at most 512 KiB; each needs at most 263 receipt bits (33 bytes). Textures/distribution have separate lifetimes; this is not total process memory.

Temporary Assets/Editor/NotepadJpegMeasurement.cs, its .meta, and newly created Assets/Editor directory/.meta were removed after measurement. Editor refresh also changed Sonar.mat's scan angle and FMODStudioCache.asset timestamps; both had been clean at baseline and were restored exactly to their original tracked contents. No production behavior or asset change is retained.

## Minimal operation and admission

Keep private helpers/records in NotepadModule, one active operation per sender across modules in the exact NetworkManager, maximum two. A tiny session table points to the owning module/record; capacity is record presence, not a separate admission lease. No terminal history or generic manager.

Record: sender, bounded fresh GUID, originating module identity, exact target reference/ItemIdentityHandle/manager, size, buffer, bitmap/count and last-progress time. After assembly it may temporarily retain one unhanded page/readiness continuation. Match sender + GUID + originating module. Unknown/stale chunks never create state. Remove server tokens/generation counters and inventory-version lifetime stamps.

Receiving is the only retained assembly phase. Detach chunk-write lookup before synchronous preflight/finalization; no explicit Finalizing enum. If page network readiness requires yielding, retain that actual bounded pre-handoff continuation. There is no producer AwaitingDelivery state after handoff, no commit polling and no retained terminal enum.

Preparation acknowledgement precedes chunks. Local pending tear retains source/index; hide/lock only pending page. Successful explicit handoff consumes/decrements/reports RipPage once; rejection restores it if module lives. No retries/result cache.

Current animation unlocks after 0.3 seconds, so legitimate tears can overlap. **Reject the new same-sender upload** across notebooks, preserve first operation and second source, show Busy and allow later deliberate retry. Do not cancel the old operation. Different players remain independent.

## Sender and target lifetime

Use RPCInfo sender/manager/asServer and connected-player validation. Submitted inventory is only a candidate: require exact current server-spawned enabled inventory, same manager/scene and owner == sender. Capture original reference and existing ItemIdentityHandle. Never later use LocalPlayer, role/list position or whichever inventory is active. Host helpers use actual local PlayerID, not fabricated RPCInfo.

Before copy/decode/spawn/handoff verify connection, reference, handle resolution, manager, scene, spawn and owner. Ordinary InventoryVersion changes are not lifetime changes. Engineer/Technician prefab identities are nonpooled (_shouldBePooled 0); ResetIdentity clears identity/manager/scene/spawn state and hierarchy IDs advance within its lifetime. Reference + handle + manager + owner suffices for current objects. No new inventory despawn notification/lifetime stamp is justified. Revisit if pooling is introduced.

Pair server manager.onPlayerLeft(PlayerID,bool) subscription/unsubscription; cancel departed sender. Existing Update outside interaction early returns checks target/expiry. Disable/despawn/destroy/session/scene teardown clears owned records/table entries/local sends/pre-handoff work, preserving base destruction. New session/reconnect never inherits operations. Closing interaction alone may leave admitted upload running, preserving current behavior.

## Chunk contract

Preparation validates GUID, supported configuration, sender/target/quota and 0 < totalSize <= 262144 before allocation.

1. Require active sender/GUID/module and valid captured target; foreign/unknown requests never change another sender's record.
2. Nonnull/nonempty chunk, length <= 1000, 0 <= offset < totalSize.
3. Require length <= totalSize - offset before addition/copy (overflow safe).
4. offset % 1000 == 0; length == min(1000, totalSize - offset).
5. Received slot: identical bytes are no-op with no timer renewal; conflict cancels only its owner. Misaligned/partial overlap rejects before copy.
6. Copy validated range, set bit/count once. Complete only every expected slot.

Final-first/out-of-order canonical slots are accepted. Missing middle never decodes/spawns and expires. Lookup detachment prevents duplicate final spawning twice. No interval merging/hashes.

## Single expiry model

Use **30 seconds unscaled inactivity** during assembly/pre-handoff readiness. Only a new valid slot or real readiness milestone refreshes; duplicates/polling do not. Existing Update/coroutines suffice. Remove 10-second/180-second alternatives and total deadline.

263 frame-paced chunks take 26.3 seconds at 10 FPS or 52.6 at 5 FPS, but progress occurs every 0.1/0.2 seconds. A total deadline would unnecessarily reject slow uploads. A stall/editor pause over 30 seconds explicitly cancels. Finite slots/bounded memory/trusted scope do not justify another timer. No upload timeout exists after handoff.

## Exact decode -> spawn -> handoff -> commit boundary

1. Detach complete assembly. Revalidate target; bounded JPEG segment/SOF preflight requires 512x512 before Unity decode, rejecting malformed/truncated/oversized dimensions.
2. Temporary LoadImage must return true with expected dimensions; dispose in finally. Validate prefab dependencies first. TornPage receives identical prevalidated bytes; no new TornPage decode-result API.
3. Instantiate once at existing pose. If necessary wait boundedly for TornPage.isServer, ItemLoot possession initialization and NetworkTransform.isSpawned. Instantiate is not proof of readiness. Existing SetImageDataAndDistribute early-returns unless isServer.
4. Revalidate target, give existing initial ownership, call existing image distribution. Before handoff, setup failure/cancellation cleans only this exact unhanded page and restores source.
5. A narrow internal host InventoryManager.TryBeginForcedPageDelivery(ItemLoot) validates exact live target/page, returns false if a forced-page handoff is already active and on true synchronously takes responsibility before returning. Preserve public ForcePickupClientRpc/generic caller behavior.
6. **True completes the upload**, meaning accepted bootstrap responsibility, not inventory membership. Consume source/report once; free buffer/map/table entry and drop ALL producer page references. Later timeout/disconnect/module teardown cannot delete, restore or resubmit it.
7. Inventory bootstrap retains one exact active page pointer; waits for existing pending/waiting/resolving work before selecting current empty slot or overflow, submits once and clears pointer through existing accepted response/possession or loss/despawn paths. Later forced-page handoffs reject explicitly; no queue/history. Optional early Busy query saves bandwidth but final admission decides.
8. Existing versioned pickup/ForcedOverflow performs authoritative commit. Keep validators, TryPickupServer, TryForcedFallbackServer, stamps/reconciliation, ownership/physics writers unchanged. Full inventory leaves new page in World with ownership removed, valid pose/ForceSync/impulse; no held item evicted. Transfer rejection stops/reports and leaves visible World page, without invented fallback, rollback or automatic retry.

Inventory integration remains necessary: current void bootstrap cannot accept/reject responsibility; WaitForPickup/ResolveForcedPickup yield-break when busy and full-branch BeginPending can fail. Limit changes to bootstrap admission/readiness/outcome bookkeeping. Unity main-thread synchronous finalization prevents callback interleaving; revalidate at actual yields. No producer cancellation after successful handoff, and no rollback of accepted commits.

## Expected files / scope

| Production file | Expected surface |
| --- | --- |
| Assets/Scripts/Notepad/NotepadModule.cs | Private bounded protocol, source outcome, exact lifetime, slot completion, expiry, JPEG preflight and one pre-handoff continuation. |
| Assets/Scripts/InventorySystem/InventoryManager.cs | Narrow explicit forced-page handoff and single-page bootstrap bookkeeping; accepted commit code unchanged. |

TornPageItem/ItemLoot/PurrNet/assets remain read-only. No new despawn API. Preserve serialization/public callbacks, RPC direction, Unsafe rules, audio, drawing and observer distribution. H03 replay and M05/general starting lifecycle remain separate. Package byte[] decoding allocates before handler; this bounds application retention, not hostile raw-wire parsing. No cryptography, generalized transfer service, workers, storage, retry protocol or lifecycle framework.

Removed: dual caps/speculative 1024 support, separate leases, retained Finalizing/terminal states, AwaitingDelivery ledger/commit polling, tokens/generations, lifetime stamps/despawn callback, absolute timer, TornPage decode API and cleanup of handed-off items. Retain only real asynchronous pre-handoff readiness and inventory-owned bootstrap.

## Verification scope and completed characterization

The Unity encoder measurement above is complete. The original regression design included host/client swapped; representative pages and cap boundaries; invalid/null/overflow/misaligned chunks; identical/conflicting duplicates; final-first/out-of-order/missing-middle; foreign sender/target; repeated prepare/stale chunks; simultaneous players/same-sender second notebook Busy; disconnect/reconnect/replacement; module/target teardown/scene reload/session restart; 5/10 FPS/stalls/expiry and duplicate nonrenewal; corrupt JPEG/dimension bomb/decode failure; delayed spawn; busy inventory; empty/full accepted pickup/World fallback; rejection/despawn before submission; producer teardown immediately before/after handoff; accepted commit survives; all buffers/maps/table entries/coroutines/subscriptions/bootstrap pointers settle. Completed compile, Editor and manual multiplayer evidence is recorded below. Cases not explicitly covered by that evidence remain additional stress/fault-injection coverage, not claimed runtime passes or a blocker to the accepted C04 resolution.

**Characterization: complete.** Actual Unity 6000.3.10f1 JPEG50 size/SOF/decode characterization passes with useful margin below the retained 256 KiB limit. The former readiness-to-begin-coding gate is superseded by the completed implementation and runtime verification below.

## Implementation status — 2026-10-04

Implemented on the current local main checkout. Production changes are limited to NotepadModule.cs and narrow InventoryManager.cs forced-page bootstrap integration; TornPageItem and accepted transfer/overflow commit methods remain unchanged.

- Admission validates GUID, positive declared size <=262144, supported 512 configuration, connected RPC sender and exact live owner inventory before allocation. Shared session records allow one per sender/two per manager. Preparation acknowledgement precedes chunks; new concurrent requests reject without replacing old records.
- StoreChunk checks canonical 1000-byte slots with subtraction-safe bounds. Identical duplicates do not change coverage/time; conflicts cancel. Full receipt count, not last offset, detaches chunk writes and triggers finalization.
- One server record retains buffer/bitmap until full coverage, then one bounded page-readiness continuation. Update checks 30-second unscaled inactivity and exact target reference/handle/owner/scene/manager; paired departure/session/despawn/disable/destroy hooks clean owned work. There is no completed-operation history or producer inventory-commit ledger.
- JPEG SOF0 dimensions are bounded before LoadImage; successful 512x512 decode is required before one spawn. The unhanded page is temporarily nonlootable using the existing ItemLoot flag already honored by pickup validation. Readiness checks replace the fixed DOTween delay; existing TornPage initialization/distribution remains unchanged.
- TryBeginForcedPageDelivery synchronously takes one exact page/version before returning true. Only then does Notepad release its page reference, buffer/admission, consume the source and report tutorial success. Inventory submits once through existing pickup/ForcedOverflow after owner pending/waiting/resolving work is idle. It settles its pointer through the existing exact Reply or item/owner/connection lifetime loss. A bounded owner readiness wait can explicitly abandon before submission, leaving the World page; it never rolls back or retries an accepted transfer.
- Local source page remains locked while pending. Disable cancels/stops local byte sending but keeps only source intent metadata until ordered success/cancellation feedback; immediately restoring it could duplicate an already handed-off page. Identity/session loss drops that intent. No buffer, retry/history or reconnect persistence is retained.

Verification complete for the implementation step: normal Unity 6000.3.10f1 compilation and PurrNet.Codegen.PostProcessor passed after the final production changes. Temporary Editor reflection harness passed 289 chunk/completeness/JPEG-header assertions, including all 263 maximum-size slots in reverse order. No standalone ILPP executable was used. The temporary script, its metadata and newly created Editor folder/metadata were removed. Editor-generated material/FMOD cache changes were restored to the initially clean baseline. git diff --check passes. Manual multiplayer verification was subsequently completed by the user as recorded below; Codex did not independently rerun those multiplayer tests.

The C04 implementation source comparison confirmed serialized fields, drawing/initialization/audio/public notebook methods, ForcePickupClientRpc, WaitForPickup, BeginPending/BeginPickup and accepted TryPickupServer/TryForcedFallbackServer were unchanged. A subsequent focused navigation correction restores remaining-page rotations after successful tearing and cancels an outstanding page-turn tween before resetting the view; it does not alter upload bounds/chunks or TornPage/inventory delivery. Source review covers exact host/client target routing, independent quotas, sender isolation, expiry/disconnect/replacement/teardown cleanup and no producer cleanup after handoff. These remain source-level checks; the supplied multiplayer results are listed separately below.

## Manual multiplayer verification and resolution — 2026-10-04

**C04 status: RESOLVED / RUNTIME VERIFIED.** The user confirms the following manual multiplayer results on the current local main checkout:

| Runtime check | Result |
| --- | --- |
| Host draw -> tear -> TornPage delivery | PASS |
| Client draw -> tear -> TornPage delivery | PASS |
| Near-simultaneous host/client tears | PASS |
| Exact target inventory delivery | PASS |
| Full-inventory World fallback | PASS |
| Page tear/navigation presentation regression | Fixed and manually retested PASS |
| Host/client page navigation after tears | PASS |
| One TornPage per tear | PASS |
| Console errors during verification | NONE |

The intermittent notebook issue where it is visibly held but drawing/navigation input is inactive occurs before upload. It remains an open H01/M05 interaction/lifecycle investigation, with its exact trigger unconfirmed by the qualified note in docs/TECH_DEBT.md. It does not keep C04 open. H03 observer image replay, general starting-item lifecycle and M05 module/surface placement also remain separate findings. No production changes or additional runtime tests were made for this documentation closure.
