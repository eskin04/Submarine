# Session / subscription lifetime cleanup

Planning baseline: current local `main`, `80a5b21`, 2026-10-06. No other checkout or worktree was used. Read AGENTS.md, ARCHITECTURE.md, CODING_STANDARDS.md, NETWORKING_STANDARDS.md, PLANS.md, TECH_DEBT.md and LIFECYCLE_MODULE_TEARDOWN.md. Re-audited the seven candidate files and locally installed PurrNet **1.19.1** (`Library/PackageCache/dev.purrnet.purrnet@f4dd26fb792b`). Evidence below is current source/control-flow evidence, not a new runtime reproduction.

Initial unrelated changes: FMODStudioCache.asset and the Oswald Bold SDF, Roboto-Bold SDF and LiberationSans SDF - Fallback TMP assets. Sonar.mat was clean. Preserve all unrelated changes; do not stage them. Only this plan is created. No production implementation, compilation or runtime tests were performed during planning.

C04 and the FIRST module interaction teardown slice are **RESOLVED / RUNTIME VERIFIED** in their documented scopes. H01/M05 remain **PARTIALLY RESOLVED / HIGH**. Do not reopen those completed slices or claim a diagnosis of the unreproduced notebook incident.

## 1. Problem and decision

Some callbacks are owned by a state visit, some by a spawned manager and some by an object lifetime. Current code mixes these boundaries. A host adds two local MainGameState handlers per visit; retained managers keep spawned subscriptions after despawn; an anonymous StressManager handler cannot be removed by a second lambda expression; three overrides bypass their identity's destruction fallback.

**Recommend OPTION 2: separate A, state/session subscriptions, from B, framework/network-object destruction cleanup.** Within A, use independent small commits rather than one manager-wide patch:

- **FIRST implementation slice A1: MainGameState callback ownership only.** One production file, one state-owned invariant, directly prevents duplicate local quit dispatch. No dependency requires changing StressManager or StationController alongside it.
- **A2: StressManager registration/delegate pairing.** Separate follow-up; the anonymous handler and early/full-spawn subscriptions share that owner.
- **A3: FloodManager and LevelManager spawned subscriptions and service registration.** Separate follow-up; preserve early station-registration order. Can be split by owner if verification proves independent.
- **B: StationController, ContractManager and TutorialWaitState framework destruction fallback.** Separate from A1, with narrow owner cleanup before required base destruction. Pool/rebind policy and feature registries require their own characterization rather than an automatic three-file rewrite.

Classes below mean **A = required in FIRST A1**, **B = real debt, separate task**, **C = already safe/obsolete claim**. These classifications are different from the two implementation tracks named A/B above.

## 2. Current source evidence: exact subscribe/remove sites

Paths in this section are under `Assets/Scripts/`; line numbers refer to this planning baseline.

