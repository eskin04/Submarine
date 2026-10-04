# H01 / M05 — scope review: supported module interruption and teardown

Review baseline: current local `main`, commit `7442a59`, 2026-10-04. The only initial working change was this untracked plan; Sonar.mat was clean. Read AGENTS.md, ARCHITECTURE.md, CODING_STANDARDS.md, NETWORKING_STANDARDS.md, TECH_DEBT.md and the previous plan. Rechecked project source and installed PurrNet/DOTween. This revision changes only this document. No implementation, Unity compilation or new runtime verification has occurred.

## 1. Decision and core problem

**OPTION 2: split the original task.** First implement supported module interaction interruption/teardown, including the small player-binding prerequisite it needs. Follow with independent player/session/subscription cleanup. Do not carry every H01 finding into the first commit.

The first task has **four required production files**: ModuleInteraction, PlayerInventory, InventoryManager and NotepadModule. Slice A and the interaction-specific portion of Slice B must be implemented atomically. The remainder of Slice B and Slices C/D remain separate work. This is not a claim that all player/session lifecycle debt is resolved.

Atomic invariant: from successful local module admission until final restoration, exactly that interaction owns its captured player resources and animation writers. Normal exit or any supported interruption ends gameplay ownership once, cancels obsolete writers, restores surviving resources to their captured state and clears the owners' interaction references. No old attempt can acquire control or mutate a later attempt. An intentional normal return/closing animation may remain owned until it completes; terminal interruption cancels it immediately.

C04 remains resolved/runtime verified, including actual Unity JPEG50 characterization, compilation/codegen and supplied host/client delivery/overflow/simultaneous-tear tests. The last-page predecessor rotation fix remains resolved/runtime verified. Preserve both.

The single held-but-unusable notebook occurrence remains **unconfirmed and not reproducible after extensive subsequent stress testing**. No failing live state/root cause was captured. It is not the architecture driver or a promised fix outcome.

## 2. Proven source failures and exact scope

Source proof describes reachable behavior under the stated trigger, not a newly reproduced runtime failure.

- ModuleInteraction.Interact disables the controller, changes parenting/camera/cursor/physics and starts unretained move/rotation tweens. OnDestroy only unsubscribes; there is no OnDisable restoration. StopInteract gates all cleanup on a surviving playerCameraTransform. Losing one reference therefore skips restoration of unrelated surviving state.
- SetInteractPosition and SetCameraPositionBack retain none of their four tween handles. Re-entry during return leaves old pose writers and a completion setting isInteracting=false. Explicit StopInteract during opening leaves an older completion setting it true. These are concrete races independent of the notebook observation.
- PlayerInventory.OnSpawned notifies OnAssignController before publishing its three statics. No despawn invalidation exists. ModuleInteraction retains the tuple and overwrites it on assignment. An active module can outlive the captured player, or restoration can use a replacement tuple instead of the player it disabled.
- InventoryManager already has substantial correct despawn cleanup. However, SetInteractItemParent starts two unretained interaction-offset tweens. SetNormalItemParent kills the currently selected transform only. Server despawn releases live held items before final local cleanup, which clears currentSlotIndex and subscriptions. An old interaction offset can then write a released item's World pose; a later camera-end event can no longer select that item. currentInteractable/currentFocusedInteractable/isHeldItemHidden are also not cleared by existing despawn hooks. This is a direct interaction consumer dependency, not a transfer-authority defect.
- NotepadModule has unretained cover-open/close tweens and a retained pageTurnTween. Exit can start closing while opening/page-turn writers survive. OnDisable/final OnDespawned/OnDestroy currently clean upload lifetime, not presentation flags/writers. A terminal exit can leave feature input ownership or an old completion changing isAnimating on a later entry. Generic module cleanup cannot own those private flags/page rotations.

Scope begins with an admitted module interaction. Handle partial mutation of that admission safely. General rejected-entry readiness policy, all arbitrary UnityEvent listeners, interactable-only disable, generic focused-target destruction and selective network-backend replacement while keeping unrelated presentation alive are separate concerns. Do not add a generic admission framework or infer notebook diagnosis from them.

Supported interruption routes: explicit StopInteract, ModuleInteraction component/root disable, object despawn/destruction through the installed hierarchy, captured local-player invalidation, scene unload/session teardown, and re-entry during return. Include partially initialized cleanup and supported pooled object re-enable. Host migration and arbitrary external TriggerDespawnEvent calls with presentation deliberately retained are not supported by this boundary.

