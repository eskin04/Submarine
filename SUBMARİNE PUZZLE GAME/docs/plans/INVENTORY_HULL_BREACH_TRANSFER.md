# Inventory transfer consistency and Hull Breach placement

Status: **Proposed implementation plan; no implementation performed.** Date: 2026-10-03.

## Problem

This is a trusted two-player Engineer/Technician co-op game. Unsafe Network Rules are intentional and remain unchanged. The objective is consistent shared possession and socket state, not comprehensive cheat prevention. Local selection, item sway, tool input and hand/camera feedback remain responsive and owner-controlled.

Today inventory slots exist only on the owner; replicated extraction reads that owner-only state on the host. Socket insertion often consists of separate extraction and placement requests. Hull plate placement clears the current slot of every observer's LocalPlayer instead of consuming the identified placing player's plate. Socket occupancy, physics, ownership and item presentation do not have one accepted transition. Current selected slots, object parents and activeSelf are used as indirect truth.

The planned behavioral change is explicit: **the host commits shared transfers and Hull occupancy/completion; the owner controls local input and immediate held visuals.** This is a focused authority/protocol change, not a general cleanup or a Network Rules migration.

## Evidence

### Instructions and baseline

Read AGENTS.md, ARCHITECTURE.md, CODING_STANDARDS.md, NETWORKING_STANDARDS.md, PLANS.md and TECH_DEBT.md. This worktree has the two audit documents; missing guidance files were read from `C:/Users/eskin/Documents/GitHub/Submarine/SUBMARİNE PUZZLE GAME`. CODING_STANDARDS.md there is empty. This plan follows PLANS.md's Problem, Evidence, Proposed Change, Files Affected, Networking Impact, Unity Serialization Risk, Behaviour Risk, Implementation Steps and Verification sections.

Unity is 6000.3.10f1; installed PurrNet is 1.19.1, locked at `266cb63efd3d858d6d2fce68c2b0b2364ca78c24`. Source line references below refer to the current baseline, commit `874539b72a7ad000af250a45bca8b42887742b8e`. Source analysis is not a reproduced multiplayer result. The user's gameplay clarification supersedes security-first wording in the earlier audit; Unsafe itself is not registered as a defect in this plan.

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

## Proposed Change

### Small state model inside existing components

No inventory framework, central transfer service, static item registry, new scene component, general command bus or station superclass. Extend the already present ItemLoot and InventoryManager; keep destination-specific checks in existing socket/station components.

1. **ItemLoot owns a host-written transfer record** using a single server-authored `SyncVar<ItemTransferState>`. Define the plain struct/enum alongside ItemLoot, not as a new component. Record: item lifetime generation, monotonically increasing revision, location (`Available`, `Inventory`, `Detached`, `Socket`, `Fixed`), holder InventoryManager when applicable, source slot, destination NetworkBehaviour when applicable, and accepted socket/drop pose. Generation increments for reuse of the same component; revision never resets during that lifetime. Preserve creation/source context for foundry, dispenser and charge socket removal.
2. **InventoryManager owns a small host-only slot array** initialized before the isOwner early return. The existing owner containers/UI remain local. Slots hold ItemLoot references and are consistent with each item's holder/slot record; they are not a second independently writable authority. Do not copy local currentSlotIndex into host truth or synchronize the complete UI inventory.
3. **An owner-authored equipped-item SyncVar** on InventoryManager carries only selected item identity for remote held presentation. It is not a possession claim and must never authorize transfer/completion. Existing `EquipSlot`, camera transitions and tool input update visuals immediately. A peer renders selection only if ItemLoot's accepted holder record agrees. Missing/late item references trigger a bounded reapply when the component/reference is ready.
4. **Hull socket owns one host-written socket snapshot**, also one composite SyncVar: round generation, crack ID/state, occupant plate, occupant item revision, accepted rotation. A point-identity mask stays local to the existing minigame; it is not additional synchronized gameplay. Existing public fields are retained as derived compatibility/presentation mirrors, not host truth. Item and socket records are correlated by generation/revision; multiple SyncVars are not assumed to arrive atomically.

Item records provide replay of current possession; socket snapshots provide replay of current crack/occupancy. Use normal latest-state synchronization and idempotent visual application rather than buffer every historical effect. A small commit-result RPC gives immediate acceptance/rejection feedback, but the persistent records remain final truth. Do not create two independent authoritative records for the same item.