| Candidate / classification | Exact registration and subscribe site | Exact removal site / identity | Actual owner and current consequence |
| --- | --- | --- | --- |
| **MainGameState — A** (`GameStates/MainGameState.cs`) | `Enter(bool):24–25`: `SettingsView.resumeGame += CloseSettingsView`, `quitGame += QuitGame`, before side guard. Server branch: `restartGame += RestartGame` at 27; tutorial `OnTutorialFinished += FinishTutorial` at 30; normal `FloodManager.OnGameEnd += HandleGameEnd` at 36. | `Exit(bool):130–139` removes the same named target/method delegates. No owner-specific OnDisable/OnDespawned/OnDestroy removal fallback. | Current state visit, local-client and server groups separately. Host has two resume/quit entries while active; one quit action can initiate two voice-leave/session-stop/lobby-load executions. Independent node destruction/disable need not execute state-machine Exit, leaving static delegates. |
| **StressManager — B** (`UtiltyEvents/StressManager.cs`) | `OnEarlySpawn:35–37`: GlobalEvents `OnRegisterUtilityStation/RegisterStation`, `OnAddStress/IncreaseStress`, `OnReduceStress/DecreaseStress`. `OnSpawned:44`: `MainGameState.startGame/StartStressManager`; 45: `currentStress.onChanged += (newVal) => ...SetStressText(newVal)`. | `OnDestroy:55–64` calls base and removes global/start handlers by matching named identity; line 62 subtracts a **different lambda expression**, not the delegate added at 45. No despawn or enable/disable pairing. | Spawned manager subscriptions. Retained despawn/respawn adds duplicate named and onChanged handlers. Old startGame callbacks can schedule ActivateSystem after despawn; duplicated active stress handlers can apply amounts multiple times. UI callbacks can update a later view or throw if MainGameView is absent. The onChanged publisher is the owner's SyncVar, not a separate static memory leak by itself. |
| **StationController — B** (`Station/StationController.cs`) | `Awake:35`: `stationState.onChanged/OnStateChanged`. `OnSpawned:49/53` publishes utility/main registration to managers on server; these are emissions, not subscriptions owned by the station. | `OnDestroy:58–61` removes the matching named SyncVar handler but **omits base.OnDestroy**. No station unregister event exists in this path. | The local listener is currently object-owned. Destruction before a prior full despawn bypasses remaining identity/module teardown. Registered station references can also outlive a removed station in manager lists, a distinct registry-membership issue. Do not treat base destruction alone as removal from these application lists. |
| **ContractManager — B; ordinary toggle pairing C** (`Contract/ContractManager.cs`) | `Start:52`: `acceptToggle.onValueChanged.AddListener(OnAcceptToggled)`. Start initializes presentation/readiness and runs once for this component instance. No network-spawn callback registers the toggle. | `OnDestroy:101–104`: matching `RemoveListener(OnAcceptToggled)`, but no null guard or base call. | Object-lifetime UI handler; no host-side double registration or repeated-enable multiplication is proved. A disabled/despawned retained manager remains callable from a surviving toggle, potentially submitting readiness RPCs outside its live lifetime. Independent missing-base fallback is real. Do not include contract loading RPCs/readiness membership in A1. |
| **TutorialWaitState — B for base call; event pairing C** (`Tutorial/State/States/TutorialWaitState.cs`) | Server `Enter(bool):18`: `MainGameState.startTutorial += OnMainTutorialStarted`, only when tutorial flag is not already set. | Handler removes itself at 24; server `Exit(bool):42–48` removes the same named handler; `OnDestroy:36–39` also removes it but omits base. | Server state visit. Normal event/Exit/destruction removal exists and uses matching identity. The machine-owned 2-second coroutine at 30–33 can transition after Exit, but that is separate delayed progression, not a missing event unsubscribe. Missing NetworkIdentity destruction fallback remains separate B. |
| **FloodManager — B** (`FloodSystem/FloodManager.cs`) | `OnSpawned:55`: `InstanceHandler.RegisterInstance(this)`; 57–60: `LevelManager.OnLevelStarted/StartFlood`, GlobalEvents `OnAddFloodPenalty/AddPenalty`, `OnStationStatusChanged/HandleStationStatusChanged`, `currentWater.onChanged/OnWaterChanged`. | `OnDestroy:64–73` calls base, unregisters by type and removes all matching named handlers. No despawn/enable pairing. | Spawned manager/service. Retained despawn leaves callbacks and a registered stale service; respawn adds delegates again. Duplicate station-status callbacks emit repeated BlackOut stat modifications; duplicate StartFlood resets pacing/queues repeatedly. Old destruction can remove a newer registered FloodManager because unregistration is type-only. Reset/game-end rules are separate. |
| **LevelManager — B** (`Level/LevelManager.cs`) | `OnEarlySpawn:23`: GlobalEvents `OnRegisterMainStation/RegisterMainStation`. `OnSpawned:29`: `MainGameState.startGame/StartLevel`; 32: `InstanceHandler.RegisterInstance(this)`. | `OnDestroy:35–41` calls base, removes matching named handlers and unregisters by type. No despawn/enable pairing. | Early/full spawned manager/service. Retained respawn duplicates start handlers; one start can select/shuffle stations and publish OnLevelStarted multiple times. Existing Contains protects list insertion, not the repeated StartLevel callback. Old instance destruction can unregister a replacement service. Station list removal/reset/replay is separate. |

## 3. Repeat/side/lifetime audit for every candidate

