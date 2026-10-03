# Project Architecture

## Purpose and constraints

Favor modular gameplay, high cohesion, explicit dependencies, composition where useful, and clear presentation/gameplay/networking boundaries. These are guidelines, not a requirement to wrap every component in an interface or a separate networking class. UI should display state and forward intent; networking should coordinate replication and ownership without unnecessarily owning puzzle rules.

The authority model is defined by existing PurrNet Network Rules and gameplay code. **Do not assume strict server authority or change authority during general refactoring. Authority changes require explicit approval and separate review.** Preserve ownership, RPC behavior, Unity serialized references, scenes, prefabs, and player-visible behavior unless a change is explicitly planned.

## Audit baseline — 2026-10-03

- Unity: `6000.3.10f1`, the Unity 6.3 family, from `ProjectSettings/ProjectVersion.txt`.
- PurrNet: installed package `1.19.1`, from `package.json` in `Library/PackageCache/dev.purrnet.purrnet@f4dd26fb792b`; Git dependency lock `266cb63efd3d858d6d2fce68c2b0b2364ca78c24` in `Packages/packages-lock.json`.
- Source baseline: commit `874539b72a7ad000af250a45bca8b42887742b8e`; all 217 C# files under `Assets/Scripts` inspected (24,712 physical lines). `InteractionSystem/Outline.cs` is embedded QuickOutline vendor code, identified by its copyright header; inspect integration but keep upstream ownership in mind.
- This worktree initially contained neither `AGENTS.md` nor `docs/`. Guidance was read from `C:/Users/eskin/Documents/GitHub/Submarine/SUBMARİNE PUZZLE GAME/AGENTS.md` and all five documents there: `ARCHITECTURE.md`, `CODING_STANDARDS.md` (empty), `NETWORKING_STANDARDS.md`, `PLANS.md`, and `TECH_DEBT.md`. Source hashes for all 217 scripts match that checkout. These two updated documents are delivered in this worktree; the original checkout is unchanged.
- PurrNet API verification used that checkout's installed package source, whose dependency lock matches this worktree. No production scripts, package files, network configuration, scenes, prefabs, or assets were changed. This is a source audit, not a Unity compile or multiplayer playtest.

## Actual module map

| Area under `Assets/Scripts` | C# files | Existing responsibility and dependencies |
| --- | ---: | --- |
| `Audio` | 6 | FMOD emitters, event channels, settings, ambience; consumes gameplay/global events. |
| `Contract` | 2 | Agreement readiness, page UI, loading and quit flow; couples PurrNet, UI, preferences and audio. |
| `FloodSystem` | 3 | Water simulation, breakdown scheduling, win/loss and visualization; consumes station state and global penalties. |
| `Gambling` | 4 | Slot-machine interaction, animation and local result calculation; sends water adjustments to server. |
| `GameStates` | 4 | PurrNet state nodes for readiness, player spawning, game and results; scene/session/UI orchestration. |
| `HeadLamp` | 1 | Local headlamp presentation. |
| `InteractionSystem` | 7 | Raycast focus, interaction events, module camera/input control, outlines; reaches inventory/tutorial/UI. Includes vendor Outline. |
| `InventorySystem` | 10 | Local slots, equipment, item parenting, ownership transfer, pickup/drop/extraction and UI; implemented chiefly in InventoryManager. |
| `Level` | 3 | Level configuration, station/player references, loading presentation; service registration and scene dependencies. |
| `Lift` | 4 | Lift movement/doors/buttons/item transport; global interaction and inventory signals. |
| `Lobby` | 4 | Game loading, level selection/UI and progress storage; PurrLobby/Steam context and scene flow. |
| `Localization` | 3 | Localization service/helper and world text. |
| `Network` | 1 | ConnectionStarter session access; networking also lives extensively in feature folders. |
| `Notepad` | 2 | Drawing, texture/JPEG/chunk transfer, torn-page spawning, pickup and placement. |
| `Player` | 4 | Stats, camera layer switching, cursor and UI hover. Player movement/framework code outside this directory is outside the requested project-owned audit. |
| `Role` | 1 | Lobby role selection UI. |
| `Station` | 121 | Shared StationController plus Airlock Seal, Engineer Lockdown, Hull Breach, Inversion Relay, Keycard Matrix, Lights Out, Magnetic Interference, Power Routing, Rules of Engagement, Security Lockdown, Spatial Sync and Thermal Runaway. Each combines different amounts of puzzle generation, validation, replication, physical modules and role-specific presentation. |
| `System` | 1 | GlobalEvents public static delegate bus for stats/penalties and other shared effects. |
| `Tutorial` | 15 | Quest assets, state nodes, progress RPCs, input locks, FMOD marker callbacks and quest/contract UI. |
| `UI` | 12 | View registry, settings, game/status/item/module/prompt presentation; subscribes to global feature signals. |
| `UtiltyEvents` | 4 | Stress/stat effects, event UI/statistics/configuration; consumes GlobalEvents and updates game UI. |
| `Voice` | 5 | Vivox authentication/channel lifecycle, radio input/filter, lobby bridge and light/shader presentation; consumes station/tutorial state. |
| **Total** | **217** | No project-owned assembly definitions or test suite found under `Assets/Scripts`. |