### Transfer request and host commit

Each request captures **the actual item reference, source slot, item generation/revision and destination context at input time**, plus a per-inventory request number. A request never means “remove whatever is current when it arrives.” Use a bounded last-result cache per inventory to resend the same answer for a duplicate request; item revision and destination guards remain the actual protection against replay. Limit one pending transfer per inventory for this first refactor; slot selection stays responsive, but another transfer waits for its result. This avoids an unnecessary prediction journal.

Host player calls the same plain server commit method directly. Remote player sends a ServerRpc with `runLocally:false` and trailing `RPCInfo info = default`; the receiver calls that method with `info.sender`. Explicitly compare the sender to this inventory's owner, because Unsafe intentionally does not enforce the attribute globally. Internal host-created grants use a distinct server helper, not a forged/default RPCInfo.

All validation occurs before changing shared state: spawned/valid item and player; item generation/revision; exact source slot membership or Available/source socket state; target slot bounds/emptiness; destination enabled/current/empty; Hull socket belongs to this station and exactly matches crack coordinates/ID/round; unchanged material/depth rules. This is consistency validation, not new anti-cheat distance/aim simulation. Existing pickup distance/local input restrictions remain.

The host then commits synchronously without yields: clear old source slot/source socket; set new item location/holder; set destination occupancy; increment revision; apply ownership and physical pose/mode; publish accepted state. Gameplay callbacks, UI, audio and tweens run after the state commit. Prevalidate components/pose before mutation; callbacks must not be able to execute a second transfer reentrantly. A callback failure should log and permit state reapplication, not undo an already published commit silently.

For socket insertion there is **one Inventory→Socket commit**, not an Inventory→Detached RPC followed by an independent placement RPC. Standalone extraction remains available only for legacy callers that truly need Detached; in-scope sockets stop using it. A failed insertion retains the original holder/slot and occupant state.

Every received remote request gets an acceptance or rejection result, including null/stale references and invalid destinations; no silent return may leave prediction pending. A pending transfer also resolves from a matching accepted snapshot if its result arrives later. On an operational timeout, reconcile against latest accepted state and, if necessary, request one targeted current-state result; do not invent another transfer ID and retry the mutation. Use the same request ID for a duplicate retry. Keep the result cache bounded (for example the last eight results); a cache miss still cannot bypass generation/revision checks. This is recovery for accidental disconnect/readiness problems, not a general retry framework.

### Exact slot clearing and duplicate/stale protection

- Replace current-slot clearing in internal transfer paths with `ClearLocalSlotIfMatches(slot, item, generation, revision)`; capture source before prediction. If user selected a drill after requesting plate placement, accepted result still clears the plate's captured slot, not the drill slot.
- Increment item revision on every accepted shared location change. Expected generation/revision mismatch rejects old pickup/drop/extraction/placement after transfer, reuse or teardown.
- Reject occupied destination, different socket/crack association, non-current round and item already Socket/Fixed. In particular, one plate cannot be attached to a second crack.
- Apply repeated accepted snapshots/results idempotently. A duplicate RPC/result must not repeat item removal, OnDrop/OnEquip, foundry release, weld initialization, water drainage, source-socket callback or placement animation.
- No client presentation path may call RemoveCurrentItem or modify host slot/occupancy records. Only the owning inventory reconciles its local UI for the identified item.
- Item despawn clears only the matching host slot/source/destination association using its generation/revision; inventory despawn invalidates that inventory's pending requests and subscriptions. Use accepted state, not LocalPlayer or activeSelf, to identify cleanup targets. Preserve PurrNet's existing owner-disconnect despawn policy rather than introducing persistent inventories or refunds.

### Ownership and physical modes