| Candidate | Host/client subscribe multiplicity | Enable/Disable | State Enter/Exit | Despawn/destroy survival |
| --- | --- | --- | --- | --- |
| MainGameState | Host bool Enter runs true then false: local resume/quit **twice**, server group once. Remote client local group once. Server-only currently also adds local handlers once. | No enable subscription, so Enable alone does not multiply. Disable does not remove existing callbacks; C# events can invoke disabled behaviours. | A correctly paired host visit adds 2 and removes 2; repeated balanced visits do **not** accumulate. Repeated/unpaired Enter or omitted Exit can accumulate. | StateMachine despawn normally calls Exit per active side; independently disabled/destroyed node cannot rely on that ordering. Inherited base destruction does not itself call StateNode.Exit. |
| StressManager | Parameterless early/full spawn hooks once for ordinary host spawn, once on each peer; not two per host. Server guards restrict stress mutation, not UI/start registrations. | No added duplicate from Enable; disabled owner still has listeners. | Not a StateNode; startGame emissions are events, not Enter hooks. | Normal destroy removes named external callbacks; lambda removal fails. Ordinary retained despawn leaves all; respawn multiplies. SyncVar pool reset clears onChanged, so pool-reset paths must not be mislabeled as guaranteed lambda accumulation. |
| StationController | Awake once; parameterless spawn once per host lifetime. Registration emissions server-only. | Awake not rerun; no enable multiplication. Disabled listener can still receive state changes. | Not a StateNode. | Own handler removed on destroy, base fallback missing. Prior explicit hierarchy despawn may already have cleaned framework state; omission does not prove every normal destruction leaks. Pool reset clears SyncVar handlers while Awake does not rerun: pooled listener rebind is distinct from base-call repair. |
| ContractManager | Unity Start once, not one call per host network side. Both peers each have their local UI instance. | No enable multiplication; listener survives disable. | Not a StateNode; no repeated Start on simple re-enable. | Own toggle removal is present on normal destroy if reference survives; retained despawn does not remove it, missing base remains. |
| TutorialWaitState | Enter true subscribes once; false does nothing. Remote client registers none. | No enable registration or removal. Current server wait can receive the event while disabled. | Normal server Exit/self-removal is paired; balanced visits do not multiply. Calling Enter repeatedly without Exit would add duplicates, not evidence that installed normal transition does so. | Destroy removes the named event, so a claim that it leaves startTutorial globally attached after ordinary destroy is obsolete. Missing base affects framework fallback separately. |
| FloodManager | Parameterless spawn once per host, once per peer. | No enable multiplication; disabled callbacks/service remain. | Not a StateNode; start event may invoke retained manager. | Named removal on destroy exists; retained despawn/respawn multiplies; type-only unregister can remove replacement. |
| LevelManager | Parameterless early/full spawn once per host, once per peer. | No enable multiplication; disabled callbacks/service remain. | Not a StateNode. | Same retained despawn/replacement risks; matching destroy removals already exist. |

## 4. Confirmed MainGameState failure and smallest correction

Installed StateMachine invokes `Enter(true)` when server, `Enter(false)` when client, then `Enter()` once (`StateMachine.cs:365–376, 548–555`). Host is both sides. MainGameState's lines 24–25 therefore append the same instance/method pair twice **before** its `if (!asServer) return`. `SettingsView.OnResume/OnQuit` (`UI/SettingsView.cs:53–66`) each invokes the corresponding static Action once; the Action invocation list contains the two entries.

Both handlers can actually execute twice. CloseSettingsView repeats HideView. QuitGame is async void: one multicast invocation can enter its body twice, call LeaveVoiceChannel twice and submit two lobby loads. Removing delegates during the first callback does not rewrite an already captured multicast invocation list. This is duplicate event dispatch, not proof of a separate async completion bug. RestartGame is added only in Enter(true), so it is **not** this host duplication.

Normal host Exit(true)/Exit(false) subtracts one matching local entry per call and leaves zero. Do not claim balanced state cycling grows the list. The smallest multiplicity correction is mutually exclusive side ownership: local settings handlers in the `!asServer` branch, server handlers in the server branch, with matching Exit branches. Parameterless Enter is an alternative but unnecessary; keep bool-side server progression unchanged.

For the full A1 lifetime invariant, **one local ownership flag is useful; one independent server-group flag is also needed**. These describe acquired registrations, not permission/authority or an async operation lock. They make duplicate Entry callbacks, overlapping Exit/Disable/despawn/destruction removals and current-state re-enable binding idempotent. Broad callback guards or a generic subscription framework are unnecessary.

