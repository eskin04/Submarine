# Tutorial quest readiness integrity

Date: 2026-10-09. Status: **RESOLVED / RUNTIME VERIFIED (user-reported test scope)**.

The focused six-file readiness slice is complete, with **10/10 reported manual tests PASS**. H01 and M05 remain **PARTIALLY RESOLVED / HIGH**. The committed TutorialWaitState, Quest6 delay/completion, audio/intro ownership and final-task color fixes are preserved. Runtime closure applies to the user's reported tests; isolated Editor evidence and remaining controlled verification gaps are recorded separately below.

## Problem

Tutorial progress and readiness commands identify a role, but not their originating quest admission. The server applies them to whichever quest is current on arrival. Shared lift/lever awards also lose their origin before reaching the receiving view. A command from Quest A must never change Quest B, including a later admission of the same quest index.

Completion can also call `machine.Next()` more than once before PurrNet drains its command queue in LateUpdate. Preventing stale readiness alone does not prevent duplicate transition admission in that interval.

## Evidence before implementation

The current-main audit is recorded in section 26 of `docs/plans/SESSION_SUBSCRIPTION_LIFECYCLE.md` and the Tutorial readiness finding in `docs/TECH_DEBT.md`.

- `TutorialQuestView.OnActionPerformed` sends `UpdateProgressServerRpc(role, completedTasksCount)`; `CheckAllTasksCompleted` sends `PlayerReadyServerRpc(role)`. Neither carries quest context. The view's `SequenceId` is a local presentation counter, not authority.
- `TutorialManager` writes progress/readiness and calls the current state's `CheckCompletion` without identifying the originating admission. Equal progress still publishes the progress event; repeated readiness still evaluates completion.
- In the actual serialized solo configuration, a host can finish A and enter B while the remote client is still displaying A. A valid remote A completion can subsequently write B's progress/readiness and satisfy solo completion. This is a source-confirmed acceptance path, not an observed runtime skip.
- ReliableOrdered delivery does not provide a global order across peers and opposing directions. Normal cooperative completion with one ready command per initialized role is narrower: both A-ready commands normally arrive before A advances. Latency alone does not prove a stale cooperative ready command.
- PurrNet permits state replacement/re-entry, including returning to the same node. Quest index alone therefore cannot distinguish two admissions of A. A -> B -> A remains NOT VERIFIED in gameplay, but must be included in this implementation's tests.
- `LiftManager.HandleLiftButtonPressed(int)` is currently both the local button event handler and an `ObserversRpc(runLocally: true)`. Its server branch calls the InputManager award RPC. Capturing context only in that server branch would already be too late.
- `EngineerLockDown_DoorSwitch.OnClicked` completes `OpenDoorLever` locally before sending the Engineer's `WaitDoorOpen` award. Host local completion can execute server readiness immediately; both effects must use a snapshot captured before either dispatch.
- The only current shared-award ServerRpc callers are LiftManager and DoorSwitch. The only current `LoadQuest` caller is TutorialQuestBaseState. The only progress-event subscriber is TutorialQuestView.

Installed PurrNet source of truth: `Library/PackageCache/dev.purrnet.purrnet@f4dd26fb792b`, package version **1.19.1**.

- `Runtime/Components/NetworkStateMachine/StateMachine.cs`: `Next` selects its destination immediately; `SetState` queues that destination; LateUpdate executes queued commands. Two Next calls while A is current enqueue B twice, rather than proving B then C. Old Exit hooks precede replacement; the state-change observer RPC precedes the new server Enter and subsequent quest-load RPC.
- `Codegen/PostProcessor.cs`: a ServerRpc called on the server executes its body immediately, including when `runLocally` is false. Preserve this host behavior.
- `Runtime/BitPacker/Packers/PackNetworkIdentity.cs` and `Codegen/GenerateSerializersProcessor.cs`: typed NetworkIdentity-derived references are registered and serialized by network ID and scene ID. A `TutorialManager` RPC argument can carry the existing network identity; unresolved references decode to null and must be rejected.
- Existing ServerRpc/ObserversRpc defaults use ReliableOrdered. Preserve directions, ownership exceptions and runLocally settings. Unsafe Network Rules remain intentional and unchanged.

## Proposed Change

### Identity model

Use **(owning TutorialManager network identity, questIndex, server-issued entryId)**. The entry ID is a nonzero `ulong`, allocated once for each actual server quest admission. Index **0 is valid Orientation**; only entry ID 0 denotes missing admission.