| Accepted location | PurrNet owner | NT / Rigidbody / loot | Presentation |
| --- | --- | --- | --- |
| Inventory | Assign holder's owner on host | NT disabled while hand-relative; body kinematic/no gravity; world pickup disabled | Root identity stays alive; render selected held visuals, hide stored ones without deactivating the entire network identity |
| Available world/lift | Remove ownership on host, preserving existing drop intent | NT enabled; host simulates Rigidbody/force; remote body kinematic unless an existing verified physics module owns replica handling; loot available | Host pose/parent and replicated motion |
| Detached legacy extraction | Preserve previous ownership, as extraction currently does | Explicit extraction mode; no inventory membership | Compatibility handoff, not an available inventory claim |
| Charge/keycard socket | Remove holder ownership on host; item governed by socket | Socket-relative, kinematic; pickup allowed only through accepted source-socket removal | Visible socket item; charging/feature rules unchanged |
| Hull Socket / Fixed | Remove holder ownership on host; host owns shared placement | Kinematic; no plate loot; preserve weld-point colliders until Fixed, then disable existing crack colliders | Visible plate, local partial weld feedback, shared Fixed state |

These ownership changes are proposed architectural behavior and must be reviewed explicitly. Existing prefab `_ownerAuth:1` on sampled plate/drill/keycard NetworkTransforms remains unchanged: removing the owner makes host control the world/socket item; selecting/holding is still responsive locally.

Do not blindly set every Rigidbody dynamic on every observer as SetItemSettings currently does. Inspect the actual prefab components before changing replica physics. No prefab edit is permitted; if the existing setup cannot implement consistent replica mode through these scripts, report that constraint rather than expanding scope.

### Responsive local and remote held presentation

- Keep `EquipSlot`, sway, inspect position, camera transition and tool input local. They must not wait for a host acknowledgement.
- For a pending remote pickup/drop/insertion, store the affected item's local slot/visual state and preview the local hand change. Do not grant ownership, trigger source-socket release, change shared occupancy or apply drop force in preview. Host player validates/commits directly and needs no separate speculative mutation.
- The installed NetworkTransform has public `StartIgnoringParentChanges()` / `StopIgnoringParentChanges()`. Wrap owner-local hand/camera preview changes and replicated presentation reparenting in a balanced try/finally scope using these APIs; disabling NT alone does not prove parent callbacks are suppressed. Only host shared commits perform network hierarchy changes. Do not mutate a child NetworkIdentity hierarchy through a temporary visual clone containing network components.
- Cache renderers/visual state in existing ItemLoot/InventoryManager; do not instantiate a second networked item or add prefab visual anchors. Keep owner-only UI/tool behavior gated by owner. Use existing PlayerInventory.HandPosition for remote placement, and verify that remote hand hierarchy follows its player; if hidden camera ancestry prevents rendering, solve through runtime presentation using existing references or report the constraint without editing prefabs.
- Accepted result reconciles the exact slot/item, then snapshot application converges all peers. Rejection restores the captured item view only if its current revision/context still matches; otherwise use the newer snapshot. Selection may have changed while pending, so restoration must not force an old selection.
- Remote equipped selection arriving before accepted possession is not rendered until records agree; possession arriving first waits for selection. Existing supported host+remote game should converge without per-frame held RPCs.

### Hull placement and completion

`TryInsertPlate` captures placing InventoryManager/item/source slot/revision and requests the single transfer. Host validates all matching context, then commits the inventory release, item Socket state, crack Active→Plated and socket occupant together. The physical plate is **not destroyed**: “consumption” means release from placing inventory into the socket for welding. The socket display never looks up LocalPlayer to consume an item.

The drill/welder/module minigame remains locally simulated. WELD reports a completed point **by identity/index**, not a bare increment, to a local four-point mask scoped to the current plate/crack. CRACK submits one completion request after the required mask is complete, carrying socket round/crack/occupant revision and local actor/tool identity. The host validates current occupancy and accepted tool possession, then accepts the existing trusted local result. No per-point/progress RPC, server aim reconstruction or new cross-player combination of partial weld counts is introduced. Partial point feedback stays local; completed crack state is shared. Validate the four existing point IDs from the plate prefab.

After a valid completion request, HULL performs the existing Plated→Fixed transition once through a plain server helper. Preserve the material/depth conditions, water drainage amount, cooldown and roll-timer rules. Gate against stale plate/round context and retain the existing Plated guard. Replace the redundant server-to-server water RPC use in this completion path with a once-only plain server event helper; do not refactor FloodManager or unrelated water logic. If a rejected completion still has the same plate context, permit deliberate retry on valid interaction without replaying welding; abandon local progress when its context changes.

