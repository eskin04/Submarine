\# Repository Instructions



This repository is a Unity 6.3 multiplayer game project.



The project uses PurrNet as its networking framework.



Primary project-owned C# source code is located under:



Assets/Scripts/



Codex may inspect the entire Unity project when necessary to understand architecture, dependencies, serialization, scenes, prefabs, packages and networking configuration.



However, the default modification scope is:



Assets/Scripts/



Do not modify other project files unless the task explicitly requires it.



\---



\# Required Documentation



Before making significant code changes, read:



\- docs/ARCHITECTURE.md

\- docs/CODING\_STANDARDS.md



For significant refactors also read:



\- docs/PLANS.md



Technical debt discovered during repository analysis should be recorded in:



\- docs/TECH\_DEBT.md



Before modifying networking-related code, also read:



\- docs/NETWORKING\_STANDARDS.md



\---



\# Core Priorities



Code changes should prioritize, in this order:



1\. Correctness

2\. Preservation of existing gameplay behavior

3\. Networking correctness

4\. Readability

5\. Maintainability

6\. Modularity

7\. Testability

8\. Appropriate runtime performance



Follow:



\- Clean Code principles

\- SOLID principles

\- composition over inheritance

\- high cohesion

\- low coupling

\- explicit dependencies



Do not over-engineer.



Do not introduce abstractions that do not provide a clear architectural benefit.



\---



\# Unity Version



This project uses:



Unity 6.3



Prefer APIs and architectural approaches compatible with Unity 6.3.



Do not replace existing code with legacy Unity APIs when a supported modern Unity 6.3 solution is more appropriate.



Before changing an API, confirm that the replacement is compatible with the packages currently installed in the project.



\---



\# Networking Framework



This project uses PurrNet.



All networking-related implementation must follow the PurrNet APIs and architecture currently installed in this repository.



Do NOT generate networking code based on:



\- Mirror

\- FishNet

\- Unity Netcode for GameObjects

\- Photon

\- Fusion

\- deprecated PurrNet syntax



unless the task explicitly asks for interoperability or migration.



Before changing network code, inspect the installed PurrNet package and existing project usage.



When unsure about PurrNet syntax or behavior, prefer the installed package source and current official PurrNet documentation over assumptions.



\---



\# PurrNet Core Rules



Network-aware behaviours should normally use:



PurrNet.NetworkBehaviour



when the class requires network lifecycle, ownership, RPC, synchronization or networking functionality.



Do not convert ordinary MonoBehaviours into NetworkBehaviours unless networking functionality is actually required.



Preserve existing:



\- NetworkIdentity relationships

\- ownership behavior

\- authority rules

\- observer behavior

\- network rules

\- RPC direction

\- synchronization semantics



Do not change network authority as part of a general Clean Code refactor.



\---



\# PurrNet RPC Rules



Use PurrNet RPC types according to their intended direction.



Client to server:



\[ServerRpc]



Server or permitted caller to observers:



\[ObserversRpc]



Targeted communication:



\[TargetRpc]



Do not substitute RPC attributes from other networking frameworks.



Before modifying an RPC, determine:



\- who calls it

\- where it executes

\- whether ownership is required

\- whether it runs locally

\- whether it is server-authoritative

\- whether observers receive it

\- whether late joiners depend on buffered behavior



Preserve these semantics during refactoring.



RPC methods should normally be small.



Prefer:



RPC

→ validation

→ gameplay/service method



instead of implementing large amounts of gameplay logic directly inside RPC methods.



\---



\# PurrNet Ownership



Treat ownership as an important gameplay and security boundary.



Before changing ownership checks, inspect whether the existing code relies on:



\- isOwner

\- IsController

\- owner

\- PlayerID

\- NetworkIdentity ownership

\- Network Rules



Do not replace one ownership concept with another without understanding the behavioral difference.



Do not remove ownership checks merely because they appear redundant.



\---



\# PurrNet Network Rules



The project's configured PurrNet Network Rules define important authority behavior.



Do not assume all projects use strict server authority.



Before changing networking logic, inspect the project's current rules and existing patterns.



Do not hard-code authority behavior that should remain controlled by Network Rules.



\---



\# Synchronization



When working with synchronized state, use the PurrNet synchronization APIs already used by the installed project version.