The manager keeps a private monotonic counter and active admission fields: exact state reference, quest data/index, entry ID and a completion-reserved flag. The base state retains its captured manager and entry ID. Keep these fields nonserialized. No per-request IDs, client generations, retry queues or generic subscription/networking framework.

- Allocate in the server `Enter(true)` path before reset/publication. Repeated initialization of an already owned admission must not allocate twice. A real Exit followed by Enter, including the same node/index, allocates a new ID.
- Do not reset the counter in ResetReadyStates, on disable or on retained despawn. Counter exhaustion fails closed rather than wrapping to an old ID.
- Namespace the counter by the owning network identity. Manager RPCs use their existing receiver identity implicitly; cross-system award RPCs explicitly carry the captured `TutorialManager` reference. Do not resolve a missing owner by substituting the latest singleton.
- Validate owner registration/current state/spawned lifetime. Release an admission only when both its captured state and entry match. Old state or old manager cleanup must not clear a newer admission.
- Exit(true), server-side despawn, disable and destruction invalidate owned admission before later commands can use it. Preserve PurrNet base hooks and host side/final hook behavior. Client Exit(false) must not independently reset server ownership. Re-enable alone does not invent an admission; a valid server Enter establishes it.
- This is admission ownership, not a redesign of retained singleton or session replacement behavior. Nonpooled network identities are the current supported setup. Network-ID recycling/pooling and reconnect reconciliation remain separate work.

### Entry synchronization

Extend the existing `RpcStartQuestBase` to carry `(TutorialManager questOwner, int questIndex, ulong entryId)`. Keep `ObserversRpc(runLocally: true)` and its existing timing. The server allocates the entry, then calls this RPC; host presentation receives that same server-issued value immediately. Remote participating observers receive it after the existing state-change RPC on the server's ordered stream.

Pass the context with the local quest data into `TutorialQuestView.LoadQuest`. Validate the exact current state, owner, quest data index and nonzero ID before loading or presenting anything. Store this context separately from the existing local SequenceId. All ordinary progress/ready dispatches use the stored context and captured owner, not a singleton lookup that could select a replacement.

Make context-aware LoadQuest report whether it accepted the admission. The base must subscribe to intro completion, capture SequenceId, set presentation ownership and show the view only after acceptance; a rejected old start must not leave false presentation ownership. Capture one immutable context snapshot at the beginning of local task completion and retain it across progress dispatch and readiness dispatch; re-check that the same presentation still owns it before the second dispatch.

Retain a client-side high-water mark of **server-issued** entries for the current owner across presentation cancellation. Ignore duplicate/older quest-start packets before LoadQuest and before changing intro ownership. For a new current owner, initialize a separate high-water mark; reject packets from a noncurrent owner. This is received-ticket validation, not a client-generated authority token. Preserve exact-sequence audio cancellation and final green/checkmark settling.

Existing quest-start RPCs are unbuffered. This plan delivers context to currently participating observers; it does not claim to repair late observer/current-state replay. A client without a valid quest-start ticket cannot send readiness and cannot award a tutorial task. Do not add buffering, replay or automatic singleton fallback silently. If supported normal startup cannot deliver the ticket with the existing ordering, stop and report the concrete dependency before widening scope.

### Server validation before effects

Provide a small manager-owned admission validation method, shared by the two readiness/progress receivers and the award bridge. It must check, before writes/events/observer forwarding:

1. A valid server lifetime and active admission exist; the exact captured state is still the manager's current quest state.
2. Owner, quest index and nonzero entry ID match that admission.
3. Completion is not reserved.
4. Role is Engineer or Technician and uses that admission's configured task list. Reject None/unknown roles rather than allowing them to evaluate completion.

For progress, accept a strictly increasing completed-task count within the configured role's task-definition count. Equal/decreasing/out-of-range values produce no write and no progress event. Count completed task definitions, **not** action repetitions or the sum of requiredAmount. Match the view's distinct action-key task representation; verify the checked-in lists contain no duplicate action keys before implementing this bound. Stop on incompatible content rather than changing assets.

Read-only asset inspection for this plan confirmed unique action keys in all fourteen role lists. Completed-definition bounds (Technician / Engineer) are: Orientation 2/2, Quest1 2/2, Quest2 5/5, Quest3 7/7, Quest4 2/2, Quest5 1/4 and Quest6 1/1. Derive bounds from the admitted quest data in production rather than hard-coding these observations.