Existing drill/welder ownership/input gates remain unchanged. LocalPlayer is allowed at local input/tool/UI boundaries; it is forbidden for host item consumption/occupancy. On repeated socket snapshots, do not reinitialize weld points and erase local or accepted progress.

### Source sockets, creation and limited compatibility

- Charge/keycard removal happens as part of accepted pickup, before publication. Polling parent/activeSelf is no longer the normal authority path. Retain only a guarded teardown diagnostic if needed; local visual reparenting must not empty a shared socket.
- Foundry slot is released only on accepted pickup of that foundry's current plate. Failed/full-slot pickup must leave `hasPlateInSlot` and subscription intact. Use existing OnPlateTakenServer as a post-commit notification rather than a new global event system.
- Keycard_Socket is a direct extraction caller and must receive a **transfer-boundary-only adaptation** to avoid leaving a broken caller of changed inventory methods. Do not change its puzzle generator, station evaluation, tester timings or dispenser layout. InitializeSocket can register initial source occupancy during its existing host runLocally call; no dispenser rewrite is required.
- Keep owner-created starting handbook behavior under Unsafe. Register/grant it through the same host slot commit once its network identity is ready; do not replace spawning authority. Keep existing tutorial exemption and normal-drop restriction.
- Keep ForcePickupClientRpc/TryForcePickup as public compatibility entry points, routing successful pickups through the same accepted transfer. Notepad encoding, upload protocol, drawing, surface placement and full-inventory throw policy are out of scope.
- PAGE needs one compatibility call-site edit: replace its no-argument RemoveCurrentItem call with ReleaseItemFromInventory(gameObject), an explicit item-aware InventoryManager method. That method locates the exact slot, captures generation/revision and requests Inventory→Detached release while preserving PAGE's local surface algorithm. A no-argument wrapper cannot infer which page invoked it after selection changed. Keep RemoveCurrentItem for external API compatibility if needed, but no in-scope placement uses it. Internal transfers use a separate identity-checked local clear helper, avoiding recursive requests. This fixes membership bookkeeping only, not notepad surface replication; do not reset the already placed surface pose. No drawing, payload or surface algorithm change.
- On Hull teardown/new generation, host invalidates old placement requests and removes only station-owned old placed plates according to existing round-reset intent. Never consume a held item or refund a plated item to a random local player. Confirm intended retained fixed-plate visuals during characterization before despawning old placed objects; logical generation invalidation is mandatory, visual cleanup must preserve the agreed gameplay behavior.

## Files Affected

The following are the complete expected production edit set for a future implementation. This plan creates no production changes.