## 3. Challenge of every original production file

Classification: **A required core; B optional defensive; C separate lifecycle debt; D no change required for this task.** Only A is authorized in the first implementation. Paths are repository-relative; method names identify current source evidence.

| Original file | Confirmed failure / owned state / reachability | Existing API and decision |
| --- | --- | --- |
| `Assets/Scripts/InteractionSystem/Interactions/ModuleInteraction.cs` | Uncancelled entry/return writers, no disable restoration, all-or-nothing camera-null gate. Owns controller lock, module/camera pose, physics mutation, cursor/camera signals, prompts and captured binding. Directly reachable in every scoped interruption. | **A.** Existing StopInteract is inadequate. Add one idempotent owned restoration path, exact handles and lifecycle routing here. |
| `Assets/Scripts/InteractionSystem/Core/Interactable.cs` | Owns isInteracting and feature UnityEvents. Missing disable cleanup and early rejection leaving the flag set are real broader concerns. For an admitted module the flag can already be cleared. | **D.** Existing StopInteract clears the flag before invoking feature stop listeners. Call it once from the module's guarded ending path. No new duplicate active state, generic admission inspection or UnityEvent rewrite. Interactable-only disable/rejected-entry debt is deferred. |
| `Assets/Scripts/InteractionSystem/Core/Interactor.cs` | currentInteractable is raycast focus, with keys/look-hit/outline UI; it is not the active module owner. Interface references can outlive destroyed focus targets. | **C.** Focus may legitimately be null during active interaction. Module teardown must not clear a new focus. Existing OnLoseFocus/raycast path suffices for ordinary focus changes; generic destroyed-focus/disable hardening is separate M05 work. |
| `Assets/Scripts/InventorySystem/PlayerInventory.cs` | Owns published local controller/camera tuple. Assignment precedes publication; no lifetime invalidation. A surviving module can retain the despawned player. | **A.** No existing invalidation API. Publish atomically and add identity-qualified interaction binding invalidation at final despawn/destroy and publisher disable. This narrow portion of Slice B is a prerequisite. |
| `Assets/Scripts/InventorySystem/InventoryManager.cs` | Owns active interaction reference, hidden-item presentation and offset writers initiated by module camera events. Despawn can release the live tween target before cancelling these writers; final cleanup drops selection/subscriptions first. | **A.** Normal camera-end restoration exists but is insufficient after release/selection loss. Track only the two interaction offsets and captured target; clear exact interaction state before held-item release/local unbinding. Transfer/bootstrap/C04 bodies remain unchanged. |
| `Assets/StarterAssets/FirstPersonController/Scripts/FirstPersonController.cs` | Local clears only on destruction; OnDestroy omits framework base. Owns its independent static, simulation/input/controller internals. | **C.** Module uses PlayerInventory's tuple and captured controller.enabled, not FirstPersonController.Local. Its existing enabled property is sufficient. Independent static/base cleanup belongs to later Slice B; normal movement disable must never mean player invalidation. |
| `Assets/Scripts/Notepad/NotepadModule.cs` | Owns isInteracting/isAnimating, cover/page writers, drawing cursor and stroke continuity. Terminal cleanup and opening/closing cancellation are absent. Directly reachable on notebook interruption. | **A.** Existing feature stop starts another animation; upload lifetime cleanup does not reset presentation. Add tiny presentation-owned reset/cancellation, preserving page data/index and the verified rotation rule. |
| `Assets/Scripts/Player/CameraLayerController.cs` | Owns computed normal/interaction masks and existing event listeners. A missed end signal leaves the interaction mask. No source proof requires changing its listener architecture for this boundary. | **D.** Existing public OnInteractionEnded signal restores its normal mask. The module can additionally restore its captured live camera mask. Enabled listener pairing would be **B** defensive work; do not include it. |
| `Assets/Scripts/GameStates/MainGameState.cs` | Host Enter(bool) binds resume/quit twice; normal Exit(bool) removes both. Update can handle Escape outside the current node; quit dispatch is unguarded. Owns state/session callbacks. | **C.** Real independent H02/Slice C debt. Module/player lifecycle hooks handle supported teardown even when this state dispatches twice. Do not change scene submission or callbacks in the first task. |
| `Assets/Scripts/Station/StationController.cs` | OnDestroy omits base, bypassing this identity's remaining framework despawn/ticker cleanup. Owns station state/listeners/registration. StopInteraction already calls module StopInteract for active interactables. | **C.** Base-destroy repair is important but not prerequisite: child ModuleInteraction disable/destroy restores its own session. No station terminal forwarding required by the normal installed despawn path. Registration/delayed message/base cleanup belongs later. |
| `Assets/Scripts/Station/Hull Breach/Technician/HullBreach_CrackSocket.cs` | Existing OnDespawned(bool) already calls module StopInteract or feature OnStopInteract. Owns welding references and accepted socket state. An ordinary stop may start a module return. | **D.** Generic disable/destroy cancels that return, including when Interactable's flag is already false. Existing feature stop ends the drill/references. No Hull hook or admission redesign required. |
| `Assets/Scripts/Station/Hull Breach/Technician/HullBreach_FoundryController.cs` | Existing despawn removes plate association, not print timer/busy state. Module presentation may be active, but its generic component is disabled/destroyed by normal object despawn. | **C.** No hard dependency for a new module hook. Deferred H01/H03 print/root-tween/busy-state debt is feature-owned. Keep accepted plate transfer architecture closed. |