For readiness, require the accepted role progress to equal that role's configured completed-task count. Reject already-ready roles without another completion evaluation. Current view dispatch order is final progress then readiness; installed ReliableOrdered and immediate host execution preserve that order. Keep all configured repetition thresholds, hidden/unlocked task gates, and solo OR/co-op AND predicates.

Scope `NotifyProgressChangedObserversRpc` and `OnPlayerProgressUpdated` with the same owner/index/entry. The view must match its admission before caching progress or updating waiting text. Cache accepted partner progress for that view admission and use it when entering the waiting presentation; do not initialize waiting text from a naked, potentially previous-entry SyncVar value. Reset the cache with presentation context. Preserve existing SyncVars and server ownership; no independently synchronized ticket fields are required.

This is gameplay consistency validation. Do not add sender authentication/role reassignment or change Unsafe rules as part of this slice.

### One-shot transition admission

`TutorialQuestBaseState.CheckCompletion` first validates its captured server admission and the current readiness predicate. It then asks the manager to reserve completion for that exact admission **before** calling Next or publishing terminal completion. Subsequent calls for that entry cannot reserve again, including reentrant event listeners and two role completions in the same frame before LateUpdate.

Do not reserve in PlayerReadyServerRpc before invoking the virtual CheckCompletion. Quest6 already reserves its own terminal publication in its override before calling base; the new manager reservation is independent and must allow that first base call.

- For indices other than 6: reserve, call existing `machine.Next()` once, retain the reservation until Exit/new admission. If Next returns false, release only the matching reservation, retain readiness, and report the rejected transition without adding automatic retries. A transition-configuration failure is a blocker to investigate, not permission to bypass CanEnter/CanExit.
- For index 6: reserve before invoking `MainGameState.OnTutorialFinished`; publish once and do not call Next. Preserve Quest6's existing delay cancellation and completionPublished guard without modifying that class.
- A queued external transition is not owned or cancelled by this guard. If another production caller bypasses the common completion flow, report that hard dependency instead of redesigning StateMachine.

### Shared lift and lever context, end to end

**Lift:**

1. LiftButton's unchanged `Action<int>` invokes a non-RPC `LiftManager.HandleLiftButtonPressed(int)` wrapper.
2. That wrapper snapshots the local view's owner/index/entry before the first network dispatch. It calls a private `ObserversRpc(runLocally: true)` carrying floor plus all three context fields.
3. Preserve the existing lift movement/door/button/tween body. Before local `SendElevatorItem`, use a context-checked view overload; do not stamp the receiving view's current quest onto the arriving command.
4. The server branch forwards the **original** owner/index/entry to `CompleteTaskForPlayerServerRpc`, with the unchanged opposite-floor role mapping and WaitForPartner action.
5. The bridge validates server admission before its observer award RPC. That observer RPC carries the original context again; the target-role client checks it against its view before OnActionPerformed.
6. Any resulting progress/readiness uses that same view ticket and captured manager. A missing/stale tutorial ticket suppresses only tutorial awards; physical lift behavior remains functional outside the tutorial and during stale tutorial delivery.

Keep LiftButton's signature and subscriptions unchanged. The award remains at button handling, not delayed until lift arrival. Preserve all existing physical RPC authority/runLocally semantics.

**Lever:**

1. After the existing OnClicked rejection guards, snapshot owner/index/entry before `RequestEngineerDoorOpenRPC` or local completion.
2. Keep the physical door request, animation, sound and their timing unchanged. That physical ServerRpc -> observer door event route does not publish tutorial readiness and needs no signature change.
3. Use the snapshot for local OpenDoorLever and for the Engineer WaitDoorOpen bridge ServerRpc -> observer award -> target view -> progress/ready flow. Never recapture after local host completion.
4. Preserve `switchHandle`, PlayerStats and role-routing conditions. Validate shared action membership for the target role in the server's active quest data before forwarding; do not turn physical door control into tutorial authority logic.

Current WaitForPartner (Quest3) and WaitDoorOpen (Quest5) have requiredAmount 1. Existing action caps suppress repeated valid awards within an entry. Context checks suppress old awards on later entries. Do not claim generic deduplication of arbitrary repeated-action awards or introduce per-award request IDs.

## Files Affected

Expected implementation scope is exactly these six existing production files:

| File | Required change |
| --- | --- |
| `Assets/Scripts/Tutorial/State/TutorialManager.cs` | Server admission ownership/counter, validation, exact release/reservation, context-aware progress/ready RPCs and progress event. |
| `Assets/Scripts/Tutorial/State/TutorialQuestBaseState.cs` | Server Begin/End admission, quest-start context delivery, guarded common completion, teardown qualification. Preserve scoped intro ownership. |
| `Assets/Scripts/Tutorial/Quest/TutorialQuestView.cs` | Store server ticket independently of SequenceId, reject old starts, snapshot dispatch context, context-checked shared-action overload, scoped partner progress/cache. |
| `Assets/Scripts/Tutorial/TutorialInputManager.cs` | Carry owner/index/entry through both shared-award RPCs; validate before forwarding and before target-role task application. |
| `Assets/Scripts/Lift/LiftManager.cs` | Local event wrapper captures context before the first observer RPC; thread context through both local and shared awards. |
| `Assets/Scripts/Station/EngineerLockDown/EngineerLockDown_DoorSwitch.cs` | Snapshot before physical/local dispatch and thread it into both tutorial awards. |

Concrete signature targets (parameter names/order can follow repository style):

- Manager `UpdateProgressServerRpc(int questIndex, ulong entryId, int roleInt, int currentProgress)` and `PlayerReadyServerRpc(int questIndex, ulong entryId, int roleInt)`: receiver is the captured owner.
- Manager `NotifyProgressChangedObserversRpc(int questIndex, ulong entryId, int roleInt, int currentProgress)`; local event adds the emitting manager reference and both context fields.
- Base `RpcStartQuestBase(TutorialManager questOwner, int questIndex, ulong entryId)`.
- View `LoadQuest(TutorialQuestData data, TutorialManager questOwner, int questIndex, ulong entryId)`; retain ordinary `OnActionPerformed(TutorialAction)` for existing synchronous callers and add a context-checked overload for network awards. Expose only the small context snapshot/match operations these callers need.
- InputManager ServerRpc and observer award both receive `(TutorialManager questOwner, int questIndex, ulong entryId, int targetRoleInt, int actionTypeInt)`.
- Lift retains local `HandleLiftButtonPressed(int)` and introduces the private floor-plus-context observer RPC. Update only the two current bridge publishers and the view's two manager RPC dispatches.

No additional production hard dependency was found. Read-only dependencies include LiftButton, TutorialOrientationState, TutorialQuest6State, EngineerLockDown_StationManager, PlayerStats, TutorialQuestData/assets and installed PurrNet. If implementation reveals another required production edit, stop before editing it and report the dependency. No new generic context/service file is planned.

## Networking Impact

RPC argument lists and generated method layout change. All peers must run the same updated build; mixed old/new builds are unsupported. Allow normal PurrNet codegen and verify typed manager-reference serialization. Do not run standalone Unity.ILPP.Trigger.

Keep ServerRpc ownership exceptions, observer directions/runLocally flags, existing NetworkIdentity relationships, role routing, server-owned SyncVars and server-authoritative progression. Host calls continue to execute immediately. No Network Rules, transport, prediction, package or MCP configuration changes. No client-local SequenceId is transmitted as authority.

## Unity Serialization Risk

Only nonserialized admission/context fields and code signatures change. Do not rename existing serialized fields, alter quest data/task lists, UnityEvents, prefab/scene references or ScriptableObjects. Preserve LiftButton event binding to the same local handler name and signature. Verify the private RPC rename has no serialized event reference before implementation; report any contrary wiring instead of editing assets.

## Behaviour Risk

- Orientation remains quest index 0 with its existing ReadContract/SignContract thresholds. The base flow supplies its ticket; Orientation requires no production edit.
- Ordinary task input, audio timing, subtitles, intro gates, checkmarks, final green settling and delayed reveal cancellation remain unchanged. Existing source/audio/color assertions should be retained and extended, not replaced with a smaller suite.
- Solo accepts either completed role; co-op requires both. A shared partner award arriving after a legitimate solo transition reservation is discarded rather than attributed to the next quest. Physical lift/door actions still proceed.
- Server completed-task bounds must match the view's distinct task representation. RadioTalk/RadioListen repetitions, RipPage and TurnManualPage thresholds must not be mistaken for the count of completed task definitions.
- Missing tickets fail closed. Late local-player initialization, contextless stale feature callbacks, replay/reconnect, static manager/service replacement and startup-lock cancellation remain independently OPEN. A stale local publisher that freshly looks up the new view can still misattribute its own semantic event; this plan protects captured commands, not all unrelated feature callback lifetimes.
- Do not widen scope to ContractView, TutorialOrientationState or unrelated station reset debt. Preserve previously reported PASS results; A -> B -> A is still an unperformed runtime gate, and no separate host/client color evidence is inferred.