| File | Exact existing methods to change | Exact responsibility change / new focused helpers |
| --- | --- | --- |
| INV | OnSpawned, OnDestroy; HandleLootAttempt, PickupServerRpc, ObserversPickupRpc; EquipSlot, RefreshActiveSlot, HideCurrentItem; SetInteractItemParent, SetNormalItemParent; DropCurrentItem, HandleLiftDrop, DropServerRpc, ObserversDropRpc; ExtractCurrentHeldItem, ObserversExtractRpc; SetItemSettings, SetExtractedItemSettings, RemoveCurrentItem; HandleStartingItems; ForcePickupClientRpc, TryForcePickup | Initialize host slots independently of local UI; capture identity/revision; replace mixed prediction/RPC bodies with local requests + guarded server commit. Add RequestPickup/Drop/Extract wrappers, ValidateTransferServer, CommitTransferServer, TryConsumeIntoSocketServer, ClearLocalSlotIfMatches, ApplyTransferResult, ApplyHeldPresentation, and small pending-result state. Add OnDespawned only for new transfer subscriptions/pending state and safe LocalPlayer clear. Add owner-auth equipped-item SyncVar and paired handler. Do not change selection keys, slot count, sway tuning, UI layout or interaction rules. |
| LOOT | LootItem; new OnSpawned/OnDespawned hooks | Preserve public loot attempt event for input. Add nested transfer struct/enum, host-auth SyncVar, guarded SetTransferStateServer, ApplyTransferState, renderer/physics caching and source-socket registration. Latest state reconstructs possession; no static registry. Existing itemData, CanBeLooted and isInElevator fields retained. |
| HULL | StartStation, ActivateCrackOnSocket, CmdTryPlacePlate, CmdFixCrack, RpcOnCrackFixed; completion use of UpdateWaterLevelServerRpc | Add round generation, socket association checks and TryPlacePlateServer/TryCompleteCrackServer. Placement delegates exact inventory consumption to guarded host commit. Completion checks current occupant/round and accepted tool possession; trust local minigame outcome. Retain state/depth/material/drainage rules. Server initializes socket before display; no LocalPlayer. |
| CRACK | Awake; TryInsertPlate; RpcActivateCrack; RpcPlacePlateInSocket; OnPointWelded; RpcOnCrackFixed; UpdateSocketVisualsAndInteraction | Add composite host-auth socket snapshot and handler. Server commit sets occupant; display never clears inventory. OnPointWelded accepts point identity, builds local deduplicated mask/context and submits one contextual completion request. Repeated occupant snapshots do not reset local progress. Retain OnSocketInteracted/OnStopInteract signatures; adjust only accepted-context use and deliberate completion retry. |
| WELD | Initialize, ApplyWeld | Retain timing; report this/stable point index rather than bare counter; reset only for new placement context. No point RPC or requiredWeldTime change. |
| PLATE | OnEnable, OnDisable, HandleLootAttempt, CmdNotifyTaken | Remove attempt-time foundry notification/isLooted mutation. Add NotifyPickupCommittedServer using existing OnPlateTakenServer after successful transfer; host-only guard. Preserve IInventoryItem methods/material field. Do not duplicate inventory consumption here. |
| FOUNDRY | FinishPrinting, HandlePlateLooted | Register exact current plate/source on host; release foundry only for its accepted transfer. Retain spawn, material choice, printing duration and animation. No foundry-rule rewrite. |
| CHARGE | TryInsertDrill, CmdPlaceDrillInStation, RpcPlaceDrillInStation; Update removal branch, ServerHandleDrillRemoved, RpcClearStation | One held→charge commit; host occupancy initialized outside observers. Add ValidateInsertServer/CommitInsertServer/CommitRemoveServer with socket revision. Charging algorithm, rates and visual RPC cadence unchanged. Polling cannot react to owner-only preview. |
| CARD | InitializeSocket; TryInsertCard, CmdPlaceCardInSocket, RpcPlaceCardInSocket; Update removal branch, ServerHandleCardRemoved, RpcClearSocket | Minimal compatibility adapter using same item transfer; add destination validation/commit/remove helpers and socket revision. Derive card ID from accepted item, preserve existing manager insert/remove APIs after accepted transition. No puzzle-rule changes. |
| PAGE | PlacePageOnSurface (inventory release call only) | Replace no-argument slot clear with explicit page-identity release. Preserve drawing, payload, pose, hologram, tutorial and surface algorithm; only a required caller adapter. |

Read-only dependencies: PLAYER, InventoryUI/InventorySlot/ItemData, LiftManager, DRILL/WELDER/MODULE, DISPENSER/Keycard_Item/Keycard_StationManager, NotepadModule, StationController/FloodManager, existing prefabs/scenes and installed PurrNet. CARD/PAGE adapters touch only calls across the changed transfer boundary, not unrelated system rules. Revise this plan before further scope expansion. Test-only files may be added separately if useful.

## Networking Impact

### RPC changes

| Existing path | Planned path / execution |
| --- | --- |
| PickupServerRpc(runLocally:true) | ServerRpc(runLocally:false, requireOwnership:true) with explicit sender/owner comparison under Unsafe. Remote sends exact item/slot/generation/revision/request. Host player calls plain helper directly. Prediction is a separate local function. |
| DropServerRpc(runLocally:true) | Same host-only request pattern; exact captured source. Host accepts pose/parent, removes ownership and applies force once. OnDrop UI/tool callbacks are local owner effects, not repeated authoritative mutation. |
| ExtractCurrentHeldItem as runLocally RPC | Public owner request wrapper; separate ServerRpc for exact standalone extraction. In-scope socket callers submit one destination transfer instead. |
| ObserversPickupRpc/DropRpc/ExtractRpc | Retire mutation-oriented bodies in favor of persistent item state and one idempotent accepted-state presentation/result path. No observer clears slots by current selection. Avoid sending both old and new paths. |
| CmdTryPlacePlate | Add placing inventory, captured source slot, item generation/revision, request ID and expected socket round/revision. Explicit sender matches placing inventory.owner. Shared placement performed once by plain host helper. |
| RpcPlacePlateInSocket(runLocally:true) | Visual compatibility method only; no LocalPlayer/removal. Host sets socket/item before broadcast. Snapshot is persistent truth; an optional unbuffered placement effect is revision-gated. |
| CmdFixCrack(int) from local bare counter | One contextual completion ServerRpc with sender/socket/plate/round and actor/tool identity. Local point mask stays local; host validates current state and commits once through TryCompleteCrackServer. Old context-free entry cannot remain an independent route. Preserve public name only if binding search requires a non-authoritative wrapper. |
| CmdNotifyTaken from loot attempt | Retired; server post-commit plain notification invokes existing foundry event once. |
| Charge/keycard extraction then placement | One accepted destination transfer, with socket-specific checks/commit/remove; no independent legacy placement route left accepting already-consumed items. |
| Commit result | TargetRpc(PlayerID target, ...) for requesting player, with result/request ID and accepted revision/context. Apply directly for local host requester instead of relying on host target loopback. Rejection carries enough accepted state to restore only the affected prediction. |

