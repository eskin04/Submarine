# Inventory transfer consistency and Hull Breach placement

Status: **Proposed implementation plan; no implementation performed.** Date: 2026-10-03.

## Problem

This is a trusted two-player Engineer/Technician co-op game. Unsafe Network Rules are intentional and remain unchanged. The objective is consistent shared possession and socket state, not comprehensive cheat prevention. Local selection, item sway, tool input and hand/camera feedback remain responsive and owner-controlled.

Today inventory slots exist only on the owner; replicated extraction reads that owner-only state on the host. Socket insertion often consists of separate extraction and placement requests. Hull plate placement clears the current slot of every observer's LocalPlayer instead of consuming the identified placing player's plate. Socket occupancy, physics, ownership and item presentation do not have one accepted transition. Current selected slots, object parents and activeSelf are used as indirect truth.

The planned behavioral change is explicit: **the host commits shared transfers and Hull occupancy/completion; the owner controls local input and immediate held visuals.** This is a focused authority/protocol change, not a general cleanup or a Network Rules migration.

## Evidence

### Instructions and baseline

All seven required guidance documents were found, nonempty and read from this current worktree. No other checkout was accessed. This plan follows the required PLANS.md sections. Current review baseline: 28e412f9f42851640c2a548598bcbd46c0c07e70; historical flow references below apply to unchanged production code.

Local project metadata identifies Unity 6000.3.10f1; local guidance identifies PurrNet 1.19.1, locked at `266cb63efd3d858d6d2fce68c2b0b2364ca78c24`. Historical source line references were recorded at commit `874539b72a7ad000af250a45bca8b42887742b8e`; the affected production files remain unchanged at the current review baseline. Installed package implementation still requires local verification as described below. Source analysis is not a reproduced multiplayer result. The user's gameplay clarification supersedes security-first wording in the earlier audit; Unsafe itself is not registered as a defect in this plan.

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

1. ItemLoot has one host-written compact ItemPossession record: location (World, Inventory, Socket, Detached), holder InventoryManager plus slot when Inventory, socket NetworkBehaviour when Socket, and version. Holder/socket are mutually exclusive. Detached preserves legacy extraction; Fixed derives from Hull crack state. PurrNet owner is a controller, not reliable slot membership.
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

One owner-authored selected-item SyncVar serves remote held rendering only. Render it only when accepted possession agrees; it never authorizes transfer or completion. Pending pickup/drop/socket previews change only local visuals, not ownership, accepted slots, source occupancy, shared force or consumption. No networked preview clone system or per-frame held RPC.

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
| World/lift | Host removes owner, sets pose/parent and applies force once through existing transform path. Inspect existing physics components before changing replica modes. |
| Detached | Preserve existing extraction owner/kinematic handoff; no inventory membership. Not a notepad-surface redesign. |
| Charge/keycard socket | Host removes holder owner; socket governs kinematic item; accepted pickup releases source occupancy. |
| Hull socket | Host removes holder owner; kinematic/non-lootable; physical plate retained for welding; Fixed belongs to crack state. |

Socket ownership removal is a disclosed behavior change; prefab ownerAuth flags stay unchanged. PurrNet ownership still does not replace item/slot checks.

Keep network identity alive where renderer hiding is needed; preserve owner-only item UI/input. Local hand/camera reparent and observer display must not emit shared-parent operations. Drop pose lives in existing transform synchronization/request payload; Hull rotation lives only in socket snapshot.

Previous source verification named StartIgnoringParentChanges/StopIgnoringParentChanges, but this current worktree has no Library package cache. Recheck these exact APIs locally before implementation; do not read another checkout or invent an equivalent. Test remote hand ancestry and NT re-enable without prefab edits.

### Hull placement

CRACK.TryInsertPlate captures placing inventory, plate/source slot, expected item/inventory/socket versions and crack ID. CmdTryPlacePlate receives that context and RPCInfo.

Host verifies sender/owner, exact membership, versions, registered socket/station/crack coordinates, active round, Active crack, empty socket and existing material/depth rules. It commits placing slot release + plate Socket membership + ownership removal + accepted socket occupant + Active -> Plated with one stamp. Only the placing inventory reconciles its exact plate slot.

RpcPlacePlateInSocket never calls LocalPlayer or RemoveCurrentItem. Consumption is Inventory -> Socket, not destruction. Another observer's selected drill/card is unaffected. Item Socket membership rejects reuse in two cracks; socket/version guards reject two plates. Wrong-material/stale/rejected placement does not consume.

No new player plate-removal/refund mechanic. Plated/fixed Hull plates reject generic loot; teardown changes only station-owned placement context.

### Welding completion

Leave WELD, drill, welder and module algorithms unchanged. ApplyWeld already guards isWelded and sets it before notification. No point RPC/mask/registry, server aiming or cross-player combining of partial counts.

CRACK.OnPointWelded keeps local count/feedback but checks the current plate's four actual point flags before requesting completion. Capture current plate/item version and crack/socket context when interaction begins. Initialize points only on a new accepted placement; repeated same-context display does not reset progress. Old callbacks cannot complete a new plate whose actual points are not welded.

Final completion identifies actor/tool and socket/crack/plate/context. Host checks accepted tool possession and current Plated occupant/versions, then accepts the trusted local outcome. Do not wait for owner-selected presentation replication. Existing Fixed guard gives one completion/drainage. Invoke drainage plainly on host after acceptance rather than nesting a server-to-server RPC. Preserve amounts/cooldown. Same-context rejection can allow deliberate retry; stale completion cannot change a newer crack.

### Sources, compatibility and scope

