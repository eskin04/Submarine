# Inventory transfer consistency and Hull Breach placement

Status: **Step 1 characterization and Step 2 dormant foundation completed; authority activation not implemented. Dependency-safe sequence revised on 2026-10-04.** Original plan date: 2026-10-03.

## Problem

This is a trusted two-player Engineer/Technician co-op game. Unsafe Network Rules are intentional and remain unchanged. The objective is consistent shared possession and socket state, not comprehensive cheat prevention. Local selection, item sway, tool input and hand/camera feedback remain responsive and owner-controlled.

At the characterized gameplay baseline, inventory membership exists only on the owner; replicated extraction reads that owner-only state on the host. Step 2 now allocates the existing containers on the host and initializes possession/inventory/socket versions, but legacy transfers do not maintain these records yet. Socket insertion still consists of separate extraction and placement requests. Hull plate placement clears the current slot of every observer's LocalPlayer instead of consuming the identified placing player's plate. Socket occupancy, physics, ownership and item presentation do not have one accepted transition. Current selected slots, object parents and activeSelf are used as indirect truth.

The planned behavioral change is explicit: **the host commits shared transfers and Hull occupancy/completion; the owner controls local input and immediate held visuals.** This is a focused authority/protocol change, not a general cleanup or a Network Rules migration.

## Evidence

### Instructions and baseline

All seven required guidance documents were found, nonempty and read from this current worktree. No other checkout was accessed. This plan follows the required PLANS.md sections. Step 1 review baseline: 1160fc735658513bd7dca31d45bbc6c83676816d; historical flow references below describe the legacy gameplay methods. Step 2 foundation additions are present in the current worktree; no production files change during this sequence revision.

Local project metadata identifies Unity 6000.3.10f1; local guidance identifies PurrNet 1.19.1, locked at `266cb63efd3d858d6d2fce68c2b0b2364ca78c24`. Historical source line references were recorded at commit `874539b72a7ad000af250a45bca8b42887742b8e`. Installed package implementation was verified locally in Step 1 below. Step 2 passed Unity's C# compiler and IL postprocessor pipeline, including PurrNet codegen, plus 20 generated-payload packing round trips; editor reload and two-peer lifecycle/gameplay tests were not observed. Future payload changes require fresh verification. Source analysis is not a reproduced multiplayer result. The user's gameplay clarification supersedes security-first wording in the earlier audit; Unsafe itself is not registered as a defect in this plan.

### Source aliases

All paths are relative to `Assets/Scripts/`:

| Alias | File / class |
| --- | --- |
| INV | `InventorySystem/InventoryManager.cs` / InventoryManager |
| LOOT | `InventorySystem/Item/ItemLoot.cs` / ItemLoot |
| PLAYER | `InventorySystem/PlayerInventory.cs` / PlayerInventory |
| HULL | `Station/Hull Breach/HullBreach_StationManager.cs` / HullBreach_StationManager |
| CRACK | `Station/Hull Breach/Technician/HullBreach_CrackSocket.cs` / HullBreach_CrackSocket |
| PLATE | `Station/Hull Breach/Technician/HullBreach_PlateItem.cs` / HullBreach_PlateItem |
| FOUNDRY | `Station/Hull Breach/Technician/HullBreach_FoundryController.cs` / HullBreach_FoundryController |
| CHARGE | `Station/Hull Breach/Technician/HullBreach_ChargeStation.cs` / HullBreach_ChargeStation |
| WELD | `Station/Hull Breach/Engineer/HullBreach_WeldPoint.cs` / HullBreach_WeldPoint |
| DRILL | `Station/Hull Breach/Engineer/HullBreach_DrillItem.cs` / HullBreach_DrillItem |
| WELDER | `Station/Hull Breach/Engineer/HullBreach_WelderItem.cs` / HullBreach_WelderItem |
| MODULE | `Station/Hull Breach/Technician/HullBreach_PlateModule.cs` / HullBreach_PlateModule |
| CARD | `Station/Keycard Matrix/Keycard_Socket.cs` / Keycard_Socket |
| DISPENSER | `Station/Keycard Matrix/Keycard_Dispenser.cs` / Keycard_Dispenser |
| PAGE | `Notepad/TornPageItem.cs` / TornPageItem |

### Current-flow inventory: questions 1–5

“Owner” refers to PurrNet ownership; a held item can be owned by its player while its NetworkTransform is disabled. This is distinct from having reliable shared possession. Host player code and host server code share a process; remote owner code and server code are separate instances.

| Flow | 1. File/class, methods | 2. Current caller | 3. Current execution side | 4. Current owner/controller | 5. Current RPC path |
| --- | --- | --- | --- | --- | --- |
| F01 pickup | LOOT.LootItem:16 → INV.HandleLootAttempt:331 / PickupServerRpc:355 | Local Interactable UnityEvent raises global OnLootAttempt; owner inventory chooses a slot | Caller executes runLocally body; host receives server body; observers apply settings | Inventory owned by player; item ownership given to inventory.owner in both prediction and server paths; NT then disabled | ServerRpc(runLocally:true, requireOwnership:false) → server ObserversPickupRpc(default runLocally:false) |
| F02 equip / held view | INV.EquipSlot:261, HideCurrentItem:306, RefreshActiveSlot:274, SetInteractItemParent:91, SetNormalItemParent:111 | Owner hotkeys/scroll, local camera/interaction events | Owner only for equip; no selected-item replication | Owner-local containers/currentSlotIndex; item still player-owned, NT disabled | None; local SetActive and reparent/tween |
| F03 normal drop | INV.DropCurrentItem:434 / DropServerRpc:493 | Owner G input, local raycast/drop position | Caller prediction and server; observer settings | Item ownership removed only server; NT re-enabled; unowned owner-auth NT falls back to server control | ServerRpc(runLocally:true) → server ObserversDropRpc |
| F04 lift drop | INV.HandleLiftDrop:474 → DropServerRpc | LiftManager global OnDropItemToLıft event; each inventory filters isOwner | Same as F03, with client-chosen lift parent/random position | Same as F03 | Same drop RPC, parentObj supplied |
| F05 extraction | INV.ExtractCurrentHeldItem:221 | CHARGE.TryInsertDrill or CARD.TryInsertCard | Caller runLocally; server remote inventory usually returns at currentSlotIndex == -1; caller also originates observer RPC under Unsafe | Item ownership is retained; NT re-enabled by extracted settings | ServerRpc(runLocally:true) → ObserversExtractRpc(default runLocally:false), not restricted to server call site |
| F06 charge insertion | CHARGE.TryInsertDrill:83 / CmdPlaceDrillInStation:107 | Local socket interaction reads LocalPlayer drill | Local extraction first; server command; runLocally observer body on server plus observer clients | Drill retains previous player owner; slot field is established in observer body | F05, then separate ServerRpc(requireOwnership:false) → ObserversRpc(runLocally:true) |
| F07 charge removal | LOOT/INV pickup → CHARGE.Update:51 / ServerHandleDrillRemoved:100 | Player loots slotted drill; host polls parent/activeSelf | Pickup prediction/server; host polling; observer clear | Pickup sets new player owner; charge station tracks plain slottedDrill | F01, then RpcClearStation(runLocally:true) from polling |
| F08 keycard insertion | CARD.TryInsertCard:55 / CmdPlaceCardInSocket:88 | Local socket interaction reads LocalPlayer card | Extraction first; server placement request; socket field set in default observer body; manager insert RPC invoked on server | Card retains player owner through extraction/insertion | F05, then ServerRpc(requireOwnership:false) → RpcPlaceCardInSocket + feature-manager insert RPC |
| F09 keycard removal / dispenser pickup | LOOT/INV pickup → CARD.Update:32 / ServerHandleCardRemoved:73; DISPENSER.DispenseCards:18 / RpcInitializeSocket:56 | Player loots card; host polls; initial dispenser spawns server | Host initialization via runLocally observer call; subsequent removal discovered by parent/activeSelf polling | Initially unowned host-spawned card; pickup assigns player owner | F01, then feature-manager remove RPC + RpcClearSocket; initial RpcInitializeSocket(runLocally:true) |
| F10 foundry plate creation / collection | FOUNDRY.FinishPrinting:63 / HandlePlateLooted:95; PLATE.HandleLootAttempt:32 / CmdNotifyTaken:40 | Host timer creates plate; PLATE also reacts independently to loot-attempt event | Creation server; notification server even when inventory rejected attempt | New plate initially unowned; actual pickup later gives player owner | CmdNotifyTaken(requireOwnership:false) invokes OnPlateTakenServer separately from F01 |
| F11 Hull plate insertion / consumption | CRACK.TryInsertPlate:122 → HULL.CmdTryPlacePlate:282 → CRACK.RpcPlacePlateInSocket:151 | Local socket interaction captures plate and current crack; RPCInfo.sender received but not used for inventory | Host validates active crack/material/depth; runLocally observer body executes host and clients | Plate keeps old player ownership; socket has no authoritative occupant/holder record | ServerRpc(requireOwnership:false) → socket ObserversRpc(runLocally:true) |
| F12 weld-point progress | WELD.ApplyWeld:23 → CRACK.OnPointWelded:177 | DRILL.CheckAndDrill, WELDER.PerformWelding, or MODULE.CheckAndDrill | Local tool simulation; DRILL/WELDER input gated by isOwner, MODULE by local interaction | Tool input owner-local; point is MonoBehaviour, plate still prior player-owned | No point RPC; local count ≥4 submits F13 |
| F13 crack completion | HULL.CmdFixCrack:320 / RpcOnCrackFixed:352 | Any local point counter reaching ≥4 | Host changes CrackData; observer fixes socket/colliders; host invokes water RPC | Host owns crack list/progression; local points/counts are not shared | ServerRpc(requireOwnership:false) → ObserversRpc; separate UpdateWaterLevelServerRpc |
| F14 forced pickup | INV.ForcePickupClientRpc:640 / TryForcePickup:648 | NotepadModule host creates page, calls inventory observer RPC | Only owning inventory handles observer body; owner initiates ordinary pickup; full-slot fallback changes pose/force locally | Page explicitly owned in producer path, then F01 may reassign; local container decides capacity | ObserversRpc → owner F01; fallback has no transfer RPC |
| F15 legacy local removal | PAGE.PlacePageOnSurface:207 → INV.RemoveCurrentItem:615 | Owner page input places page, then clears current slot; also used internally by F03/F05/F11 | Local only for page; every observer in F11; owner in F03/F05 | Page keeps existing owner; no canonical inventory release | No RPC in RemoveCurrentItem |
| F16 crack activation / reused socket | HULL.StartStation:70 / ActivateCrackOnSocket:259 → CRACK.RpcActivateCrack:141 | Host round/spawn logic | Server generates crack; runLocally observer sets socket flags | Host controls activeCracks; socket occupancy is plain fields | ObserversRpc(runLocally:true); clears slottedPlate reference, not a coherent item cleanup |
| F17 starting handbook | INV.HandleStartingItems:239 | Owner inventory OnSpawned, outside tutorial | Owner instantiates, then F01 executes prediction/server | Unsafe permits owner spawn; ownership assigned in F01 | Owner Instantiate → F01; no separate server creation contract |