ReliableOrdered is appropriate for these discrete requests/results; no reliability migration. Attributes use installed lowercase named arguments. Verify generated packing for the proposed plain structs and NetworkBehaviour/GameObject references before implementation proceeds; existing project RPCs already use these object references, but custom SyncVar packing still needs compilation.

Installed source checked: RPC attributes and RPCInfo (`Runtime/CoreModules/RPCs/RPCSignature.cs`); NetworkIdentity send/receive validation; SyncVar constructor `(T initialValue = default, float sendIntervalInSeconds = 0f, bool ownerAuth = false)` and latest observer state; lifecycle hooks; NetworkTransform parent-change suppression. Never infer host duplication simply from runLocally. Remote observer presentation must not originate a new shared-parent command under Unsafe.

### Authority, identity and synchronization

- **Authority:** shared transfers and Hull crack results have one host commit point; local selection/progress remains owner-controlled. The model is intentionally mixed and operation-specific.
- **Ownership:** host assigns holder owner on pickup and removes owner when dropping/socketing; standalone compatibility extraction preserves existing owner. These explicit changes prevent an old holder controlling a socket object. No global authority conversion.
- **NetworkIdentity:** reuse ItemLoot, InventoryManager and existing socket identities; no components added to assets, no GUID changes. Keeping identity roots active while hiding renderers is a deliberate presentation/lifecycle change to verify.
- **Synchronization:** persistent latest item/socket/equipped records plus revision-gated result/effect application. Do not assume cross-object snapshot atomicity or callback ordering. Reapply when referenced player/item becomes available; never use a fixed one-second delay as readiness.
- **Network Rules:** remain Unsafe; no package/preset/identity override changes.
- **Prediction:** bounded local visual preview only; no PurrDiction/tick changes or new physics prediction system. Host authoritative impulse, responsive local equipment.

### Host behavior

Host-owned input routes directly to the server helper and gets a local result once; remote requests use sender context and host slot ledger. Host gameplay never reads LocalPlayer to resolve the remote inventory. Host initializes socket state before observers. On host, a snapshot callback/result/effect can all be observed, so revisions and presentation transition checks prevent repeated callbacks/animations/consumption. No forced round trip for the host player and no reliance on host being a client observer to establish gameplay state.

### Remote client behavior

Owner selects/equips and previews hand changes immediately; request waits only for shared acceptance. Other player's inventory/UI is never cleared. On accept the exact captured slot is reconciled, even after local selection changes. On reject/current-state correction only the affected predicted item is restored. Remote sees the other player's selected accepted item and socket plate via persistent state; physics remains a replica of host world movement. Delayed or duplicate results do not overwrite newer state.

## Unity Serialization Risk

Risk: **Medium** because existing network components gain synchronized runtime fields and held presentation changes; public UnityEvent targets must remain stable.