## Implementation Steps

1. Recheck main/status and the connected Editor; preserve unrelated changes. Reconfirm scoped callers, task-key uniqueness and PurrNet identity serialization. Capture existing compilation/Console baseline.
2. Implement manager admission state/counter, exact lifecycle release and validation first. Keep reservations specific to the captured entry.
3. Extend base admission/start RPC and view context delivery together. Preserve local audio/intro SequenceId logic; prevent old/duplicate starts from cancelling a newer presentation.
4. Update the two view dispatches, both manager RPCs and scoped progress notification/cache. Validate before any effect; reject duplicates without raising events. Add the common completion reservation after the ready predicate and before Next/terminal publication.
5. Thread original context through both InputManager RPCs, the lift's first RPC and lever capture point. Check every hop and both local awards. Missing context must not disable physical gameplay.
6. Review all call sites and compatibility; compile through the normal Unity 6000.3.10f1 Editor/PurrNet workflow. Stop on any extra production dependency. No standalone ILPP execution.
7. Run focused isolated Editor/source verification, remove temporary verifier code/metadata, refresh normally and verify final cleanup compilation. Review scoped diff/status; do not stage/commit automatically.
8. Document only the implemented readiness slice as awaiting manual runtime verification until the manual gates below pass. Do not resolve H01/M05 or other Tutorial findings.

## Verification

### Automated/source gates for implementation

Tests must exercise guards and side effects, not merely count text matches. The existing read-only discovery found no project Tutorial test suite; choose isolated Editor tests or the established temporary verifier workflow. Never mutate live scene objects to fabricate evidence.

| Case | Required result |
| --- | --- |
| Valid entry, progress then ready | Correct role count/one progress event; unchanged solo/co-op completion predicate. |
| A command after server enters B | No count/ready write, progress event, award observer publication or progression. Assert all effects individually. |
| A -> B -> A, including same-node re-entry | Old A entry rejected; new A entry accepted despite matching quest index. One fresh server ticket per real admission. |
| Duplicate/equal/decreasing/out-of-range progress; invalid role/zero ticket | No effects; valid subsequent progress still succeeds. |
| Duplicate readiness and two legitimate role completions before LateUpdate | At most one queued transition from the old admission; no repeated B admission. |
| Next returns false | Only the exact reservation is released; no forced transition/retry or clearing a replacement entry. |
| Quest6, duplicate and reentrant completion | One terminal event; no Next; existing delay/coroutine ownership tests remain valid. |
| Host immediate calls and remote ordered calls | Final progress is accepted before ready; no dependence on SyncVar delivery timing or client-generated counters. |
| Lift's first hop delayed until a new entry | Original header survives floor RPC, server bridge and observer award. Both local SendElevatorItem and remote WaitForPartner reject stale context; physical movement remains unchanged. |
| Lever local completion followed by award | Both dispatches use the pre-completion snapshot, including host solo completion; stale WaitDoorOpen cannot credit a later entry. |
| Valid repeated shared awards | Current requiredAmount=1 tasks credit once; unlocked/intro/action caps and target role remain correct. |
| Progress observer delayed; view reuse/old start RPC | Wrong owner/index/entry cannot update waiting text, cancel current audio or replace current UI; equal start ticket is idempotent. |
| Exit, disable, retained despawn, destroy, replacement cleanup | Admission closes before effects; old cleanup cannot clear new ownership. Counter continues on retained reuse. Valid later Enter delivers a fresh ticket. |
| Orientation and all configured role lists | Index 0 accepted; role mapping and completed-definition bounds match current assets; repeated-action thresholds preserved. |

Retain existing recorded PASS evidence: **528 audio/intro Editor assertions, 31 color Editor assertions, 24 original source assertions, 8 focused color source assertions, Unity compilation and PurrNet codegen**. Those are previous results, not new verification of this plan. Add readiness-specific tests without claiming their results before execution.

### Manual host/client gates after implementation