### Current-flow inventory: questions 6–9

| Flow | 6. Item state before | 7. Item state after | 8. Is host final writer? | 9. Local prediction / presentation |
| --- | --- | --- | --- | --- |
| F01 | World/foundry/socket object; collider/loot normally enabled; no explicit possession version | Owner slot populated; hand parent; kinematic/no gravity; collider/loot/NT disabled. Non-owner inventory replicas hide item GameObject | Partial: server gives ownership, but no server slot ledger; owner controls slot/visibility | Immediate mutation of actual object + local slot/UI |
| F02 | Item in selected or stored slot | Selected root active and OnEquip; previous root inactive/OnUnequip; hand/camera offsets local | No; intentionally local, but remote held selection is absent | Immediate; desired to preserve |
| F03 | Item in owner slot, NT disabled, kinematic | Owner clears current slot; host sets pose, removes owner, enables NT and applies force; observer settings enable non-kinematic bodies too | Partial: host pose/force/ownership, owner-only slot removal and per-peer physics settings | Immediate detach/OnDrop/slot clear; no rejection recovery |
| F04 | Same, possibly handbook because lift path has different guards | Parent becomes supplied lift; elevator flag/event set on prediction and host; ownership removed | Partial as F03 | Immediate; elevator/tutorial callbacks can precede accepted state |
| F05 | Held item in current owner slot | Local slot cleared; NT/loot/collider enabled, body kinematic; ownership retained; no destination yet | No: remote server normally does nothing | Immediate; owner item can remain inactive because extracted helper only activates non-owner |
| F06 | Held drill; then detached/extracted intermediate | Plain slottedDrill assigned, drill visible under stationDrillSlot, collider enabled, kinematic; charging starts on host | Occupancy indirectly established by observer body; no atomic source release/destination acceptance | Extraction predicted before station acceptance |
| F07 | Charging drill under stationDrillSlot | Pickup changes parent/activity; host later clears station pointer/visuals | Not one transition: occupancy catches up via poll | Pickup predicted; charging/removal can overlap |
| F08 | Held card; then extracted intermediate | Socket points to card on observers; host puzzle slots updated through separate manager call; card visible/kinematic | Partial; host socket pointer depends on host-client observer execution | Extraction predicted before socket accepts; rejection has no restore |
| F09 | Slotted/dispenser card | Held, then socket/puzzle removal discovered by polling | Host updates logical puzzle slots later, not atomically with pickup | Immediate pickup; possible one-frame mismatch |
| F10 | Printed, scaled/tweened plate under foundry slot; foundry hasPlateInSlot true | Loot attempt clears foundry busy-slot state and unsubscribes, even if player had no free slot; actual item transfer separate | Host writes foundry flag, but based on attempt rather than accepted transfer | Plate isLooted set immediately; no accepted/rejected reset |
| F11 | Held plate with owner container entry, NT/loot/root collider disabled; CrackData Active | CrackData Plated; every observer clears its current local slot; plate reparented and active; ownership/held settings are not coherently finalized; point count reset | Host writes crack list, not correct player slot consumption; socket depends on LocalPlayer | No explicit placement preview; observer body mutates local gameplay |
| F12 | Unwelded local point, local progress 0..requiredWeldTime | Local point isWelded true, highlight disabled, socket count incremented | No shared point writer; local simulation is appropriate but completion context missing | Immediate tool motion/progress/charge drain |
| F13 | Host crack Plated; local counter ≥4 | Host crack Fixed once; clients disable socket and all child colliders; water drainage requested once for accepted state transition | Mostly yes: Plated guard prevents repeat drainage for same crack, but occupant/round/point evidence not correlated | Local points finish before host result |
| F14 | Host-spawned page, slot capacity unknown on host | Held via F01 if owner finds slot; else local throw/pose without accepted shared release | Partial; depends on owner slot and producer ownership | Local handling; notepad payload/drawing not part of this refactor |
| F15 | Owner-selected page/item, pose already changed by page caller | Only current local slot data/UI cleared; no item identity check or shared release | No | Immediate slot clear |
| F16 | Socket may retain previous placed object/fixed flags | Socket fields reset to new crack, slottedPlate null; prior object can remain physically present | Host controls crack record but occupancy/item cleanup not unified | Replicated effects only in intent, with plain gameplay fields |
| F17 | New owner-spawned handbook | Last slot filled via F01; normal G drop blocked, lift path historically different | Partial as F01 | Immediate local startup |

### Current-flow inventory: questions 10–13

“Twice” distinguishes intentional caller/server execution from duplicate shared mutation. Installed PurrNet suppresses relevant runLocally host transport replay; the attribute is not intrinsically broken.

| Flow | 10. Can the operation execute twice? | 11. Can another player's held item be affected? | 12. Can stale state be accepted? | 13. Incorrect host LocalPlayer dependency? |
| --- | --- | --- | --- | --- |
| F01 | Caller/server bodies both run for remote owner; repeated/competing requests have no item/slot version | Yes, competing pickups can overwrite ownership/possession | Yes, slot/item availability not checked on host | No direct LocalPlayer; owner-only slots are a different hidden dependency |
| F02 | Repeated same-index equip guarded; callbacks/presentation may repeat | Normally no | No shared validation; remote display never learns selection | No; local context is appropriate |
| F03 | Caller/server OnDrop and elevator side effects can both run; repeated drop has no possession guard | A stale/wrong object or slot change can affect wrong item | Yes | No direct LocalPlayer; RemoveCurrentItem targets current rather than captured slot |
| F04 | As F03; multiple event delivery can submit again | As F03 | Yes, including stale lift target | No |
| F05 | Caller/server paths; client-originated observer forwarding; no transfer ID/revision | Current slot may change; external wrong-inventory call possible | Yes | No direct LocalPlayer; server reads remote owner-only state |
| F06 | Repeated placement can overwrite slottedDrill; no occupancy guard in command | Drill object not bound to caller possession | Yes | Not in host command; local input lookup is appropriate |
| F07 | Poll can observe a temporary parent/activity change as removal | Competing pickup can affect drill holder | Yes; parent/activity is indirect truth | No |
| F08 | Repeated/competing placement can overwrite socket; separate extraction | Card not bound to caller possession; current-slot clearing may be wrong | Yes | Host pointer initialized via presentation, not direct LocalPlayer |
| F09 | Poll/remove callbacks can disagree with duplicate pickup | Competing pickup can affect card holder | Yes | No |
| F10 | One local isLooted gate, but multiple peers/attempt vs pickup paths are independent | Rejected attempt can free foundry while another player still interacts with plate | Yes, attempt accepted without transfer | No |
| F11 | Active→Plated rejects repeated same-crack command, but same plate can be used for different active cracks; invalid socket can be supplied | **Yes: each observer removes its own current item** | Yes: no caller possession/revision/socket association check | **Yes: socket body returns if host has no local inventory and otherwise consumes host's local selected item** |
| F12 | ApplyWeld guards local isWelded; socket counter is not point-identity based; different peers have independent counters | Does not directly consume inventory; can complete someone else's placement without correlated context | Yes after reinitialization/old callbacks; local progress has no epoch | No host writer here; local tool lookup is appropriate |
| F13 | Existing Plated state guard suppresses repeat same-crack completion; independent counters still submit | No direct item removal, but submitted crack need not match current plate/tool session | Yes, lacks explicit round/occupant context; crack IDs currently increase, which mitigates ID reuse | Socket fixed presentation uses local drill only to stop local minigame; that local effect is appropriate |
| F14 | Repeated force requests can cause repeated F01 | Wrong target inventory would affect that inventory; no canonical transfer guard | Yes | No |
| F15 | Can clear repeatedly; each observer does it in F11 | **Yes in F11; page caller can clear a newly selected item rather than the page** | Yes | **Yes when called by F11**; page input itself appropriately uses local context |
| F16 | Repeated activation resets count/occupancy presentation | Leaves stale placed-item associations; no direct slot clear | Yes, observer activation lacks a generation/version | No |
| F17 | Repeated initialization/spawn can create another item without coordinated slot acceptance | Normally no | Startup/spawn readiness and slot occupancy not versioned | No |

### Important current limitations