## 5. Exact FIRST implementation scope and invariant

Only `Assets/Scripts/GameStates/MainGameState.cs` is required in A1. Audit/supporting reads may inspect SettingsView and installed PurrNet, but no change is needed there.

Invariant: while this node is enabled, live and current, each local settings callback belongs to its client-side state visit once, and each server callback to its server-side visit once. Exit/Disable or applicable despawn removes owned callbacks; re-enable restores only callbacks for a still-live current visit, without replaying startGame/startTutorial. Independent node destruction removes them before framework teardown. Cleanup is idempotent and cannot remove another node's delegates.

Scope is callback membership only. Preserve Enter server game/tutorial start order, machine transitions, settings presentation bodies, restart/quit bodies and public StartGame. Do not move Escape Update into StateUpdate in this slice, reset tutorial/restart flags, introduce quit-operation tokens, cancel an already-running async quit/restart, alter voice code, or modify scene loading.

## 6. Required subscription ownership table (A1)

| Owner | Event / publisher | Handler | Subscribe boundary | Unsubscribe boundary | Fallback teardown | Host/client | Idempotence rule |
| --- | --- | --- | --- | --- | --- | --- | --- |
| MainGameState client state visit | SettingsView.resumeGame | CloseSettingsView | Enabled Enter(false); eligible current-state OnEnable rebind | Exit(false), OnDisable, client OnDespawned(false) | Final OnDespawned and OnDestroy | Once host-local and remote client; none server-only | Local flag gates whole pair; same named target/method |
| Same client visit | SettingsView.quitGame | QuitGame | Same as resume | Same as resume | Same as resume | One local dispatch per event | Clear ownership before removing pair; repeated cleanup no-op |
| MainGameState server state visit | SettingsView.restartGame | RestartGame | Enabled Enter(true); eligible current-state OnEnable rebind | Exit(true), OnDisable, server OnDespawned(true) | Final OnDespawned and OnDestroy | Once on host/server; none remote | Server-group flag; do not replay start signals on rebind |
| Same server normal-game visit | FloodManager.OnGameEnd | HandleGameEnd | Enter(true) in existing normal branch; rebind same group | Server group cleanup, regardless of mutable isTutorial | Same server fallbacks | Host/server once; none remote | Remove both alternative end delegates safely; only own target |
| Same server tutorial visit | MainGameState.OnTutorialFinished | FinishTutorial | Enter(true) before existing startTutorial emission; eligible rebind | Server group cleanup | Same server fallbacks | Host/server once; none remote | Exactly one alternative end handler acquired; same target/method |

startGame and startTutorial are outgoing publications, not registrations to move to OnEnable. Preserve normal OnGameEnd attachment timing relative to startGame unless a separate re-entrant start failure is proved. Never remove another instance's delegates or clear entire static Actions.

## 7. Host/client and enabled lifetime behavior

Proposed Enter(false) binds the local pair and returns. Enter(true) acquires the server group while preserving the existing tutorial/normal branches and their publications. Matching Exit(bool) unbinds only that side. Client-only never owns restart/game-end/tutorial-finish handlers; server-only never owns resume/quit UI handlers. Host owns both distinct groups once.

OnDisable removes both owned groups; OnEnable rebinds only if `isSpawned && isCurrentState`, using live `isClient`/`isServer` to select groups. It must not emit startGame/startTutorial or run RestartGame/QuitGame. An inactive StateNode being enabled is not admission to this state. Enter on a disabled component may still be dispatched by the framework: skip binding while disabled, preserve existing server progression, and allow eligible re-enable to bind later. Normal network spawn is not a second state visit.

Side-specific OnDespawned(bool) unbinds that group's callbacks without relying on isServer/isClient having already changed; final parameterless OnDespawned removes any residue. Clear owned flags before removal. OnDestroy removes both groups, then calls base in finally. Shared helper removal must be safe before admission and after any combination of these boundaries. During machine teardown, Exit may already have removed the same group; repeat cleanup is harmless.

