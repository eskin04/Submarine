\# Networking Instructions



These instructions apply to code under this directory.



The project uses PurrNet.



The installed PurrNet version and existing project code are the source of truth.



\---



\# Framework



Use PurrNet APIs only.



Do not generate networking syntax from:



\- Mirror

\- FishNet

\- NGO

\- Photon

\- Fusion



unless explicitly requested.



\---



\# NetworkBehaviour



Use NetworkBehaviour when the class genuinely needs PurrNet networking features.



Do not make every gameplay component a NetworkBehaviour.



Prefer separating networking coordination from pure gameplay logic where that improves clarity.



\---



\# RPC



Use:



\[ServerRpc]



for client-to-server execution when appropriate.



Use:



\[ObserversRpc]



for observer RPC behavior.



Use:



\[TargetRpc]



for targeted client execution.



Respect the parameters supported by the installed PurrNet version.



Examples may include:



RequireOwnership

RunLocally

RequireServer

BufferLast



Do not add or remove these options without understanding the behavioral impact.



\---



\# Ownership



Ownership checks are behavioral requirements, not style issues.



Preserve:



isOwner



IsController



owner



PlayerID



NetworkIdentity ownership



unless an explicit architectural change is requested.



\---



\# Authority



Do not assume strict server authority.



Inspect Network Rules and existing code.



Do not change authority boundaries during general cleanup.



\---



\# Synchronization



Use synchronization mechanisms available in the installed PurrNet version.



Preserve:



\- ownerAuth

\- ownerOnly

\- observer scope

\- server synchronization

\- late join behavior



Do not duplicate synchronized data unnecessarily.



\---



\# RPC Architecture



Keep RPC methods focused.



Preferred:



RPC

→ validation

→ gameplay method



Avoid large gameplay systems embedded directly inside RPC methods.



\---



\# Host Behaviour



Remember that host mode may execute server and client paths in the same process.



When changing lifecycle or RPC logic, consider:



\- host

\- dedicated server if applicable

\- remote client



Do not assume that code executing correctly as host proves remote-client correctness.



\---



\# OnSpawned



When working with PurrNet lifecycle callbacks such as:



OnSpawned(bool asServer)



preserve client/server execution semantics.



Do not replace PurrNet lifecycle methods with ordinary Unity lifecycle methods unless equivalent behavior is proven.



\---



\# Prediction



If PurrDiction or another PurrNet prediction system is present:



\- inspect the installed package

\- follow tick-based simulation

\- preserve prediction ownership

\- preserve reconciliation

\- preserve input collection timing



Do not move predicted physics into FixedUpdate unless the framework explicitly requires it.



\---



\# Network Refactor Checklist



Before completing networking changes verify:



\- correct RPC attribute

\- correct RPC direction

\- ownership preserved

\- authority preserved

\- host path checked

\- remote client path considered

\- synchronization preserved

\- no accidental duplicate execution

\- no unrelated gameplay behavior changed