- Hull plates have **no supported player removal mechanic** once plated/fixed. The item remains in the socket for welding. This plan must not add a plate-refund/removal mechanic. Socket removal in scope means existing charge/keycard pickup and controlled Hull teardown, not a new gameplay action.
- PLATE's taken notification is a foundry lifecycle notification, not inventory consumption. It currently fires on attempt, so it must move to accepted pickup.
- F13 already rejects a second Fixed transition for the same crack. Preserve that guard and water/material/depth rules; do not describe all completion as currently unguarded.
- Pickup disables NT and hides the root on non-owners, and equip has no remote state. This explains missing remote held presentation. Simply broadcasting another SetActive does not establish possession or selection.
- Keycard/charge parents and activeSelf are polled as socket occupancy. Local visual hiding/reparenting can therefore be mistaken for a shared removal.

### Step 1 — current-worktree API and characterization verification

Verified on 2026-10-03 at HEAD `1160fc735658513bd7dca31d45bbc6c83676816d`. All seven guidance documents were read locally. `git diff 28e412f9f42851640c2a548598bcbd46c0c07e70 HEAD -- Assets/Scripts` is empty; the F01-F17 source evidence above still applies. The user reports a clean Unity compile and Play Mode baseline. This step independently verifies source, package semantics and serialized assets; it does **not** claim instrumented two-peer gameplay, latency, ownership or physics tests were performed.

#### Installed API verification

Source root for every package reference below is this worktree's `Library/PackageCache/dev.purrnet.purrnet@f4dd26fb792b/`. Its `package.json` reports **1.19.1**. No package or guidance source from another checkout was used.

| API / behavior | Local source and verified consequence |
| --- | --- |
| NetworkBehaviour / NetworkIdentity | `Runtime/Components/NetworkBehaviour/NetworkBehaviour.cs` inherits NetworkIdentity directly. Each component is an identity; do not assume one separate root NetworkIdentity owns all script state. |
| Ownership/controller | `Runtime/Components/NetworkIdentity/NetworkIdentity.cs:243-270,319,968,1078-1113`: `owner` is nullable PlayerID, host reads server owner; `isOwner` requires spawned local-player match; `isController` falls back to server without a connected owner. `IsController(bool ownerHasAuthority)` selects owner/controller or server behavior; it is not interchangeable with isOwner. |
| Assign/remove ownership | `GiveOwnership(PlayerID, bool silent=false, bool? propagateToChildren=null)` and nullable overload; `RemoveOwnership(bool? propagateToChildren=null)` exist. `Runtime/CoreModules/Ownership/GlobalOwnershipModule.cs:834` gathers all identities in the item hierarchy when propagation is enabled. Unsafe enables propagation by default. Keep the item-root operation and verify LOOT/NT/tool component owners together; do not assign only LOOT while leaving the tool controller behind. |
| RPC attributes | `Runtime/CoreModules/RPCs/{ServerRpcAttribute,ObserversRpcAttribute,TargetRpcAttribute}.cs`: exact lowercase arguments. All default to ReliableOrdered and runLocally:false. ServerRpc defaults requireOwnership:true. ObserversRpc/TargetRpc default requireServer:true and bufferLast:false. TargetRpc's leading PlayerID is routing, not payload actor authority. |
| RPCInfo | `RPCSignature.cs:9` has `PlayerID sender`, `bool asServer`, manager/signature fields. `RPCModule.cs` builds receive context; `Codegen/PostProcessor.cs:2060` recognizes trailing RPCInfo. Match received sender to the inventory owner; do not manufacture default context for plain host calls. |
| runLocally / host dispatch | `Codegen/PostProcessor.cs:2048-2139` executes caller body only for runLocally:true; commented server special-case is inactive. `NetworkIdentity.Broadcasting.cs:249,273,359` suppresses relevant host/local retransmission. A runLocally:false ServerRpc called by host is a transport operation, **not** synchronous invocation of its gameplay body. Caller + remote server execution is expected; runLocally alone does not prove double host consumption. |
| Unsafe permissions | `Defaults/NetworkRules/Unsafe.asset` and `NetworkIdentity.Broadcasting.cs:457-620`: ownership/server attribute checks are bypassed intentionally. ServerRpc receive still requires server side and observing sender. Plain host methods must check isServer; explicit actor/membership checks provide the proposed consistency boundary. |
| SyncVar | `Runtime/Components/NetworkModule/SyncVar.cs:14-190,241-345`: constructor `SyncVar(T initialValue=default, float sendIntervalInSeconds=0f, bool ownerAuth=false)`; `.value`, `.onChanged`, `.onChangedWithOld` and `.FlushImmediately()` exist. Default is server-controlled; ownerAuth selection is presentation only. Module sends latest state on observer addition; events fire immediately on local assignment. No atomic delivery across several SyncVars; defer callbacks until accepted fields are coherent. Its internal packet counter is **not** the gameplay version stamp. |
| Spawn/despawn | `NetworkIdentity.cs:1164-1320`: parameterless OnSpawned once at first side spawn, bool overload per side; parameterless OnDespawned after final side despawn, bool per side. Initialize containers once, bootstrap authoritative versions on server side, bind owner UI once; never reinitialize host containers on the later client-side callback. Base OnDestroy performs teardown. |
| NetworkTransform | `Runtime/Components/NetworkBehaviour/NetworkTransform.cs:142-163,246-320,541-575`: `ownerAuth`, ForceSync and ClearInterpolation exist. Enable resets compression state and may force current pose; disable unsubscribes local latest-update work, **does not despawn identity**. Registered NT module is separate; queued transform/parent traffic is not cancelled by setting enabled=false. NT does not establish Rigidbody authority or kinematic mode for the project. |
| Parent suppression | StartIgnoringParentChanges/StopIgnoringParentChanges exist on **NetworkTransform**, use a boolean rather than nesting counter, and Stop does not resend current parent. OnTransformParentChanged tests spawned/ignore/syncParent, not enabled. `Runtime/CoreModules/HierarchyV2/HierarchyV2.cs:540` synchronizes nearest spawned parent plus child path and rejects cross-scene parenting. Disabling NT is not adequate parent suppression. |
| Packing | `Runtime/BitPacker/Packers/{PackNetworkIdentity,PackUIntegers}.cs`, `Codegen/GenerateSerializersProcessor.cs:137-180,682-760`: ulong/int/bool, enums, Unity vectors/rotation, field-based structs, PlayerID, NetworkID/SceneID and concrete NetworkIdentity-derived references have packing paths. Existing project CrackData/RPC component payloads confirm usage. Proposed structs must still pass Unity codegen/round-trip checks when introduced; no hypothetical struct was compiled in this step. |
| Unresolved references | PackNetworkIdentity.ReadIdentity resolves now by scene + ID and returns null if missing/type mismatch; it does **not** keep a deferred handle. GameObject/Transform packing can use hierarchy paths or prefab fallback. Persistent snapshots/selection/reply correlation must retain raw SceneID + NetworkID (including scope), not only a component reference that may decode null. Resolve with public HierarchyFactory.TryGetIdentity and reapply locally when the counterpart spawns. No custom registry or result cache. |

#### F01-F17 physical and authority characterization

This table complements the caller/RPC/membership/duplicate tables above. “Held profile”: NT disabled, Rigidbody kinematic/no gravity, **root** collider and ItemLoot disabled; root SetActive(false) on non-owner inventory instances, while owner's selected root is enabled. Child colliders/renderers are not separately disabled. “Extracted profile”: NT/root collider/loot enabled, Rigidbody kinematic/no gravity, non-owner root activated. “Dropped profile”: NT/root collider/loot enabled, Rigidbody dynamic/gravity on every settings recipient, host alone sets pose/removes owner/applies throw. These are actual helpers, not recommended replica physics.