## 4. Why Hull/Foundry/station hooks leave scope

Installed PurrNet 1.19.1 `Runtime/CoreModules/HierarchyV2/HierarchyV2.cs`, Despawn at 1401–1485, triggers network callbacks, unregisters identities and puts the object back through HierarchyPool. `Pool/HierarchyPool.cs:132–191` deactivates pooled virtual nodes with SetActive(false), or destroys non-pooled objects; the scene-pool path at 197–242 also deactivates pooled children. This supplies generic Unity disable/destroy cleanup for the inspected module objects. Do not assume a network hook alone disables an arbitrary retained presentation; the evidence is the normal hierarchy path.

Socket: its existing network callback ends welding and calls StopInteract. The generic module's terminal cleanup must still cancel return handles when logical interaction has already ended. This needs no socket edit.

Foundry: a new generic OnDisable handles its module resources. Resetting printingTimer/isPrinting or cancelling the printed network root's feature tween is a separate invariant. No failing module state bypassing the normal hierarchy cleanup has been established that requires a Foundry hook now.

Station: children receive their own disable/destroy. StationController's missing base can lose framework cleanup for that identity, but fixing it is not needed to restore the child's captured player/camera. No station-wide component scan or new registry is needed.

If a future supported path deliberately retains active presentation after independently removing its backend, design its forwarding explicitly then. Do not use hypothetical pooling/host-migration requirements to expand this task.

## 5. State ownership and capture

| Owner | Capture / mutations | Restoration responsibility |
| --- | --- | --- |
| ModuleInteraction | Exact admitted controller/player identity; controller.enabled; interaction camera parent, local pose and activeSelf; module parent/home pose; rb.isKinematic; collider.enabled; main camera/mask; cursor lock/visibility when unlocking; issued signals/prompts. | Restore surviving captured objects independently, not generic true/false defaults. Keep immutable home baseline until return is completed/cancelled; re-entry must not capture the halfway return pose as home. Never restore into a replacement player's tuple. |
| PlayerInventory | Published local controller/camera tuple and publishing identity. | Publish before notification, notify end before replacing/forgetting the old binding, clear only its matching tuple. An old owner cannot clear a newer player's statics. |
| Interactable | IsInteracting and feature stop event. | Existing StopInteract; module marks itself ending before calling it once, preventing recursive/double feature exit. No target calls through a destroyed Unity-backed interface. |
| InventoryManager | currentInteractable, own focus cache, isHeldItemHidden, exact offset target/two tweens. | Clear matching active interaction; restore only still-owned hidden presentation. Kill offsets before target release/selection discard. Own despawn clears its own focus cache; ending a module must not erase unrelated new focus. |
| NotepadModule | isInteracting, isAnimating, cover/page handles, isDrawingCursorActive, lastDrawPosition, cover/page presentation. | Cancel exact feature writers, clear flags/cursor/stroke continuity and normalize current page presentation. Preserve pages/textures/currentPageIndex and upload state. |
| Interactor / controller internals / session managers | Focus and raw input/simulation/session registrations. | Remain their owners. Module only restores captured controller.enabled; it does not rewrite input maps, simulation, focus or session state. |
| CameraLayerController / CursorManager / UI/highlight | Existing interaction signals and presentation APIs. | Module sends matching end signals only if it issued start; use surviving public APIs. No infrastructure edit or private-mask access. |