Already-running async handlers are separate lifetime work: removing a delegate prevents subsequent dispatch, not cancellation of a callback already selected/in flight. First-slice tests must distinguish that from a new post-teardown event invoking the old node.

## 8. Installed framework destruction semantics

Source truth: `NetworkBehaviour.cs` is an empty subclass of NetworkIdentity; `StateNode.cs` inherits it and has empty default Enter/Exit. `NetworkIdentity.cs:619–673` documents parameterless spawn/early-spawn/final-despawn once on host; bool variants run per side. Implementation at 1166–1335 invokes parameterless spawn on `_spawnedCount == 0`, early spawn once until reset, and final despawn after last side. Spawn flags are cleared **after** despawn callbacks. Never use an isSpawned check inside final removal to accidentally rebind.

`NetworkIdentity.OnDestroy:1117–1125`, except application quitting, calls TriggerDespawnEvent(false), TriggerDespawnEvent(true), then clears `_ticker`. TriggerDespawnEvent is side-spawn guarded, runs InternalOnDespawn, identity/external-module callbacks, decrements lifetime count and clears module collections on final exit. InternalOnDespawn at 492–514 removes registered tick/player/scene callbacks where applicable. SyncVar.OnDespawned (`NetworkModule/SyncVar.cs:163–175`) flushes as applicable and unsubscribes its tick manager; it does **not** clear arbitrary onChanged delegates. SyncVar.OnPoolReset at 74–84 does clear onChanged. These are distinct boundaries.

StateMachine.OnDespawned(bool):116–124 calls current node Exit(asServer); final hook at 126–133 calls Exit(). StateNode's inherited destruction does not itself call Exit; independent node disable/destruction cannot rely on the machine remaining available to do so.

StationController, ContractManager and TutorialWaitState all override OnDestroy without this base call. Their ordinary own handler cleanup does not substitute for identity/module finalization. Omitting base can leave remaining spawned/module/tick state unfinalized on direct destruction of a still-spawned component. These exact types do not implement IPlayerEvents/IServerSceneEvents/ITick; do not invent a direct station player-listener leak. SyncVars and framework identity lifetime are concrete affected systems. Normal HierarchyV2 destruction may have already called TriggerDespawnEvent; then the guarded base is a fallback/no-op, not evidence of a failure on every destroy.

HierarchyV2.ManualDespawn (`HierarchyV2.cs:1860–1875`) triggers despawn/unregister without requiring Unity destruction. This proves a retained-despawn path exists; supported game topology and reuse must be exercised before runtime certification. Pool deactivation alone is not proof that application static events were removed.

For future B repairs, detach owned handlers first and guarantee `base.OnDestroy()` in finally. Contract cleanup must tolerate an absent/destroyed toggle. Tutorial removal remains matching and idempotent. Station removes OnStateChanged before base/module finalization can call presentation; preserve module lifetime semantics. Do not clear StateChanged wholesale or assume this removes station membership from Stress/Level lists. Do not edit the package.

## 9. Proposed minimal fixes for deferred owners

- **Stress:** replace the two lambda expressions with one named `HandleStressChanged(float)` method (or one retained Action if unavoidable). Same target/method on add/remove. Use owner-local idempotent early/full-spawn subscription groups, remove at final despawn with destruction fallback, and define enabled policy in its separate plan. Parameterless hooks avoid host duplication. Preserve the early station-registration window. Keep Invoke(ActivateSystem), grace timers, stress rules and event selection out unless their cancellation is separately authorized; registration cleanup alone does not stop an already scheduled Invoke. Do not claim a named handler alone solves despawn lifetime.
- **Flood/Level:** pair acquired spawned groups with final despawn and safe destruction fallback. Before unregistering, `TryGetInstance<T>` and ReferenceEquals must prove the registry still points to this owner; no type-only removal of a replacement. Preserve Level early registration before stations publish. Define supported disable/re-enable and retained respawn boundaries without replaying level start or clearing game state. Do not blindly move everything to OnEnable or OnSpawned: registration events can already have fired. Registry list removal/replay is separate.
- **Contract:** matching toggle removal already exists. Missing base repair is separate B; retained disabled/despawned UI admission requires its own narrowly verified lifetime policy, not a guessed move of all Start setup to OnEnable. Exclude readiness membership and loading presentation RPCs.
- **TutorialWait:** subscription pairing on normal Exit/self-removal is already safe. Base fallback repair is separate B; do not fold the machine-owned delay into this event plan.
- **Station:** missing base fallback is real B. Listener rebind after SyncVar pool reset and application station-list membership are additional independent issues, not solved by one base call; characterize them separately.