| Flow | Current exact membership/selection | Physical hierarchy/visibility and source/destination | Host / remote side effects and authority |
| --- | --- | --- | --- |
| F01 | RPC names GameObject + target slot; only owning inventory writes Data/PhysicalObject. Host remote inventory has no containers; no source-slot proof. | Held profile; caller/server reparent to that inventory HandPosition, apply Data offsets. Source socket still follows parent/activity later. | NT GiveOwnership(owner), with hierarchy propagation; owner updates UI, root visibility and OnEquip. Elevator flag/event can run caller and server. Host ownership is shared; local slots/selection are not host truth. |
| F02 | Same-index EquipSlot returns; other index hides old root and shows new. No remote selected state. | Hand -> local InteractCameraTrans tween -> Hand; NT remains disabled; CanOperate hiding is different from root hiding. | OnEquip/OnUnequip, sway, prompts, handbook inspect setup and drill charge RPC side effects are local. Remote display must not call these callbacks to mimic owner selection. |
| F03 | Input captures object, RPC clears whichever current slot exists when body executes, not captured source slot. | Dropped profile, null parent; former holder controller becomes unowned server controller after removal. | Remote caller OnDrop/slot clear, server OnDrop + pose/ownership/force, observer settings. Shared force has one writer but transfer acceptance has none. |
| F04 | Selected object as F03; no ordinary-G handbook exemption in this branch. | Lift parent, random local X converted world pose, no throw force. | Elevator global notification occurs caller/server; tutorial UseElevator invoked immediately by local handler. Preserve this intent with once-only accepted shared event and local feedback. |
| F05 | Source is local currentSlotIndex, owner clears it. Host remote inventory returns at -1. | Extracted profile, ownership retained; helper itself does not detach/change pose. Owner inactive root may remain inactive. | Remote caller originates observer RPC under Unsafe; server can do nothing. No socket occupancy yet. Preserve only explicit legacy Detached route; do not use this intermediate for sockets. |
| F06 | Captured drill after independent F05; no sender/membership/empty destination proof. | Parent stationDrillSlot, zero pose; collider/root enabled, kinematic. Loot/NT inherited from extraction, previous owner retained. | Server invokes runLocally observer body, establishing plain slottedDrill then charging Update; all peers update station UI/collider. Shared occupancy must move out of observer body. |
| F07 | F01 selects target inventory slot; old charge source membership not checked. | Pickup applies held profile/new hand; station later notices parent/activeSelf mismatch. | Host clears plain source pointer through poll and runLocally clear RPC; charging can continue before removal is discovered. Accepted pickup must release exact source synchronously. |
| F08 | Captured card + client CardID, then F05; no exact source or destination occupancy check. | Card becomes socket child/zero pose, active/collider enabled, kinematic; NT/loot inherited, owner retained. | Server sends default observer body (host client later fills slottedCard) and separate manager ServerRpc; puzzle occupancy and physical occupancy are separate writes. |
| F09 | Initial card is host-created; InitializeCard runs locally/observers and establishes host myData. Later F01 is not bound to source. | Dispenser card under cardSlot with kinematic body/scale tween; InitializeSocket via runLocally observer. Removal is parent/activity poll. | Manager remove RPC has runLocally:false too. Existing InitializeSocket is the narrow bootstrap adapter; dispenser itself need not be edited if host initialization is added there. |
| F10 | Exact attemptedLoot equality, but no accepted inventory slot/result. isLooted set on attempt. | Foundry plate parent plateSlot, kinematic + print scale tween; collector then held profile. | OnPlateTakenServer releases foundry busy slot independently of F01, including full-inventory failure. Callback must identify the registered current plate and occur after acceptance. |
| F11 | RPC captures plate/crack/socket; observer clears its own LocalPlayer selected slot, never proving placer/source. | Plate socket child/accepted rotation/root active; held NT/loot/root collider remain disabled, body kinematic; child weld colliders remain available. | Host validates active crack/material/depth and writes Plated; observer initializes actual points and socket fields only if LocalPlayer exists. Exact placing inventory release must replace this path. |
| F12 | Tools use local equip/input. WeldPoint has own progress/isWelded and socket reference; no inventory mutation. | Actual point colliders on physical plate; ApplyWeld guards isWelded, sets flag before OnPointWelded. | Local highlight/charge/tool animation; no synchronized progress. Source callers include drill, welder and module, but serialized asset search found no MODULE/WELDER script GUID usage in current prefab/scene YAML. Do not add a new tool restriction or mask for hypothetical callers. |
| F13 | Only crackID submitted; no captured occupant/tool/socket version. | Manager observer disables all socket child colliders, socket isFixed display/particles; held drill stopped only locally. | Host Plated -> Fixed guard already protects repeated same-crack drainage; UpdateWaterLevelServerRpc is a separate queued host RPC. Preserve drainage amount and spawning cooldown when replacing with plain accepted host call. |
| F14 | Producer targets inventory/assigns page ownership; successful forced pickup uses first empty slot, full path does not enter inventory. | New page world's physics until F01; full case changes owner-local pose/adds force. | ForcePickupClientRpc is actually ObserversRpc with owner body filter. Payload/image transfer and full-slot fallback remain outside scope; bootstrap cannot assume page starts unowned. |
| F15 | RemoveCurrentItem clears selected slot without item identity. Page placing captures itself but calls selected-slot API. | PAGE activates self, detaches, sets local pose/collider/kinematic/no gravity; does not restore NT or ItemLoot. | No shared membership release. Exact page-release adapter is needed; do not claim this slice repairs surface pose/payload replication. Existing parent-before-release must be suppressed while item is held. |
| F16 | No player slot involved. nextCrackID increases; StartStation clears activeCracks only if previously inactive. | RpcActivateCrack resets plain socket occupant/flags, not old physical plate membership/children. | Host generates/registers crack; observer visual/state reset. Stamp activation and exact previous-occupant cleanup; no broad inventory reset or plate-refund mechanic. |
| F17 | Owner outside tutorial instantiates handbook and requests last slot. Engineer prefab has handbook; Technician reference is null. | Engineer handbook NT ownerAuth:false (other inspected transfer items ownerAuth:true); all have syncParent:true. Held profile follows F01. | Client-created Unsafe spawn is permitted, but new identity/server version readiness must precede non-predicted request. Starting bootstrap needs a local pending grant after spawn/version readiness, not version-zero acceptance or a new retry queue. |

#### Assumption decisions and required corrections

- **Reuse containers: suitable with separated writers.** The private nested InventoryItemContainer has only Data and PhysicalObject, no UI/LocalPlayer dependency. All accesses/writes are inside INV. Allocate once on host and owner, retaining owner-only input/UI subscriptions; host keeps currentSlotIndex=-1 for remote inventories. Data is display metadata, not IsEmpty authority. Split owner preview/Equip/settings from accepted writes at pickup, removal, extraction and PAGE release. On a host these are the same array: owner reconciliation cannot clear/rewrite an already committed slot. Non-owner remote inventory replicas need no second inventory array.
- **LOOT state: existing component is suitable.** All inspected plate/drill/keycard/page/starting-handbook prefabs already contain ItemLoot and NT. No new component/wiring is needed. Keep LOOT enabled for state application and use CanBeLooted/collider gates for interaction; owner-only item behavior stays controlled separately. Smallest network record: location, one contextual identity handle (holder OR socket), source slot when held, and ulong version. Holder/socket runtime references are resolved views. World/foundry source uses exact existing source registration; no generalized source registry. Socket occupant handle/version is necessary inverse occupancy, not a second possession authority.
- **Retain raw handles, not extra duplicate fields.** Represent contextual identity by SceneID + nullable NetworkID; selected/occupant handles and replies use the same representation. The record lives on the exact LOOT identity, so it needs no redundant self ID. Named request wrappers can resolve handles and require spawned components; captured item handle/version survive null decode for rejection correlation. Reapply unresolved current presentation from stored handles on relevant spawn/readiness; no network resync protocol. Source evidence proves this small correction is needed for the existing replay guarantee.
- **Versions remain sufficient.** Fresh host stamp on every inventory/socket initialization, item creation/reuse and accepted membership/occupancy change; reject uninitialized zero versions. Comparing expected item + inventory + relevant source/destination socket versions rejects duplicates, delay and all three leave/return cases. One inventory stamp deliberately rejects changes in any slot; conservative but appropriate here. Reused identities receive new stamps; session connections invalidate previous-session requests. No request IDs/cache/history/generation split required.
- **Parent handling is local and explicit.** Keep ignore mode throughout held possession and Detached legacy placement so PAGE's existing local detach cannot emit shared parent changes. Do not simply toggle ignore around pickup then allow later camera/hand changes through. On accepted World/socket transition, establish host canonical parent/pose deliberately, switch ignore mode in a balanced nonnested sequence, and apply compatible presentation on peers. StopIgnoringParentChanges does not publish the current parent. Avoid two competing parent writers and reject cross-scene destinations.
- **Remote held hierarchy works without prefab changes.** Both role prefabs bind HandPosition `1164954421322645208` under active PlayerCameraRoot `4960467761367258317`, root parent `3821038881943497407`. FPC disables virtual-camera component `8111440803468131654`/its GameObject, not HandPos's parent. PlayerInteractCamera starts inactive and is activated/reparented only for local module interaction. Remote presentation stays under remote HandPos, never remote InteractCameraTrans. ItemSway disables itself for non-owner. Keep item root alive, cache original renderer/UI visibility, hide stored renderers, disable owner-only canvases on observers, and preserve original renderer states on reveal; do not invoke item OnEquip/OnUnequip/OnDrop on observers. Existing MainCamera mask normally excludes layer 6, interaction mask layers 7/8; test layer-dependent visibility rather than changing prefab/camera assets. Remote hand follows existing body/camera-root state, not a new aiming protocol or visual clone.
- **Replica physics needs an explicit choice.** Inspected transfer prefabs have plain Rigidbody + NT; NT has no automatic authority-based isKinematic assignment. Accepted world item should simulate force/gravity on host, while noncontrolling replica Rigidbody remains kinematic and follows NT. Socket/held bodies are kinematic. This is the focused consistency correction, not a general physics redesign. Test collisions and NT re-enable/interpolation after owner removal.
- **Hull joint operation is feasible.** Preserve IsPlateValidForCrack exactly (200-400,401-650,651-800; front/back versus sides and material), active round, registered socket coordinates, Active -> Plated, rotation and point.Initialize on a new placement. Commit inventory/item/occupancy/crack together before observer display; apply water-count update once after acceptance. All physical plate prefabs have four WeldPoint components; never remove another observer's local slot.
- **Charge/Keycard joint insertion is feasible.** Move source release/destination occupancy before physics/display; keep chargeRate/timings and keycard type/index rules. **One necessary file addition:** Keycard_StationManager's six insert/remove RPCs are queued even when called by host. Extract their existing bodies into six plain host-only methods; leave RPC wrappers/signatures/bindings for existing callers and have CARD call plain methods after all transfer validation, within the host operation. This removes a demonstrated second commit writer/delayed puzzle occupancy, without rewriting tester/generator rules. DISPENSER remains read-only through host-aware InitializeSocket.
- **Existing weld flags suffice, but retry needs a local entry point.** Inspect exactly the four current plate points before completion; capture current context and submitting player's accepted tool, preserving the currently wired drill path. Existing ApplyWeld will never notify again after all flags become true: on a same-context rejection or deliberate re-interaction, CRACK must explicitly re-evaluate all flags and resubmit current context. Do not retry a stale replaced plate or reset progress on repeated same-context snapshots. Host accepts trusted local completion, not its own unreplicated point flags. No WELD edit/mask/point protocol.
- **Serialized bindings checked read-only.** Searched all local Assets prefab/scene YAML for proposed public method names and script GUIDs. Confirmed LootItem on plate/drill/keycard/torn-page prefabs; Hull prefab and EfeLevelTestScene bind OnSocketInteracted/OnStopInteract; Hull binds StartStation; charge/keycard bind HandleInteraction. Keep those names and parameterless signatures. No matched persistent targets for EquipSlot/RemoveCurrentItem/ExtractCurrentHeldItem/ForcePickupClientRpc/InitializeSocket/OnPointWelded/CmdTryPlacePlate/CmdFixCrack were found in that search. Retain compatibility wrappers rather than treating absence as permission to break external API. Preserve all fields/GUIDs and do not modify assets.

