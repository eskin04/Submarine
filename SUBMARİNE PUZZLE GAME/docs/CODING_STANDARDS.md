Coding Standards
These standards apply to project-owned C# code in this Unity project.
Project environment:
Unity 6.3
C#
PurrNet networking
Two-player cooperative puzzle game
Detailed networking-specific rules are defined in:
`docs/NETWORKING\_STANDARDS.md`
The goal is simple, correct, readable, maintainable and modular code.
The goal is NOT maximum abstraction or enterprise-style architecture.
---
Core Priorities
Code changes should prioritize:
Correctness
Preservation of intended gameplay behavior
Multiplayer consistency
Readability
Maintainability
Modularity
Testability
Appropriate runtime performance
Follow:
Clean Code principles
SOLID principles where they provide practical value
composition over inheritance
high cohesion
low coupling
explicit dependencies
focused changes
Do not over-engineer.
Do not introduce architecture, patterns or abstractions that do not solve a
concrete problem in the current project.
---
Naming
Use intention-revealing names.
Prefer:
`PlayerMovementController`
`ApplyDamage`
`TryInsertPlate`
`CurrentHealth`
`isGrounded`
`canInteract`
`hasAuthority`
Avoid vague names such as:
`Helper`
`Utils`
`Thing`
`Stuff`
`Temp`
`Data`
unless the name genuinely describes the responsibility.
Names such as `Manager` should be used only when the class actually coordinates
or manages a well-defined system.
Boolean names should describe a condition.
Prefer:
`isActive`
`hasItem`
`canRepair`
`isOwner`
Avoid unclear names such as:
`activeFlag`
`check`
`stateBool`
---
Methods
Methods should have one clear purpose.
Avoid:
excessively long methods
deep nesting
excessive parameters
hidden side effects
duplicated logic
Prefer guard clauses when they improve readability.
Example:
```csharp
if (!CanInteract())
    return;

Interact();
```
Prefer this over unnecessary nested conditional blocks.
Extract complicated conditions into clearly named methods when doing so improves
readability.
Do not split trivial logic into many tiny methods when that makes the execution
flow harder to follow.
---
Classes
A class should have one primary responsibility.
Avoid large classes that unnecessarily combine:
input
gameplay rules
networking
UI
inventory
audio
animation
persistence
Separate responsibilities when they evolve independently or create excessive
coupling.
Do not split classes merely to satisfy a design rule.
A focused MonoBehaviour containing closely related behavior is preferable to a
large collection of unnecessary wrapper classes.
---
SOLID
SOLID is a design guideline, not a mechanical requirement.
Single Responsibility Principle
A class should have one primary reason to change.
Separate independently changing concerns when practical.
Examples:
gameplay logic
network transport
presentation
input
persistence
should not be unnecessarily coupled.
---
Open/Closed Principle
Prefer composition or focused strategies when multiple implementations are
realistically expected.
Do not create extension points for hypothetical future requirements.
---
Liskov Substitution Principle
Derived classes must remain valid substitutes for their base classes.
Avoid inheritance hierarchies where subclasses regularly disable or override
unrelated parent behavior.
Prefer composition when behavior varies independently.
---
Interface Segregation Principle
Interfaces should remain small and focused.
Reasonable examples include:
`IDamageable`
`IInteractable`
`ITargetable`
Do not create an interface for every class.
Interfaces should normally provide at least one of:
meaningful decoupling
multiple implementations
useful testability
a stable module boundary
---
Dependency Inversion Principle
High-level gameplay code should not unnecessarily depend on low-level
implementation details.
Dependencies should be explicit.
Do not introduce a dependency injection framework unless the project develops a
real need for one.
---
Composition Over Inheritance
Prefer Unity component composition over deep inheritance hierarchies.
Avoid structures such as:
Entity
→ LivingEntity
→ Character
→ Humanoid
→ Player
→ NetworkPlayer
when focused components express the behavior more clearly.
Inheritance is acceptable when the relationship is genuinely stable and
conceptually correct.
---
MonoBehaviour
MonoBehaviour classes should mainly coordinate Unity-specific behavior.
Avoid placing large amounts of unrelated gameplay logic directly inside:
`Awake`
`Start`
`Update`
`FixedUpdate`
`LateUpdate`
`OnEnable`
`OnDisable`
Lifecycle methods should remain easy to understand.
Prefer:
```csharp
private void Update()
{
    HandleInput();
    UpdateInteraction();
}
```
over a large multi-purpose `Update()` method.
---
Unity Lifecycle
Respect Unity object lifecycle.
Event subscriptions must have clear ownership.
Common patterns include:
OnEnable
→ subscribe
OnDisable
→ unsubscribe
or, for network lifecycle:
OnSpawned
→ subscribe
OnDespawned
→ unsubscribe
Choose the lifecycle matching the lifetime of the behavior.
Do not mix lifecycle models without understanding their consequences.
Prevent duplicate event subscriptions.
Always consider host mode when networking is involved.
---
Unity Serialization
Treat serialized data as part of the project's compatibility surface.
Do not casually rename serialized fields.
If a serialized field must be renamed, use:
```csharp
\[FormerlySerializedAs("oldFieldName")]
```
when appropriate.
Preserve:
Inspector references
Prefab references
Scene references
ScriptableObject references
UnityEvents
animation references
`.meta` GUIDs
Do not change serialized field types without checking their usage.
Do not rename public methods that may be UnityEvent targets without first
checking serialized references.
---
Fields and Encapsulation
Fields should normally be private.
For Inspector dependencies prefer:
```csharp
\[SerializeField] private Transform firePoint;
```
over unnecessarily public fields.
Expose read-only state when practical.
Prefer:
```csharp
public float CurrentHealth { get; private set; }
```
over:
```csharp
public float currentHealth;
```
State changes should occur through meaningful operations.
Prefer:
```csharp
health.ApplyDamage(amount);
```
instead of another class directly modifying health internals.
Do not expose mutable collections unless external mutation is intentional.
---
Dependencies
Dependencies should be explicit.
For MonoBehaviours prefer, as appropriate:
serialized references
initialization-time `GetComponent`
explicit initialization
For plain C# classes prefer constructor dependencies where practical.
Avoid runtime dependency discovery such as:
`GameObject.Find`
`FindObjectOfType`
`FindAnyObjectByType`
unless there is a clear reason.
Do not use a service locator as the default architecture.
---
Singletons and Static State
Do not introduce a Singleton merely for convenient global access.
Singletons may be appropriate for genuine application-level services.
Avoid unnecessary mutable static state.
Be particularly careful with static events because they may:
retain destroyed objects
create duplicate listeners
hide dependencies
survive scene transitions unexpectedly
---
Events
Events should reduce coupling rather than hide execution flow.
For every event consider:
who owns it
who subscribes
when subscription occurs
when unsubscription occurs
whether host mode can register twice
whether scene reload creates duplicate listeners
Prefer a direct dependency when an event adds no architectural value.
---
Update and Performance
Treat per-frame code as performance-sensitive.
Avoid unnecessary work inside:
`Update`
`FixedUpdate`
`LateUpdate`
Examples include:
unnecessary LINQ allocations
repeated `GetComponent`
`GameObject.Find`
repeated hierarchy searches
temporary collections
unnecessary string creation
frequent `Instantiate` / `Destroy`
Cache references where appropriate.
Do not perform speculative optimization where performance is irrelevant.
---
Physics
For ordinary Unity physics, perform physics-related work according to Unity's
physics lifecycle.
Do not directly manipulate Transform for Rigidbody-controlled objects unless
intentional.
If PurrNet or PurrDiction controls simulation timing, follow the framework's
installed lifecycle instead of blindly moving logic into `FixedUpdate`.
---
Object Lifetime and Pooling
Consider object pooling for objects created and destroyed frequently.
Examples may include:
projectiles
temporary effects
frequently spawned gameplay objects
Do not introduce pooling when object creation frequency does not justify the
additional complexity.
---
ScriptableObjects
Use ScriptableObjects primarily for:
configuration
reusable static game data
definitions
tunable gameplay values
Avoid using ScriptableObjects as uncontrolled mutable global runtime state unless
that architecture is explicitly intended.
---
Networking Separation
Detailed PurrNet rules are defined in:
`docs/NETWORKING\_STANDARDS.md`
At the code-design level, clearly distinguish:
local input
local presentation
owner-controlled behavior
shared gameplay state
host/server-owned progression
replicated presentation
Do not mix all of these responsibilities inside one large NetworkBehaviour when
they can be separated without unnecessary complexity.
---
Multiplayer Authority
This is a trusted two-player cooperative game.
Anti-cheat is not the primary networking goal.
The main goals are:
consistent shared state
predictable host/client behavior
explicit authority boundaries
responsive player-controlled behavior
avoidance of accidental desynchronization
minimal networking complexity
Do not make every operation server-authoritative simply because networking is
present.
Authority should match the operation.
---
Local and Owner-Controlled Behavior
Local or owner-controlled behavior may remain immediately responsive.
Examples may include:
input
camera
selected inventory slot
interaction highlighting
item sway
local tool animation
local puzzle/minigame feedback
Presentation-only actions should not require an unnecessary network round trip.
---
Shared Gameplay State
Shared state should have a clearly defined writer.
Examples include:
item possession
item transfer
socket occupancy
station state
accepted puzzle completion
water level
shared penalties and rewards
Where appropriate, the host should commit shared transitions once.
Presentation should react to accepted gameplay state rather than independently
decide gameplay outcomes.
---
Persistent vs Presentation State
Do not use temporary visual state as authoritative gameplay state.
Examples of unreliable authority sources include:
`activeSelf`
current selected UI slot
visual parent
animation state
when they are only presentation details.
Important shared state should remain reconstructable after:
delayed initialization
observer re-add
relevant network spawn ordering
Do not rely only on transient presentation RPCs for persistent gameplay state.
---
Inventory and Item Transfers
Inventory UI and currently selected slots may remain owner-local.
Shared item possession must not depend solely on local UI state.
When a shared transfer is performed, identify:
the exact item
the exact source
the exact destination
Prevent:
stale operations changing newer state
duplicate acceptance
unrelated selected items being removed
multiple competing authoritative writers
A transfer must not mean:
> remove whatever item happens to be selected when the request executes
when the original interaction referred to a specific item.
Keep the implementation proportional to the needs of this two-player game.
Do not build a generalized transaction framework unless the actual code requires
one.
---
Presentation
Presentation code may manage:
animation
UI
sound
particles
held-item visuals
repair effects
Presentation code should not independently:
consume inventory
complete puzzles
change water level
progress rounds
assign network ownership
unless that presentation component is explicitly designed as the authoritative
path.
---
Coroutines and Async Work
Respect Unity and network object lifetime.
Consider:
GameObject destruction
scene unload
network despawn
round reset
cancellation
stale delayed callbacks
Delayed operations must not mutate a new round or reused object without checking
that their context is still valid.
---
Magic Values
Avoid unexplained important gameplay constants.
Use where appropriate:
named constants
serialized configuration
enums
ScriptableObjects
Do not extract every trivial literal into a constant if doing so reduces
readability.
---
Logging
Logs should provide actionable diagnostic information.
Avoid `Debug.Log` spam inside:
Update loops
physics loops
networking hot paths
frequently repeated callbacks
Remove temporary debugging output before completing a refactor unless it has
lasting diagnostic value.
---
Comments
Code structure and naming should explain WHAT the code does.
Comments should primarily explain WHY.
Useful comments include:
PurrNet-specific constraints
Unity lifecycle quirks
non-obvious ownership decisions
compatibility requirements
performance tradeoffs
temporary workarounds
Avoid comments that merely repeat obvious code.
---
Error Handling
Do not silently ignore invalid shared state.
Expected invalid user actions should fail safely.
Impossible programming states should produce useful diagnostics.
Do not add null checks everywhere simply to hide invalid initialization.
Required dependencies should normally be guaranteed during setup.
---
Refactoring
When refactoring:
Preserve intended gameplay behavior unless a change is explicitly requested.
Preserve network semantics unless the approved plan explicitly changes them.
Keep changes focused.
Avoid unrelated rewrites.
Preserve serialized references.
Avoid speculative abstractions.
Prefer small reviewable changes.
Review the final diff.
Consider both host and remote-client paths.
Do not combine architectural cleanup with unrelated feature development.
---
Refactor Scope
Solve the specific problem identified by the task.
Do not use one refactor as an opportunity to redesign neighboring systems.
For example, an inventory consistency refactor should not also redesign:
voice chat
station generation
level progression
unrelated puzzles
UI layout
save systems
unless required by the task.
---
Testing
Pure gameplay logic should be testable without a Scene when practical.
Use Unity Edit Mode tests for isolated logic where valuable.
Use Play Mode or actual multiplayer testing for behavior involving:
MonoBehaviour lifecycle
physics
scenes
PurrNet RPCs
ownership
synchronization
Networking changes should consider at minimum:
host player
remote player
Engineer / Technician roles swapped
duplicate requests
stale requests
rejected operations
disconnect/despawn where relevant
Static reasoning alone is not sufficient to declare a multiplayer networking
refactor verified.
---
Final Review Checklist
Before considering a code change complete, verify:
Is intended gameplay behavior preserved?
Is shared multiplayer state consistent?
Are authority and ownership semantics intentional?
Are APIs compatible with the installed PurrNet version?
Were both host and remote-client paths considered?
Were serialized fields preserved?
Were UnityEvent targets preserved?
Were unrelated files left untouched?
Was unnecessary abstraction avoided?
Are responsibilities clearer than before?
Did the change solve the actual problem?