## 10. Obsolete / resolved historical claims

1. No project script has OnDespawned / inventory local references are never cleaned: obsolete after verified earlier slices; not a new task premise.
2. C04 or module-teardown correctness remains open: resolved/runtime verified, excluded.
3. Stress/Flood/Level parameterless spawn hooks subscribe twice solely because host: false for installed 1.19.1. Retained respawn without removal is the actual multiplicity path.
4. MainGameState balanced Enter/Exit visits inevitably accumulate listeners: false; host adds two/removes two. Active duplicate dispatch and missing independent-node teardown remain.
5. TutorialWaitState lacks normal startTutorial event cleanup: false; handler, server Exit and destruction already remove matching delegates. Delay cancellation and base destruction are independent findings.
6. Contract toggle subscribe/remove identities mismatch or Enable repeats Start: false. Missing base and retained inactive UI admission remain.
7. Flood/Level lack normal destruction removal, or Stress lacks any named removals: false. Despawn pairing, Stress lambda and identity-qualified registry removal remain.
8. Required base OnDestroy can be replaced by arbitrary MonoBehaviour cleanup: false; installed package performs real identity/module finalization. Conversely, claiming every normal hierarchy destroy leaks is unsupported.

These corrections are recorded in this plan; TECH_DEBT statuses are not changed during this planning task.

## 11. Expected production files and deferred findings

**FIRST A1: exactly `Assets/Scripts/GameStates/MainGameState.cs`.** One-file implementation, small scope. If a hard dependency outside it is proved, stop and revise scope before editing. SettingsView and PurrNet require no change.

Separate A2: StressManager.cs. Separate A3: FloodManager.cs and LevelManager.cs, split further if appropriate. Separate B: StationController.cs, ContractManager.cs, TutorialWaitState.cs, only for independently reviewed framework/lifetime fixes. They are not required in A1 and should not share its first implementation commit.

Deferred: inactive-node Escape handling, restart/tutorial reset and already-running async quit/restart; Stress activation Invoke/timers; Contract delayed loading/readiness; TutorialWait machine-owned coroutine and tutorial breakdown; station delayed messages/list membership/pool-rebind/reset; remaining player/controller references; generic forced items; content replay; H06 reconnect/readiness; voice/Vivox; Interactor focus/admission; RPC cadence; broad class extraction; authority/Network Rules migration. Completed Inventory/Hull, C04 and module return/teardown stay closed.

## 12. Networking impact

A1 changes local/server callback membership, not transport or authority. No new or modified RPC, direction/attributes, SyncVar schema, NetworkIdentity relationship, ownership, prediction or Network Rules. Keep restart/game-end/tutorial progression server-side; local settings once per client, including host acting as its local client. Preserve existing outgoing start signals and scene operations. Do not replace installed PurrNet callback semantics with another framework. Server-only callback counts may be characterized, but dedicated-server gameplay support is not introduced/certified.

## 13. Serialization and behaviour risk

Serialization risk **Low**: retain class/file/meta, all public/serialized fields, enum types, UnityEvent targets and StartGame signature. Only private transient ownership flags/helpers and lifecycle overrides are proposed. No Prefab/Scene/ScriptableObject/package/ProjectSettings or FMOD change.

Behaviour risk **Medium**: side selection, disabled-current-state rebinding, teardown callback ordering and tutorial versus game-end attachment need real host/client checks. Do not use flags to suppress legitimate new game-start publications or alter async quit/restart. Do not subscribe inactive nodes on Enable. Application startup order for manager follow-ups must be preserved; those are not silently bundled with A1.

## 14. Implementation steps — future work only