1. Normal Orientation through Quest6 in solo and co-op; swap Engineer/Technician host/client roles. Verify role-specific task thresholds, one waiting/progress presentation, terminal completion once and no skipped tasks.
2. Near-simultaneous final tasks and same-frame completion before LateUpdate. Count quest admission/progression directly where needed; functional behavior alone does not prove cardinality.
3. Supported latency scenario: server enters B while the remote remains on A in solo. Deliver A's completion and verify B's counts/readiness remain untouched; valid B completion still works.
4. Controlled stale/duplicate command tests and explicit A -> B -> A/same-node re-entry. Use a focused temporary test harness if no normal input path exists; do not invent a production navigation/debug API. Record separately that this re-entry test was actually performed.
5. Quest3 lift in both directions and Quest5 lever with both role assignments: valid target award once, delayed old award rejected, physical motion/door timing unchanged. Include the first lift hop, not just the final award receiver.
6. Exit/teardown/re-entry and supported retained lifetime, including old cleanup after replacement. Test missing-context behavior without claiming late observer/reconnect replay has been implemented.
7. Recheck ordinary/final checkmarks and green color in at least two quests, normal audio/intro/UI, waiting text and no stale audio callbacks. Record actual host/client coverage rather than extending earlier color evidence.
8. Compare Console with the baseline: no new readiness exceptions, missing-reference callbacks or duplicate progression. Preserve unrelated errors for their own work.

### Read-only planning verification (before implementation)

Official Unity MCP `editor_status`, `recompile_status` and `console_status` confirmed the current checkout, **Unity 6000.3.10f1**, stopped Play Mode, not compiling, and compilation up to date with no compilation failure. Current Console baseline remains **4 errors / 8 warnings**; this is not a clean-runtime claim. Earlier audit inspection attributed baseline entries to unrelated Outline/AudioManager and historical verifier messages.

No Play Mode, Editor C# execution, test execution, compilation request or codegen request was performed during planning. Earlier discovery listed one package-only Edit Mode test, not a project Tutorial suite. Installed PurrNet 1.19.1 source was inspected; no package/configuration change was made.

## Implementation checkpoint — 2026-10-09

Implemented exactly the six production files listed above; no additional production dependency was required. The existing quest-start observer RPC distributes the manager/index/nonzero server entry ticket. The view captures that ticket independently of its presentation SequenceId. All progress/readiness and shared-award paths preserve origin, validate before effects, and reject stale or duplicate admission. Waiting progress is scoped to the ticket. Common completion reserves the entry before queued Next or terminal publication.

Two focused refinements handle synchronous callbacks: server Exit closes admission before presentation cleanup; progress/readiness re-check the entry after SyncVar writes, and readiness evaluates its captured state rather than selecting a replacement state. Isolated reentrant-listener checks cover both paths. Lift capture is before the first observer hop; lever capture is before physical/local dispatch. Existing physical timing/role mapping, solo/co-op predicates, index 0, repetitions and Quest6's terminal override are preserved.

Verification performed through official Unity MCP:

- **251 isolated Editor assertions PASS** on the final source, superseding the earlier 236-check run. Coverage: both role orders in simulated host/server and solo/co-op paths, valid/invalid/stale/duplicate commands, progress notification dispatch cardinality, one queued Next before LateUpdate, A -> B -> A tickets, scoped client presentation/progress cache, shared-award validation and generated observer dispatch, actual Quest3/Quest5 role-task membership, SyncVar-triggered re-entry, 20 retained disable/despawn cycles, replacement safety, counter exhaustion and Quest6 terminal completion once.
- **31 focused source assertions PASS** for all six files and **19 compiled-IL/RPC-signature assertions PASS**, including validation/reservation ordering and original-context capture before the first lift/lever dispatch.
- The fixtures used hidden temporary objects, synthetic server/client flags and compiled handlers without starting peers, transports or Play Mode. The bridge's original compiled ServerRpc body was invoked directly to isolate admission from network lookup; its generated observer sender was exercised with no observers. PurrNet primitive/identity packers were temporarily initialized in memory for Edit Mode. These tests do not prove real transport latency, identity resolution on remote peers, physical lift/door animation or native multiplayer lifecycle scheduling.
- No temporary source verifier, metadata or diagnostic hooks were added to the repository. Fixture objects, ScriptableObjects, delegates and singleton references were cleaned/restored in finally blocks. Final normal compilation/assembly reload resets temporary in-memory native packer initialization.
- **Final clean Unity 6000.3.10f1 compilation and normal PurrNet codegen PASS.** Official MCP reported completed, failed=false, no errors and compilationFailed=false after `CompilationPipeline.RequestScriptCompilation(CleanBuildCache)`. The 19 compiled-IL/signature assertions passed again against the final assemblies. Read-only cleanup checks confirmed zero fixture objects, native test packer initialization cleared by reload, stopped Play Mode and an unmodified/not-dirty LobbyScene. Scoped Git whitespace checks passed, including the untracked plan. No temporary verifier files or metadata exist.
- Final Editor session Console has **zero errors and 19 warnings**, all from unchanged Dreamteck/PurrLobby source (obsolete APIs/unused members); none identify the six changed scripts. The earlier session also surfaced an unrelated FMOD EventCache duplicate-key deserialization exception during normal refresh. No FMOD repair was attempted. Existing FMOD/TMP local asset changes remain untouched; their hashes were unchanged across the final cleanup compile/checks.