## Current topology and authority

The game is organized around a two-player host/client cooperative session with engineer and technician roles. `PlayerSpawningState` maps network-player list entries to lobby-member role entries by index. PurrNet state machines coordinate readiness and scene/game/tutorial progression. Steam/PurrLobby and Vivox are adjacent session services, not interchangeable player identifiers.

`Assets/Prefabs/Managers/NetworkManager.prefab:58` references NetworkRules GUID `d8799083b96579a42b8b2d03e4ceae98`. It resolves to installed PurrNet `Defaults/NetworkRules/Unsafe.asset`. Direct manager references in AgreementScene, TutorialScene, ThanksScene and the playtest/station build scenes use this GUID too. Null `_networkRules` fields on individual identities are not proof of a different manager policy; effective prefab/scene overrides must be checked for each migration.

The installed Unsafe preset sets `ignoreRequireServerAttribute = 1` and `ignoreRequireOwnerAttribute = 1`. PurrNet's `NetworkRules.ShouldIgnoreRequireServer/Owner` and `NetworkIdentity.Broadcasting.ValidateSendingRPC` consult these settings. **An RPC attribute is therefore not an effective ownership/server authorization boundary in these manager configurations.** Existing code intentionally or accidentally depends on client-originated observer calls, client spawning/prediction and ownership/parent changes permitted by these rules. Server-gated SyncVars and explicit `isServer` checks still matter; the rules do not turn every write into a valid write.

Most station managers generate/evaluate puzzles on the server and broadcast visuals, but this is not uniform authority. Slot results are calculated locally; inventory slots are owner-only; hull sockets act on each observer's local inventory; several replicated methods also mutate gameplay. Host topology hides server state that is populated only through client observer execution. Dedicated-server compatibility is not established by this audit and must be an explicit product decision.

## Installed PurrNet semantics verified

These details come from package source, not syntax borrowed from another networking library:

| API | Installed behavior and project implication |
| --- | --- |
| `[ServerRpc(...)]` | Valid lowercase named arguments include `runLocally` and `requireOwnership`. Defaults: ReliableOrdered, `runLocally: false`, `requireOwnership: true`. Unsafe can bypass the ownership requirement. `RPCInfo info = default` supplies sender context when correctly handled. |
| `[ObserversRpc(...)]` | Valid arguments include `runLocally`, `bufferLast`, `requireServer`, `excludeOwner`, `excludeSender`. Defaults: ReliableOrdered, `runLocally: false`, `bufferLast: false`, `requireServer: true`. Unsafe can allow client-originated calls. Do not assume the body runs immediately on the calling server. |
| `[TargetRpc(...)]` | Defaults also include `runLocally: false`, `bufferLast: false`, `requireServer: true`; target-player routing is distinct from trusted sender authorization. |
| `runLocally: true` | Codegen invokes the original method on the caller. Transport skips applicable local replay; it is not automatically a host double-execution defect. Client-side prediction can still run before server validation, and forwarding another RPC from it needs deliberate origin/recipient handling. |
| `OnSpawned()` / `OnSpawned(bool)` | Parameterless hook runs once on a host; bool hook runs for server and client sides. `OnEarlySpawned(bool)` occurs before complete ownership/module readiness. Do not label every parameterless subscription as doubled on host. |
| StateNode `Enter(bool)` / `Exit(bool)` | StateMachine invokes each active side, then the parameterless overload. A host executes both bool calls. StateUpdate is dispatched to the current node; a node's ordinary Unity Update is not inherently restricted to the current state. |
| `SyncVar<T>` | Default ownerAuth is false; writes check controller authority after spawn. `OnObserverAdded` sends latest state. Implicit conversion to T is implemented, so `if (!isRoundActive)` with SyncVar<bool> is valid. Plain arrays/lists/fields and unbuffered RPC history do not acquire SyncVar replay semantics. |
| `NetworkIdentity.OnDestroy()` | Base implementation triggers network despawn cleanup and ticker teardown. Overriding it without calling base is materially different from omitting an empty MonoBehaviour hook. Spawn/despawn subscriptions need matching lifecycle boundaries, not only destruction cleanup. |