Step 1 conclusion: **YES, with these corrections, Step 2 is safe to begin when authorized.** No Step 2 implementation was performed. Baseline compilation is user-reported; two-peer characterization and proposed payload round-trip/Unity compilation remain runtime verification work, not passed results. No missing local package/API blocker remains.

## Proposed Change

### Review outcome and instruction alignment

**Recommendation: simplify the current plan.** Preserve F01-F17 evidence and exact-item safeguards; remove optional protocol machinery.

All seven requested documents are present, nonempty and were read from this worktree. No other checkout was accessed. CODING_STANDARDS.md now contains the intended trusted two-player guidance. Historical audit provenance/security wording does not authorize external guidance or a rule migration.

The previous plan followed PLANS.md and disclosed authority changes, but excess state/recovery machinery conflicted with AGENTS.md's "Do not over-engineer," CODING_STANDARDS.md's instruction to avoid speculative abstractions and generalized transactions without an actual need, and NETWORKING_STANDARDS.md's prohibition on unnecessary synchronized duplication. The remaining host transfer/ownership changes are explicit architectural proposals, not cleanup or permission to implement.

### Concepts retained, reduced or removed

| Concept | Decision and reason |
| --- | --- |
| ItemTransferState | Reduce to compact ItemPossession: location, holder/slot OR socket, version. No stored drop pose, Fixed state, material or creation history. |
| Separate host slot array | Remove. Initialize/reuse existing containers on host as an inverse index of accepted possession; local UI is its view. |
| Item generation plus revision | Replace with one host-issued version stamp changing on creation/reuse and each shared change. |
| Request IDs and result cache | Remove. Exact operation/item/expected versions identify a pending action; first acceptance invalidates repeats. Identical-success replay is unnecessary. |
| Retry queue, timeout resync protocol, transaction history | Remove. Reliable discrete RPCs, one pending local view and current-state replies suffice; no automatic mutation retry. |
| Socket snapshots | Keep only occupant/version; Hull also needs crack ID/state and rotation. Required for replay and stale destination rejection. |
| Equipped-item synchronization | Keep one owner-authored selected identity, presentation-only; the other player cannot derive selection. |
| Stale rejection | Keep item, inventory and socket version checks. Empty/matching state alone fails after an item/slot/socket leaves and returns. |
| Reconciliation | Keep exact-item acknowledgement and one small local visual preview; no prediction journal. |
| New weld mask/point infrastructure | Remove. Existing ApplyWeld guards isWelded; inspect current plate's actual point flags before contextual final completion. WELD remains unchanged. |
| General transfer service/destination interface/source registry | Do not introduce. Named methods in existing components cover the current destinations. |

### Required authoritative state

1. ItemLoot has one host-written compact ItemPossession record: location (World, Inventory, Socket, Detached), one contextual SceneID + nullable NetworkID handle resolving to holder InventoryManager plus slot when Inventory or socket NetworkBehaviour when Socket, and version. Holder/socket are mutually exclusive. Detached preserves legacy extraction; Fixed derives from Hull crack state. PurrNet owner is a controller, not reliable slot membership.
2. InventoryManager initializes its existing containers before the isOwner UI branch on host. They index accepted item membership; no second host array or separately synchronized slot collection. Host checks exact PhysicalObject identity, not currentSlotIndex or Data-null-based IsEmpty. Add one host-authored inventory version for slot mutation history.
3. Existing sockets retain occupant and socket version. Hull's narrow coherent snapshot also carries crack ID/state and accepted rotation. Typed public occupant fields remain compatibility mirrors. Charge/keycard use explicit occupancy instead of visual-parent/activity polling.
4. HullBreach_StationManager retains activeCracks, isRoundActive and current crack-ID allocation. Socket version + crack ID handle reset; no generic round generation protocol.

A small private monotonically increasing ulong token counter in an existing component assigns fresh host versions on creation/reuse and accepted changes. It is one counter, not a registry, cache, Singleton, log or version service. Invoke only on host/main thread; do not reset while current-session messages can arrive, and fail safely instead of wrapping. A new/reused item gets a fresh stamp, preventing old context acceptance even if a network reference is reused. Normal session teardown invalidates old connections; no host migration/persistence feature is introduced.

Stamp every changed item/inventory/socket record with the same accepted commit token. Unchanged records need no update. This does not make different SyncVars arrive atomically: presentation waits for compatible current records. A socket-only Fixed transition stamps the socket, not an unchanged inventory.

Item possession is truth; containers are its bounded inverse index, written only by the same host operation or exact despawn cleanup. An inconsistency is a diagnostic/init fault, not a reason to fall back to LocalPlayer/UI. No independently mutable second authority.

### Why each version safeguard remains

- Item leaves and returns to the same player/slot: item version rejects an old request even though identity/membership match again.
- A delayed pickup targets empty slot 3; another item fills and empties it while the first world item is unchanged: inventory version rejects that old destination request. One inventory version is simpler than per-slot versions.
- Charge/keycard socket fills and empties before an old insertion arrives: socket version rejects it despite being empty again.
- First success changes versions/membership; repeating it cannot consume/place again without a cache.
- Fresh creation/reuse stamp replaces a separate generation field.
- Current socket/crack/plate context and Plated -> Fixed guard reject stale/duplicate completion and drainage.

### Required local state and responsiveness

Keep currentSlotIndex, UI, sway, camera, tool input/animation and partial welding local.

At most one shared action is pending per local inventory: operation, exact item, captured slots/destination and expected versions, plus affected visual state. It is not a queue. Selection/camera/tool feedback remain responsive; another transfer waits for resolution. Do not mutate accepted containers speculatively on host or client.

One owner-authored selected-item handle SyncVar serves remote held rendering only; raw scene/identity IDs survive unresolved references, as established in Step 1. Render it only when accepted possession agrees; it never authorizes transfer or completion. Pending pickup/drop/socket previews change only local visuals, not ownership, accepted slots, source occupancy, shared force or consumption. No networked preview clone system or per-frame held RPC.

### Concrete RPC flow

1. Capture exact item/source/destination and item/inventory/socket versions at input; preview only affected local hand state.
2. Host player calls a named plain host method directly. Remote sends the operation-specific ServerRpc with runLocally:false and trailing RPCInfo info = default.
3. Under Unsafe explicitly match sender to the identified inventory owner. Validate versions, exact source slot, target slot and destination context before mutation. Internal grants use a plain host helper, not fabricated RPCInfo.
4. Commit synchronously without yield: exact source release, destination occupancy, item membership, version changes, ownership and physical settings. Guard reentrant acceptance; callbacks/audio/events happen after accepted state.
5. One TargetRpc reply to requester (direct local host result) echoes operation/item/expected context and current accepted item/inventory/slot/socket state. Rejection is current truth, not a command to restore old state.
6. Other peers reconstruct from synchronized current state. Presentation/replies never consume or assign ownership.

No request IDs, cached results, replay log, extensible executor or automatic retry. ReliableOrdered discrete requests/replies remain. A repeated accepted request may get stale/current-state instead of identical cached success; shared state is unchanged.

Ignore replies older than accepted versions. Resolve a local preview only for its exact context; apply current truth without forcing former selection. A late rejection cannot clear a newer item/slot. Every host-received request replies, including null/stale references. Disconnect/despawn clears local pending views. A UI watchdog may abandon a preview but cannot infer host rejection or retry mutation; later current state still converges.

### Single host commit and exact consumption

Use named TryPickupServer, TryDropServer, TryExtractServer and TryPlacePlateServer methods in existing classes. Share short membership/version helpers only where duplication exists. No generalized transaction class.

Inventory -> socket is one accepted operation, not extraction followed by independent placement. Destination checks and source consumption run together. Clear the captured item/slot of the captured inventory; never resolve another player through LocalPlayer/currentSlotIndex. Local clear applies only to that exact identity/slot and accepted version.

Changed selection cannot alter which item is consumed. Repeated replies/snapshots cannot re-run OnDrop, source release, placement animations or UI removal. Validate all components before mutation; presentation failure logs/reapplies truth rather than rolling back a published commit.

### Ownership and presentation

| Accepted location | Ownership/state |
| --- | --- |
| Inventory | Host assigns holder owner; NT disabled for hand-relative presentation; kinematic; world loot disabled. Local equip remains immediate. |
| World/lift | Host removes owner, sets pose/parent and simulates force/gravity once through existing transform path; noncontrolling replica Rigidbody is kinematic and follows NT. Plain inspected Rigidbody/NT provides no automatic replica physics gate. |
| Detached | No inventory membership. Explicit legacy extraction preserves its owner/kinematic handoff; PAGE's membership-only release preserves its existing local pose/physics/NT behavior instead of applying extraction settings. Not a notepad-surface redesign. |
| Charge/keycard socket | Host removes holder owner; socket governs kinematic item; accepted pickup releases source occupancy. |
| Hull socket | Host removes holder owner; kinematic/non-lootable; physical plate retained for welding; Fixed belongs to crack state. |

Socket ownership removal is a disclosed behavior change; prefab ownerAuth flags stay unchanged. PurrNet ownership still does not replace item/slot checks.

Keep network identity alive where renderer hiding is needed; preserve owner-only item UI/input. Local hand/camera reparent and observer display must not emit shared-parent operations. Drop pose lives in existing transform synchronization/request payload; Hull rotation lives only in socket snapshot.

Step 1 locally verified NetworkTransform.StartIgnoringParentChanges/StopIgnoringParentChanges. Hold ignore mode throughout local hand/camera/legacy Detached parenting; enabled=false is not parent suppression. Stop does not resend parent. Publish accepted host world/socket parent deliberately, with balanced nonnested suppression, and test queued parent/transform ordering. Use active remote HandPos, not inactive interaction-camera ancestry; preserve root identity and original renderer visibility while hiding owner-only UI. No prefab edits.

### Hull placement