Entry captures the baseline before the first mutation, claims the narrow current-local-module handle and starts exact owned entry writers. While active, only that captured binding may receive module input. Exit first ends gameplay ownership and cancels entry writers; restoration/signals/reference release do not wait for the visual return to finish. Normal return retains only the module's return writers. Terminal cleanup cancels all remaining writers and restores/snaps live resources immediately.

Cursor ownership is limited to changes this module made. Restore the captured lock/visibility if no existing SettingsView overlay currently owns them; preserve that overlay's existing cursor policy. Clear only module-owned hover/custom-cursor state and prompts. Do not invent a global camera/cursor ownership service. If a parent has been destroyed, detach a surviving camera safely; do not dereference the lost parent or abort the rest of cleanup.

## 6. Owned cleanup and references

Use one private idempotent cleanup path in ModuleInteraction with normal/terminal intent and a small transient phase (idle/entering/active/returning/ending as needed). A narrow current-local-module reference permits captured-player invalidation; it is not a project-wide lifecycle registry. Guard re-entrancy before invoking existing stop events. Keep public serialized UnityEvent targets, Interact and StopInteract signatures intact.

Bind/unbind PlayerInventory assignment/invalidation with ModuleInteraction's enabled lifetime. A late-enabled module reads the published tuple. Finish an old attempt before replacing its binding. Cleanup remains safe if Awake/admission completed only partly, a service vanished, or disable/despawn/destroy calls repeat. StopInteract must cancel a return even after its active flag was cleared.

InventoryManager needs a narrow identity-qualified end/forget helper for module interaction, called by the captured module at end and by inventory despawn **before** releasing held presentation, clearing selection or subscriptions. It also requests current module end for its captured player if despawn occurs first. PlayerInventory invalidation invokes the same idempotent module end. This avoids dependence on component/network callback ordering.

Track and kill only the two item interaction-offset handles against their captured target, not DOTween.Kill on every tween attached to that transform. On normal end restore through existing parent/offset behavior only while accepted inventory membership still belongs to this owner. If a target was already released, cancel obsolete writers without reparenting it or restoring hidden state into World. Clear reference/hidden state explicitly; do not rely solely on next-frame Update. No StopAllCoroutines on normal module exit and no transaction cancellation/retry.

## 7. Player/controller invalidation decision

**Slice A plus this narrow Slice B portion are atomic.** A module-only OnDisable solution cannot handle its local player being removed while the module remains active. PlayerInventory is the existing binding publisher; adding matching invalidation here avoids changes to MainGameState or every station.

Use final parameterless OnDespawned for identity-lifetime invalidation, OnDestroy as fallback preserving framework base, and publisher OnDisable for component/pooled reuse. Re-enable republishes only a live local spawned identity; OnSpawned performs normal initial publication. Validate ownership when publishing, but do not rely on isOwner remaining true during cleanup: match the exact previously published identity. Notify teardown while captured references are still available, then clear the matching tuple.

PurrNet NetworkIdentity.TriggerDespawnEvent at 1271–1335 invokes side hooks separately and parameterless OnDespawned when the last side leaves; flags are cleared afterward. Do not invent an onDespawned event or wait for isSpawned polling. Host cleanup must be idempotent and local-only.

FirstPersonController.Local and its missing base OnDestroy remain separate Slice B debt. Clearing that static is not required to restore the controller instance captured through PlayerInventory. No broad player lifetime redesign or ownership-change protocol belongs here.

## 8. Notepad-specific decision

**Required tiny feature-owned changes**, based on source writers/flags, not the unconfirmed incident. Retain one coverTween plus pageTurnTween. Cancel an earlier cover/page writer before a conflicting entry/exit starts. Normal close keeps its existing timing and presentation; terminal end cancels cover/page writers and leaves isInteracting=false, isAnimating=false, drawing cursor cleared, lastDrawPosition reset, cover closed and current page stack normalized to the existing currentPageIndex/active-page rule.

ModuleInteraction provides a narrow instance terminal notification after the existing Interactable.StopInteract event. Notepad subscribes to that module instance and resets its presentation; no generic teardown interface/component is introduced. Its own OnDisable/final OnDespawned/OnDestroy also request idempotent module end and reset presentation. Guard feature stop during disabled/terminal cleanup so it cannot start a fresh closing writer after an earlier reset. This covers ModuleInteraction-only disable as well as notebook-root disable, regardless of callback ordering.