1. Recheck main/status and this source baseline; preserve unrelated assets. Confirm completed statuses remain closed. Read guidance again if it changed.
2. Characterize MainGameState invocation-list counts for true/false Enter/Exit, including host-local events, with removable instrumentation/isolated checks that preserve real side dispatch. Record baseline duplicate quit without repeatedly performing destructive session exits in an automated fixture.
3. Move local resume/quit acquisition/removal into client-side branches. Keep server publications and server end/restart membership order unchanged.
4. Add narrow local/server acquisition flags/helpers; clear matching ownership on Exit, Disable and appropriate side/final despawn. Rebind eligible current state on Enable without emitting start signals. Add safe OnDestroy cleanup preserving base in finally.
5. Review entry while disabled, independent node versus machine teardown, partial entry and repeated removal. Confirm no callback remains in static invocation lists after owner teardown. Do not solve excluded Update/async/reset problems.
6. Normal Unity 6000.3.10f1 compilation and PurrNet codegen; never standalone ILPP.Trigger. Run git diff --check, serialized/API/networking diff review and remove all temporary verification code/metas.
7. Complete the verification matrix, then record only A1 as resolved within scope. H01/M05 and unrelated H02 remain qualified/open. Do not start A2/A3/B automatically.

## 15. Rollback strategy

Commit A1 separately with MainGameState and its authorized verification documentation only. Roll back that focused change as a unit if legitimate state admission/settings/session progression regresses. Do not revert completed module/C04/Inventory-Hull work, clear global publisher Actions, reset Network Rules, or include unrelated local asset changes. No serialized migration or prefab rollback is required. Keep baseline and post-change callback count evidence for review.

## 16. Verification matrix — planned, NOT RUN

| Test / topology | Required callback evidence | Visible/manual outcome |
| --- | --- | --- |
| First host Enter(true), Enter(false) | Local resume/quit count 1 each, server restart count 1; correct tutorial/game-end alternative count 1 | Resume hides Settings once; one quit request initiates one local leave/session stop/lobby load |
| Remote client Enter(false) | Local pair 1; server group 0 | Local resume/quit works once; restart remains unavailable as before |
| Server-side-only characterization | Local pair 0, server group once | No local UI lookup from a server-only settings subscription; does not certify gameplay topology |
| Matching host/client Exit | All this visit's delegates 0; each side removes only own group | No new event dispatch into old node |
| Second Enter and repeated balanced cycles | Same counts as first, never 2/3; one handler invocation per matching emitted event | Repeated settings use has no duplicate close/quit/restart effects |
| Repeated Enter without duplicate admission; repeated cleanup | Acquired groups do not multiply; repeated remove safe | No duplicate callback side effects; distinguish existing outgoing start signals from subscription ownership |
| Disable current owner, emit events, re-enable | 0 while disabled; eligible current group restored once; zero startGame/startTutorial replay on Enable | No old settings/progression callback during disabled lifetime; settings works after re-enable |
| Enable noncurrent node; Exit while disabled | No registration; flags stay clear | Enabling an inactive node does not gain settings callbacks |
| Node side/final despawn and independent destroy before machine | Matching groups removed regardless of callback order; zero post-teardown invocation; required base reached once/guarded framework cleanup | No NullReference/destroyed-object callback from events after teardown |
| Machine despawn/destroy before node; scene unload/session re-entry | Exit plus node fallback idempotent; static list has no old target; new visit owns only new delegates | One new session/settings response, no accumulated old scene effects |
| Normal game and tutorial on host/client | Existing server start order and one end/restart handler preserved; one event yields one expected server callback | Normal game-end/tutorial-finish transition unchanged; one restart submission |
| Delegate snapshot / in-flight async distinction | Removed handler absent from future dispatch; do not claim removal cancels a previously running async body | Separately record any old async completion as deferred, not a newly duplicated registration |
| Final checks | Compilation/codegen and diff check pass; serialized/RPC/authority surface unchanged; temporary files removed | Console: no exceptions, destroyed-object callbacks or duplicate event effects |

Use host plus remote client and relevant role-swapped flows. Count actual handler invocations; merely seeing an idempotent HideView is insufficient proof of one callback. Use supported scene/session re-entry and installed network despawn APIs; no invented pooling/reconnect topology. Capture subscription identity, owner instance and invocation counts at transitions rather than per-frame logs. Unsubscribe any test observers and restore static fixtures. This plan contains no test-pass claims for A1.

**FIRST slice design readiness: YES**, for MainGameState callback ownership only. Implementation/compilation/runtime readiness is not claimed. Stop after planning.