- Do not rename existing serialized fields, script classes/files, enum values, prefab IDs, ItemData or CrackData fields. Do not change existing field types or `.meta` GUIDs.
- Keep OnSocketInteracted, OnStopInteract, LootItem and other serialized no-argument entry points. HullBreach.prefab contains persistent bindings to the socket methods; sampled plate prefab binds ItemLoot.LootItem.
- New host slot arrays, request state, render caches and point maps are runtime-only. New SyncVars must initialize in code and tolerate existing assets without new inspector assignments. Keep public slottedPlate/currentCrackID/etc as mirrors for current consumers, not serialized truth.
- No Prefab, Scene, ScriptableObject, Network Rules or package modification. Inspect asset references read-only and compile new network data packing with installed PurrNet. Never hand-edit Unity YAML for this change.
- New runtime generation/revision is not a save-format change; old/new builds are not protocol-compatible during a live session. Both friends must run the same build after deployment; use fresh sessions during testing.

## Behaviour Risk

Preserve slot ordering, keys/scroll lock, local camera positions, sway, drop distance/raycast placement, lift-parent intent, handbook exemptions, drill/weld timing and charge rate, foundry print timing/materials, Hull depth/material validation and water drainage/cooldown. Changes intentionally include correct acceptance/rejection, remote held visibility, host-only world physics, transactional socket removal and contextual once-only crack completion. Partial weld progress remains local, preserving the existing minigame rather than adding cooperative partial-progress combining.

Most likely regressions: visual flashes on rejected prediction; owner-only item UI visible remotely; remote hand following inactive camera ancestry; duplicate OnEquip/OnDrop side effects; stale NT interpolation after re-enable; world item parenting/physics on lift; charging one extra frame after pickup; keycard feature callback firing twice; placement snapshot resetting completed welds; starting item/reference arriving after initial snapshot. Treat these as verification requirements, not reasons to add a broad framework.

Do not fix unrelated performance, station authority, voice, stress, role selection, notepad protocol or generalized inventory architecture. Preserve unresolved out-of-scope behaviors explicitly rather than claiming this plan repairs every inventory-related feature.

## Implementation Steps

1. **Characterize only:** capture host/remote traces for F01–F17 with both role assignments; log item identity, inventory owner, NT owner/controller, slot, parent, visibility, Rigidbody mode, crack/socket and side. Confirm remote hand hierarchy, plate point IDs, source socket initialization and existing physics modules. Search UnityEvent/public method bindings before signature changes.
2. **Add minimal records:** ItemLoot state + InventoryManager host slots/equipped presentation + Hull socket snapshot. Verify custom packing, initialization before owner early-return, and current-state replay. No new prefab component. Ensure compatibility bootstrap for host-spawned source items and owner-created handbook.
3. **Convert pickup/drop:** separate preview from guarded commit; exact item/slot clearing and rejection recovery; accepted foundry release; lift event/OnDrop applied once; host-only impulse. Wire ForcePickup success to same path. Keep the legacy path disabled/removed in code so two writers cannot coexist.
4. **Convert extraction/socket adapters as one coherent slice:** standalone explicit extraction wrapper; single charge/keycard insertion; accepted source removal replaces polling authority. Add ReleaseItemFromInventory and adapt PAGE's one release call to explicit page identity without changing its surface algorithm. Update every direct extraction caller before deleting old RPC behavior.
5. **Convert Hull placement:** validate identified inventory/item/socket/round; commit source release and occupant together; remove observer LocalPlayer consumption; retain physical plate, disable plate loot, share accepted rotation. Reject duplicate/occupied/stale placement.
6. **Correlate weld completion:** point IDs/mask + round/occupant context, locally responsive progress, once-only host completion/drainage and idempotent fixed display. Preserve current gameplay constants. Handle teardown/new round invalidation.
7. **Finish held presentation/replay:** renderer rather than identity hiding, parent suppression, equipped/possession correlation, interpolation handoff and spawn-reference ordering. No continuous RPC for selection or hand pose.
8. **Verify and review diff:** run the cases below; review exact authority/RPC/ownership changes against this plan and serialized/public API preservation. Commit related transfer work together; do not merge an intermediate state with two competing paths or broken socket callers.

## Rollback Strategy

Keep the baseline commit and document-only plan unchanged as reference. Implement in small reviewable commits with explicit dependencies, but release only a coherent transfer slice. If a regression cannot be corrected in scope, revert the dependent transfer commits together, including caller adapters and synchronized records; do not revert only one socket or leave the old and new RPC paths enabled together. No prefab/scene/save migration is expected, so rollback should restore behavior by reverting project-owned C# and any test-only files. End active sessions and restart both peers after rollback because network protocol layouts changed. Avoid runtime feature toggles that permit two authority models inside one session.