Do not alter upload limits/chunks/sender validation/handoff, localSend coroutine semantics, TearPage delivery, audio setup, page count/content, navigation rules or last-page predecessor normalization. Keep C04's existing lifecycle calls intact. No automatic entry retry or notebook incident-specific repair.

## 9. Interrupted timelines and tween semantics

Installed `Assets/Plugins/Demigiant/DOTween/DOTween.XML` documents TweenExtensions.Kill(Tween,bool): complete=true completes before killing. Use **Kill(false)**; this terminates the owned writer without running its completion. OnKill is a separate synchronous callback if assigned. Set the ending guard first, kill each exact handle, then clear it; avoid completion/OnKill code that re-enters or clears a newer handle. A completion already executed before cancellation is handled by normal phase/restoration logic.

| Timeline | Writers / cancellation | Callbacks and final ownership/state |
| --- | --- | --- |
| A: enter → opening → active → exit → return | Entry move/rotation (and feature cover), then exact return/close handles. | Current completions alone may advance presentation. Gameplay ownership ends at exit; surviving player/camera/physics/references restored; normal return completes home pose and clears handles. |
| B: exit before opening completes | Kill entry move/rotation and conflicting feature opening/page writers before exit. | Old entry completion cannot activate the ended attempt. Normal return/close may run; terminal exit snaps instead. |
| C: re-enter during return | Kill old return/close before admitting the next attempt; settle old restoration first and retain original home baseline. | Old completion cannot clear the new flag or move its pose. Only new handles own the new attempt. |
| D: disable during opening/active/return | Terminal path kills all exact module/feature handles, even if Interactable's flag is already false. | No return completion remains. Restore all surviving captured resources independently, release matching references, leave idle presentation. |
| E: supported despawn/destroy | Same terminal path via feature/player final hooks and generic hierarchy disable/destroy. Cancel inventory offsets before released-item mutations. | Repeated callback order is harmless; no old writer targets a released item/new player. Missing resources are skipped individually. |

**No generation/token protocol is required.** These races have explicit synchronous entry/exit and retained tween handles. Verify Kill(false) behavior with the installed assembly during implementation. Do not introduce a central tween registry, cancellation framework or generation IDs unless new concrete asynchronous evidence invalidates this model.

## 10. First implementation files and invariants

Required production files only:

1. `Assets/Scripts/InteractionSystem/Interactions/ModuleInteraction.cs`
2. `Assets/Scripts/InventorySystem/PlayerInventory.cs`
3. `Assets/Scripts/InventorySystem/InventoryManager.cs`
4. `Assets/Scripts/Notepad/NotepadModule.cs`

Testable invariants:

- Exit/terminal cleanup is idempotent and safe after partial initialization.
- Each admitted attempt invokes existing feature stop once; cleanup recursion cannot reacquire control.
- Every owned writer is killed before its handle/target is discarded or reused.
- Older entry/return/cover/page/item-offset writers cannot affect the next attempt.
- Movement, camera parent/pose/activation/mask, cursor policy and collider/Rigidbody restore captured surviving state; a pre-disabled controller/collider remains disabled.
- Each owner clears only its matching active reference. Focus and active interaction remain distinct.
- Player invalidation ends the captured attempt before replacement/forgetting; old teardown cannot clear the new player.
- Item-offset cancellation precedes accepted held-item release; cleanup never reverses an inventory commit.
- Normal interaction timing, page data, C04 delivery and authority remain unchanged.

## 11. Networking and serialization impact

Local presentation/restoration only. No new RPC, RPC direction/attribute change, SyncVar/schema change, authority/ownership change, prediction change or Network Rules edit. Preserve server held-item release and all existing commit/version semantics. Parameterless final lifecycle hooks avoid assuming bool callbacks run once on host. Restore only the captured local peer's resources.

No serialized field rename/type change, public UnityEvent target removal, new component/NetworkBehaviour conversion, prefab/scene/ScriptableObject/package/ProjectSettings or FMOD change. Private transient fields and narrow local notifications/helpers suffice. Regression risk is medium: callback ordering, normal/terminal animation distinction and captured-state restoration need host/client verification.

## 12. Implementation steps — future work only