Source locations within the installed package: `Runtime/CoreModules/RPCs/{ServerRpcAttribute,ObserversRpcAttribute,TargetRpcAttribute}.cs`, `Runtime/Components/NetworkIdentity/{NetworkIdentity,NetworkIdentity.Broadcasting}.cs`, `Runtime/Components/NetworkBehaviour/NetworkRules.cs`, `Runtime/Components/NetworkModule/SyncVar.cs`, `Runtime/Components/NetworkStateMachine/{StateNode,StateMachine}.cs`, `Codegen/PostProcessor.cs` (local-body dispatch around 2048–2139), and `Defaults/NetworkRules/Unsafe.asset`. The `OnSpawned`/cleanup and RPC restriction checks were traced directly; no unsupported PurrNet syntax was inferred from capitalization or SyncVar shorthand.

## Important existing data flows

1. **Session:** lobby members/roles → PurrNet connection/readiness → player spawn/ownership → MainGameState → FloodManager/start signals → level/station setup. Contract readiness can drive a network scene load. Vivox joins alongside lobby lifecycle.
2. **Station:** breakdown scheduler → StationController state → feature manager → puzzle data/SyncVars/observer effects → engineer/technician controls → ServerRpc → validation/penalty/repair → flood/global UI/audio. Rules and display are frequently in the same NetworkBehaviour.
3. **Inventory:** owner input/local containers → runLocally pickup/drop/extract → object ownership/parent/physics changes → observer presentation. Socket placement separately consumes inventory and updates a station; there is no single validated transaction shared by these paths.
4. **Tutorial:** state transition → quest/view/FMOD audio markers and progress → local input gating → progress/ready RPCs → next state. Delayed callbacks and audio completion currently lack a consistent quest generation token.
5. **Shared services:** public static events, Singleton properties and PurrNet InstanceHandler tie gameplay to UI/audio/camera/session initialization order. Some local effects assume a local player exists even while running replicated code.

## Refactoring boundaries to introduce only when useful

- Extract deterministic puzzle generation/validation from large station NetworkBehaviours, starting with one station. PowerRoutingCore and SpatialSyncCore are useful existing separation examples; retain focused plain data/types and avoid a generic station framework before common requirements are demonstrated.
- Keep network adapters responsible for caller/context validation, ownership, RPC flow, synchronized state and observer snapshots. Keep presentation adapters responsible for meshes, lights, text, tweens and audio. Gameplay owns round state and accepted commands.
- Give inventory transfers one documented transaction and explicit item/player context. Keep local equipment/camera/UI separate from replicated item state. Treat changes to authority/prediction/ownership as behavior changes requiring their own approved plan.
- Scope events to a station/session when multiple instances can exist. Prefer serialized component dependencies or small purpose-specific APIs over global service lookups. Retain appropriate pure static helpers and intentional service entry points.
- Add narrow verification seams for seeded random generation, round transitions, transfer validation and async quest/channel lifecycle. Preserve Unity-facing components and serialized field identities through staged extraction; do not casually rename types, files, fields, enums or move scripts without their `.meta` files.

## Validation constraints

Before an implementation, write the problem, exact files, proposed boundaries, behavior/authority/serialization effects, migration steps and verification in a separate plan following `PLANS.md`. See `TECH_DEBT.md` for evidence and priority. Use host plus remote client, both role assignments, repeat rounds, disconnect/rejoin, scene reload and observer re-entry. Include dedicated server only if it is intended to be supported. Snapshot support must be tested even if lobby policy prevents mid-game joining, because observer membership can change independently.