CRACK.TryInsertPlate captures placing inventory, plate/source slot, expected item/inventory/socket versions and crack ID. CmdTryPlacePlate receives that context and RPCInfo.

Host verifies sender/owner, exact membership, versions, registered socket/station/crack coordinates, active round, Active crack, empty socket and existing material/depth rules. It commits placing slot release + plate Socket membership + ownership removal + accepted socket occupant + Active -> Plated with one stamp. Only the placing inventory reconciles its exact plate slot.

RpcPlacePlateInSocket never calls LocalPlayer or RemoveCurrentItem. Consumption is Inventory -> Socket, not destruction. Another observer's selected drill/card is unaffected. Item Socket membership rejects reuse in two cracks; socket/version guards reject two plates. Wrong-material/stale/rejected placement does not consume.

No new player plate-removal/refund mechanic. Plated/fixed Hull plates reject generic loot; teardown changes only station-owned placement context.

### Welding completion

Leave WELD, drill, welder and module algorithms unchanged. ApplyWeld already guards isWelded and sets it before notification. No point RPC/mask/registry, server aiming or cross-player combining of partial counts.

CRACK.OnPointWelded keeps local count/feedback but checks the current plate's four actual point flags before requesting completion. Capture current plate/item version and crack/socket context when interaction begins. Initialize points only on a new accepted placement; repeated same-context display does not reset progress. Old callbacks cannot complete a new plate whose actual points are not welded.

Final completion identifies actor/tool and socket/crack/plate/context. Host checks accepted tool possession and current Plated occupant/versions, then accepts the trusted local outcome. Do not wait for owner-selected presentation replication. Existing Fixed guard gives one completion/drainage. Invoke drainage plainly on host after acceptance rather than nesting a server-to-server RPC. Preserve amounts/cooldown. Same-context rejection/re-interaction must explicitly re-evaluate the four local flags in CRACK before deliberate resubmission, since guarded ApplyWeld cannot notify again once welded. Stale completion cannot change a newer crack.

### Sources, compatibility and scope

- Foundry releases its exact current plate only on accepted pickup, using existing OnPlateTakenServer after commit. Failed/full inventory attempt leaves it occupied; retire attempt-time isLooted/notification.
- Charge/keycard source removal happens inside accepted pickup, not parent/activeSelf polling. Register initial source occupancy in existing host spawn/init paths.
- Keycard_Socket receives only required transfer-boundary adapter. KEYMAN extracts the six existing insert/remove bodies into plain host methods with unchanged RPC wrappers; CARD calls the plain methods within accepted transfer so puzzle occupancy is not queued separately. Preserve puzzle callbacks once, generator/tester/dispenser algorithms unchanged.
- PAGE changes only its release call to an exact-item API such as ReleaseItemFromInventory(gameObject). This requests exact membership release, not a second extraction/drop or SetExtractedItemSettings call. Preserve drawing/payload/surface pose/tutorial logic and current local NT/physics contract; no unrelated surface replication claim. Held/Detached parent suppression prevents its existing local detach from publishing shared ancestry.
- Owner-created starting handbook remains allowed under Unsafe; defer the single startup grant until item spawn and fresh host version are ready, then bootstrap accepted slot; never accept zero version. Engineer has a configured handbook; Technician has none. Preserve tutorial/drop exemptions. Successful forced page pickup follows the same path; full-slot policy stays out of scope.
- Standalone extraction captures item/slot locally and requests Detached; in-scope sockets no longer call it first.
- Despawn clears only matching accepted item/slot/socket context and local handlers; existing disconnect policy retained, no persistence/refunds.
- Activation/reset changes socket version. Preserve intended fixed-plate visuals; no broad round inventory cleanup or held-item consumption.

## Files Affected

**Ten expected production files** for the next coherent authority activation slice, using the aliases below. Five already contain Step 2 foundations. WELD stays unchanged, while a narrow KEYMAN bridge is required by locally verified queued-RPC semantics. No production files changed during this sequence revision. No new component/framework/interface/service required; small data structs stay alongside existing components.

| Alias | Exact methods to change | Responsibility changes |
| --- | --- | --- |
| INV | OnSpawned/OnDestroy; HandleLootAttempt/PickupServerRpc/ObserversPickupRpc; DropCurrentItem/HandleLiftDrop/DropServerRpc/ObserversDropRpc; ExtractCurrentHeldItem/ObserversExtractRpc; RemoveCurrentItem; SetItemSettings/SetExtractedItemSettings; EquipSlot/RefreshActiveSlot/HideCurrentItem; SetInteractItemParent/SetNormalItemParent; HandleStartingItems/ForcePickupClientRpc/TryForcePickup | Reuse containers on host; inventory version; separate concrete host methods/visual preview. Add exact ClearLocalSlotIfMatches, ReleaseItemFromInventory, ApplyTransferReply/ApplyHeldPresentation and selected identity. No second array/cache. New lifecycle handlers only for introduced state. |
| LOOT | LootItem; new spawn/despawn/state handlers | Compact possession/version issuer; accepted state view. Retain existing itemData/CanBeLooted/isInElevator. No history/generation pair/pose duplication. |
| HULL | StartStation/ActivateCrackOnSocket; CmdTryPlacePlate/CmdFixCrack/RpcOnCrackFixed; completion use of UpdateWaterLevelServerRpc | Concrete host placement/completion; exact context/versions; initialize state before observers; preserve validity/drainage. |
| CRACK | Awake/TryInsertPlate; RpcActivateCrack/RpcPlacePlateInSocket; OnPointWelded/RpcOnCrackFixed/UpdateSocketVisualsAndInteraction; context use in OnSocketInteracted/OnStopInteract | Narrow snapshot; remove all observer consumption; actual point checks/contextual final request. Keep UnityEvent signatures; no point mask. |
| PLATE | OnEnable/OnDisable/HandleLootAttempt/CmdNotifyTaken | Retire attempt notification; plain host NotifyPickupAcceptedServer invokes existing taken event after success. |
| FOUNDRY | FinishPrinting/HandlePlateLooted | Exact source registration/release after acceptance; print/material/animation unchanged. |
| CHARGE | TryInsertDrill/CmdPlaceDrillInStation/RpcPlaceDrillInStation; Update removal branch/ServerHandleDrillRemoved/RpcClearStation | Accepted insertion/removal and occupant/version; no polling authority; charge rates/cadence unchanged. |
| CARD | InitializeSocket/TryInsertCard/CmdPlaceCardInSocket/RpcPlaceCardInSocket; Update removal branch/ServerHandleCardRemoved/RpcClearSocket | Required destination/source adapter; derive card ID from item; existing puzzle updates once. |
| PAGE | PlacePageOnSurface, release call only | Exact page identity; remaining algorithm untouched. |
| KEYMAN: Station/Keycard Matrix/Keycard_StationManager.cs | TechnicianInsertCardRPC/TechnicianRemoveCardRPC, EngineerInsertCardRPC/EngineerRemoveCardRPC, TesterInsertCardRPC/TesterRemoveCardRPC; six extracted plain host bodies | Preserve wrapper signatures and rules; synchronous accepted puzzle occupancy instead of nested queued ServerRpc. No tester/generator rewrite. |

WELD/DRILL/WELDER/MODULE, PLAYER, UI/data/sway, lift, DISPENSER/Keycard data, NotepadModule, StationController/FloodManager, assets and package remain read-only. Revise plan before expanding this set.

## Networking Impact

- RPC: concrete ServerRpc runLocally:false + RPCInfo; host plain call; one exact-context TargetRpc acknowledgement; current synchronized views replace mutating observers. No request-ID/cache/retry infrastructure.
- Authority: host shared commit; owner selection/input/local welding; presentation cannot consume/progress.
- Ownership: assign on pickup, remove on drop/socket, preserve standalone extraction. Changes explicitly proposed, not hidden cleanup.
- Identity: existing components/GUIDs; no prefab additions. Verify held visibility does not deactivate required state.
- Synchronization: compact item membership, inventory version, socket occupancy/crack state and selected identity. Existing NT handles world pose. No duplicated inventory collection/partial weld state.
- Prediction: one local preview, not a new physics/PurrDiction model.
- Rules: Unsafe unchanged; no asset/package/project settings changes.
- Replay: latest state reconstructs current possession/occupancy; no historical consumption/effect replay.

Host code never resolves remote inventory via LocalPlayer or host observer execution. Host-client callbacks apply idempotent display only. Remote owner selection is immediate; accepted reply clears exact captured slot. Out-of-order item/socket/selected records wait for compatible context instead of inventing state.

Local package.json verifies PurrNet 1.19.1; packages-lock.json locks 266cb63efd3d858d6d2fce68c2b0b2364ca78c24 and project metadata identifies Unity 6000.3.10f1. Step 1 verified local lifecycle, RPC, parent and packing implementation; Step 2's foundation structs passed compilation/codegen and packing round trips. New request/reply payloads and subsequent changes require their own verification. Use exact lowercase RPC arguments; TargetRpc begins with PlayerID target and trailing RPCInfo is receive context. Dedicated server is not added as a product requirement.

## Unity Serialization Risk

Medium: runtime protocol/held display changes; assets stay untouched. Preserve field names/types, class/file names, enum values, ItemData/CrackData, script GUIDs and UnityEvents including LootItem/OnSocketInteracted/OnStopInteract. New state initializes in code without Inspector wiring; public occupants remain mirrors.

No Prefab/Scene/ScriptableObject/Network Rules/ProjectSettings/Package/assembly edit. Both peers need matching new build/fresh session. Compile packing and inspect representative bindings during implementation.

## Behaviour Risk

Preserve keys/slots/scroll, sway/camera, drop/lift intent, handbook exemptions, tool charge/weld timing, foundry timing/materials, Hull validity/drainage/cooldown and keycard rules.