## Verification

Use Unity 6000.3.10f1 and locked PurrNet 1.19.1. Compile/codegen first; source reasoning alone does not close the plan. Host plus remote client is required, with Engineer/Technician swapped. Dedicated server is not a new product requirement; nevertheless server commit helpers must not require a local player.

| Test | Expected result |
| --- | --- |
| Host and remote ordinary pickup/equip/drop | One host transfer revision each; correct slot/owner/pose; immediate local selection; remote sees accepted held item; drop force once on host. |
| Two friends pick the same item nearly simultaneously | Exactly one accepted holder; loser restores latest world/held view and receives no duplicate slot entry. |
| Repeat same request; replay old revision after drop/re-pickup | Duplicate answer may repeat, but no revision/consumption/effects repeated; old request rejected. |
| Change selected slot while plate/drop/extraction request pending | Captured plate/item slot reconciles; newly selected item remains untouched and visible as appropriate. |
| Rejected pickup with full inventory / rejected insert | No host slot, ownership, source socket/foundry or occupant mutation; local preview safely restores. |
| Foundry loot attempt without free slot, then successful pickup | Foundry remains occupied after rejection; releases once after acceptance; no second print on top of retained plate. |
| Remote places plate while host holds drill/card and vice versa | Only placing inventory loses its captured plate; other held item remains unchanged; one physical plate in one socket. |
| Two plates race for same crack / same plate for two cracks | At most one accepted occupant; rejected plate remains held; no second source consumption. |
| Wrong material/depth; socket from another crack/station | Existing warning retained; no transfer; correct association mandatory. |
| Old placement arrives after socket/round reset or item reuse | Generation/revision mismatch rejects; no stale occupant or new-round inventory loss. |
| Try loot plated/fixed Hull plate | No new removal mechanic; no transfer out of occupied Hull socket through generic pickup. |
| Charge insertion/pickup with immediate reinteraction | Inventory and charge occupancy change in same commit; host charging stops once removed; preview reparent cannot trigger removal. |
| Keycard dispenser pickup, insert and remove on both peers | One inventory/socket change and one existing puzzle callback; no generator/tester rule change. |
| Normal and lift drop, handbook guards | Preserve existing allowed/restricted paths, lift pose/flags and tutorial callback; no duplicate lift event/impulse. |
| Standalone extraction and explicit page release | Exact page/item released, even if selection changed; surface algorithm/payload untouched; no second transfer from internal clear helper. |
| Starting handbook / forced page pickup | Existing creation/grant behavior retained; accepted host slot established once; full-slot notepad fallback unchanged and documented outside shared-surface guarantees. |
| Complete weld point twice | Local progress responsive; each local point identity counted once; one contextual completion request when all four are locally complete. |
| Two peers finish the same plated crack | Existing local minigame preserved; host accepts one Fixed transition/drainage. Partial counts are not combined into a new mechanic. |
| Old point/completion after new plate/round; repeat complete | Rejected or idempotent; no reset of newer weld progress, no extra drainage. |
| Host receiving commit result, SyncVar callback and effect | One local slot clear, foundry/source callback, placement animation and completion effect. |
| Observer removal/re-add or late observer if supported | Correct holder/equipped item, occupancy/pose/crack/Fixed state reconstructed; no historical consumption. A repeated same-context snapshot does not reset ongoing local welding; partial weld replay is not added. |
| Latency 100–250 ms; rapid equip/camera switch during pending operation | Held selection/sway/input immediate; no extra ownership/parent commands from preview; accepted state converges without incorrect restore. |
| Disconnect/despawn during pending request; reconnect/new inventory | Pending views/registrations cleared, destroyed refs not reused; no callback into old inventory. Existing owner-disconnect item policy preserved; do not add persistence/refund mechanics. |
| Scene unload/reload and all sampled item assets | No missing script/UnityEvent references; identities remain registered while held; NT re-enable has no stale snap; root/renderer/tool UI states correct. |

Add meaningful test-only invariant checks for revision rejection, exact-slot clear, occupied destination, once-only commit and point-mask completion if the project test setup permits them. Multiplayer execution remains required for hierarchy, RPC dispatch, ownership and presentation. Record measured results, unresolved constraints and any scope changes before marking implementation complete.
