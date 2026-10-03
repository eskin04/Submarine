\# Project Architecture



\## Technology



Engine:



Unity 6.3



Language:



C#



Networking:



PurrNet



Primary source directory:



Assets/Scripts/



\---



\# Architectural Goals



The project should favor:



\- modular gameplay systems

\- clear ownership of responsibilities

\- composition over inheritance

\- low coupling

\- high cohesion

\- explicit dependencies

\- clear networking boundaries



\---



\# Main Layers



A useful conceptual separation is:



Presentation

↓

Gameplay

↓

Core



Networking interacts with gameplay but should not unnecessarily own gameplay rules.



Example:



PlayerNetwork

↓

PlayerCombat

↓

Weapon

↓

Damage system



This is a guideline rather than a rigid layering framework.



\---



\# Networking Architecture



PurrNet is the networking framework.



NetworkBehaviour classes should primarily handle:



\- PurrNet lifecycle

\- ownership

\- RPC communication

\- synchronization

\- remote state presentation



Gameplay rules should preferably live in focused gameplay components where doing so improves clarity.



Avoid creating separate networking wrappers when they add no value.



\---



\# Authority



The project's authority model is defined by existing PurrNet Network Rules and existing gameplay architecture.



Do not assume strict server authority.



Do not change authority behavior during general refactoring.



Authority changes require explicit approval and separate review.



\---



\# Ownership



PurrNet ownership semantics must be preserved.



Before modifying player or network-controlled components, inspect:



\- owner

\- isOwner

\- IsController

\- PlayerID

\- NetworkIdentity

\- Network Rules



Ownership-related behavior is part of gameplay behavior.



\---



\# RPC Flow



Typical RPC flow should remain intentional.



Example:



Local player input

↓

Local gameplay validation

↓

ServerRpc if server execution is required

↓

Server gameplay logic

↓

ObserversRpc / synchronized state if replication is required



Not every action requires this exact pattern.



Follow the project's configured Network Rules and current authority model.



\---



\# Modules



The exact module list should be updated after repository analysis.



Suggested high-level structure:



Core



Player



Gameplay



Combat



Interaction



Networking



UI



Audio



Infrastructure



\---



\# Core



Contains foundational systems shared by multiple features.



Core should not depend on UI or feature-specific gameplay modules without a clear reason.



\---



\# Player



Contains player-specific systems such as:



\- movement

\- interaction

\- health

\- abilities

\- input adapters



Networking-specific responsibilities should be separated where this improves maintainability.



\---



\# Gameplay



Contains game rules and mechanics.



Gameplay logic should not depend unnecessarily on UI.



\---



\# Networking



Contains networking-specific coordination where appropriate.



This may include:



\- player network behaviour

\- network session logic

\- synchronized gameplay adapters

\- connection-related systems



PurrNet remains the networking implementation.



\---



\# UI



UI should display state and forward user intention.



UI should not own core gameplay rules.



\---



\# Dependencies



Prefer clear dependency directions.



Avoid circular dependencies.



Avoid reaching into another module's private state.



Use narrow public APIs.



\---



\# Future Architecture Updates



After the initial repository audit, update this file with:



\- actual modules

\- major dependencies

\- important data flows

\- networking topology

\- authority decisions

\- critical architectural constraints