Do not invent synchronization syntax.



Preserve:



\- owner-authoritative behavior

\- server-authoritative behavior

\- observer visibility

\- owner-only state

\- late join synchronization



Avoid synchronizing values that can be derived locally unless synchronization is necessary.



\---



\# PurrNet Version Compatibility



PurrNet evolves quickly.



The installed project version is the source of truth.



Before generating new PurrNet networking code:



1\. inspect Packages/manifest.json

2\. inspect the installed PurrNet package if necessary

3\. inspect existing PurrNet code in Assets/Scripts

4\. follow the syntax used by the installed version



Do not blindly copy syntax from unrelated versions.



\---



\# Networking and Gameplay Separation



Separate network transport concerns from gameplay rules when practical.



Preferred structure:



NetworkBehaviour

&#x20;   ↓

Gameplay/Application logic

&#x20;   ↓

Domain logic



For example:



PlayerNetwork

&#x20;   ↓

PlayerCombat

&#x20;   ↓

Weapon / Damage logic



Avoid placing all gameplay rules directly inside a NetworkBehaviour.



However, do not split classes purely to satisfy an architectural pattern.



\---



\# Networked Player Input



Keep input, simulation and network transport responsibilities clear.



Avoid combining:



\- raw input collection

\- gameplay decisions

\- RPC transport

\- state synchronization

\- visual presentation



inside one large class.



If the project uses prediction or PurrDiction, preserve its tick-based simulation model.



Do not move predicted physics logic into FixedUpdate if the installed prediction framework manages physics ticks itself.



\---



\# Modification Scope



Codex may inspect:



\- Assets/

\- Packages/

\- ProjectSettings/



when required.



Primary modification scope:



Assets/Scripts/



Unless explicitly requested, do not modify:



\- Assets/Scenes/

\- Assets/Prefabs/

\- Assets/Animations/

\- Assets/Materials/

\- Assets/Textures/

\- Assets/Models/

\- ProjectSettings/

\- Packages/



These locations may be inspected when necessary.



Never modify generated directories:



\- Library/

\- Temp/

\- Logs/

\- obj/



\---



\# Unity Serialization Safety



Be extremely careful with serialized fields.



Do not rename serialized fields casually.



A field rename can break:



\- Prefab references

\- Scene references

\- ScriptableObject data

\- UnityEvents

\- Inspector assignments



When a serialized field must be renamed, preserve compatibility when appropriate with:



\[FormerlySerializedAs("oldFieldName")]



Do not change serialized field types without checking their Unity usage.



\---



\# Prefabs and Scenes



Do not modify Prefabs or Scenes during a code-quality refactor unless explicitly required.



You may inspect them to understand:



\- serialized references

\- NetworkIdentity relationships

\- component dependencies

\- PurrNet configuration



Do not manually rewrite Unity YAML files unless the task explicitly requires it.



\---



\# Third-Party Code



Do not refactor third-party package code.



This includes PurrNet and other installed assets unless explicitly requested.



Prefer adapting project-owned code to public package APIs.



\---



\# Refactoring Workflow



For significant refactors, follow this order:



1\. Read repository instructions.

2\. Inspect the relevant system.

3\. Identify dependencies.

4\. Identify Unity serialization risks.

5\. Identify PurrNet networking risks.

6\. Produce a refactor plan.

7\. Make incremental changes.

8\. Review the final diff.

9\. Verify networking semantics remain unchanged.



Do not refactor the entire project in one uncontrolled rewrite.



\---



\# Required Behaviour Preservation



Unless explicitly requested, preserve:



\- gameplay behavior

\- networking behavior

\- RPC direction

\- ownership

\- authority

\- prediction behavior

\- synchronization

\- Prefab references

\- Scene references

\- ScriptableObject references



A Clean Code refactor must not silently change game behavior.



\---



\# Final Verification



Before completing a task, check:



\- Does the code compile conceptually?

\- Are PurrNet APIs from the installed version used?

\- Were any serialized fields renamed?

\- Were RPC semantics changed?

\- Was ownership behavior changed?

\- Were Network Rules assumptions introduced?

\- Were unrelated files modified?

\- Did networking code become easier to understand?

\- Was unnecessary abstraction introduced?

\- Is existing gameplay behavior preserved?