1. Reconfirm main/status and current prefab enter/stop bindings; preserve unrelated changes. Recheck callback ordering and captured inventory membership before coding.
2. Implement PlayerInventory atomic publication and exact-owner invalidation, plus ModuleInteraction's captured restoration, owned tween handles, narrow active-module reference and enabled bindings. Treat these as one API change.
3. Add InventoryManager's exact offset ownership and matching interaction cleanup. Route despawn before release/unbinding. Keep accepted transfers/bootstrap/upload handoff untouched.
4. Add Notepad's exact cover ownership/presentation terminal reset and instance module notification. Preserve existing normal presentation, C04 calls and last-page rule. Exercise both component/root disable ordering.
5. Review four-file diff for event recursion, missing-resource paths, serialized/networking changes and accidental deferred fixes. Compile through normal Unity 6000.3.10f1 Editor Refresh or supported batch workflow/PurrNet codegen; never standalone ILPP.Trigger. Run git diff --check and the core gate.

Keep the four-file invariant coherent in review/rollback; do not ship half-integrated invalidation helpers. Roll back this scoped change together if needed, preserving committed C04/Inventory-Hull/presentation fixes. Stop and revise scope if another production dependency is actually proved necessary; do not add optional fixes while coding.

## 13. Core implementation gate — planned, not run

Use a real host and remote client with both role assignments, existing domain/scene reload-disabled settings, and normal supported scene teardown. Controlled temporary runtime inspection is acceptable; remove it afterward.

| Cases | Required evidence |
| --- | --- |
| Normal movable/stationary enter → active → exit; repeated cycles | Existing feature behavior/timing; exactly one stop; captured movement/camera/cursor/physics restored. |
| Explicit exit during opening; re-enter during return | Old move/rotation/cover completions never modify the new attempt; no halfway-home baseline capture. |
| Module component/root disable during opening, active and return | Immediate surviving-state restoration; feature flags/writers cleared; repeated disable/destroy safe. |
| Supported hierarchy despawn/destroy active or returning; scene unload | Pooled deactivate or destroy reaches generic cleanup; socket's prior stop does not leave a return writer. Test Notepad and Foundry module presentation without altering printing/authority. |
| Captured player despawn/replacement while module survives; session restart | Identity-qualified invalidation, no stale tuple/control, no old cleanup clearing replacement. No unrelated manager subscription certification. |
| Held item during offset movement and owner despawn | Cancel exact offsets before World release; no later World pose mutation, no accepted membership rollback. Matching hidden/reference state cleared. |
| Notebook opening/closing/page-turn interruption | Feature-only flags settle; cover/page writers cancelled; current content/index retained; verified tear/navigation presentation preserved. Not a claim of reproducing the incident. |
| Missing camera/parent/UI; pre-disabled movement/collider; non-default physics/mask; settings overlay | Independent cleanup of remaining resources, captured state rather than defaults, existing overlay cursor policy respected. |
| Host/client and final review | No stale active interaction, old tween effect or console errors; normal compilation/codegen and diff check pass. Small host/client tear/delivery/full-inventory regression confirms C04 preservation. |

Do not gate this task on unrelated round-reset, forced-startup request or session-manager subscription tests.

## 14. Deferred lifecycle slices and follow-up tests

- **SLICE A — module teardown:** the first task above. Interactable-only disable/rejected-entry policy and Interactor destroyed-focus cleanup remain separate M05 work; do not conflate focus with activity.
- **SLICE B — player/despawn references:** first task includes only PlayerInventory binding invalidation and its direct inventory/module consumers. Follow-up covers FirstPersonController.Local/base lifecycle and any independently proved remaining player references. Verify retained player replacement and existing SettingsView controller use.
- **SLICE C — session/subscriptions:** MainGameState host duplicate callbacks/current-state Escape/terminal request guards; Flood/Level/Stress spawn-to-destroy registrations; Stress unmatched lambda and delayed activation; identity-safe service unregistering; StationController/Contract/TutorialWait missing base calls. Separate implementation units where owners differ. Verify host callback counts, retained spawn/despawn/rebind and no callbacks after lifetime end.
- **SLICE D — other lifecycle debt:** Contract delayed loading/waits/page presentation, TutorialWait machine-owned coroutine after Exit, station delayed messages/registry replay, Foundry print/busy/root tween, generic starting/forced-request suppression/outcome policy, TornPage/Keycard payload replay and H06 readiness/reconnect membership. Give each its own invariant and plan; preserve completed authority/C04 work.

First task readiness: **YES**, within this four-file boundary. This is design readiness, not compilation/runtime certification. H01/M05 remain partially resolved/High until their independently tracked debt is addressed; the notebook occurrence remains unconfirmed. No production code was changed during this review.