Preserve earlier recorded results (528 audio/intro Editor, 31 color Editor, 24 original source and 8 color source assertions; prior Unity/codegen and user-reported normal audio/UI/color PASS). They were not rerun as complete suites in this readiness checkpoint. A -> B -> A passed only the isolated admission tests; actual controlled runtime re-entry remains **NOT VERIFIED**. The implementation checkpoint above records automated verification; subsequent user manual evidence is recorded below. No separate host/client color evidence is inferred.

## Remaining risks and readiness

The readiness implementation remains within the six-file boundary above. The user subsequently reported **10/10 readiness tests PASS**. The individual test identities and host/client/role combinations were not separately enumerated, so the earlier proposed test matrix is not claimed as comprehensively completed. Controlled stale-RPC injection and explicit A -> B -> A/same-quest runtime re-entry remain verification gaps; **A -> B -> A controlled re-entry is NOT VERIFIED**. Observer replay/reconnect remains **OPEN**; missing context fails closed.

Only this readiness slice is **RESOLVED / RUNTIME VERIFIED (user-reported test scope)**. H01/M05 remain **PARTIALLY RESOLVED / HIGH**. Other Tutorial findings stay OPEN. Documentation changes are limited to this plan, SESSION_SUBSCRIPTION_LIFECYCLE.md and TECH_DEBT.md. Unrelated assets are not edited or staged by this task. Nothing is staged or committed.

## Lift follow-up fixes and manual closeout — 2026-10-09

- **Falling cargo: FIXED / MANUALLY VERIFIED.** The original deposit made lift cargo a dynamic World body; presentation restoration also discarded the moving-child path. ItemLoot preserves the server's accepted parent and restores client parenting through PurrNet's stored child path. The source defects existed before the readiness changes.
- **Hovering cargo: FIXED / MANUALLY VERIFIED.** Immediate kinematic attachment prevented settling. The server now wakes deposited cargo with gravity enabled, waits for Rigidbody sleep, and secures its physics-settled pose before LiftManager starts observer movement. Arrival remains lootable and ordinary World drops retain gravity. This is a follow-up physical transport correction; readiness award context and role routing remain intact. Lift physics evidence: **154 Editor assertions and 17 source assertions PASS**, plus normal Unity 6000.3.10f1 compilation/PurrNet codegen and scoped diff checks.
- **Host pickup leaving the client light ON: FIXED / MANUALLY VERIFIED in the tested multiplayer flow.** The old OFF receiver rejected the update while its replica cargo parent was stale. LiftManager now derives occupancy on the server after accepted transfers and applies that result on observers without a hierarchy veto. Remaining cargo keeps ON; final removal turns OFF. Server reconciliation covers removals without pickup events, without adding a duplicate replicated light state. Lift light evidence: **115 occupancy assertions, 34 physics assertions and 12 source assertions PASS**, plus normal compilation/codegen and scoped diff checks.

The user confirmed all three Lift fixes. No unreported host/client sender/retriever matrix, controlled stale-RPC injection or comprehensive runtime coverage is inferred. The follow-up production scope is **ItemLoot.cs and LiftManager.cs**; the combined checkpoint contains the original six readiness scripts plus ItemLoot.cs. Physics verification used isolated Unity Editor fixtures, including copied lift-floor collider geometry; it does not substitute for multiplayer transport tests. Fixtures were removed and no temporary verifier files were retained. All earlier automated and reported manual PASS evidence remains preserved.