Intentional fixes: exact consumption, one socket commit, remote held view, socket ownership handoff, stale/competing rejection and once-only completion. Risks: rejected-preview flash, remote UI/camera ancestry, NT interpolation, replica physics, source ordering, duplicate callbacks and completion retry. One pending transfer restricts overlapping shared commands, not ordinary local input. An item must be accepted before a second shared transfer uses it.

No voice/stress/role/level/random-event/notepad-protocol redesign or fresh security project.

## Implementation Steps

### Sequencing conflict and activation rule

The former Step 3 activated host membership before former Steps 4 and 5 replaced all removal writers. This was not a safe independently playable commit. `ExtractCurrentHeldItem` clears owner-local state, while the remote inventory's host instance returns at currentSlotIndex == -1. Charge/keycard then independently place that item. `RpcPlacePlateInSocket` calls every observer's LocalPlayer.RemoveCurrentItem; PAGE also clears the selected slot instead of the exact page. None maintains Step 2 possession or versions.

After an authoritative pickup, those calls could leave the host reporting Inventory while the item is physically in a socket/on a surface. A delayed drop could then validate obsolete membership; alternatively, legitimate subsequent pickup could be rejected. Teaching parameterless RemoveCurrentItem to update host state cannot recover the missing placer/item context and could make Hull's wrong-player removal authoritative.

**Activation boundary:** before normal gameplay starts using authoritative membership/version validation, every production entry point that can acquire, remove, overwrite or reset accepted inventory membership must use the same exact host-written state. No owner/observer may directly clear accepted containers. This includes initialization/grants, socket removal, teardown/despawn and relevant public compatibility entry points, not only G-drop. Audit all writes/callers before the activation commit. A newly discovered bypass blocks that commit until its concrete operation is covered within the approved scope.

Choose direct final migration for Charge/Keycard and Hull. An exact extraction adapter would still leave separate extraction/placement requests, failed-insertion orphan state, queued keycard updates and a later removal task. It does not produce a smaller safe playable slice. No temporary transfer adapters, shadow inventory, compatibility service, destination interface, request journal or migration feature flag are proposed.

### Revised order and commit boundaries

1. **Characterization/API verification — completed.** Keep F01-F17 and the local PurrNet corrections as historical source evidence. Refresh relevant callers/bindings before implementation; do not claim unperformed two-peer tests.
2. **Dormant shared-state foundation — completed.** Existing host containers, compact possession/retained handles, one stamp mechanism, inventory/socket versions and Hull snapshot are initialized. They are not accepted gameplay truth yet. Step 2's item readiness permits later bootstrap; accepted starting/forced grants are wired in the next slice. Foundation compilation/codegen/packing passed, with runtime lifecycle verification remaining.
3. **Coherent inventory authority activation — next production slice; one commit.** Combine the membership-affecting parts of former Steps 3, 4 and 5, plus minimum state application/lifecycle work from former Step 7. All preparation and caller replacements below belong to this commit; none is an independently playable partial activation.
4. **Contextual welding completion — separate commit after activation.** Complete former Step 6: actor/tool/plate/crack/socket expected context, actual existing local point flags and explicit same-context re-evaluation, plain host drainage once, rejection/re-interaction handling. Preserve local responsiveness, WELD/tools and puzzle rules. It must not reintroduce inventory consumption or change accepted occupant membership.
5. **Remote selected held presentation — separate commit after activation.** Finish the presentation-only portion of former Step 7: owner-selected retained handle, remote active HandPos rendering, owner-only UI suppression, unresolved selected-handle replay and visibility/interpolation tests. Membership reconstruction, exact local reconciliation and parent suppression cannot wait until this step; only remote selection/display finalization may wait. This step never authorizes a transfer or invokes owner item callbacks on observers.
6. **Full multiplayer verification and release review.** Former Step 8, in addition to the checks required in each commit. Exercise the entire transfer/placement/welding/display cycle with both roles, latency, repeated rounds, observer re-add and disconnect. Do not declare the full refactor verified from compilation or source checks alone.

### Slice 3 internal implementation order

These phases are an implementation work order inside one commit, not optional migration modes or separately released commits. During preparation, legacy gameplay remains the only transfer writer; new concrete methods stay unwired. Replace all callers together before committing/running the activated slice in Play Mode. There is no runtime switch between competing authorities.

**3A — exact operations and minimum local application.** In INV/LOOT, prepare named host pickup/drop/extract/page-release methods, sender/owner validation, exact item/source/destination checks, item/inventory versions, retained-handle requests/replies and one pending local action. Commit synchronously without yielding; issue one token to changed records before callbacks. Reuse containers as the host inverse index. Local previews never write accepted membership. Build current-state reconciliation, exact slot application, unresolved-handle readiness, spawn/despawn cleanup and held/Detached parent suppression now. Keep roots/ItemLoot available for state application and hide presentation without relying on root inactivity as membership. Preserve owner selection, UI, sway, camera and OnEquip/OnUnequip semantics. No generalized transfer executor or full-inventory UI synchronization.

**3B — direct final source/destination operations.** Prepare Charge/Keycard Inventory -> Socket and Socket -> Inventory as single accepted host operations. In CHARGE/CARD, validate exact item/placing inventory/source slot/versions and occupied/empty destination; register initial socket occupancy on host after identities are ready. Remove extract-then-place calls and parent/activeSelf polling as gameplay authority. Accepted pickup releases the exact source socket in the same commit; charge stops and existing puzzle insert/remove callbacks execute once in order. Extract KEYMAN's six existing insert/remove bodies into plain host methods, keeping RPC wrappers for compatible callers; CARD uses the plain methods. Preserve charge cadence, card rules, dispenser/tester/generator behavior and public serialized fields as presentation mirrors.

**3C — move Hull placement into activation.** Prepare HULL/CRACK's final Inventory -> Hull Socket joint operation now, not after pickup becomes authoritative. Capture placing inventory, exact plate/slot, item/inventory/socket versions and crack context. Validate sender, membership, registered crack/socket, active round, material/depth and empty occupancy before one commit of source release, Socket possession, ownership removal, occupant/snapshot and Active -> Plated. Observer placement applies physical plate/weld setup once for that accepted context and never calls LocalPlayer.RemoveCurrentItem. Reject loot/removal of plated/fixed Hull plates; no refund mechanic. Activate/reset/despawn of sockets and plate identities must stamp or clear only matching accepted context, preserving fixed visuals. Keep existing guarded Plated -> Fixed completion/drainage behavior during this slice, but route its narrow socket-state write through the same host snapshot/version so later completion cannot leave that snapshot contradictory. Full tool/context completion validation and retry handling stay in revised Step 4; WELD and drilling algorithms remain untouched.

**3D — exact page release, grants and accepted callbacks.** PAGE identifies itself via ReleaseItemFromInventory(gameObject); INV captures that page's accepted source inventory/slot and versions, never whichever slot is selected later. Accepted release changes Inventory -> Detached, clears exactly that slot and stamps the same records, preserving the page's existing ownership/local surface pose/physics/NT contract. It does not use SetExtractedItemSettings or add a surface protocol. Capture context before local placement side effects; treat existing local placement as pending presentation and reconcile from current truth on rejection, without resurrecting an old selected item. Drawing, payload, texture, tutorial and surface algorithms remain outside scope except the minimum ordering/reconciliation required for this release.

Standalone extraction is a separate final named Inventory -> Detached operation with captured item/source slot/versions; it retains its documented ownership/kinematic handoff. Preserve the parameterless public ExtractCurrentHeldItem signature as an owner-input wrapper that captures context locally and submits that operation; its host implementation never reads currentSlotIndex. It is no longer a socket insertion intermediate. Replace every known RemoveCurrentItem caller; preserve a public signature if required for compatibility, but it must have no observer/server selected-slot mutation path. Any retained owner-input wrapper captures exact context before submitting a final named operation; any unexpected non-owner invocation fails safely with a diagnostic. This wrapper is not a temporary second writer.

Starting handbook and successful forced pickup use the same final pickup acceptance. Preserve existing spawning authority and full-slot fallback; wait for valid spawned identities and nonzero host stamps before one pending startup/grant is submitted. No zero-version acceptance, retry queue or notepad producer redesign. PLATE retires attempt-time isLooted/CmdNotifyTaken behavior; FOUNDRY tracks/releases only its exact current printed plate through the existing taken event after accepted pickup. Rejected/full pickup leaves foundry occupancy unchanged. Lift shared notifications and drop force run once after accepted host commit; tutorial/owner callbacks reconcile only the exact accepted context. No observer may independently free a source or apply shared force.

**3E — wire and remove bypasses together.** Switch ordinary pickup/drop/lift, extraction, both socket families, Hull placement, PAGE release and starting/forced grants to the prepared methods in the same commit. Retire old runLocally mutation bodies and mutating observers, not their serialized method names/bindings. Verify all source/destination initialization, resets/despawns and callbacks have the same writer. Do not commit if any bypass remains. Begin from a fresh matching host/client session; do not import live legacy owner-only slots into an active authority model or reconcile them from LocalPlayer.

### Exact first-slice files

All paths below are under Assets/Scripts. These are the same ten files already approved, with no new service/component or asset:

| File | Required activation responsibility |
| --- | --- |
| InventorySystem/InventoryManager.cs | Concrete host operations, exact requests/replies, containers/versions, owner preview/reconciliation, standalone extraction/page release, grants, legacy-entry replacement, lifecycle cleanup. |
| InventorySystem/Item/ItemLoot.cs | Host possession/stamp writes, retained-handle readiness/current-state application, exact despawn cleanup; preserve existing serialized data. |
| Station/Hull Breach/Technician/HullBreach_ChargeStation.cs | Final joint insertion/removal, host occupancy initialization, post-commit charge callbacks; retire polling and extraction intermediate. |
| Station/Keycard Matrix/Keycard_Socket.cs | Final joint insertion/removal and initial occupancy, same-commit puzzle updates; retire polling and extraction intermediate. |
| Station/Keycard Matrix/Keycard_StationManager.cs | Six narrow plain host insert/remove bridges; preserve wrappers and puzzle algorithms. |
| Station/Hull Breach/HullBreach_StationManager.cs | Joint placing-inventory/plate/socket/crack commit and lifecycle context; minimum existing Fixed transition snapshot/stamp compatibility only. |
| Station/Hull Breach/Technician/HullBreach_CrackSocket.cs | Accepted placement/snapshot application; eliminate observer consumption, context-safe weld initialization and narrow existing completion display compatibility. |
| Station/Hull Breach/Technician/HullBreach_PlateItem.cs | Notify only accepted foundry pickup; remove attempt-time source release. |
| Station/Hull Breach/Technician/HullBreach_FoundryController.cs | Exact printed source registration and accepted source callback. |
| Notepad/TornPageItem.cs | Exact page membership-release call and minimum pending/rejection ordering; no drawing/payload/surface redesign. |

If another production file is required, stop and revise scope before editing it. In particular, keep Lift, NotepadModule, dispenser, tools/WELD and UI assets read-only. Do not add automatic state adoption or a second authoritative collection to avoid this boundary.

### Why this is the smallest coherent active slice

Every acquisition and inventory-removal writer participates in the first active commit. Hull moves earlier because placement consumes inventory; preserving its observer removal would immediately invalidate the new model. Charge/Keycard go directly to their final joint operations because an extraction adapter cannot validate/commit the destination atomically. PAGE and standalone extraction receive their final narrow operations. Foundry and lift callbacks accompany the transfers they describe. Starting/forced grants and exact teardown cannot wait because they also create/remove accepted membership.

Remote selection rendering and full welding validation do not themselves remove inventory membership, so they remain separate follow-up commits. Their minimum shared-state compatibility is included now; no later step is needed to repair an inventory contradiction introduced by activation. The slice is broader in file count than the rejected pickup/drop-only step, but adds fewer temporary states and protocols. Commit atomicity does not mean SyncVars arrive atomically: current-state application/replies still wait for compatible versions and resolved handles.

**Temporary compatibility adapters: none.** Public input/RPC wrappers and KEYMAN's plain bridges are permanent, concrete compatibility boundaries using the final operations. There is no later adapter-removal phase. If implementation evidence requires a temporary adapter, stop for a plan revision specifying its exact item/slot/actor state writes and removal commit; do not introduce one implicitly.

### Activation commit verification gate

- Search the complete project-owned source for container assignments/Clear, RemoveCurrentItem, ExtractCurrentHeldItem, pickup/drop RPCs, possession/occupancy writes and placement callbacks. Every remaining caller must have a documented exact-context writer or presentation-only role. Check public methods against serialized bindings read-only.
- Compile/codegen and round-trip all new request/reply payloads against the local PurrNet 1.19.1 package. No production activation commit with unresolved compilation or packing errors.
- Verify exact item/source slot/destination, matching sender, fresh item/inventory/source-and-destination socket versions and nonzero readiness before mutation. Test duplicate/competing requests and each leave/return stale case. Check reentrant events cannot accept a second commit.
- Exercise pickup -> drop/lift, pickup -> standalone extract -> pickup, pickup -> Charge/Keycard insert -> remove -> drop, pickup -> Hull place, and forced page pickup -> exact page release -> pickup where existing gameplay supports it. Compare host possession, inverse containers and socket occupancy after every accepted/rejected step, with roles swapped. Test slot changes/pending actions; another observer's held item must survive Hull placement.
- Verify foundry full/rejected pickup, callback counts, charge stop, six keycard puzzle bridges, ownership propagation, once-only host force/lift notification, starting-item readiness, despawn/reset and pending rejection/current-state order. Preview may not free a source or consume a slot.
- Report source checks and actual host/remote runtime tests separately. Compilation/source reasoning allows review of the complete slice, not a claim that multiplayer behavior passed. The commit has no intentionally unresolved inventory-writer contradiction; disclose remaining welding/remote-selected-display limitations until revised Steps 4/5 are complete.

### Mapping from the previous sequence

| Previous step | Revised placement |
| --- | --- |
| 1 characterization | 1, completed; historical evidence retained. |
| 2 foundation | 2, completed and dormant; accepted grant wiring moves into 3. |
| 3 pickup/drop | 3, combined activation boundary; never activated alone. |
| 4 extraction, Charge/Keycard, page, KEYMAN | 3, final concrete operations in the same activation commit. |
| 5 Hull placement | 3, moved earlier with pickup/drop and removal writers. |
| 6 welding completion | 4; minimum same-snapshot Fixed-state compatibility already in 3. |
| 7 held display/replay | Split: minimum exact current-state application/parent/lifecycle work in 3; remote selected rendering finalization in 5. |
| 8 verification/release | 6, with compile/source/runtime checks also attached to each earlier slice. |

## Rollback Strategy

Revert the entire revised Step 3 activation commit across its ten files together, including callers/protocol records; this returns to the existing dormant Step 2 foundation with legacy gameplay as its sole transfer writer. Revert dependent completion/presentation commits first or together. Never roll back only pickup or a socket writer, and never reconstruct live legacy membership from local UI. No live old/new writer flag or partial socket-only rollback. No asset/save migration expected; restart both peers after revert. Keep characterization evidence. Authority activation is not implemented yet.

## Verification

Unity 6.3/PurrNet 1.19.1 compile/codegen and host+remote tests, roles swapped, are required for the active migration. Step 2 foundation compilation/codegen and packing round trips passed; no multiplayer/runtime test below is claimed passed. Revised Step 3's activation gate applies before any later completion/display step.

| Test | Expected |
| --- | --- |
| Host/remote pickup/equip/drop | Exact holder/slot/owner, one host force/commit, responsive selection and remote held view. |
| Two players pick same item | One accepted holder; loser reconciles without duplicate slot. |
| Duplicate request | No second consumption/callback; newer current truth returned without cache. |
| Item leaves/returns to same slot | Old item version rejected. |
| Empty slot fills/empties while world item unchanged | Old pickup rejected by inventory version. |
| Socket fills/empties before stale insertion | Socket version rejects it. |
| Recreated/reused item, scene/session restart | Fresh stamp; old context/session cannot mutate it. |
| Selection changes while placement/drop pending | Exact captured slot clears; new selected item survives. |
| Host holds drill while remote places plate, then reverse | Only placing inventory loses its plate. |
| Same plate/two cracks; two plates/one crack | One item/occupant; no repeated source consumption. |
| Wrong material/depth/socket, inactive/reset crack | No shared mutation; existing warning/current-state reply. |
| Full/rejected pickup | Foundry/source remains occupied; preview restores current truth. |
| Charge pickup during visual preview | Host charging stops on accepted removal, not parent/activity. |
| Keycard dispense/insert/remove | One existing puzzle callback; rules unchanged. |
| Lift/handbook/forced page pickup | Exemptions/pose/source retained; callbacks/force once. |
| Exact PAGE release | Page removed, changed selected item survives; algorithms unchanged. |
| Repeated ApplyWeld/OnPointWelded | Existing guard + actual flags do not fabricate completion; no WELD edit. |
| Two completion submissions; stale old plate | One Fixed/drainage, current newer crack unaffected. |
| Late rejection/duplicate reply/snapshot ordering | No newer membership rollback, extra UI clear or effect. |
| Observer re-add/reference ordering | Current holder/selection/socket/Fixed reconstruct; no historical consume or same-context weld reset. |
| 100-250 ms latency with equip/camera | Immediate local held feedback; only shared arbitration waits. |
| Disconnect/despawn pending | Matching associations/handlers/views cleaned under existing policy. |
| Assets/physics/NT re-enable | No missing bindings, remote UI leaks, competing physics or stale snap. |
| Snapshot precedes counterpart spawn | Retained raw handle resolves later; null component decoding cannot discard occupancy/selection or request correlation. |
| Keycard rapid insertion/removal | Plain host bridge updates puzzle membership in commit order; no queued second manager mutation. |
| Same-context completed weld retry | Explicit CRACK re-evaluation can resubmit valid current flags; no dependence on ApplyWeld notifying twice. |
| Starting item before host readiness | Zero version is not accepted; one pending bootstrap resolves after spawned identity and initial host version. |

Focused Edit Mode invariant checks for exact slot/version/destination are useful; multiplayer remains necessary for RPC/identity/hierarchy. No test framework abstraction solely for this refactor.

## Comparison and recommendation

| Dimension | A: previous plan | B: simplified plan |
| --- | --- | --- |
| Complexity | Broad transfer record, second slot array, generation/revision, request IDs/cache/recovery and point-mask work | Compact possession, reused slots, one version mechanism, narrow sockets, selected identity and local preview/reply |
| Introduced concepts | About 10-12 mechanisms, depending on grouping | About 6 cohesive mechanisms; concrete functions rather than transaction plumbing |
| Production files | 10 | 10 after Step 1 correction: WELD unchanged; KEYMAN needs a narrow synchronous bridge. Complexity reduction is in protocol/state, not file count. |
| Networking risk | More correlated state, packing, recovery and callback ordering | Lower integration surface; still significant shared authority change |
| Maintenance | Caches/eviction, generation reset, pose duplication, retries and masks | Membership/version invariants, current views and existing callbacks |
| Defect coverage | Can solve defects with excess infrastructure | Preserves exact item/slot, single commit, occupancy, duplicates/stale/other-player protection and responsive visuals |

**Keep the simplified architecture and combine its authority activation boundary.** Keep the detailed flow evidence, mandatory host bookkeeping, meaningful versions, occupancy and replay. Remove speculative generalized transaction/retry/cache/generation/point machinery. Step 2 is complete; the next production slice is revised Step 3 across all ten concrete transfer-boundary files, not the superseded pickup/drop-only step. This sequence revision changes documentation only and does not authorize production edits in this task.
