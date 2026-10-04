# C04 — Notepad upload integrity and lifetime

Reviewed on current local main, commit 028874b, 2026-10-04. All required repository guidance and the original plan were read. This replaces the earlier conditional design. Only this plan is changed; no production implementation was performed. The Unity encoder characterization below was completed.

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

## Verification and readiness

The Unity encoder measurement above is complete. When separately authorized, implement incrementally and compile against installed APIs. Required regressions (not run): host/client swapped; representative pages and cap boundaries; invalid/null/overflow/misaligned chunks; identical/conflicting duplicates; final-first/out-of-order/missing-middle; foreign sender/target; repeated prepare/stale chunks; simultaneous players/same-sender second notebook Busy; disconnect/reconnect/replacement; module/target teardown/scene reload/session restart; 5/10 FPS/stalls/expiry and duplicate nonrenewal; corrupt JPEG/dimension bomb/decode failure; delayed spawn; busy inventory; empty/full accepted pickup/World fallback; rejection/despawn before submission; producer teardown immediately before/after handoff; accepted commit survives; all buffers/maps/table entries/coroutines/subscriptions/bootstrap pointers settle. Review serialization, authority/RPC semantics and prohibited-scope diff.

**Readiness: YES.** Actual Unity 6000.3.10f1 JPEG50 size/SOF/decode characterization passes with useful margin below 256 KiB. No remaining characterization gate blocks implementation. This is readiness to begin coding, not verification of the unimplemented refactor: compile/protocol/multiplayer regressions remain required. Stop this task after measurement and plan update.