- Foundry releases its exact current plate only on accepted pickup, using existing OnPlateTakenServer after commit. Failed/full inventory attempt leaves it occupied; retire attempt-time isLooted/notification.
- Charge/keycard source removal happens inside accepted pickup, not parent/activeSelf polling. Register initial source occupancy in existing host spawn/init paths.
- Keycard_Socket receives only required transfer-boundary adapter; preserve existing puzzle insert/remove callbacks once, generator/tester/dispenser unchanged.
- PAGE changes only its release call to an exact-item API such as ReleaseItemFromInventory(gameObject). Preserve drawing/payload/surface pose/tutorial logic; no unrelated surface replication claim.
- Owner-created starting handbook remains allowed under Unsafe; bootstrap fresh item version and accepted slot, preserve tutorial/drop exemptions. Successful forced page pickup follows the same path; full-slot policy stays out of scope.
- Standalone extraction captures item/slot locally and requests Detached; in-scope sockets no longer call it first.
- Despawn clears only matching accepted item/slot/socket context and local handlers; existing disconnect policy retained, no persistence/refunds.
- Activation/reset changes socket version. Preserve intended fixed-plate visuals; no broad round inventory cleanup or held-item consumption.

## Files Affected

**Nine expected production files**, down from ten. None changed during this review. No new component/framework/interface/service required; small data structs stay alongside existing components.

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

WELD/DRILL/WELDER/MODULE, PLAYER, UI/data/sway, lift, DISPENSER/Keycard manager/data, NotepadModule, StationController/FloodManager, assets and package remain read-only. Revise plan before expanding this set.

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

Local documents identify Unity 6000.3.10f1 and PurrNet 1.19.1; local packages-lock.json locks 266cb63efd3d858d6d2fce68c2b0b2364ca78c24. No Library cache exists here: this review cannot freshly verify installed implementation. Compilation/packing of compact structs/references, ownerAuth SyncVar, exact lifecycle/parent APIs must be checked against locally installed 1.19.1 before implementation. Use local project lowercase ServerRpc/ObserversRpc/TargetRpc patterns; never substitute another framework. Dedicated server is not added as a product requirement.

## Unity Serialization Risk

Medium: runtime protocol/held display changes; assets stay untouched. Preserve field names/types, class/file names, enum values, ItemData/CrackData, script GUIDs and UnityEvents including LootItem/OnSocketInteracted/OnStopInteract. New state initializes in code without Inspector wiring; public occupants remain mirrors.

No Prefab/Scene/ScriptableObject/Network Rules/ProjectSettings/Package/assembly edit. Both peers need matching new build/fresh session. Compile packing and inspect representative bindings during implementation.

## Behaviour Risk

Preserve keys/slots/scroll, sway/camera, drop/lift intent, handbook exemptions, tool charge/weld timing, foundry timing/materials, Hull validity/drainage/cooldown and keycard rules.

Intentional fixes: exact consumption, one socket commit, remote held view, socket ownership handoff, stale/competing rejection and once-only completion. Risks: rejected-preview flash, remote UI/camera ancestry, NT interpolation, replica physics, source ordering, duplicate callbacks and completion retry. One pending transfer restricts overlapping shared commands, not ordinary local input. An item must be accepted before a second shared transfer uses it.

No voice/stress/role/level/random-event/notepad-protocol redesign or fresh security project.

## Implementation Steps

1. Characterize F01-F17 with both roles/host and remote: exact membership/owner/parent, source callbacks, hand ancestry and current point guards.
2. Compact possession and narrow socket state; reuse containers, fresh versions and spawn bootstrap. Verify local package/packing before more code work.
3. Split pickup/drop preview and named host methods; inventory/item/destination versions, exact replies and post-commit foundry/lift events. No cache/second array.
4. Convert extraction and charge/keycard callers coherently; eliminate extract-then-place and polling authority. PAGE gets only exact release call.
5. Hull joint inventory/item/socket/crack placement; delete LocalPlayer observer consumption; preserve material/depth.
6. Contextual final welding completion with actual existing point flags; WELD/tools unchanged; once-only Fixed/drainage.
7. Selected accepted held display/latest-state replay; no second parent writer.
8. Execute tests/review diff; release coherent slice without competing old/new writers.

## Rollback Strategy

Revert dependent production commits together, including callers/protocol records. No live old/new writer flag or partial socket-only rollback. No asset/save migration expected; restart both peers after revert. Keep characterization evidence. Implementation is not started.

## Verification

Unity 6.3/PurrNet 1.19.1 compile/codegen and host+remote tests, roles swapped, are required; none claimed passed now.

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

Focused Edit Mode invariant checks for exact slot/version/destination are useful; multiplayer remains necessary for RPC/identity/hierarchy. No test framework abstraction solely for this refactor.

## Comparison and recommendation

| Dimension | A: previous plan | B: simplified plan |
| --- | --- | --- |
| Complexity | Broad transfer record, second slot array, generation/revision, request IDs/cache/recovery and point-mask work | Compact possession, reused slots, one version mechanism, narrow sockets, selected identity and local preview/reply |
| Introduced concepts | About 10-12 mechanisms, depending on grouping | About 6 cohesive mechanisms; concrete functions rather than transaction plumbing |
| Production files | 10 | 9; WELD unchanged. Existing direct callers/sources still need adapters. |
| Networking risk | More correlated state, packing, recovery and callback ordering | Lower integration surface; still significant shared authority change |
| Maintenance | Caches/eviction, generation reset, pose duplication, retries and masks | Membership/version invariants, current views and existing callbacks |
| Defect coverage | Can solve defects with excess infrastructure | Preserves exact item/slot, single commit, occupancy, duplicates/stale/other-player protection and responsive visuals |

**Simplify current plan.** Keep the detailed flow evidence, mandatory host bookkeeping, meaningful versions, occupancy and replay. Remove speculative generalized transaction/retry/cache/generation/point machinery. Implementation remains unstarted.
